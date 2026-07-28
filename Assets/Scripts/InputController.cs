using UnityEngine;
using System;

public class InputController : MonoBehaviour
{
    internal InputSystem controls;
    public static event Action<Vector2> OnMoveInput;
    public static event Action<bool> OnSprintInput;
    public static event Action OnJumpStart;

    void OnEnable()
    {
        controls.PlayerMovement.Enable();
    }

    void OnDisable()
    {
        controls.PlayerMovement.Disable();
    }

    void Awake()
    {
        controls = new InputSystem();
        controls.PlayerMovement.Jump.performed += ctx => OnJumpStart?.Invoke();
        controls.PlayerMovement.Move.performed += ctx => OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
        controls.PlayerMovement.Move.canceled += ctx => OnMoveInput?.Invoke(Vector2.zero);
        controls.PlayerMovement.Sprint.performed += ctx => OnSprintInput?.Invoke(true);
        controls.PlayerMovement.Sprint.canceled += ctx => OnSprintInput?.Invoke(false);
    }
}
