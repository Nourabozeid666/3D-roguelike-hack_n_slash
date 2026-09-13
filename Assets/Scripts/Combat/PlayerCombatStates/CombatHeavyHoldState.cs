using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CombatHeavyHoldState : State<CombatController>
{
    private Animator _animator;
    private AnimatorOverrideController _OverrideController;
    private AttackData _currentAttack;
    private int hashAnimationState;
    private int hashAnimationTransition;
    public CombatHeavyHoldState(Animator animator, AnimatorOverrideController overrideController, Text attackDebugText = null)
    {
        _animator = animator;
        _OverrideController = overrideController;
        hashAnimationState = Animator.StringToHash("HeavyHoldAttack");
        hashAnimationTransition = Animator.StringToHash("AttackTransition");
    }

    public override bool CanEnter()
    {
        return _owner._playerController.CharacterState != null ? _owner._playerController.CharacterState.CanAttack : true;
    }

    public override void Enter()
    {
        AttackData attack = _owner.CombatContext.currentAttack;
        _currentAttack = attack;
        if (_owner != null && _owner._playerController != null)
        {
            _owner._playerController.SetAttackDebugText($"Current Attack: {attack.AttackName}");
        }

        _animator.speed = _owner.CombatContext.attackSpeed;
        _OverrideController["HeavyHoldAttack"] = attack.Animation;
        _animator.Play(hashAnimationState, 0, 0f);

        // Damp horizontal momentum if grounded (Rule 5)
        if (_owner._playerController.CharacterState != null && _owner._playerController.CharacterState.IsGrounded)
        {
            _owner._playerController.ResetHorizontalVelocity();
        }

        Vector3 attackDir = _owner.AcquireCombatTargetDirection(out Transform target, _currentAttack.LungeDistance, out float adaptedLunge);
        AdjustRotationDuringLunge(attackDir).Forget();
        ExecuteLunge(adaptedLunge).Forget();
        _owner.ResetHitboxTargets();
        _owner.EnableHitbox();
        _owner.equipmentSystem.SetTrailActive(true);
        _owner._playerController.SetCanMove(false);
    }

    private UniTask ExecuteLunge(float lungeDistance)
    {
        float lungeDuration = _currentAttack.LungeDuration;
        return UniTask.WaitWhile(() =>
        {
            if (_owner == null || _owner._playerController == null || !_owner._playerController.CharacterState.IsAttacking) return false;
            if (Time.timeScale <= 0f) return true;
            UseLunge(_currentAttack.LungeDirection, lungeDistance);
            lungeDuration -= Time.deltaTime;
            return lungeDuration > 0f;
        });
    }

    private UniTask AdjustRotationDuringLunge(Vector3 targetDirection)
    {
        const float angleThreshold = 1f; // degrees
        float alpha = 0.15f; // Slerp factor for smooth rotation
        Transform model = _owner.ReferencesContext != null && _owner.ReferencesContext.playerModel != null ? _owner.ReferencesContext.playerModel : _owner.transform;
        return UniTask.WaitUntil(() =>
        {
            if (_owner == null || _owner._playerController == null || !_owner._playerController.CharacterState.IsAttacking) return true;
            if (Time.timeScale <= 0f) return false;
            if (targetDirection == Vector3.zero) return true;
            _owner._playerController.CustomRotate(targetDirection, alpha);
            float angle = Vector3.Angle(model.forward, targetDirection);
            alpha += 0.08f;
            alpha = Mathf.Clamp01(alpha); // Ensure alpha stays within [0, 1]
            return angle <= angleThreshold;
        });
    }

    private void UseLunge(Vector3 lungeDirection, float lungeDistance)
    {
        _owner._playerController.AddDirectionalForce(lungeDirection * lungeDistance, ForceMode.Force);
    }

    public override void Update()
    {
        // Skip checks while in transition so stateInfo still returns the previous animation's normalizedTime
        if (_animator.IsInTransition(0)) return;
        
        // Check if the animation is done                    
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("HeavyHoldAttack")
        && stateInfo.normalizedTime >= _currentAttack.RecoveryStartTime
        && _owner.CombatContext.isAttacking)
        {
            _owner.CombatContext.isAttacking = false;
            _owner.CombatContext.isRecovering = true;
            _stateMachine.SetState<CombatRecoveryState>();
        }
        if (!stateInfo.IsName("HeavyHoldAttack"))
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public override void Exit()
    {
        _animator.speed = 1f;
        // _OverrideController["AttackTransition"] = _OverrideController["HeavyHoldAttack"];
        // _animator.CrossFade(hashAnimationTransition, 0f, 0, _currentAttack.RecoveryStartTime);
        _owner.DisableHitbox();
        _owner.equipmentSystem.SetTrailActive(false);
        // _owner._playerController.SetCanMove(true);
    }
}
