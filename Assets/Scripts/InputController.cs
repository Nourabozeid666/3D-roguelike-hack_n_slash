using UnityEngine;
using System;

public class InputController : MonoBehaviour
{
    internal InputSystem controls;
    public static event Action<Vector2> OnMoveInput;
    public static event Action<bool> OnSprintInput;
    public static event Action OnJumpStart;
    public static event Action OnLightAttackStart;
    public static event Action OnLightAttackEnd;
    public static event Action OnHeavyAttackStart;
    public static event Action OnHeavyAttackEnd;

    // Debug Inputs
    public bool enableDebugInputs = true;
    public static event Action OnDebugInput1;
    public static event Action OnDebugInput2;
    public static event Action OnDebugInput3;

    void OnEnable()
    {
        controls.PlayerMovement.Enable();
        controls.Combat.Enable();
        if (enableDebugInputs)
        {
           controls.Debug.Enable();
        }
    }

    void OnDisable()
    {
        controls.PlayerMovement.Disable();
        controls.Combat.Disable();
        if (enableDebugInputs)
        {
            controls.Debug.Disable();
        }
    }

    void Awake()
    {
        controls = new InputSystem();
        // Movement
        controls.PlayerMovement.Jump.performed += ctx => OnJumpStart?.Invoke();
        controls.PlayerMovement.Move.performed += ctx => OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
        controls.PlayerMovement.Move.canceled += ctx => OnMoveInput?.Invoke(Vector2.zero);
        controls.PlayerMovement.Sprint.performed += ctx => OnSprintInput?.Invoke(true);
        controls.PlayerMovement.Sprint.canceled += ctx => OnSprintInput?.Invoke(false);
        // Combat
        controls.Combat.LightAttack.performed += ctx => OnLightAttackStart?.Invoke();
        controls.Combat.LightAttack.canceled += ctx => OnLightAttackEnd?.Invoke();
        controls.Combat.HeavyAttack.performed += ctx => OnHeavyAttackStart?.Invoke();
        controls.Combat.HeavyAttack.canceled += ctx => OnHeavyAttackEnd?.Invoke();
        // Debug
        controls.Debug.Debug1.performed += ctx => OnDebugInput1?.Invoke();
        controls.Debug.Debug2.performed += ctx => OnDebugInput2?.Invoke();
        controls.Debug.Debug3.performed += ctx => OnDebugInput3?.Invoke();
    }
}
