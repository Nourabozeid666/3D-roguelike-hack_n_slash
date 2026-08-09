using UnityEngine;

public class PlayerIdleState : State<PlayerController>
{
    private Animator _animator;
    private int hashAnimationState;

    public PlayerIdleState(Animator animator)
    {
        _animator = animator;
        hashAnimationState = Animator.StringToHash("CombatIdle");
    }

    public override void Enter()
    {
        _owner.UpdateGroundDrag(1f);
        // _owner.UseDrag = true;
        if (_owner.MoveDirection.magnitude >= 0.1f)
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
        if (!_owner.CombatController.CombatContext.isAttacking)
        {
            _animator.Play(hashAnimationState, 0, 0f);
        }
    }

    public override void Update()
    {
        if (_owner.MoveDirection.magnitude >= 0.1f)
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
    }

    public override void Exit()
    {

    }
}
