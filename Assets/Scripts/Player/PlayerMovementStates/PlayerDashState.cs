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

    public override void Enter()
    {
        _owner.UseDrag = false;
        _owner.UseCustomGravity = false;
        _owner.CanMove = false;
        _dashDuration = _owner.context.dashDuration;
        _dashSpeed = _owner.context.dashSpeed;
        if (!_owner.CombatController.CombatContext.isAttacking)
        {
            _animator.CrossFade(_dashAnimationHash, 0.1f);
        }
        DashCoroutine().Forget();
    }

    public override void Update()
    {
        Vector3 dashDirection = _owner.MoveDirectionToWorldSpace();
        _owner.referencesContext.rb.AddForce(dashDirection * _dashSpeed, ForceMode.Impulse);
    }

    private async UniTask DashCoroutine()
    {
        await UniTask.Delay((int)(_dashDuration * 1000));
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
        _owner.CanMove = true;
        _owner.UseDrag = true;
    }
}