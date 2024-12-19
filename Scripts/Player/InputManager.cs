using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour

{
    public static PlayerInput PlayerInput;

    public static Vector2 Movement;
    public static Vector2 MousePosition;
    public static bool JumpWasPressed;
    public static bool JumpIsHeld;
    public static bool JumpWasReleased;
    public static bool RunIsHeld;
    public static bool DashWasPressed;
    public static bool LeftClickWasPressed;
    public static bool LeftClickWasHold;
    public static bool RightClickWasPressed;
    public static bool RightClickWasHold;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _runAction;
    private InputAction _dash;
    private InputAction _mousePosition;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();
        
        _moveAction = PlayerInput.actions["Move"];
        _jumpAction = PlayerInput.actions["Jump"];
        _runAction = PlayerInput.actions["Run"];
        _dash = PlayerInput.actions["Dash"];
        _mousePosition = PlayerInput.actions["Drag and hold"];
    }
    private void Update()
    {
        Movement = _moveAction.ReadValue<Vector2>();
        MousePosition = _mousePosition.ReadValue<Vector2>();

        JumpWasPressed = _jumpAction.WasPressedThisFrame();
        JumpIsHeld = _jumpAction.IsPressed();
        JumpWasReleased = _jumpAction.WasReleasedThisFrame();
        DashWasPressed = _dash.WasPressedThisFrame();
        RunIsHeld = _runAction.IsPressed();

        LeftClickWasPressed = Mouse.current.leftButton.wasPressedThisFrame;
        LeftClickWasHold = Mouse.current.leftButton.isPressed;
        RightClickWasPressed = Mouse.current.rightButton.wasPressedThisFrame;
        RightClickWasHold = Mouse.current.rightButton.isPressed;

    }
}