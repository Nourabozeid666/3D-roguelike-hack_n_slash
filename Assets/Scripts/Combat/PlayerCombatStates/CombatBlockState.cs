using UnityEngine;

public class CombatBlockState : State<CombatController>
{
    private Animator _animator;
    private int hashAnimationState;
    private int hashAnimationHoldState;

    public CombatBlockState(Animator animator)
    {
        _animator = animator;
        hashAnimationState = Animator.StringToHash("CombatBlock");
        hashAnimationHoldState = Animator.StringToHash("CombatBlockHold");
    }

    public override void Enter()
    {
        _owner._playerController.SetCanMove(false);
        _animator.Play(hashAnimationState, 0);
        _owner.CombatContext.isBlocking = true;
        _owner.CombatContext.isParrying = true;
    }
    
    public override void Update()
    {
        if (!_owner.CombatContext.isParrying) return;
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("CombatBlock") && stateInfo.normalizedTime >= _owner.CombatContext.parryEndTime)
        {
            _owner.CombatContext.isParrying = false;
            _animator.Play(hashAnimationHoldState, 0);
        }

    }

    public override void Exit()
    {
        _owner.CombatContext.isParrying = false;
        _owner.CombatContext.isBlocking = false;
    }
}
