using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.Cinemachine.CinemachineFreeLookModifier;

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

    public class TimeQueueItem
    {
        public TimeQueueItem(float modifier)
        {
            Modifier = modifier;
            Count = 1;
        }

        public float Modifier;
        public int Count;
    }

    public static TimeManager Instance { get; private set; }
    public bool MenuPause { get; private set; }

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

    private float modifiedTimeScale;
    private readonly Dictionary<int, TimeQueueItem> timeRequests = new();
    private bool modify;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(Instance);
            ResetTimeScale();
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"Duplicate Singleton {Instance.GetType().Name} Found on {name}!");
            Destroy(this);
        }
    }

    private void LateUpdate()
    {
        if (modify) ApplyTimeScaleMultiply();
    }

    private IEnumerator CountUndoMultiply(float scale, float time, int id = 0)
    {
        yield return new WaitForGameSeconds(time);
        RemoveMultiply(scale, id);
    }
    private void ModifyTime(float scale)
    {
        modifiedTimeScale *= scale;
        modify = true;
    }
    public void MultiplyTimeScale(float scale, int id = 0)
    {
        if (timeRequests.TryGetValue(id, out TimeQueueItem item))
        {
            if (item.Count == 0)
            {
                item.Count++;
                ModifyTime(scale);
            }
        }
        else
        {
            ModifyTime(scale);
            timeRequests.Add(id, new TimeQueueItem(scale));
        }
    }
    public void MultiplyTimeScale(float scale, float time, int id = 0)
    {
        MultiplyTimeScale(scale, id);
        StartCoroutine(CountUndoMultiply(scale, time));
    }
    public void RemoveMultiply(float scale, int id = 0)
    {
        if (timeRequests.TryGetValue(id,out TimeQueueItem item) && item.Count > 0)
        {
            item.Count--;
            ModifyTime(1 / scale);
        }
    }

    public void ResetTimeScale()
    {
        modifiedTimeScale = DefaultTimeScale;
        Time.timeScale = DefaultTimeScale;
        Time.fixedDeltaTime = DefaultFixedTimestep;
        Time.maximumDeltaTime = DefaultMaximumAllowedTimestep;
        Time.maximumParticleDeltaTime = DefaultMaximumParticleTimestep;
    }

    private void ApplyTimeScaleMultiply()
    {
        Time.timeScale = modifiedTimeScale;
        if (adjustFixedTimestep) Time.fixedDeltaTime = DefaultFixedTimestep * modifiedTimeScale;
        if (adjustDefaultMaximumAllowedTimestep) Time.maximumDeltaTime = DefaultMaximumAllowedTimestep * modifiedTimeScale;
        if (adjustMaximumParticleTimestep) Time.maximumParticleDeltaTime = modifiedTimeScale;
        modify = false;
    }
}
