using UnityEngine;

public class CombatIdleState : State<CombatController>
{
    private Animator _animator;
    private int hashAnimationState;

    public CombatIdleState(Animator animator)
    {
        _animator = animator;
        hashAnimationState = Animator.StringToHash("CombatIdle");
    }

    public override void Enter()
    {
        _owner._playerController.SetCanMove(true);
        _animator.CrossFade(hashAnimationState, 0.1f, 0);
        _owner.CombatContext.isAttacking = false;
        _owner.CombatContext.isRecovering = false;
        _owner.CombatContext.isCharging = false;
        _owner.CombatContext.isStaggered = false;
        _owner.CombatContext.currentAttack = null;
        _owner.CombatContext.currentInputType = InputType.None;        
        _owner.CombatContext.queuedAttack = null; 
        _owner.CombatContext.queuedInputType = InputType.None;
    }
    
    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}
