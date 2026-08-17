using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour, IEntityProvider
{
    [SerializeField] internal PlayerContext context = new PlayerContext();
    [SerializeField] internal PlayerEntity playerEntity = new PlayerEntity();
    [SerializeField] internal ReferencesContext referencesContext = new ReferencesContext();
    // [SerializeField] internal CombatContext combatContext = new CombatContext();
    [SerializeField] private CombatController combatController;
    private StateMachine<PlayerController> _stateMachine;
    // private CombatSystem _combatSystem;

    internal bool queueJump = false;
    internal Vector2 MoveDirection { get { return context.moveDirection; } }
    internal bool IsSprinting { get { return context.isSprinting; } }
    internal float groundDrag { get { return context.customDrag; } set { context.customDrag = value; } }
    internal Vector3 Velocity { get { return referencesContext.rb.linearVelocity; } set { referencesContext.rb.linearVelocity = value; } }
    internal bool UseCustomGravity { get { return context.useCustomGravity; } set { context.useCustomGravity = value; } }
    internal bool CanMove { get { return context.canMove; } set { context.canMove = value; } }
    internal bool UseDrag { get { return context.useDrag; } set { context.useDrag = value; } }
    internal CombatController CombatController { get { return combatController; } }
    public StateMachine<PlayerController> StateMachine { get { return _stateMachine; } }
    public IEntity Entity {get { return playerEntity; } }
    public ReferencesContext ReferencesContext { get { return referencesContext; } set { referencesContext = value; } }
    public CharacterState CharacterState { get; private set; }

    void Awake()
    {
        combatController = GetComponent<CombatController>();
        CharacterState = new CharacterState(this, combatController);
        // context.animator = referencesContext.playerModel.GetComponent<Animator>();
        _stateMachine = new StateMachine<PlayerController>(this, referencesContext.debugText);
        _stateMachine.AddState(new PlayerIdleState(referencesContext.animator));
        _stateMachine.AddState(new PlayerMoveState(referencesContext.animator));
        _stateMachine.AddState(new PlayerJumpState(referencesContext.animator));
        _stateMachine.AddState(new PlayerLandState(referencesContext.animator));
        _stateMachine.AddState(new PlayerSlideState(referencesContext.animator));
        _stateMachine.AddState(new PlayerSprintState(referencesContext.animator));
        _stateMachine.AddState(new PlayerDashState(referencesContext.animator));
        // _stateMachine.AddState(new PlayerWallRunState(referencesContext.animator));
        _stateMachine.SetState<PlayerIdleState>();
    }

    void Start()
    {

    }

    void OnEnable()
    {
        // Subscribe to action events
        InputController.OnJumpStart += HandleJumpInput;
        InputController.OnMoveInput += HandleMoveInput;
        InputController.OnSprintInput += HandleSprintInput;
    }

    void OnDisable()
    {
        // Unsubscribe from action events
        InputController.OnJumpStart -= HandleJumpInput;
        InputController.OnMoveInput -= HandleMoveInput;
        InputController.OnSprintInput -= HandleSprintInput;
    }

    private void HandleJumpInput()
    {
        if (CharacterState != null && !CharacterState.CanJump) return;
        if (!IsGrounded())
        {
            queueJump = true;
        }
        else
        {
            Jump();
        }
    }

    private void HandleMoveInput(Vector2 value)
    {
        context.moveDirection = value;
    }

    private void HandleSprintInput(bool isSprinting)
    {
        context.isSprinting = isSprinting;
        if (isSprinting && (CharacterState == null || CharacterState.CanDash))
        {
            _stateMachine.SetState<PlayerDashState>();
        }
    }

    public void ResetHorizontalVelocity()
    {
        referencesContext.rb.linearVelocity = new Vector3(0f, referencesContext.rb.linearVelocity.y, 0f);
    }


    void ApplyCustomGravity()
    {
        if (!context.useCustomGravity) return;
        float multiplier;

        // Determine which phase of jump
        if (referencesContext.rb.linearVelocity.y > context.apexThreshold)
        {
            // Rising
            multiplier = context.risingMultiplier;
        }
        else if (referencesContext.rb.linearVelocity.y > -context.apexThreshold)
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
        referencesContext.rb.AddForce(gravityForce, ForceMode.Acceleration);
    }

    void ApplyCustomDrag()
    {
        if (!context.useDrag) return;
        if (Math.Abs(referencesContext.rb.linearVelocity.x) < 0.1f && Math.Abs(referencesContext.rb.linearVelocity.z) < 0.1f)
        {
            referencesContext.rb.linearVelocity = new Vector3(0, referencesContext.rb.linearVelocity.y, 0);
            return;
        }
        float dragValue = context.customDrag;
        referencesContext.rb.linearVelocity = new Vector3(referencesContext.rb.linearVelocity.x * (1 - dragValue), referencesContext.rb.linearVelocity.y, referencesContext.rb.linearVelocity.z * (1 - dragValue));

    }

    void MaxVelocityUpdate()
    {
        if (referencesContext.rb.linearVelocity.magnitude > context.MaxVelocity)
        {
            Vector3 limitedVelocity = referencesContext.rb.linearVelocity.normalized * context.MaxVelocity;
            referencesContext.rb.linearVelocity = new Vector3(limitedVelocity.x, referencesContext.rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    void FixedUpdate()
    {
        MaxVelocityUpdate();
        ApplyCustomGravity();
        ApplyCustomDrag();
        Move(context.moveDirection);
    }
    
    void Update()
    {
        _stateMachine.Update();
        Rotate(MoveDirectionToWorldSpace());
        if (queueJump && IsGrounded() && CharacterState != null && CharacterState.CanJump)
        {
            Jump();
            queueJump = false;
        }
        else if (CharacterState != null && !CharacterState.CanJump)
        {
            queueJump = false;
        }
    }

    public Vector3 MoveDirectionToWorldSpace()
    {
        Vector3 frontCam = new Vector3(referencesContext.playerCamera.forward.x, 0, referencesContext.playerCamera.forward.z).normalized;
        Vector3 rightCam = referencesContext.playerCamera.right.normalized;
        return frontCam * context.moveDirection.y + rightCam * context.moveDirection.x;
    }

    public bool IsGrounded()
    {
        Debug.DrawRay(transform.position, Vector3.down * 1.3f, Color.red);
        return Physics.Raycast(transform.position, Vector3.down, 1.3f, referencesContext.groundLayer);
    }

    public bool CloseToWallRight()
    {   
        Debug.DrawRay(transform.position, referencesContext.playerModel.right * referencesContext.playerModel.localScale.x * 0.7f, Color.blue);
        return Physics.Raycast(transform.position, referencesContext.playerModel.right, out context.RightRaycast, referencesContext.playerModel.localScale.x * 0.7f, referencesContext.wallLayer);
    }

    public bool CloseToWallLeft()
    {
        Debug.DrawRay(transform.position, -referencesContext.playerModel.right * referencesContext.playerModel.localScale.x * 0.7f, Color.green);
        return Physics.Raycast(transform.position, -referencesContext.playerModel.right, out context.LeftRaycast, referencesContext.playerModel.localScale.x * 0.7f, referencesContext.wallLayer);
    }

    public void UpdateSpeed()
    {
        context.speed = context.isSprinting ? context.sprintSpeed : context.walkSpeed;
    }

    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        referencesContext.rb.AddForce(force, mode);
    }

    public void AddDirectionalForce(Vector3 direction, ForceMode mode = ForceMode.Force)
    {
        Vector3 forward = referencesContext.playerModel.forward * direction.z;
        Vector3 right = referencesContext.playerModel.right * direction.x;
        Vector3 up = referencesContext.playerModel.up * direction.y;
        Vector3 force = forward + right + up;
        referencesContext.rb.AddForce(force, mode);
    }

    void Move(Vector2 direction)
    {
        if (CharacterState != null ? !CharacterState.CanMove : !context.canMove) return;
        float moveX = direction.y * (context.speed * (IsGrounded() && !_stateMachine.CheckState<PlayerJumpState>() ? 1 : context.airMoveSpeedMultiplier));
        float moveZ = direction.x * (context.speed * (IsGrounded() && !_stateMachine.CheckState<PlayerJumpState>() ? 1 : context.airMoveSpeedMultiplier));
        Vector3 frontCam = new Vector3(referencesContext.playerCamera.forward.x, 0, referencesContext.playerCamera.forward.z).normalized;
        referencesContext.rb.AddForce(frontCam * moveX, ForceMode.VelocityChange);
        referencesContext.rb.AddForce(referencesContext.playerCamera.right * moveZ, ForceMode.VelocityChange);

    }

    void Rotate(Vector3 direction)
    {
        if (CharacterState != null ? !CharacterState.CanMove : !context.canMove) return;
        Quaternion targetRotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : referencesContext.playerModel.rotation;
        if (targetRotation != null && targetRotation != referencesContext.playerModel.rotation && direction != Vector3.zero)
        {
            referencesContext.playerModel.rotation = Quaternion.Slerp(referencesContext.playerModel.rotation, targetRotation, 0.1f);
        }
    }

    public void CustomRotate(Vector3 direction)
    {
        Quaternion targetRotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : referencesContext.playerModel.rotation;
        if (targetRotation != null && targetRotation != referencesContext.playerModel.rotation && direction != Vector3.zero)
        {
            referencesContext.playerModel.rotation = Quaternion.Slerp(referencesContext.playerModel.rotation, targetRotation, 0.1f);
        }
    }

    void Jump()
    {
        if (CharacterState != null ? !CharacterState.CanJump : !context.canMove) return;
        _stateMachine.SetState<PlayerJumpState>();
        Vector3 appliedJumpForce = Vector3.up * context.jumpForce * (context.speed == context.sprintSpeed ? 1.2f : 1f);
        if (context.moveDirection.magnitude > 0f)
        {
            float moveX = context.moveDirection.y;
            float moveZ = context.moveDirection.x;
            Vector3 frontJumpDirection = new Vector3(referencesContext.playerCamera.forward.x, 0f, referencesContext.playerCamera.forward.z).normalized * moveX * context.jumpForwardPush;
            Vector3 rightJumpDirection = referencesContext.playerCamera.right.normalized * moveZ * context.jumpForwardPush;
            referencesContext.rb.AddForce(frontJumpDirection, ForceMode.Impulse);
            referencesContext.rb.AddForce(rightJumpDirection, ForceMode.Impulse);
        }
        referencesContext.rb.AddForce(appliedJumpForce, ForceMode.Impulse);
    }

    internal void UpdateGroundDrag(float newDrag)
    {
        groundDrag = newDrag;
    }

    public void SetCanMove(bool canMove)
    {
        context.canMove = canMove;
    }
}
