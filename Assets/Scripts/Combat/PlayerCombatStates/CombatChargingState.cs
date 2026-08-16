using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CombatChargingState : State<CombatController>
{
    private Animator _animator;
    private Text _attackDebugText;
    private AttackData _currentAttack;
    private WeaponObject _weaponObject;
    private AnimatorOverrideController _OverrideController;
    private int hashAnimationState;
    public CombatChargingState(Animator animator, AnimatorOverrideController overrideController, Text attackDebugText)
    {
        _animator = animator;
        _OverrideController = overrideController;
        _attackDebugText = attackDebugText;
        hashAnimationState = Animator.StringToHash("AttackCharging");
    }

    public override void Enter()
    {
        _currentAttack = _owner.CombatContext.currentAttack;
        _weaponObject = _owner.CombatContext.currentWeapon;
        AnimationClip animationClip;
        if (_currentAttack == null)
        {
            animationClip = _weaponObject.ChargeAnimation;
        }
        else
        {
            animationClip = _currentAttack.ChargeAnimationOverride != null ? _currentAttack.ChargeAnimationOverride : _weaponObject.ChargeAnimation;
        }
        _OverrideController["AttackCharging"] = animationClip;
        _animator.Play(hashAnimationState, 0, 0f);
        _owner._playerController.SetCanMove(false);
    }

    public override void Update()
    {

    }

    public override void Exit()
    {
        _owner.CombatContext.isCharging = false;
        _owner._playerController.SetCanMove(true);
    }
}
