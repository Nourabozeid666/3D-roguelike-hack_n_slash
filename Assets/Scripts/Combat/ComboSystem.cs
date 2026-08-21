using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ComboSystem
{
    private CombatController owner;

    private float queueCooldown = 0.1f;
    private bool canQueue = true;
    public ComboSystem(CombatController owner)
    {
        this.owner = owner;
    }
    public void QueueCombo(InputType inputType)
    {
        Debug.Log("First Condition: " + (owner.CombatContext.queuedInputType != inputType).ToString() + " && " + (owner.CombatContext.currentAttack != null).ToString() + " && " + (owner.CombatContext.currentAttack != null ? owner.CombatContext.currentAttack.GetNext(inputType) != null : false).ToString());
        if (!canQueue) { Debug.Log("Cannot queue attack yet. Cooldown active."); return; }
        ;
        canQueue = false;
        WaitForQueueCooldown().Forget();
        if (owner.CombatContext.queuedInputType != inputType && owner.CombatContext.currentAttack != null && owner.CombatContext.currentAttack.GetNext(inputType) != null)
        {
            owner.CombatContext.queuedAttack = owner.CombatContext.currentAttack.GetNext(inputType);
            owner.CombatContext.queuedInputType = inputType;
            Debug.Log("Queued attack: " + owner.CombatContext.queuedAttack.name + " (from current attack) and InputType: " + inputType);
        }
        else if (owner.CombatContext.queuedInputType != inputType)
        {
            owner.CombatContext.queuedAttack = owner.equipmentSystem.CurrentWeapon.EntryAttacks.Dict.GetValueOrDefault(inputType);
            owner.CombatContext.queuedInputType = inputType;
            Debug.Log("Queued attack: " + owner.CombatContext.queuedAttack.name + " (from weapon entry attack) and InputType: " + inputType);
        }
        else
        {
            Debug.Log("No valid attack found for InputType: " + inputType);
        }
        Debug.Log("Current attack: " + (owner.CombatContext.currentAttack != null ? owner.CombatContext.currentAttack.name : "None"));
        Debug.Log("Weapon entry attacks: " + string.Join(", ", owner.equipmentSystem.CurrentWeapon.EntryAttacks.Dict.Keys));

        float bufferDuration = 0.2f;
        owner.CombatContext.bufferExpiryTime = Time.time + bufferDuration;
        Debug.Log("Buffer expiry time set to: " + owner.CombatContext.bufferExpiryTime);
    }

    public void ResetQueuedAttack()
    {
        owner.CombatContext.queuedAttack = null;
        owner.CombatContext.queuedInputType = InputType.None;
        Debug.Log("Queued attack reset.");
    }

    public void ResetCurrentAttack()
    {
        owner.CombatContext.currentAttack = null;
        owner.CombatContext.currentInputType = InputType.None;
        Debug.Log("Current attack reset.");
    }

    bool CheckNextHeldAttack(InputType inputType, float holdTime)
    {
        AttackData targetAttack = null;
        if (owner.CombatContext.currentAttack != null)
        {
            targetAttack = owner.CombatContext.currentAttack.GetNext(inputType);
        }
        if (targetAttack == null)
        {
            targetAttack = owner.equipmentSystem.CurrentWeapon.EntryAttacks.Dict.GetValueOrDefault(inputType);
        }

        if (targetAttack != null && targetAttack.IsHoldAttack)
        {
            if (holdTime >= targetAttack.HoldTime)
            {
                Debug.Log("Hold time sufficient for attack: " + targetAttack.name + " with hold time: " + holdTime);
                return true;
            }
            else
            {
                Debug.Log("Hold time insufficient for attack: " + targetAttack.name + " with hold time: " + holdTime);
            }
        }
        return false;
    }

    public void CheckInput()
    {
        var input = owner.CombatContext.inputState;
        if (owner._playerController.CharacterState != null && !owner._playerController.CharacterState.CanAttack)
        {
            input.lightAttackReleased = false;
            input.heavyAttackReleased = false;
            return;
        }

        if (input.lightAttackReleased)
        {
            owner.CombatContext.inputState.lightAttackReleased = false;
            if (CheckNextHeldAttack(InputType.LightHold, input.lightHoldTimeAtRelease))
            {
                QueueCombo(InputType.LightHold);
            }
            else
            {
                QueueCombo(InputType.LightAttack);
            }
            owner.CombatContext.lastInputTime = Time.time;
        }
        else if (input.heavyAttackReleased)
        {
            owner.CombatContext.inputState.heavyAttackReleased = false;
            if (CheckNextHeldAttack(InputType.HeavyHold, input.heavyHoldTimeAtRelease))
            {
                QueueCombo(InputType.HeavyHold);
            }
            else
            {
                QueueCombo(InputType.HeavyAttack);
            }
            owner.CombatContext.lastInputTime = Time.time;
        }
        else if (input.lightAttackPressed
        && owner.CombatContext.lightHoldTime > 0.15f
        && !owner.CombatContext.isCharging && !owner.CombatContext.isAttacking)
        {
            owner.CombatContext.isCharging = true;
            owner.StateMachine.SetState<CombatChargingState>();
        }
        else if (input.heavyAttackPressed
        && owner.CombatContext.heavyHoldTime > 0.15f
        && !owner.CombatContext.isCharging && !owner.CombatContext.isAttacking)
        {
            owner.CombatContext.isCharging = true;
            owner.StateMachine.SetState<CombatChargingState>();
        }
        else
        {
            if (owner.CombatContext.queuedAttack != null && Time.time > owner.CombatContext.bufferExpiryTime)
            {
                ResetQueuedAttack();
            }
        }
    }

    public void ExecuteCombo()
    {
        if (owner.CombatContext.queuedAttack != null)
        {
            owner.CombatContext.previousAttack = owner.CombatContext.currentAttack;
            owner.CombatContext.currentAttack = owner.CombatContext.queuedAttack;
            owner.CombatContext.currentInputType = owner.CombatContext.queuedInputType;
            owner.CombatContext.queuedAttack = null;
            owner.CombatContext.queuedInputType = InputType.None;
        }
        ExecuteAttack();
    }

    public UniTask WaitForQueueCooldown()
    {
        return UniTask.Delay(System.TimeSpan.FromSeconds(queueCooldown)).ContinueWith(() =>
        {
            canQueue = true;
        });
    }

    void ExecuteAttack()
    {
        Debug.Log("Executing attack: " + owner.CombatContext.currentAttack.name + " with InputType: " + owner.CombatContext.currentInputType);
        owner.CombatContext.isAttacking = true;
        switch (owner.CombatContext.currentInputType)
        {
            case InputType.LightAttack:
                owner.StateMachine.SetState<CombatLightAttackState>();
                break;
            case InputType.HeavyAttack:
                owner.StateMachine.SetState<CombatHeavyAttackState>();
                break;
            case InputType.LightHold:
                owner.StateMachine.SetState<CombatLightHoldState>();
                break;
            case InputType.HeavyHold:
                owner.StateMachine.SetState<CombatHeavyHoldState>();
                break;
        }
    }
}