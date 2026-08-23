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
    public event Action<IStatModifier> OnModifierAdded;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public float BaseDamage => baseDamage;
    public float BaseDefense => baseDefense;
    public float AttackSpeed => attackSpeed;
    public float CritChance => critChance;
    public float WeaponLength => weaponLength;
    public float WeaponSize => weaponSize;
    public bool IsDead => isDead;

    public IStatModifier[] Modifiers => modifiers;

    // NOTE: these self-subscriptions are the Combat branch's intended pattern (the events double as
    // external entry points, e.g. EquipmentSystem raising OnModifierAdded). Keep them in sync with
    // raisers: TakeDamage/Heal must NEVER raise OnDamageTaken/OnHealed themselves, or this wiring
    // recurses infinitely. All current damage flows call TakeDamage() directly.
    public PlayerEntity()
    {
        OnDamageTaken += TakeDamage;
        OnHealed += Heal;
        OnMaxHealthChanged += SetMaxHealth;
        OnModifierAdded += HandleModifierAdded;
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
        // Clamped at zero: a hit weaker than defense deals 0 - it must never heal the player.
        return Mathf.Max(0f, damage - modifiedDefense * Constants.ALPHA);
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
            // Fire exactly once per life: repeated post-death hits must not re-raise OnDied.
            if (!isDead)
            {
                isDead = true;
                OnDied?.Invoke();
            }
        };
    }

    public void Heal(float healAmount)
    {
        health += healAmount;
        if (health > maxHealth) health = maxHealth;
        isDead = false; // revived: allow a future death notification
    }

    public void SetMaxHealth(float maxHealth)
    {
        float healthPercentage = health / this.maxHealth;
        this.maxHealth = maxHealth;
        health = healthPercentage * maxHealth;
    }

    void HandleModifierAdded(IStatModifier modifier)
    {
        if (modifier.TargetStat == StatType.MaxHealth)
        {
            SetMaxHealth(modifier.GetValue(maxHealth, this));
        }
    }
}
