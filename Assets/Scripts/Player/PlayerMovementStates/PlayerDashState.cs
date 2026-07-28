using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks; 

public class PlayerDashState : State<PlayerController>
{
    private Animator _animator;
    private float _dashDuration = 0.2f; // Duration of the dash in seconds
    private float _dashSpeed = 10f; // Speed of the dash

    public PlayerDashState(Animator animator)
    {
        _animator = animator;
    }

    public override async void Enter()
    {
        _owner.UseDrag = false;
        _owner.UseCustomGravity = false;
        _owner.CanMove = false;
        await DashCoroutine();
    }

    public override void Update()
    {
        
    }

    private async UniTask DashCoroutine()
    {
        _owner.context.rb.AddForce(_owner.MoveDirection * _dashSpeed, ForceMode.Impulse);
        await UniTask.Delay((int)(_dashDuration * 1000));
        _stateMachine.SetState<PlayerIdleState>();
    }

    public override void Exit()
    {
        _owner.UseCustomGravity = true;
        _owner.CanMove = true;
        _owner.UseDrag = true;
    }
}