using UnityEngine;
using System;

[CreateAssetMenu(fileName = "BasicModifier", menuName = "StatModifiers/BasicModifier")]
public class BasicModifier : ScriptableObject, IStatModifier
{
    [SerializeField] private float baseValue = 10f;
    [SerializeField] private StatModifierType modifierType = StatModifierType.Additive;
    [SerializeField] private StatModifierPolarity modifierPolarity = StatModifierPolarity.Positive;

    public StatType TargetStat => StatType.AttackDamage;
    public StatModifierType _modifierType => modifierType;
    public StatModifierPolarity _modifierPolarity => modifierPolarity;

    public float GetValue(float baseValue, IEntity entity)
    {
        return baseValue + this.baseValue;
    }
}