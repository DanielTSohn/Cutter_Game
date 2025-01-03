using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

public class PlayerInputReader
{
    /// <summary>
    /// Disables aim action callback
    /// </summary>
    public bool DisableAim = true;
    /// <summary>
    /// Disables cut action callback
    /// </summary>
    public bool DisableCut = true;

    public event Action<Vector2> AimDirectionUpdated;
    public event Action<bool> CutPerformed;

    private Camera playerCamera;

    public PlayerInputReader(PlayerInput playerInput)
    {
        RegisterActions(playerInput);
    }

    /// <summary>
    /// Registers actions to input actions via lookup.
    /// Assigns camera based input if assigned
    /// </summary>
    /// <param name="playerInput">Player input to read from</param>
    public void RegisterActions(PlayerInput playerInput)
    {
        var inputActions = playerInput.actions.FindActionMap(playerInput.defaultActionMap).actions.ToDictionary((InputAction action) => action.name);

        inputActions["Aim Relative"].performed += ReadAimRelative;
        inputActions["Cut"].performed += ReadCut;

        playerCamera = playerInput.camera;
        if (playerCamera != null ) 
            inputActions["Aim Position"].performed += ReadAimPosition;
    }

    /// <summary>
    /// Enables all input
    /// </summary>
    public void EnableInput()
    {
        DisableAim = false;
        DisableCut = false;
    }
    /// <summary>
    /// Disables all input
    /// </summary>
    public void DisableInput()
    {
        DisableAim = true;
        DisableCut = true;
    }

    /// <summary>
    /// Reads aim already in vector format
    /// </summary>
    /// <param name="aimRelative"></param>
    private void ReadAimRelative(InputAction.CallbackContext aimRelative)
    {
        AimDirectionUpdated?.Invoke(aimRelative.ReadValue<Vector2>());
    }

    /// <summary>
    /// Reads aim as screen position and translating to relative vector
    /// Disabled if Menu paused
    /// </summary>
    /// <param name="aimPosition"></param>
    private void ReadAimPosition(InputAction.CallbackContext aimPosition)
    {
        if (DisableAim || TimeManager.Instance.MenuPause) return;

        Vector2 relativePosition = aimPosition.ReadValue<Vector2>();
        relativePosition.x /= playerCamera.scaledPixelWidth;
        relativePosition.y /= playerCamera.scaledPixelHeight;

        AimDirectionUpdated?.Invoke(Vector2.ClampMagnitude((relativePosition - (Vector2.one * 0.5f)) * 2, 1));
    }

    /// <summary>
    /// Reads cut input
    /// Disabled if Menu paused
    /// </summary>
    /// <param name="slice"></param>
    private void ReadCut(InputAction.CallbackContext slice)
    {
        if (DisableCut || TimeManager.Instance.MenuPause) return;

        CutPerformed?.Invoke(slice.performed);
    }
}
