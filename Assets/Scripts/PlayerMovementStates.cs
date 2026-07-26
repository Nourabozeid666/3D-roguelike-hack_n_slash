using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum WallRunDirection
{
    Right,
    Left,
    None
}



public class PlayerIdleState : State<PlayerController>
{
    private Animator _animator;
    private int hashAnimationState;

    public PlayerIdleState(Animator animator)
    {
        _animator = animator;
        //hashAnimationState = Animator.StringToHash("Idle");
    }

    public override void Enter()
    {
        _owner.UpdateGroundDrag(1f);
        // _owner.UseDrag = true;
        //_animator.Play(hashAnimationState, 0, 0f);
        if (_owner.MoveDirection.magnitude >= 0.1f)
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
    }

    public override void Update()
    {
        if (_owner.MoveDirection.magnitude >= 0.1f)
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
    }

    public override void Exit()
    {

    }
}

public class PlayerMoveState : State<PlayerController>
{
    private Animator _animator;
    private int hashAnimationState;

    public PlayerMoveState(Animator animator)
    {
        _animator = animator;
        //hashAnimationState = Animator.StringToHash("Walk");
    }

    public override void Enter()
    {
        _owner.UpdateGroundDrag(1f);
          //  _animator.Play(hashAnimationState, 0, 0f);
        if (_owner.MoveDirection.magnitude < 0.1f)
        {
            _stateMachine.SetState<PlayerIdleState>();
        }
        if (_owner.IsSprinting)
        {
            _stateMachine.SetState<PlayerSprintState>();
        }
        else
        {
            _owner.UpdateSpeed();
        }
    }

    public override void Update()
    {
        if (_owner.MoveDirection.magnitude < 0.1f)
        {
            _stateMachine.SetState<PlayerIdleState>();
        }
        if (_owner.IsSprinting)
        {
            _stateMachine.SetState<PlayerSprintState>();
        }
    }

    public override void Exit()
    {

    }
}

public class PlayerSprintState : State<PlayerController>
{
    private Animator _animator;
    private int hashAnimationState;

    public PlayerSprintState(Animator animator)
    {
     //   _animator = animator;
       // hashAnimationState = Animator.StringToHash("Sprint");
    }

    public override void Enter()
    {
        _owner.UpdateGroundDrag(1f);
        //_animator.Play(hashAnimationState, 0, 0f);
        if (_owner.MoveDirection.magnitude < 0.1f)
        {
            _stateMachine.SetState<PlayerIdleState>();
        }
        if (!_owner.IsSprinting)
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
        else
            _owner.UpdateSpeed();


    }

    public override void Update()
    {
        if (_owner.MoveDirection.magnitude < 0.1f)
        {
            _stateMachine.SetState<PlayerIdleState>();
        }
        if (!_owner.IsSprinting)
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
    }

    public override void Exit()
    {

    }
}

public class PlayerJumpState : State<PlayerController>
{
    private Animator _animator;

    public PlayerJumpState(Animator animator)
    {
        _animator = animator;
    }

    public override void Enter()
    {
        // Debug.Log("Entered Jump State");
        _owner.UseDrag = false;
    }

    public override void Update()
    {
        _owner.StartCoroutine(UpdateCoroutine());
    }

    IEnumerator UpdateCoroutine()
    {
        _owner.UpdateGroundDrag(0.1f);
        yield return new WaitForSeconds(0.2f);
        if (_owner.IsGrounded())
        {
            _stateMachine.SetState<PlayerLandState>();
        }
    }

    public override void Exit()
    {

    }
}

public class PlayerLandState : State<PlayerController>
{
    private Animator _animator;

    public PlayerLandState(Animator animator)
    {
        _animator = animator;
    }

    public override void Enter()
    {
        // _owner.groundDrag = 2f;
        // Vector3 newForce = new Vector3(_owner.playerCamera.forward.x, 0f, _owner.playerCamera.forward.y).normalized;
        // _owner.AddForce(newForce * _owner.MoveDirection.y * _owner.LandPushOffForce, ForceMode.VelocityChange);
        // _owner.AddForce(_owner.playerCamera.right * _owner.MoveDirection.x * _owner.LandPushOffForce, ForceMode.VelocityChange);
        // trying to retain current force and prevent sudden stop
        //  Vector3 horizontalVelocity = new Vector3(_owner.Velocity.x, 0f, _owner.Velocity.z);
        //  Vector3 landingForce = horizontalVelocity.normalized * _owner.LandPushOffForce;
        //  _owner.AddForce(landingForce, ForceMode.VelocityChange);
        // await Task.Delay(200);
        // _owner.groundDrag = 5f;
        // _stateMachine.SetState<PlayerIdleState>();
        // fixed with adding physics materal
        // no delay for now as there's no need
        // this will be used later as an early jump trigger if key was queued
        _owner.UpdateGroundDrag(1f);
        _owner.UseDrag = true;
        _stateMachine.SetState<PlayerIdleState>();
    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}

public class PlayerSlideState : State<PlayerController>
{
    private Animator _animator;

    public PlayerSlideState(Animator animator)
    {
        _animator = animator;
    }

    public override void Enter()
    {
    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}

public class PlayerWallRunState : State<PlayerController>
{
    private Animator _animator;
    private bool canCancel = false;
    private WallRunDirection wallRunDirection;
    private WallRunDirection lastWallRunDirection = WallRunDirection.None;
    private RaycastHit wallRaycast;
    private Vector3 normalVector;
    private Vector3 wallRunDirectionVector;
    private float wallRunSpeed = 13f;
    private float jumpOffForce = 30f;
    private bool hasJumpedOffWall = false;


