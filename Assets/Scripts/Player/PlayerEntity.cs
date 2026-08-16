using UnityEngine;
using System;

[Serializable]
public class PlayerEntity : IEntity
{
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseDefense = 5f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float critChance = 0.1f;
    [SerializeField] private float weaponLength = 1f;
    [SerializeField] private float weaponSize = 1f;
    [SerializeField] private bool isDead = false;

    [SerializeField] private IStatModifier[] modifiers = new IStatModifier[0];

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action<float> OnMaxHealthChanged;
    public event Action OnDied;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public float BaseDamage => baseDamage;
    public float BaseDefense => baseDefense;

    public IStatModifier[] Modifiers => modifiers;

    public PlayerEntity()
    {
        OnDamageTaken += TakeDamage;
        OnHealed += Heal;
        OnMaxHealthChanged += SetMaxHealth;
    }
    private float CalculateDamageReduction(float damage)
    {
        float modifiedDefense = baseDefense;
        for (int i = 0; i < modifiers.Length; i++)
        {
            if (modifiers[i].TargetStat == StatType.Defense)
            {
                modifiedDefense = modifiers[i].GetValue(modifiedDefense, this);
            }
        }
        return damage - (modifiedDefense * Constants.ALPHA);
    }

    private float CalculateAttackDamage()
    {
        float modifiedDamage = baseDamage;
        for (int i = 0; i < modifiers.Length; i++)
        {
            if (modifiers[i].TargetStat == StatType.AttackDamage)
            {
                modifiedDamage = modifiers[i].GetValue(modifiedDamage, this);
            }
        }
        return modifiedDamage;
    }

    public void TakeDamage(float damage)
    {
        health -= CalculateDamageReduction(damage);
        if (health < 0) {
            health = 0;
            isDead = true;
            OnDied?.Invoke();
        };
    }

    public void Heal(float healAmount)
    {
        health += healAmount;
        if (health > maxHealth) health = maxHealth;
    }

    public void SetMaxHealth(float maxHealth)
    {
        float healthPercentage = health / this.maxHealth;
        this.maxHealth = maxHealth;
        health = healthPercentage * maxHealth;
    }

    void OnModifierAdded(IStatModifier modifier)
    {
        if (modifier.TargetStat == StatType.MaxHealth)
        {
            SetMaxHealth(modifier.GetValue(maxHealth, this));
        }
    }
}
