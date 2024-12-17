using UnityEngine;
using VInspector;

public class Sliceable : MonoBehaviour
{
    public long PierceValue;

    [Foldout("Feedback Properties")]
    [SerializeField, Range(0, 1)]
    private float timeMultiplier;
    [SerializeField, Min(0)]
    private int id;
    [SerializeField, Min(0)]
    private float hangTime;

    public void Initialize(Sliceable parent)
    {
        PierceValue = parent.PierceValue;
        timeMultiplier = parent.timeMultiplier;
        id = parent.id;
        hangTime = parent.hangTime;
        gameObject.layer = parent.gameObject.layer;
        gameObject.tag = parent.gameObject.tag;
    }

    public void TriggerHitFeedback()
    {
        if (hangTime > 0) TimeManager.Instance.MultiplyTimeScale(timeMultiplier, hangTime, id);
    }
}
