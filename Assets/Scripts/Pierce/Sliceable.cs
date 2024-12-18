using UnityEngine;
using VInspector;

public class Sliceable : MonoBehaviour
{
    public long PierceValue;

    [Foldout("Feedback Properties")]
    [SerializeField, Range(0, 1)]
    private float timeMultiplier;
    [SerializeField, Min(0)]
    private float hangTime;
    [SerializeField, Range(0, 1)]
    private float inTransitionProportion;
    [SerializeField, Range(0, 1)]
    private float outTransitionProportion;

    private void OnValidate()
    {
        if (inTransitionProportion + outTransitionProportion > 1)
        {
            if (inTransitionProportion > outTransitionProportion)
            {
                outTransitionProportion = 1 - inTransitionProportion;
            } 
            else
            {
                inTransitionProportion = 1 - outTransitionProportion;
            }
        }
    }

    public void Initialize(Sliceable parent)
    {
        PierceValue = parent.PierceValue;
        timeMultiplier = parent.timeMultiplier;
        hangTime = parent.hangTime;
        gameObject.layer = parent.gameObject.layer;
        gameObject.tag = parent.gameObject.tag;
    }

    public void TriggerHitFeedback()
    {
        if (hangTime > 0) TimeManager.Instance.MultiplyTimeScaleSmooth(name + "onHit", timeMultiplier, hangTime, 0.1f, 0.1f);
    }
    public void TriggerSliceFeedback()
    {
        if (hangTime > 0) TimeManager.Instance.MultiplyTimeScaleSmooth(name + "onSlice", timeMultiplier, hangTime, 0.25f, 0.25f);
    }
}
