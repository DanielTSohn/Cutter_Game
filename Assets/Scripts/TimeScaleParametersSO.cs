using UnityEngine;

[CreateAssetMenu(fileName = "Default Parameters", menuName = "Time Parameters")]
public class TimeScaleParametersSO : ScriptableObject
{
    public TimeScaleParameters Parameters = new()
    {
        ID = "Default",
        Multiplier = 0.5f,
        Time = 1,
        InProportion = 0.2f,
        OutProportion = 0.2f
    };

    public string ID => Parameters.ID;
    public float Multiplier => Parameters.Multiplier;
    public float Time => Parameters.Time;
    public float InProportion => Parameters.InProportion;
    public float OutProportion => Parameters.OutProportion;
}
