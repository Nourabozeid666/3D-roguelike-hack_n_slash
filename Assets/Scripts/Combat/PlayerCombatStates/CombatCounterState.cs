using Cysharp.Threading.Tasks;
using UnityEngine;

public class CombatCounterState : State<CombatController>
{
    private Animator _animator;
    private int hashAnimationState;
    private int counterAttackHash;
    private float attackWindowDuration = 0.5f; // Window to execute a counter-attack
    private float windowEndTime = 0f;
    private bool hasCountered = false;

    public CombatCounterState(Animator animator)
    {
        _animator = animator;
        hashAnimationState = Animator.StringToHash("CombatCounter");
        counterAttackHash = Animator.StringToHash("CounterAttack");
    }

    private UniTask ExecuteLunge(float lungeDuration, Vector3 lungeDirection, float lungeDistance)
    {
        return UniTask.WaitWhile(() =>
        {
            if (!_stateMachine.CheckState<CombatCounterState>()) return false;
            UseLunge(lungeDirection, lungeDistance);
            lungeDuration -= Time.deltaTime;
            return lungeDuration > 0f;
        });
    }

    private UniTask AdjustRotationDuringLunge(Vector3 moveDirection)
    {
        const float angleThreshold = 0.05f; // degrees
        float alpha = 0.1f; // Slerp factor for smooth rotation
        return UniTask.WaitUntil(() =>
        {
            if (moveDirection == Vector3.zero) return true;
            _owner._playerController.CustomRotate(moveDirection, alpha);
            float angle = Vector3.Angle(_owner.transform.forward, moveDirection);
            alpha += 0.1f;
            alpha = Mathf.Clamp01(alpha); // Ensure alpha stays within [0, 1]
            return angle <= angleThreshold || !_stateMachine.CheckState<CombatCounterState>();
        });
    }

    private void UseLunge(Vector3 lungeDirection, float lungeDistance)
    {
        _owner._playerController.AddDirectionalForce(lungeDirection * lungeDistance, ForceMode.Force);
    }

    public override void Enter()
    {
        hasCountered = false;
        _animator.speed = _owner.CombatContext.attackSpeed;
        _owner._playerController.SetCanMove(false);
        _animator.Play(hashAnimationState, 0, 0f);
        windowEndTime = Time.time + attackWindowDuration;

        Vector3 targetDirection = GetTargetDirection();
        if (targetDirection != Vector3.zero)
        {
            AdjustRotationDuringLunge(targetDirection).Forget();
        }
    }

    private Vector3 GetTargetDirection()
    {
        Vector3 dir;
        if (_owner.CombatContext.currentTargetPos != null)
        {
            dir = _owner.CombatContext.currentTargetPos.position - _owner.transform.position;
        }
        else
        {
            dir = Vector3.forward;
        }
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : _owner.ReferencesContext.playerModel.transform.forward;
    }

    private void TriggerCounterAttack()
    {
        hasCountered = true;
        _animator.Play(counterAttackHash, 0, 0f);

        if (_owner.equipmentSystem.CurrentWeapon?.Trail != null)
        {
            _owner.equipmentSystem.CurrentWeapon.Trail.Begin();
        }

        Vector3 lungeDirection = GetTargetDirection();
        AdjustRotationDuringLunge(lungeDirection).Forget();
        ExecuteLunge(0.15f, lungeDirection, 500f).Forget();
    }

    public override void Update()
    {
        if (hasCountered)
        {
            if (_animator.IsInTransition(0)) return;

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("CounterAttack"))
            {
                if (stateInfo.normalizedTime >= 1f)
                {
                    _stateMachine.SetState<CombatIdleState>();
                }
            }
            else
            {
                _stateMachine.SetState<CombatIdleState>();
            }
            return;
        }

        // Check if an attack input was registered during the counter window
        var inputState = _owner.CombatContext.inputState;
        if (inputState.lightAttackPressed || inputState.heavyAttackPressed || inputState.lightAttackReleased || inputState.heavyAttackReleased)
        {
            _owner.CombatContext.inputState.lightAttackReleased = false;
            _owner.CombatContext.inputState.heavyAttackReleased = false;
            TriggerCounterAttack();
            return;
        }

        // Reaction window timed out without attack input
        if (Time.time >= windowEndTime)
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public override void Exit()
    {
        _animator.speed = 1f;
        if (_owner.equipmentSystem.CurrentWeapon?.Trail != null)
        {
            _owner.equipmentSystem.CurrentWeapon.Trail.End();
        }
        hasCountered = false;
        windowEndTime = 0f;
        _owner._playerController.SetCanMove(true);
        _owner.ComboSystem.ResetQueuedAttack();
    }
}