    public PlayerWallRunState(Animator animator)
    {
        _animator = animator;
    }

    IEnumerator waitForCancel()
    {

        yield return new WaitForSeconds(0.1f);
        canCancel = true;

    }

    IEnumerator waitForMove()
    {
        yield return new WaitForSeconds(0.1f);
        _owner.CanMove = true;
    }

    IEnumerator restoreDrag()
    {
        yield return new WaitForSeconds(0.4f);
        _owner.UseDrag = true;
    }

    void ExtraJumpOffForce()
    {
        // yield return new WaitForSeconds(0f);
        float extraForceDuration = 0.2f;
        float timer = 0f;
        while (timer < extraForceDuration)
        {
            timer += Time.deltaTime;
            // Vector3 appliedJumpForce = Vector3.up * (_owner.jumpForce + (_owner.IsSprinting ? 2f : 1.25f)) * Time.deltaTime;
            Vector3 frontJumpDirection = new Vector3(_owner.context.playerCamera.forward.x, 0f, _owner.context.playerCamera.forward.z).normalized * _owner.context.jumpForwardPush * jumpOffForce;
            Vector3 rightJumpDirection = wallRunDirection == WallRunDirection.Right ?
                -_owner.context.playerModel.right * jumpOffForce :
                 _owner.context.playerModel.right * jumpOffForce;
            _owner.AddForce(frontJumpDirection, ForceMode.Acceleration);
            _owner.AddForce(rightJumpDirection, ForceMode.Acceleration);
        }
    }

    void JumpOffWall(InputAction.CallbackContext context)
    {
        if (!canCancel) return;
        hasJumpedOffWall = true;
        _owner.queueJump = false; // reset jump queue
        _stateMachine.SetState<PlayerJumpState>();
        Vector3 appliedJumpForce = Vector3.up * (_owner.context.jumpForce + (_owner.IsSprinting ? 2f : 1.25f));

        // float moveZ = _owner.context.moveDirection.x;
        Vector3 frontJumpDirection = new Vector3(_owner.context.playerCamera.forward.x, 0f, _owner.context.playerCamera.forward.z).normalized * _owner.context.jumpForwardPush;
        Vector3 rightJumpDirection = wallRunDirection == WallRunDirection.Right ?
            -_owner.context.playerModel.right * jumpOffForce :
             _owner.context.playerModel.right * jumpOffForce;
        Debug.DrawLine(_owner.transform.position, _owner.transform.position + frontJumpDirection, Color.red, 5f);
        Debug.DrawLine(_owner.transform.position, _owner.transform.position + rightJumpDirection, Color.blue, 5f);
        _owner.AddForce(frontJumpDirection, ForceMode.Impulse);
        _owner.AddForce(rightJumpDirection, ForceMode.Impulse);
        _owner.AddForce(appliedJumpForce, ForceMode.Impulse);
    }

    public override void Enter()
    {
        _owner.UseDrag = false;
        _owner.controls.PlayerMovement.Jump.performed += JumpOffWall;
        _owner.UseCustomGravity = false;
        _owner.CanMove = false;
        if (_owner.CloseToWallRight())
            wallRunDirection = WallRunDirection.Right;
        if (_owner.CloseToWallLeft())
            wallRunDirection = WallRunDirection.Left;
        // if (lastWallRunDirection == wallRunDirection && Time.time - _owner.lastwallrunTime < 0.5f)
        // {
        //     _stateMachine.SetState<PlayerIdleState>();
        //     return;
        // }
        wallRaycast = (wallRunDirection == WallRunDirection.Right) ? _owner.context.RightRaycast : _owner.context.LeftRaycast;
        normalVector = wallRaycast.normal;
        wallRunDirectionVector = (wallRunDirection == WallRunDirection.Right) ? Vector3.Cross(-normalVector, Vector3.up) : Vector3.Cross(normalVector, Vector3.up);
        _owner.Velocity = wallRunDirectionVector * wallRunSpeed - Vector3.up * 2f;
        _owner.StartCoroutine(waitForCancel());
    }

    public override void Update()
    {
        _owner.Velocity = wallRunDirectionVector * wallRunSpeed - Vector3.up * 2f;
        if (hasJumpedOffWall) return; // prevent state change right after jumping off wall
        if (!_owner.CloseToWallRight() && wallRunDirection == WallRunDirection.Right)
        {
            Debug.Log("No longer close to right wall, landing");
            _stateMachine.SetState<PlayerLandState>();
        }
        else if (!_owner.CloseToWallLeft() && wallRunDirection == WallRunDirection.Left)
        {
            Debug.Log("No longer close to left wall, landing");
            _stateMachine.SetState<PlayerLandState>();
        }
        else if (_owner.IsGrounded())
        {
            Debug.Log("Landed on ground, end wall run");
            _stateMachine.SetState<PlayerLandState>();
        }
    }

    public override void Exit()
    {
        _owner.UseCustomGravity = true;
        // _owner.context.CanMove = true;
        _owner.StartCoroutine(waitForMove());
        canCancel = false;
        hasJumpedOffWall = false;
        // if (lastWallRunDirection != wallRunDirection)
        _owner.context.lastwallrunTime = Time.time;
        lastWallRunDirection = wallRunDirection;
        _owner.controls.PlayerMovement.Jump.performed -= JumpOffWall;
    }
}