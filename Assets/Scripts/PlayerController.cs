using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] internal PlayerContext context = new PlayerContext();
    // [SerializeField] internal CombatContext combatContext = new CombatContext();

    internal InputSystem controls;
    private StateMachine<PlayerController> _stateMachine;
    // private CombatSystem _combatSystem;

    internal bool queueJump = false;
    internal Vector2 MoveDirection { get { return context.moveDirection; } }
    internal bool IsSprinting { get { return context.isSprinting; } }
    internal float groundDrag { get { return context.customDrag; } set { context.customDrag = value; } }
    internal Vector3 Velocity { get { return context.rb.linearVelocity; } set { context.rb.linearVelocity = value; } }
    internal bool UseCustomGravity { get { return context.useCustomGravity; } set { context.useCustomGravity = value; } }
    internal bool CanMove { get { return context.canMove; } set { context.canMove = value; } }
    internal bool UseDrag { get { return context.useDrag; } set { context.useDrag = value; } }

    void Awake()
    {
        _stateMachine = new StateMachine<PlayerController>(this, context.debugText);
        _stateMachine.AddState(new PlayerIdleState(context.animator));
        _stateMachine.AddState(new PlayerMoveState(context.animator));
        _stateMachine.AddState(new PlayerJumpState(context.animator));
        _stateMachine.AddState(new PlayerLandState(context.animator));
        _stateMachine.AddState(new PlayerSlideState(context.animator));
        _stateMachine.AddState(new PlayerSprintState(context.animator));
        _stateMachine.AddState(new PlayerWallRunState(context.animator));
        _stateMachine.SetState<PlayerIdleState>();
        controls = new InputSystem();

        // // Initialize combat system
        // _combatSystem = GetComponent<CombatSystem>();
        // if (_combatSystem != null)
        // {
        //     // Combat system is ready to use
        // }
    }

    void OnEnable()
    {
        controls.PlayerMovement.Enable();
    }

    void OnDisable()
    {
        controls.PlayerMovement.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Subscribe to action events
        controls.PlayerMovement.Jump.performed += ctx =>
        {
            if (!IsGrounded())
            {
                queueJump = true;
                // Debug.Log("Jump Queued");
            }
            else
            {
                Jump();
            }
        };
        controls.PlayerMovement.Move.performed += ctx => context.moveDirection = ctx.ReadValue<Vector2>();
        controls.PlayerMovement.Move.canceled += ctx => context.moveDirection = Vector2.zero;
        controls.PlayerMovement.Sprint.performed += ctx => context.isSprinting = true;
        controls.PlayerMovement.Sprint.canceled += ctx => context.isSprinting = false;
        // Debug.Log(Vector3.up * (gravity * risingMultiplier));
    }


    void ApplyCustomGravity()
    {
        if (!context.useCustomGravity) return;
        float multiplier;

        // Determine which phase of jump
        if (context.rb.linearVelocity.y > context.apexThreshold)
        {
            // Rising
            multiplier = context.risingMultiplier;
        }
        else if (context.rb.linearVelocity.y > -context.apexThreshold)
        {
            // At apex (that floaty feeling)
            multiplier = context.apexMultiplier;
        }
        else
        {
            // Falling (heavier, more responsive)
            multiplier = context.fallingMultiplier;
        }

        // Apply the gravity force
        Vector3 gravityForce = Vector3.up * (context.gravity * multiplier);
        context.rb.AddForce(gravityForce, ForceMode.Acceleration);
    }

    void ApplyCustomDrag()
    {
        if (!context.useDrag) return;
        if (Math.Abs(context.rb.linearVelocity.x) < 0.1f && Math.Abs(context.rb.linearVelocity.z) < 0.1f)
        {
            context.rb.linearVelocity = new Vector3(0, context.rb.linearVelocity.y, 0);
            return;
        }
        float dragValue = context.customDrag;
        context.rb.linearVelocity = new Vector3(context.rb.linearVelocity.x * (1 - dragValue), context.rb.linearVelocity.y, context.rb.linearVelocity.z * (1 - dragValue));

    }

    void MaxVelocityUpdate()
    {
        if (context.rb.linearVelocity.magnitude > context.MaxVelocity)
        {
            Vector3 limitedVelocity = context.rb.linearVelocity.normalized * context.MaxVelocity;
            context.rb.linearVelocity = new Vector3(limitedVelocity.x, context.rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    void FixedUpdate()
    {
        _stateMachine.Update();
        ApplyCustomGravity();
        ApplyCustomDrag();
        Move(context.moveDirection);
    }
    
    void Update()
    {
        Rotate(context.moveDirection);
        MaxVelocityUpdate();
        if (queueJump && IsGrounded())
        {
            Jump();
            queueJump = false;
        }


        if ((CloseToWallRight() || CloseToWallLeft()) && !IsGrounded() && context.rb.linearVelocity.y < 0f && Time.time - context.lastwallrunTime > 0.6f)
        {
            _stateMachine.SetState<PlayerWallRunState>();
        } else
        {
            // Debug.Log("Not close to wall" + CloseToWallLeft() + CloseToWallRight());
            // Debug.Log("Move Dir" + moveDirection);
            // Debug.Log("Is Grounded" + IsGrounded());
        }
    }

    public bool IsGrounded()
    {
        Debug.DrawRay(transform.position, Vector3.down * 1.3f, Color.red);
        return Physics.Raycast(transform.position, Vector3.down, 1.3f, context.groundLayer);
    }

    public bool CloseToWallRight()
    {   
        Debug.DrawRay(transform.position, context.playerModel.right * context.playerModel.localScale.x * 0.7f, Color.blue);
        return Physics.Raycast(transform.position, context.playerModel.right, out context.RightRaycast, context.playerModel.localScale.x * 0.7f, context.wallLayer);
    }

    public bool CloseToWallLeft()
    {
        Debug.DrawRay(transform.position, -context.playerModel.right * context.playerModel.localScale.x * 0.7f, Color.green);
        return Physics.Raycast(transform.position, -context.playerModel.right, out context.LeftRaycast, context.playerModel.localScale.x * 0.7f, context.wallLayer);
    }

    public void UpdateSpeed()
    {
        context.speed = context.isSprinting ? context.sprintSpeed : context.walkSpeed;
    }

    public void AddForce(Vector3 force, ForceMode mode)
    {
        context.rb.AddForce(force, mode);
    }

    void Move(Vector2 direction)
    {
        // if (direction == Vector2.zero && IsGrounded())
        // {
        //     if (rb.velocity.magnitude > 0.1f)
        //         rb.AddForce(-rb.velocity * customDrag * Time.deltaTime, ForceMode.Acceleration);
        //     else
        //         rb.velocity = new Vector3(0, rb.velocity.y, 0);
        // }
        // else
        if (!context.canMove) return;
        float moveX = direction.y * (context.speed * (IsGrounded() && !_stateMachine.CheckState<PlayerJumpState>() ? 1 : context.airMoveSpeedMultiplier));
        float moveZ = direction.x * (context.speed * (IsGrounded() && !_stateMachine.CheckState<PlayerJumpState>() ? 1 : context.airMoveSpeedMultiplier));
        Vector3 frontCam = new Vector3(context.playerCamera.forward.x, 0, context.playerCamera.forward.z).normalized;
        context.rb.AddForce(frontCam * moveX, ForceMode.VelocityChange);
        context.rb.AddForce(context.playerCamera.right * moveZ, ForceMode.VelocityChange);

    }

    void Rotate(Vector2 direction)
    {
        Vector3 groundVelocity = new Vector3(context.rb.linearVelocity.x, 0, context.rb.linearVelocity.z);
        Quaternion targetRotation = groundVelocity != Vector3.zero ? Quaternion.LookRotation(groundVelocity) : context.playerModel.rotation;
        if (targetRotation != null && targetRotation != context.playerModel.rotation && direction != Vector2.zero)
        {
            context.playerModel.rotation = Quaternion.Slerp(context.playerModel.rotation, targetRotation, 0.1f);
        }
    }

    void Jump()
    {
        _stateMachine.SetState<PlayerJumpState>();
        Vector3 appliedJumpForce = Vector3.up * context.jumpForce * (context.speed == context.sprintSpeed ? 1.2f : 1f);
        if (context.moveDirection.magnitude > 0f)
        {
            float moveX = context.moveDirection.y;
            float moveZ = context.moveDirection.x;
            Vector3 frontJumpDirection = new Vector3(context.playerCamera.forward.x, 0f, context.playerCamera.forward.z).normalized * moveX * context.jumpForwardPush;
            Vector3 rightJumpDirection = context.playerCamera.right.normalized * moveZ * context.jumpForwardPush;
            context.rb.AddForce(frontJumpDirection, ForceMode.Impulse);
            context.rb.AddForce(rightJumpDirection, ForceMode.Impulse);
        }
        context.rb.AddForce(appliedJumpForce, ForceMode.Impulse);
    }

    internal void UpdateGroundDrag(float newDrag)
    {
        groundDrag = newDrag;
    }

    /// <summary>
    /// Get the combat system for external control/queries
    /// </summary>
    // public CombatSystem GetCombatSystem()
    // {
    //     return _combatSystem;
    // }
}
