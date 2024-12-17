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
    private ManualBehaviour planeCutter;

    [Foldout("Cut Plane")]
    [SerializeField]
    private InputRotator.AxisSetting axisSetting;
    [EndFoldout]

    private PlayerInputReader playerInputReader;
    private InputRotator inputRotator;

    private void OnValidate()
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

    private void Awake()
    {
        playerInputReader.AimDirectionUpdated += GetAngle;
        playerInputReader.CutPerformed += RequestCut;
    }

    private void GetAngle(Vector2 angle)
    {
        inputRotator.SetRotation(Vector2.SignedAngle(Vector2.right, angle));
    }
    private void RequestCut(bool cut)
    {
        planeCutter.Cut();
    }
}
