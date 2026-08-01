using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class CombatContext
{
    [Header("Current Weapon Data")]
    [SerializeField] internal WeaponObject currentWeapon;
    [SerializeField] internal AttackData currentAttack;
    [SerializeField] internal AttackData queuedAttack;
    
    [Header("Input Data")]
    [SerializeField] internal string inputString = "";
    [SerializeField] internal InputState inputState;

    [Header("Running Values")]
    [SerializeField] internal bool isAttacking = false; // True when in middle of an attack animation
    [SerializeField] internal float lightholdTime = 0f; // Increase after .performed and reset after .canceled
    [SerializeField] internal float heavyholdTime = 0f; // Increase after .performed and reset after .canceled
}

internal struct InputState
{
    internal bool lightAttackPressed;
    internal bool heavyAttackPressed;
}
