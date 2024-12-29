using DynamicMeshCutter;
using UnityEngine;

public class Sliceable : MonoBehaviour
{
    public MeshTarget MeshTarget => meshTarget;
    [SerializeField]
    private MeshTarget meshTarget;

    public int PierceValue { get => pierceValue; set => pierceValue = value; }
    [SerializeField]
    private int pierceValue;

    [SerializeField]
    private TimeScaleParametersSO hitParameters, sliceParameters;

    public virtual void Initialize(Sliceable parent, MeshTarget target)
    {
        PierceValue = parent.PierceValue;
        hitParameters = parent.hitParameters;
        sliceParameters = parent.sliceParameters;
        meshTarget = target;
    }

    public void TriggerHitFeedback()
    {
        if (hitParameters.Time > 0) TimeManager.Instance.MultiplyTimeScaleSmooth(hitParameters.Parameters);
    }
    public void TriggerSliceFeedback()
    {
        if (sliceParameters.Time > 0) TimeManager.Instance.MultiplyTimeScaleSmooth(sliceParameters.Parameters);
    }
}
