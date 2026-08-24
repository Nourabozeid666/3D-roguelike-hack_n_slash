using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using static StaggerSeverity;
public class CombatStaggerState : State<CombatController>
{
    
    private Animator _animator;
    private int hashAnimationState;
    private bool canCancel = false;
    public CombatStaggerState(Animator animator)
    {
        _animator = animator;
        hashAnimationState = Animator.StringToHash("CombatStagger");
        
    }

    private void HandleSprintInput(bool isSprinting)
    {
        Debug.Log($"Sprint Input Detected: {isSprinting}, Can Cancel: {canCancel}");
        if (isSprinting && canCancel)
        {
            _owner._playerController.StateMachine.SetState<PlayerDashState>();
        }
    }
    public override void Enter()
    {
        InputController.OnSprintInput += HandleSprintInput;
        _owner.CombatContext.isStaggered = true;
        _owner._playerController.SetCanMove(false);
        _animator.Play(hashAnimationState, 0);
        WaitForCancel().Forget();
        
    }

    UniTask WaitForCancel()
    {
        float staggerDuration = GetStaggerDuration(_owner.CombatContext.currentStaggerSeverity);
        float staggerEndTime = Time.time + staggerDuration;
        return UniTask.WaitUntil(() => Time.time >= staggerEndTime * 0.5f).ContinueWith(() =>
        {
            canCancel = true;
            _owner.CombatContext.isStaggered = false;
            var currentStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return UniTask.WaitUntil(() => Time.time >= staggerEndTime && currentStateInfo.normalizedTime >= 1f || Time.time >= staggerEndTime + 1f); // Includes Failsafe
        }).ContinueWith(() =>
        {
            _stateMachine.SetState<CombatIdleState>();
        });
    }

    public override void Update()
    {
        if (canCancel)
        {
            if (_owner._playerController.StateMachine.CheckState<PlayerDashState>())
            {
                _stateMachine.SetState<CombatIdleState>();
            }
        }
    }

    public override void Exit()
    {
        InputController.OnSprintInput -= HandleSprintInput;
        canCancel = false;
        _owner._playerController.SetCanMove(true);
        _owner.CombatContext.currentStaggerSeverity = Severity.None;
    }
}