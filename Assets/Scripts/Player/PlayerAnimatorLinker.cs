using System;
using UnityEngine;

public class PlayerAnimatorLinker : MonoBehaviour
{
    [Serializable]
    private struct IKControlPoint
    {
        public Transform Transform;
        public Vector3 LocalPosition;
    }

    [Header("Dependencies"), Space(5)]
    [SerializeField]
    private Animator animator;

    [Header("IK Parameters"), Space(5)]
    [SerializeField]
    private Transform parent;
    [SerializeField, Tooltip("Points for IK updating")]
    private IKControlPoint pointA, pointB;

    private TwoPointIKFollower follower;

    private void Awake()
    {
        follower = new(parent, pointA.Transform, pointA.LocalPosition, pointB.Transform, pointB.LocalPosition);   
    }

    public void SetAim(Vector2 aim, float magnitude)
    {
        animator.SetFloat("X", aim.x);
        animator.SetFloat("Y", aim.y);
        animator.SetFloat("Magnitude", magnitude);

        follower.UpdatePositions();
    }
}
