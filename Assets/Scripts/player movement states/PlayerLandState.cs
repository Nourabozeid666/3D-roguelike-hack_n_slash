using UnityEngine;

public class PlayerLandState : State<PlayerController>
{
    private Animator _animator;

    public PlayerLandState(Animator animator)
    {
        _animator = animator;
    }

    public override void Enter()
    {
        // _owner.groundDrag = 2f;
        // Vector3 newForce = new Vector3(_owner.playerCamera.forward.x, 0f, _owner.playerCamera.forward.y).normalized;
        // _owner.AddForce(newForce * _owner.MoveDirection.y * _owner.LandPushOffForce, ForceMode.VelocityChange);
        // _owner.AddForce(_owner.playerCamera.right * _owner.MoveDirection.x * _owner.LandPushOffForce, ForceMode.VelocityChange);
        // trying to retain current force and prevent sudden stop
        //  Vector3 horizontalVelocity = new Vector3(_owner.Velocity.x, 0f, _owner.Velocity.z);
        //  Vector3 landingForce = horizontalVelocity.normalized * _owner.LandPushOffForce;
        //  _owner.AddForce(landingForce, ForceMode.VelocityChange);
        // await Task.Delay(200);
        // _owner.groundDrag = 5f;
        // _stateMachine.SetState<PlayerIdleState>();
        // fixed with adding physics materal
        // no delay for now as there's no need
        // this will be used later as an early jump trigger if key was queued
        _owner.UpdateGroundDrag(1f);
        _owner.UseDrag = true;
        _stateMachine.SetState<PlayerIdleState>();
    }

    public override void Update()
    {

    }

    public override void Exit()
    {

    }
}
