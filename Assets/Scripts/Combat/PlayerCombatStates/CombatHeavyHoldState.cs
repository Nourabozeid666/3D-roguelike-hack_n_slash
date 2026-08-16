using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CombatHeavyHoldState : State<CombatController>
{
    private Animator _animator;
    private AnimatorOverrideController _OverrideController;
    private Text _attackDebugText;
    private AttackData _currentAttack;
    private int hashAnimationState;
    private int hashAnimationTransition;
    public CombatHeavyHoldState(Animator animator, AnimatorOverrideController overrideController, Text attackDebugText)
    {
        _animator = animator;
        _OverrideController = overrideController;
        _attackDebugText = attackDebugText;
        hashAnimationState = Animator.StringToHash("HeavyHoldAttack");
        hashAnimationTransition = Animator.StringToHash("AttackTransition");
    }

    public override void Enter()
    {
        AttackData attack = _owner.CombatContext.currentAttack;
        _currentAttack = attack;
        _attackDebugText.text = $"Current Attack: {attack.AttackName}";

        _OverrideController["HeavyHoldAttack"] = attack.Animation;
        _animator.Play(hashAnimationState, 0, 0f);

        // Cancel dash if attack started during dash (Rule 4)
        if (_owner._playerController.CharacterState != null && _owner._playerController.CharacterState.IsDashing)
        {
            _owner._playerController.StateMachine.SetState<PlayerIdleState>();
        }

        // Damp horizontal momentum if grounded (Rule 5)
        if (_owner._playerController.CharacterState != null && _owner._playerController.CharacterState.IsGrounded)
        {
            _owner._playerController.ResetHorizontalVelocity();
        }

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
        }
        if (stateInfo.IsName("HeavyHoldAttack")
        && stateInfo.normalizedTime >= _currentAttack.ComboWindow + _currentAttack.RecoveryStartTime
        && !_owner.CombatContext.isAttacking)
        {
            _stateMachine.SetState<CombatIdleState>();
        }
        if (!stateInfo.IsName("HeavyHoldAttack"))
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public override void Exit()
    {
        _OverrideController["AttackTransition"] = _OverrideController["HeavyHoldAttack"];
        _animator.CrossFade(hashAnimationTransition, 0f, 0, _currentAttack.RecoveryStartTime);
        if (_owner.CombatContext.currentWeapon?.Trail != null)
        {
            _owner.CombatContext.currentWeapon.Trail.End();
        }
        _owner._playerController.SetCanMove(true);
    }
}
