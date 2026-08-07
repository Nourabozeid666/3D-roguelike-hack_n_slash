using System.Collections.Generic;
using UnityEngine;

public class ComboSystem
{
    private CombatController owner;

    public ComboSystem(CombatController owner)
    {
        this.owner = owner;
    }
    public void QueueCombo(InputType inputType)
    {
        if (owner.CombatContext.queuedInputType != inputType && owner.CombatContext.currentAttack != null && owner.CombatContext.currentAttack.GetNext(inputType) != null)
        {
            owner.CombatContext.queuedAttack = owner.CombatContext.currentAttack.GetNext(inputType);
            owner.CombatContext.queuedInputType = inputType;
            Debug.Log("Queued attack: " + owner.CombatContext.queuedAttack.name + " (from current attack) and InputType: " + inputType);
        } else if (owner.CombatContext.queuedInputType != inputType)
        {
            owner.CombatContext.queuedAttack = owner.CombatContext.currentWeapon.EntryAttacks.Dict.GetValueOrDefault(inputType);
            owner.CombatContext.queuedInputType = inputType;
            Debug.Log("Queued attack: " + owner.CombatContext.queuedAttack.name + " (from weapon entry attack) and InputType: " + inputType);
        } else
        {
            Debug.Log("No valid attack found for InputType: " + inputType);
            Debug.Log("Current attack: " + (owner.CombatContext.currentAttack != null ? owner.CombatContext.currentAttack.name : "None"));
            Debug.Log("Weapon entry attacks: " + string.Join(", ", owner.CombatContext.currentWeapon.EntryAttacks.Dict.Keys));
        }

        float bufferDuration = owner.CombatContext.currentAttack?.ComboWindow ?? 0.5f;
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

    public void CheckInput()
    {
        switch (owner.CombatContext.inputState)
        {
            case InputState inputState when inputState.lightAttackPressed && owner.CombatContext.lightHoldTime > 0.5f:
                QueueCombo(InputType.LightHold);
                owner.CombatContext.lastInputTime = Time.time;
                break;
            case InputState inputState when inputState.heavyAttackPressed && owner.CombatContext.heavyHoldTime > 0.5f:
                QueueCombo(InputType.HeavyHold);
                owner.CombatContext.lastInputTime = Time.time;
                break;
            case InputState inputState when inputState.lightAttackPressed:
                QueueCombo(InputType.LightAttack);
                owner.CombatContext.lastInputTime = Time.time;
                break;
            case InputState inputState when inputState.heavyAttackPressed:
                QueueCombo(InputType.HeavyAttack);
                owner.CombatContext.lastInputTime = Time.time;
                break;
            default:
                if (owner.CombatContext.queuedAttack != null && Time.time > owner.CombatContext.bufferExpiryTime)
                {
                    ResetQueuedAttack();
                }
                break;
        }
    }

    public void ExecuteCombo()
    {
        if (owner.CombatContext.queuedAttack != null)
        {
            owner.CombatContext.currentAttack = owner.CombatContext.queuedAttack;
            owner.CombatContext.currentInputType = owner.CombatContext.queuedInputType;
            owner.CombatContext.queuedAttack = null;
        }
        ExecuteAttack();
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
                // owner.StateMachine.SetState<CombatHeavyAttackState>();
                break;
            case InputType.LightHold:
                // owner.StateMachine.SetState<CombatLightHoldAttackState>();
                break;
            case InputType.HeavyHold:
                // owner.StateMachine.SetState<CombatHeavyHoldAttackState>();
                break;
        }
    }
}