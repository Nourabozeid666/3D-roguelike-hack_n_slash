using UnityEngine;
using System;
using System.Collections.Generic;

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
    [SerializeField] private string attackName;
    [SerializeField] private float damageMultiplier = 1f; // default
    [SerializeField] private AnimationClip animation;
    [SerializeField] private float comboWindow = 0.5f; // default
    [SerializeField] private float holdTime = 0.5f; // default
    [SerializeField] private Vector3 lungeDirection = Vector3.forward; // default
    [SerializeField] private float lungeDistance = 1f; // default
    [SerializeField] private bool isHoldAttack = false;

    [SerializeField] private AttackTransition[] transitions;
    public string AttackName { get { return attackName; } }
    public float DamageMultiplier { get { return damageMultiplier; } }
    public AnimationClip Animation { get { return animation; } }
    public float ComboWindow { get { return comboWindow; } }
    public float HoldTime { get { return holdTime; } }
    public Vector3 LungeDirection { get { return lungeDirection; } }
    public float LungeDistance { get { return lungeDistance; } }
    public bool IsHoldAttack { get { return isHoldAttack; } }

    public AttackData GetNext(InputType input)
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
private void PrintAllCombos(string comboString = "", HashSet<AttackData> visited = null)
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