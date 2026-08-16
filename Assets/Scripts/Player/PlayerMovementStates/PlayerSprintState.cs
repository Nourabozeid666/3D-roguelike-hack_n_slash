using UnityEngine;

public class PlayerSprintState : State<PlayerController>
{
    private Animator _animator;
    private int hashAnimationState;

    public PlayerSprintState(Animator animator)
    {
     //   _animator = animator;
       // hashAnimationState = Animator.StringToHash("Sprint");
    }

    public override bool CanEnter()
    {
        return _owner.CharacterState != null ? _owner.CharacterState.CanTransitionToMove : true;
    }

    public override void Enter()
    {
        _owner.UpdateGroundDrag(1f);
        //_animator.Play(hashAnimationState, 0, 0f);
        if (_owner.MoveDirection.magnitude < 0.1f)
        {
            _stateMachine.SetState<PlayerIdleState>();
        }
        if (!_owner.IsSprinting)
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
        else
            _owner.UpdateSpeed();
    }

    public override void Update()
    {
        if (_owner.CharacterState != null && !_owner.CharacterState.CanTransitionToMove)
        {
            _stateMachine.SetState<PlayerIdleState>();
            return;
        }

        if (_owner.MoveDirection.magnitude < 0.1f)
        {
            _stateMachine.SetState<PlayerIdleState>();
        }
        if (!_owner.IsSprinting)
        {
            _stateMachine.SetState<PlayerMoveState>();
        }
    }

    public override void Exit()
    {

    }
}
