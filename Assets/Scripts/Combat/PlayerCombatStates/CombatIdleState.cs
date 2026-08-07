using UnityEngine;

public class CombatIdleState : State<CombatController>
{
    private Animator _animator;
    private int hashAnimationState;

    public CombatIdleState(Animator animator)
    {
        _animator = animator;

        //hashAnimationState = Animator.StringToHash("Idle");
    }

    public override void Enter()
    {
        _owner.CombatContext.isAttacking = false;
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
