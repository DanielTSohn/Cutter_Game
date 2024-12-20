using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class TimeManager : MonoBehaviour
{
    public class WaitForGameSeconds : CustomYieldInstruction
    {
        public WaitForGameSeconds(float seconds)
        {
            m_Seconds = seconds;
        }

        private float m_Seconds;
        public override bool keepWaiting
        {
            get
            {
                if (!Instance.MenuPause) m_Seconds -= Time.unscaledDeltaTime;
                return m_Seconds > 0;
            }
        }
    }

    public class TimeModifier
    {
        public TimeModifier(float value)
        {
            Value = value;
        }

        public float Value;
        public int Count = 1;
        public float Duration;
    }

    public static TimeManager Instance { get; private set; }
    public bool MenuPause { get; private set; }

    [SerializeField]
    private MMTweenType tweenIn;
    [SerializeField]
    private MMTweenType tweenOut;
    
    [SerializeField]
    private bool adjustFixedTimestep;
    public float DefaultFixedTimestep => defaultFixedTimestep;
    [SerializeField, MMCondition("adjustFixedTimestep", true)]
    private float defaultFixedTimestep = 0.02f;

    [SerializeField]
    private bool adjustDefaultMaximumAllowedTimestep;
    public float DefaultMaximumAllowedTimestep => defaultMaximumAllowedTimestep;
    [SerializeField, MMCondition("adjustDefaultMaximumAllowedTimestep", true)]
    private float defaultMaximumAllowedTimestep = 0.3333333f;

    public float DefaultTimeScale => defaultTimeScale;
    [SerializeField]
    private float defaultTimeScale = 1;

    [SerializeField]
    private bool adjustMaximumParticleTimestep;
    public float DefaultMaximumParticleTimestep => defaultMaximumParticleTimestep;
    [SerializeField, MMCondition("adjustMaximumParticleTimestep", true)]
    private float defaultMaximumParticleTimestep = 0.03f;

    private readonly Dictionary<string, TimeModifier> timeModifiers = new();
    private bool modify;
    
    private WaitUntil waitUntilMenu;
    private WaitWhile waitWhileMenu;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(Instance);
            waitUntilMenu = new(() => MenuPause);
            waitWhileMenu = new(() => MenuPause);
            ResetTimeScale();
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"Duplicate Singleton {Instance.GetType().Name} Found on {name}!");
            Destroy(this);
        }
    }

    private void Update()
    {
        if (modify) ApplyTimeScaleMultiply();
    }

    [Button]
    private void TestMenuPause()
    {
        MenuPause = true;
    }
    [Button]
    private void TestMenuResume()
    {
        MenuPause = false;
    }

    private IEnumerator LerpMultiply(string key, float scale, float duration)
    {
        AddTimeModifier(key, scale);
        float time = 0;
        while (time < duration)
        {
            timeModifiers[key].Value = Mathf.Lerp(1, scale, tweenIn.Evaluate(time / duration));
            SetModifyFlag();

            if (!MenuPause)
            {
                time += Time.unscaledDeltaTime;
                yield return null;
            }
            else yield return waitWhileMenu;
        }

        timeModifiers[key].Value = scale;
        SetModifyFlag();
    }
    private IEnumerator LerpRemoveMultiply(string key, float scale, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            timeModifiers[key].Value = Mathf.Lerp(scale, 1, tweenOut.Evaluate(time / duration));
            SetModifyFlag();

            if (!MenuPause)
            {
                time += Time.unscaledDeltaTime;
                yield return null;
            }
            else yield return waitWhileMenu;
        }

        RemoveMultiply(key);
    }

    private IEnumerator CountUndoMultiply(string key, float scale, float time)
    {
        MultiplyTimeScale(key, scale);
        yield return new WaitForGameSeconds(time);
        RemoveMultiply(key);
    }
    private IEnumerator CountUndoMultiplyLerp(string key, float scale, float duration, float smoothIn, float smoothOut)
    {
        bool modify = true;
        if (timeModifiers.TryGetValue(key, out var modifier) && modifier.Count > 0)
        {
            Debug.Log(modifier.Count);
            modify = false;
            modifier.Duration = duration * (1 - smoothIn - smoothOut);
            modifier.Count++;
        }
        Debug.Log(modify);
        if (modify)
        {
            float inDuration = duration * smoothIn;
            float outDuration = duration * smoothOut;
            yield return StartCoroutine(LerpMultiply(key, scale, inDuration));
            timeModifiers[key].Duration = duration - inDuration - outDuration;
            while (timeModifiers.TryGetValue(key, out var value) && value.Count > 0 && value.Duration > 0)
            {
                if (!MenuPause)
                {
                    value.Duration -= Time.unscaledDeltaTime;
                    yield return null;
                }
                else yield return waitWhileMenu;
            }
            yield return StartCoroutine(LerpRemoveMultiply(key, scale, outDuration));
        }
        else
        {
            yield return new WaitForGameSeconds(duration * (1 - smoothIn - smoothOut));
            RemoveMultiply(key);
        }
    }

    private void SetModifyFlag()
    {
        if (!modify) modify = true;
    }

    public void AddTimeModifier(string key, float scale)
    {
        if (timeModifiers.TryGetValue(key, out var modifier))
        {
            if (modifier.Count++ > 0)
            {
                return;
            }
            else SetModifyFlag();
        }
        else
        {
            timeModifiers.Add(key, new(scale));
            SetModifyFlag();
        }
    }

    public void MultiplyTimeScale(string key, float scale)
    {
        AddTimeModifier(key, scale);
    }
    public void MultiplyTimeScale(string key, float scale, float time)
    {
        StartCoroutine(CountUndoMultiply(key, scale , time));
    }

    public void MultiplyTimeScaleSmooth(string key, float scale, float smoothTime)
    {
        StartCoroutine(LerpMultiply(key, scale, smoothTime));
    }
    public void MultiplyTimeScaleSmooth(string key, float scale, float time, float smoothIn, float smoothOut)
    {
        StartCoroutine(CountUndoMultiplyLerp(key, scale, time, smoothIn, smoothOut));
    }

    public void RemoveMultiply(string key)
    {
        if (timeModifiers.TryGetValue(key, out var modifier))
        {
            if (modifier.Count > 0 && --modifier.Count <= 0)
            {
                SetModifyFlag();
            }
        }
    }

    public void ResetTimeScale()
    {
        Time.timeScale = DefaultTimeScale;
        Time.fixedDeltaTime = DefaultFixedTimestep;
        Time.maximumDeltaTime = DefaultMaximumAllowedTimestep;
        Time.maximumParticleDeltaTime = DefaultMaximumParticleTimestep;
    }

    private void ApplyTimeScaleMultiply()
    {
        double calculatedScale = DefaultTimeScale;
        System.Threading.Tasks.Parallel.ForEach(timeModifiers.Values, (item) => { if (item.Count > 0) calculatedScale *= item.Value; });

        float castModifiedScale = (float)calculatedScale;
        Time.timeScale = castModifiedScale;
        if (adjustFixedTimestep) Time.fixedDeltaTime = DefaultFixedTimestep * castModifiedScale;
        if (adjustDefaultMaximumAllowedTimestep) Time.maximumDeltaTime = DefaultMaximumAllowedTimestep * castModifiedScale;
        if (adjustMaximumParticleTimestep) Time.maximumParticleDeltaTime = castModifiedScale;
        modify = false;
    }
}
