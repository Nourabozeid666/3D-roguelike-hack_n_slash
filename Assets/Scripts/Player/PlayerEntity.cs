using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

[Serializable]
public class PlayerEntity : IEntity
{
    PlayerController playerController;
    CombatController combatController;
    public PlayerEntity(PlayerController playerController, CombatController combatController)
    {
        this.playerController = playerController;
        this.combatController = combatController;
    }

    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseDefense = 5f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float weaponLength = 1f;
    [SerializeField] private float weaponSize = 1f;
    [SerializeField] private float critChance = 0.1f;
    [SerializeField] private float critMultiplier = 1.25f;
    [SerializeField] private bool isDead = false;

    [SerializeField] private List<IStatModifier> modifiers = new List<IStatModifier>();

    public event Action<float, AttackEffectData> OnDamageTaken;
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
    public float CritMultiplier => critMultiplier;
    public float WeaponLength => weaponLength;
    public float WeaponSize => weaponSize;
    public bool IsDead => isDead;

    public List<IStatModifier> Modifiers => modifiers;

    // NOTE: these self-subscriptions are the Combat branch's intended pattern (the events double as
    // external entry points, e.g. EquipmentSystem raising OnModifierAdded). Keep them in sync with
    // raisers: TakeDamage/Heal must NEVER raise OnDamageTaken/OnHealed themselves, or this wiring
    // recurses infinitely. All current damage flows call TakeDamage() directly.
    public PlayerEntity()
    {

    }

    public void Initialize(PlayerController playerController, CombatController combatController)
    {
        this.playerController = playerController;
        this.combatController = combatController;
    }

    private float CalculateDamageReduction(float damage)
    {
        float modifiedDefense = baseDefense;
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].TargetStat == StatType.Defense)
            {
                modifiedDefense = modifiers[i].GetValue(modifiedDefense, this);
            }
        }
        // Clamped at zero: a hit weaker than defense deals 0 - it must never heal the player.
        return Mathf.Max(0f, damage - modifiedDefense * Constants.ALPHA);
    }

    public float CalculateAttackDamage(float multiplier = 1f)
    {
        float modifiedDamage = baseDamage;
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].TargetStat == StatType.AttackDamage)
            {
                modifiedDamage = modifiers[i].GetValue(modifiedDamage, this);
            }
        }
        return modifiedDamage * multiplier;
    }

    public void TakeDamage(float damage, AttackEffectData effectData = null)
    {
        bool isParry = combatController.CheckParry(out float multiplier, out bool isBlock);
        if (combatController != null && isParry)
        {
            combatController.CounterParry();
            return; // Exit early: parry handled, no further damage processing needed.
        }
        health -= CalculateDamageReduction(damage * multiplier);
        if (isBlock)
        {
            // Add some knockback ? 
            combatController.ExecuteKnockback(0.1f, -playerController.ReferencesContext.playerModel.forward, 200f).Forget();
        }
        else
        {
            OnDamageTaken?.Invoke(damage, effectData);
        }
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

    private void AddModifier(IStatModifier modifier)
    {
        modifiers.Add(modifier);
        if (modifier.TargetStat == StatType.MaxHealth)
        {
            SetMaxHealth(modifier.GetValue(maxHealth, this));
        }
        OnModifierAdded?.Invoke(modifier);
    }
}
