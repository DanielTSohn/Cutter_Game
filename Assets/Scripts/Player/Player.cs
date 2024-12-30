using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

public class Player : MonoBehaviour
{
    [Header("Dependencies"), Space(5)]
    [SerializeField]
    private PlayerInput playerInput;
    [SerializeField]
    private Transform targetPlane;
    [SerializeField]
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
        playerInputReader.AimDirectionUpdated += GetAngle;
        playerInputReader.CutPerformed += RequestCut;
    }

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

    private void GetAngle(Vector2 angle)
    {
        inputRotator.SetRotation(Vector2.SignedAngle(Vector2.right, angle));
    }
    private void RequestCut(bool cut)
    {
        cutter.StartCut();
    }
}
