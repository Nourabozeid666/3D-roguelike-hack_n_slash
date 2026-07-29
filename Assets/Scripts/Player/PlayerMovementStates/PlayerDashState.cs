using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks; 

public class PlayerDashState : State<PlayerController>
{
    private Animator _animator;
    private float _dashDuration = 0.2f; // Duration of the dash in seconds
    private float _dashSpeed = 50f; // Speed of the dash

    public PlayerDashState(Animator animator)
    {
        _animator = animator;
    }

    public override async void Enter()
    {
        _owner.UseDrag = false;
        _owner.UseCustomGravity = false;
        _owner.CanMove = false;
        _dashDuration = _owner.context.dashDuration;
        _dashSpeed = _owner.context.dashSpeed;
        await DashCoroutine();
    }

    public override void Update()
    {
        Vector3 dashDirection = _owner.MoveDirectionToWorldSpace();
        _owner.context.rb.AddForce(dashDirection * _dashSpeed, ForceMode.Impulse);
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
        _owner.UseCustomGravity = true;
        _owner.CanMove = true;
        _owner.UseDrag = true;
    }
}