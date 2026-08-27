using Cysharp.Threading.Tasks;
using UnityEngine;

public class CombatCounterState : State<CombatController>
{
    private Animator _animator;
    private int hashAnimationState;
    private int counterAttackHash;
    private float attackWindowDuration = 0.5f; // Window to execute a counter-attack
    private float currentTime = 0f;
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
        _owner._playerController.SetCanMove(false);
        _animator.Play(hashAnimationState, 0);
        currentTime = Time.time + attackWindowDuration;
        if (_owner.CombatContext.currentTargetPos != null)
        {
            AdjustRotationDuringLunge(_owner.CombatContext.currentTargetPos.position - _owner.transform.position).Forget();
        }
        else
        {
            Debug.LogWarning("No target position set for counter-attack.");
        }
    }

    public override void Update()
    {
        if (hasCountered)
        {
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("CombatCounter"))
            {
                _stateMachine.SetState<CombatIdleState>();
                return;
            }
            ;
            if (stateInfo.normalizedTime >= 1f)
            {
                _stateMachine.SetState<CombatIdleState>();
                return;
            }
            ;
        }
        if (!hasCountered) return;
        if (currentTime >= _owner.CombatContext.lastInputTime)
        {
            _animator.Play(counterAttackHash, 0);
            hasCountered = true;
            if (_owner.CombatContext.currentTargetPos != null)
            {
                Vector3 lungeDirection = (_owner.CombatContext.currentTargetPos.position - _owner.transform.position).normalized;
                float lungeDistance = 5f; // Adjust as needed
                float lungeDuration = 0.2f; // Adjust as needed
                ExecuteLunge(lungeDuration, lungeDirection, lungeDistance).Forget();
            }
            else
            {
                ExecuteLunge(0.3f, _owner.ReferencesContext.playerModel.transform.forward, 300f).Forget();
            }
        }
        if (currentTime < Time.time)
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public override void Exit()
    {
        hasCountered = false;
        currentTime = 0f;
        _owner._playerController.SetCanMove(true);
    }
}
