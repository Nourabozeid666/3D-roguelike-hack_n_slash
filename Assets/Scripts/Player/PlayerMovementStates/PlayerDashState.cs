using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks; 

public class PlayerDashState : State<PlayerController>
{
    private Animator _animator;
    private float _dashDuration = 0.2f; // Duration of the dash in seconds
    private float _dashSpeed = 50f; // Speed of the dash
    private int _dashAnimationHash = Animator.StringToHash("DashForward");

    public PlayerDashState(Animator animator)
    {
        _animator = animator;
    }

    public override bool CanEnter()
    {
        return _owner.CharacterState != null ? _owner.CharacterState.CanDash : true;
    }

    private Vector3 _dashDirection;

    public override void Enter()
    {
        _owner.UseDrag = false;
        _owner.UseCustomGravity = false;
        _dashDuration = _owner.context.dashDuration;
        _dashSpeed = _owner.context.dashSpeed;

        _dashDirection = _owner.MoveDirectionToWorldSpace();
        if (_dashDirection == Vector3.zero)
        {
            _dashDirection = _owner.referencesContext.playerModel.forward;
        }

        if (!_owner.CombatController.CombatContext.isAttacking)
        {
            _animator.CrossFade(_dashAnimationHash, 0.1f);
        }
        
        DashCoroutine().Forget();
    }

    public override void Update()
    {
        if (_owner.CharacterState != null && !_owner.CharacterState.IsGrounded)
        {
            _owner.referencesContext.rb.AddForce(_dashDirection * _dashSpeed * 0.25f * Time.deltaTime, ForceMode.Impulse);
        }
        else
        {
            _owner.referencesContext.rb.AddForce(_dashDirection * _dashSpeed * Time.deltaTime, ForceMode.Impulse);
        }
    }

    private async UniTask DashCoroutine()
    {
        await UniTask.Delay((int)(_dashDuration * 1000));
        if (!_stateMachine.CheckState<PlayerDashState>()) return;

        if (_owner.MoveDirection.magnitude < 0.1f)
        {
            _stateMachine.SetState<PlayerIdleState>();
        }
        else if (_owner.IsSprinting)
        {
            _stateMachine.SetState<PlayerSprintState>();
        }
        else
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
    }

    public override void Exit()
    {
        _animator.CrossFade(Animator.StringToHash("CombatIdle"), 0.1f);
        _owner.UseCustomGravity = true;
        _owner.UseDrag = true;
    }
}