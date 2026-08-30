using UnityEngine;
using System;

public class CombatRecoveryState : State<CombatController>
{
    private Animator _animator;
    private string _currentAttackName;

    public CombatRecoveryState(Animator animator, AnimatorOverrideController overrideController)
    {
        _animator = animator;
    }

    string GetCurrentAttack()
    {
        switch (_owner.CombatContext.currentInputType)
        {
            case InputType.LightAttack:
                return "LightAttack";
            case InputType.HeavyAttack:
                return "HeavyAttack";
            case InputType.LightHold:
                return "LightHoldAttack";
            case InputType.HeavyHold:
                return "HeavyHoldAttack";
            default:
                return "None";
        }
    }

    public override void Enter()
    {
        _currentAttackName = GetCurrentAttack();
        _animator.speed = _owner.CombatContext.attackSpeed;
        _owner._playerController.SetCanMove(false);
    }

    public override void Update()
    {
        if (_animator.IsInTransition(0)) return;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (_owner.CombatContext.currentAttack != null
        && stateInfo.IsName(_currentAttackName)
        && stateInfo.normalizedTime >= _owner.CombatContext.currentAttack.ComboWindow + _owner.CombatContext.currentAttack.RecoveryStartTime
        && !_owner.CombatContext.isAttacking)
        {
            _stateMachine.SetState<CombatIdleState>();
        }
        if (!stateInfo.IsName(_currentAttackName))
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public override void Exit()
    {
        _animator.speed = 1f;
        if (_owner._playerController.CharacterState != null && _owner._playerController.CharacterState.IsDashing)
        {
            // Cancel any queued non-attack actions when exiting the recovery state
            _owner.ComboSystem.ResetQueuedAttack();
            _owner.ResetBuffer();
        }
        _owner.CombatContext.isRecovering = false;
        _owner._playerController.SetCanMove(true);
    }
}