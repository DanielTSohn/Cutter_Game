using UnityEngine;

public class TwoPointIKFollower
{
    public TwoPointIKFollower(Transform parent, Transform aTransform, Vector3 aLocalPoint, Transform bTransform, Vector3 bLocalPoint)
    {
        target = parent;
        transformA = aTransform;
        pointA = aLocalPoint;
        transformB = bTransform;
        pointB = bLocalPoint;
    }

    private readonly Transform target;
    private readonly Transform transformA;
    private readonly Transform transformB;

    public Vector3 pointA;
    public Vector3 pointB;

    public void UpdatePositions()
    {
        transformA.position = target.transform.TransformPoint(pointA);
        transformB.position = target.transform.TransformPoint(pointB);
    }

    public void SetPoints(Vector3 a, Vector3 b)
    {
        pointA = a;
        pointB = b;
    }
}
