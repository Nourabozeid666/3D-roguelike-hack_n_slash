using UnityEngine;
using System;
using System.Collections.Generic;
using static StaggerSeverity;

public enum InputType { None, LightAttack, HeavyAttack, LightHold, HeavyHold }

[Serializable]
public struct AttackTransition
{
    public InputType input;
    public AttackData nextAttack;
}


[CreateAssetMenu(fileName = "New Attack", menuName = "Weapons/Attack")]
public class AttackData : ScriptableObject
{
    #nullable enable

    [SerializeField] private string attackName = "New Attack";
    [SerializeField] private AttackEffectData effectData = new AttackEffectData();
    [SerializeField] private AnimationClip animation;
    [SerializeField] private AnimationClip? chargeAnimationOverride;
    [SerializeField] private float comboWindow = 0.5f; // default
    [SerializeField, Range(0f, 1f)] private float recoveryStartTime = 0.75f; // default
    [SerializeField] private float holdTime = 0.5f; // default
    [SerializeField] private Vector3 lungeDirection = Vector3.forward; // default
    [SerializeField] private float lungeDistance = 1f; // default
    [SerializeField] private float lungeDuration = 0.5f; // default
    [SerializeField] private InputType inputType = InputType.LightAttack; // default
    [SerializeField] private bool isHoldAttack = false;

    [SerializeField] private AttackTransition[] transitions;
    public string AttackName { get { return attackName; } }
    public AttackEffectData EffectData { get { return effectData; } }
    public AnimationClip Animation { get { return animation; } }
    public AnimationClip? ChargeAnimationOverride { get { return chargeAnimationOverride; } }
    public float ComboWindow { get { return comboWindow; } }
    public float RecoveryStartTime { get { return recoveryStartTime; } }
    public float HoldTime { get { return holdTime; } }
    public Vector3 LungeDirection { get { return lungeDirection; } }
    public float LungeDistance { get { return lungeDistance; } }
    public float LungeDuration { get { return lungeDuration; } }
    public InputType InputType { get { return inputType; } }
    public bool IsHoldAttack { get { return isHoldAttack; } }

    public AttackData? GetNext(InputType input)
    {
        foreach (var t in transitions)
            if (t.input == input)
                return t.nextAttack;
        return null;
    }


    #if UNITY_EDITOR
    private void OnValidate()
    {
        var seen = new HashSet<InputType>();
        foreach (var t in transitions)
        {
            if (!seen.Add(t.input))
            {
                Debug.LogWarning($"[{name}] Duplicate transition for input '{t.input}' — only the first will be used.", this);
            }
        }
    }

    #endif
private void PrintAllCombos(string comboString = "", HashSet<AttackData>? visited = null)
{
    visited ??= new HashSet<AttackData>();
    if (!visited.Add(this))
    {
        Debug.Log(comboString + " -> [LOOP: " + AttackName + "]");
        return;
    }

    var currentCombo = comboString == "" ? AttackName : comboString;

    // No transitions, or all transitions are null/empty -> this is an endpoint
    if (transitions == null || transitions.Length == 0)
    {
        Debug.Log(currentCombo);
        return;
    }

    bool printedAny = false;
    foreach (var t in transitions)
    {
        if (t.nextAttack == null) continue; // skip empty slots, don't silently vanish

        printedAny = true;
        var newComboString = currentCombo + " -> " + t.nextAttack.AttackName;
        t.nextAttack.PrintAllCombos(newComboString, new HashSet<AttackData>(visited));
    }

    if (!printedAny)
        Debug.Log(currentCombo); // all transitions were empty, treat as endpoint
}
    [ContextMenu("Print All Combos")] private void PrintAllCombosMenuEntry() => PrintAllCombos();
}

[Serializable]
public class AttackEffectData
{
    [Header("Damage & Scaling")]
    [Range(0.25f, 5f)] public float multiplier = 1f;

    [Header("Poise & Stagger Resolution")]
    public StaggerTier appliedStagger = StaggerTier.Normal;
    public PoiseTier selfPoise = PoiseTier.Normal;

    [Header("Combat Feel")]
    public float hitstopDuration = 0.03f;
    public Vector3 knockbackForce = new Vector3(0, 0, 5f);
    public bool canDeflect = false;
}