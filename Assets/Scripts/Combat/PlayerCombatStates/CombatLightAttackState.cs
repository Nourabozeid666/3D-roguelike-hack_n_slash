using System;
using Unity.VisualScripting;
using UnityEngine;

public class CombatLightAttackState : State<CombatController>
{
    private Animator _animator;
    private AnimatorOverrideController _OverrideController;
    private AttackData _currentAttack;
    private int hashAnimationState;
    public CombatLightAttackState(Animator animator)
    {
        _animator = animator;
        _OverrideController = _OverrideController != null ? _OverrideController : new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _OverrideController;
        hashAnimationState = Animator.StringToHash("LightAttack");
    }

    public override void Enter()
    {
        AttackData attack = _owner.CombatContext.currentAttack;
        _currentAttack = attack;
        _OverrideController["LightAttack"] = attack.Animation;

        _animator.CrossFade(hashAnimationState, 0.1f, 0);
    }

    public override void Update()
    {
        // Check if the animation is done                    
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("LightAttack")
        && stateInfo.normalizedTime >= 1f
        && _owner.CombatContext.isAttacking)
        {
            _owner.CombatContext.isAttacking = false;
        }
        if (stateInfo.IsName("LightAttack")
        && stateInfo.normalizedTime >= _currentAttack.ComboWindow + 1f
        && !_owner.CombatContext.isAttacking)
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public override void Exit()
    {

    }
}
