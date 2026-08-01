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
        
    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}
