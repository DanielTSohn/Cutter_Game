using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

public class Player : MonoBehaviour
{
    [Header("Dependencies"), Space(5)]
    [SerializeField, Tooltip("Input Reader")]
    private PlayerInput playerInput;
    [SerializeField, Tooltip("Transform plane to rotate")]
    private Transform targetPlane;
    [SerializeField, Tooltip("Links input to animations")]
    private PlayerAnimatorLinker animatorLinker;
    [SerializeField, Tooltip("Manages mesh cutting")]
    private Cutter cutter;

    [Foldout("Cut Plane")]
    [SerializeField]
    private InputRotator.AxisSetting axisSetting;
    [EndFoldout]

    private PlayerInputReader playerInputReader;
    private InputRotator inputRotator;

    private void OnValidate()
    {
        Initialize();
    }

    private void Awake()
    {
        Initialize();
        playerInputReader.AimDirectionUpdated += SetAngle;
        playerInputReader.CutPerformed += RequestCut;

        playerInputReader.EnableInput();
    }

    /// <summary>
    /// Gets and registers actions of player input along with initializing compositioned classes
    /// </summary>
    private void Initialize()
    {
        if (playerInput != null)
        {
            if (playerInputReader == null)
                playerInputReader = new(playerInput);
            else
                playerInputReader.RegisterActions(playerInput);
        }

        if (targetPlane != null)
            inputRotator ??= new(targetPlane, axisSetting);
    }

    /// <summary>
    /// Sets angle for the input rotator with given angle
    /// Reverses direction based on slice direction
    /// </summary>
    /// <param name="angle">Angle using cartesian coordinates</param>
    private void SetAngle(Vector2 angle)
    {
        if (cutter.SliceRight) angle *= -1;

        inputRotator.SetRotation(Vector2.SignedAngle(Vector2.right, angle));

        animatorLinker.SetAim(angle.normalized, angle.magnitude);
    }
    /// <summary>
    /// Requests cut to cutter
    /// </summary>
    /// <param name="cut">True to request cut, false otherwise</param>
    private void RequestCut(bool cut)
    {
        if (cut) cutter.StartCut();
    }
}
