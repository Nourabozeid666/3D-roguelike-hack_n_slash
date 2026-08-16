using UnityEngine;
using System;

public enum StatModifierType
{
    Additive,
    Multiplicative
}

public enum StatModifierPolarity
{
    Positive,
    Negative
}

public enum StatType
{
    MaxHealth,
    Defense,
    AttackDamage,
    AttackSpeed,
    CritChance,
    WeaponLength,
    WeaponSize,
}

public interface IStatModifier
{
    StatType TargetStat { get; }
    StatModifierType _modifierType { get; }
    StatModifierPolarity _modifierPolarity { get; }

    float GetValue(float baseValue, IEntity entity);
}