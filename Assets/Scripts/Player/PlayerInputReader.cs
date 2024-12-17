using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader
{
    public event Action<Vector2> AimDirectionUpdated;
    public event Action<bool> CutPerformed;

    private Camera playerCamera;
    private Vector2 screenCenter = new();

    public PlayerInputReader(PlayerInput playerInput)
    {
        RegisterActions(playerInput);
    }

    public void RegisterActions(PlayerInput playerInput)
    {
        var inputActions = playerInput.actions.FindActionMap(playerInput.defaultActionMap).actions.ToDictionary((InputAction action) => action.name);

        inputActions["Aim Relative"].performed += ReadAimRelative;
        inputActions["Cut"].performed += ReadCut;

        playerCamera = playerInput.camera;
        if (playerCamera != null ) 
            inputActions["Aim Position"].performed += ReadAimPosition;
    }

    private void ReadAimRelative(InputAction.CallbackContext aim)
    {
        AimDirectionUpdated?.Invoke(aim.ReadValue<Vector2>());
    }

    private void ReadAimPosition(InputAction.CallbackContext aimRelative)
    {
        screenCenter.x = Screen.width / 2;
        screenCenter.y = Screen.height / 2;

        AimDirectionUpdated?.Invoke((aimRelative.ReadValue<Vector2>() - screenCenter).normalized);
    }

    private void ReadCut(InputAction.CallbackContext slice)
    {
        CutPerformed?.Invoke(slice.performed);
    }
}
