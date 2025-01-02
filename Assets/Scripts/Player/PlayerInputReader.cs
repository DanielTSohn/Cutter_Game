using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader
{
    public bool DisableAim;
    public bool DisableCut;

    public event Action<Vector2> AimDirectionUpdated;
    public event Action<bool> CutPerformed;

    private Camera playerCamera;

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

    public void EnableInput()
    {
        DisableAim = false;
        DisableCut = false;
    }
    public void DisableInput()
    {
        DisableAim = true;
        DisableCut = true;
    }

    private void ReadAimRelative(InputAction.CallbackContext aimRelative)
    {
        AimDirectionUpdated?.Invoke(aimRelative.ReadValue<Vector2>());
    }

    private void ReadAimPosition(InputAction.CallbackContext aimPosition)
    {
        if (DisableAim || TimeManager.Instance.MenuPause) return;

        Vector2 relativePosition = aimPosition.ReadValue<Vector2>();
        relativePosition.x /= playerCamera.scaledPixelWidth;
        relativePosition.y /= playerCamera.scaledPixelHeight;

        AimDirectionUpdated?.Invoke(Vector2.ClampMagnitude((relativePosition - (Vector2.one * 0.5f)) * 2, 1));
    }

    private void ReadCut(InputAction.CallbackContext slice)
    {
        if (DisableCut || TimeManager.Instance.MenuPause) return;

        CutPerformed?.Invoke(slice.performed);
    }
}
