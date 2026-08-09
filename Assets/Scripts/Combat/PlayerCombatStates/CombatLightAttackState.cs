using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CombatLightAttackState : State<CombatController>
{
    private Animator _animator;
    private AnimatorOverrideController _OverrideController;
    private Text _attackDebugText;
    private AttackData _currentAttack;
    private int hashAnimationState;
    private int hashAnimationTransition;
    public CombatLightAttackState(Animator animator, Text attackDebugText)
    {
        _animator = animator;
        _OverrideController = _OverrideController != null ? (AnimatorOverrideController)_animator.runtimeAnimatorController : new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _OverrideController;
        _attackDebugText = attackDebugText;
        hashAnimationState = Animator.StringToHash("LightAttack");
        hashAnimationTransition = Animator.StringToHash("AttackTransition");
    }

    public override void Enter()
    {
        AttackData attack = _owner.CombatContext.currentAttack;
        _currentAttack = attack;
        _attackDebugText.text = $"Current Attack: {attack.AttackName}";

        _OverrideController["LightAttack"] = attack.Animation;
        _animator.Play(hashAnimationState, 0, 0f);
        ExecuteLunge().Forget();
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
        // so stateInfo still returns the previous animation's normalizedTime
        if (_animator.IsInTransition(0)) return;
        
        // Check if the animation is done                    
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        // Debug.Log("Current Animation State Normalized Time: " + stateInfo.normalizedTime);
        if (stateInfo.IsName("LightAttack")
        && stateInfo.normalizedTime >= _currentAttack.RecoveryStartTime
        && _owner.CombatContext.isAttacking)
        {
            _owner.CombatContext.isAttacking = false;
        }
        if (stateInfo.IsName("LightAttack")
        && stateInfo.normalizedTime >= _currentAttack.ComboWindow + _currentAttack.RecoveryStartTime
        && !_owner.CombatContext.isAttacking)
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public override void Exit()
    {
        _OverrideController["AttackTransition"] = _OverrideController["LightAttack"];
        _animator.CrossFade(hashAnimationTransition, 0f, 0, _currentAttack.RecoveryStartTime);
    }
}
