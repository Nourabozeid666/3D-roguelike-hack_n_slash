using System.Collections;
using UnityEngine;

public class PlayerJumpState : State<PlayerController>
{
    private Animator _animator;

    public PlayerJumpState(Animator animator)
    {
        _animator = animator;
    }

    public override void Enter()
    {
        // Debug.Log("Entered Jump State");
        // _owner.UseDrag = false;
    }

    public override void Update()
    {
        _owner.StartCoroutine(UpdateCoroutine());
    }

    IEnumerator UpdateCoroutine()
    {
        _owner.UpdateGroundDrag(0.5f);
        yield return new WaitForSeconds(0.2f);
        if (_owner.IsGrounded())
        {
            _stateMachine.SetState<PlayerLandState>();
        }
    }

    public override void Exit()
    {

    }
}
