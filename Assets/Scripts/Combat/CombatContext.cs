using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using static StaggerSeverity;

[Serializable]
public class CombatContext
{
    [Header("Current Weapon Data")]
    [SerializeField] internal AnimatorOverrideController overrideController;
    [SerializeField] internal float attackSpeed = 1f;
    [SerializeField] internal AttackData currentAttack;
    [SerializeField] internal AttackData queuedAttack;
    [SerializeField] internal AttackData previousAttack;
    [SerializeField] internal InputType currentInputType;
    [SerializeField] internal InputType queuedInputType;

    [Header("Enemy Interaction Data")]
    [SerializeField] internal Transform currentTargetPos;
    [SerializeField] internal float timeSinceDamageTaken = Mathf.Infinity;

    [Header("Poise & Stagger Data")]
    [SerializeField] internal float staggerImmunityTimer = Mathf.Infinity;
    [SerializeField] internal float staggerImmunityDuration = 1f; // Time in seconds of stagger immunity after being staggered
    [SerializeField] internal Severity currentStaggerSeverity = Severity.None;
    [SerializeField] internal bool isStaggered = false;
    [Header("Defense Data")]
    [SerializeField] internal float blockMultiplier = 0.25f; // Multiplier for damage
    [SerializeField] internal float parryMultiplier = 0f; // Multiplier for damage
    [SerializeField, Range(0f, 1f)] internal float parryEndTime = 0.5f; // How long will parry last in animation
    // Clash is when 2 entities attack at the same time
    [SerializeField, Range(0f, 1f)] internal float clashParryEndTime = 0.25f; // How long will clash parry last in animation
    [SerializeField] internal bool isBlocking = false;
    [SerializeField] internal bool isParrying = false;

    [Header("Input Data")]
    [SerializeField] internal string inputString = "";
    [SerializeField] internal InputState inputState;

    [Header("Running Values")]
    [SerializeField] internal bool canAttack = true; // True when not in middle of an attack animation or recovery frames
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
