using UnityEngine;

public class PlayerAnimatorLinker : MonoBehaviour
{
    [Header("Dependencies"), Space(5)]
    [SerializeField]
    private Animator animator;

    public void SetAim(Vector2 aim, float magnitude)
    {
        animator.SetFloat("X", aim.x);
        animator.SetFloat("Y", aim.y);
        animator.SetFloat("Magnitude", magnitude);
    }
}
