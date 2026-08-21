using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CombatHeavyAttackState : State<CombatController>
{
    private Animator _animator;
    private AnimatorOverrideController _OverrideController;
    private Text _attackDebugText;
    private AttackData _currentAttack;
    private int hashAnimationState;
    private int hashAnimationTransition;
    public CombatHeavyAttackState(Animator animator, AnimatorOverrideController overrideController, Text attackDebugText)
    {
        _animator = animator;
        _OverrideController = overrideController;
        _attackDebugText = attackDebugText;
        hashAnimationState = Animator.StringToHash("HeavyAttack");
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
        _attackDebugText.text = $"Current Attack: {attack.AttackName}";

        _OverrideController["HeavyAttack"] = attack.Animation;
        _animator.Play(hashAnimationState, 0, 0f);

        // Damp horizontal momentum if grounded (Rule 5)
        if (_owner._playerController.CharacterState != null && _owner._playerController.CharacterState.IsGrounded)
        {
            _owner._playerController.ResetHorizontalVelocity();
        }

        AdjustRotationDuringLunge(_owner._playerController.MoveDirectionToWorldSpace()).Forget();
        ExecuteLunge().Forget();
        if (_owner.CombatContext.currentWeapon?.Trail != null)
        {
            _owner.CombatContext.currentWeapon.Trail.Begin();
        }
        _owner._playerController.SetCanMove(false);
    }

    private UniTask ExecuteLunge()
    {
        float lungeDuration = _currentAttack.LungeDuration;
        return UniTask.WaitWhile(() =>
        {
            UseLunge(_currentAttack.LungeDirection, _currentAttack.LungeDistance);
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
            return angle <= angleThreshold || !_owner._playerController.CharacterState.IsAttacking;
        });
    }

    private void UseLunge(Vector3 lungeDirection, float lungeDistance)
    {
        _owner._playerController.AddDirectionalForce(lungeDirection * lungeDistance, ForceMode.Force);
    }

    public override void Update()
    {
        // so stateInfo still returns the previous animation's normalizedTime
        if (_animator.IsInTransition(0)) return;
        
        // Check if the animation is done                    
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        // Debug.Log("Current Animation State Normalized Time: " + stateInfo.normalizedTime);
        if (stateInfo.IsName("HeavyAttack")
        && stateInfo.normalizedTime >= _currentAttack.RecoveryStartTime
        && _owner.CombatContext.isAttacking)
        {
            _owner.CombatContext.isAttacking = false;
            _owner.CombatContext.isRecovering = true;
            _stateMachine.SetState<CombatRecoveryState>();
        }
        if (!stateInfo.IsName("HeavyAttack"))
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public override void Exit()
    {
        // _OverrideController["AttackTransition"] = _OverrideController["HeavyAttack"];
        // _animator.CrossFade(hashAnimationTransition, 0f, 0, _currentAttack.RecoveryStartTime);
        if (_owner.CombatContext.currentWeapon?.Trail != null)
        {
            _owner.CombatContext.currentWeapon.Trail.End();
        }
        // _owner._playerController.SetCanMove(true);
    }
}
