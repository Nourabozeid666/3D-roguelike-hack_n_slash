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
    Health,
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
    StatModifierType ModifierType { get; }
    StatModifierPolarity ModifierPolarity { get; }

    float Chance { get; }

    float GetValue(float baseValue, IEntity entity);
}