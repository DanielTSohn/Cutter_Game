using UnityEngine;

public class Target : MonoBehaviour
{
    public int PierceValue { get => pierceValue; set => pierceValue = value; }
    [SerializeField]
    private int pierceValue;

    [SerializeField]
    private TimeScaleParametersSO feedbackParameters;

    public void OnSlice()
    {
        if (feedbackParameters != null && feedbackParameters.Time > 0) TimeManager.Instance.MultiplyTimeScaleSmooth(feedbackParameters.Parameters);
     
        enabled = false;
    }
}
