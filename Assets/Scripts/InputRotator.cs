using UnityEngine;

public class InputRotator
{
    public enum AxisSetting { X, Y, Z }

    public InputRotator(Transform targetTransform, AxisSetting settings)
    {
        target = targetTransform;
        axisSettings = settings;
    }

    private readonly Transform target;
    private readonly AxisSetting axisSettings;

    public void SetRotation(float angle)
    {
        Vector3 eulerRotation = target.eulerAngles;

        switch(axisSettings)
        {
            case AxisSetting.X:
                eulerRotation.x = angle;
                break;
            case AxisSetting.Y:
                eulerRotation.y = angle;
                break;
            case AxisSetting.Z:
                eulerRotation.z = angle;
                break;
        }

        target.rotation = Quaternion.Euler(eulerRotation);
    }
}
