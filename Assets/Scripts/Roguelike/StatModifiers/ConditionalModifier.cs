using UnityEngine;
using System;

public enum Condition
{
    GreaterThan,
    LessThan,
    EqualTo
}

[CreateAssetMenu(fileName = "Conditional Effect", menuName = "Stat Modifiers/Conditional Effect")]
public class ConditionalEffect : ScriptableObject, IStatModifier
{
    [SerializeField] private float baseValue = 10f;
    [SerializeField] private StatModifierType _modifierType = StatModifierType.Additive;
    [SerializeField] private StatModifierPolarity _modifierPolarity = StatModifierPolarity.Positive;
    [SerializeField] private StatType _targetStat = StatType.AttackDamage;
    [SerializeField] private StatType _conditionStat = StatType.MaxHealth;
    [SerializeField] private Condition _condition = Condition.GreaterThan;
    [SerializeField, Range(0f, 100f)] private float _conditionPercentageValue = 50f;
    [SerializeField, Range(0f, 1f)] private float _chance = 1f;

    public StatType TargetStat => _targetStat;
    public StatModifierType ModifierType => _modifierType;
    public StatModifierPolarity ModifierPolarity => _modifierPolarity;
    public StatType ConditionStat => _conditionStat;
    public float ConditionPercentageValue => _conditionPercentageValue;
    public Condition Condition => _condition;
    public float Chance => _chance;

    public float GetStatValue(IEntity entity, StatType statType)
    {
        switch (statType)
        {
            case StatType.MaxHealth:
                return entity.MaxHealth;
            case StatType.Health:
                return entity.Health;
            case StatType.Defense:
                return entity.BaseDefense;
            case StatType.AttackDamage:
                return entity.BaseDamage;
            default:
                Debug.LogWarning($"StatType {statType} not handled in GetStatValue.");
                return 0f;
        }
    }

    float GetCalculatedValue(float targetStat)
    {
        switch (_modifierType)
        {
            case StatModifierType.Additive:
                return targetStat + baseValue;
            case StatModifierType.Multiplicative:
                return targetStat * baseValue;
            default:
                Debug.LogWarning($"Modifier type {_modifierType} not handled in GetCalculatedValue.");
                return 0f;
        }
    }

    public float GetValue(float baseValue, IEntity entity)
    {
        if (entity == null)
        {
            Debug.LogWarning("Entity is null. Returning base value.");
            return _modifierType == StatModifierType.Additive ? 0f : 1f;
        }
        float statValue = GetStatValue(entity, _conditionStat);
        if (_condition == Condition.GreaterThan && statValue > _conditionPercentageValue ||
            _condition == Condition.LessThan && statValue < _conditionPercentageValue ||
            _condition == Condition.EqualTo && Mathf.Approximately(statValue, _conditionPercentageValue))
        {
            return GetCalculatedValue(baseValue);
        } else
        {
            return _modifierType == StatModifierType.Additive ? 0f : 1f;
        }
    }

    void OnValidate()
    {
        if (_chance < 0f)
        {
            _chance = 0f;
        }
        else if (_chance > 1f)
        {
            _chance = 1f;
        }
    }
}