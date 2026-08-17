using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class CombatContext
{
    [Header("Current Weapon Data")]
    [SerializeField] internal AnimatorOverrideController overrideController;
    [SerializeField] internal WeaponObject currentWeapon;
    [SerializeField] internal AttackData currentAttack;
    [SerializeField] internal AttackData queuedAttack;
    [SerializeField] internal AttackData previousAttack;
    [SerializeField] internal InputType currentInputType;
    [SerializeField] internal InputType queuedInputType;

    [Header("Enemy Interaction Data")]
    [SerializeField] internal Transform currentTargetPos;
    
    [Header("Input Data")]
    [SerializeField] internal string inputString = "";
    [SerializeField] internal InputState inputState;

    [Header("Running Values")]
    [SerializeField] internal bool isAttacking = false; // True when in middle of active hit frames of an attack animation
    [SerializeField] internal bool isRecovering = false; // True during recovery frames of an attack animation
    [SerializeField] internal bool isCharging = false; // True when in middle of a charge animation
    [SerializeField] internal float lightHoldTime = 0f; // Increase after .performed and reset after .canceled
    [SerializeField] internal float heavyHoldTime = 0f; // Increase after .performed and reset after .canceled
    [SerializeField] internal float lastInputTime = 0f; // Increase after .canceled and reset after .performed
    [SerializeField] internal float bufferExpiryTime = Mathf.Infinity; // Time at which the queued input expires
}

internal struct InputState
{
    internal bool lightAttackPressed;
    internal bool heavyAttackPressed;
    internal bool lightAttackReleased;
    internal bool heavyAttackReleased;
    internal float lightHoldTimeAtRelease;
    internal float heavyHoldTimeAtRelease;
}
