using UnityEngine;
using System;

[Serializable]
public class PlayerEntity : IEntity
{
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseDefense = 5f;

    [SerializeField] private IStatModifier[] addedDamage = new IStatModifier[0];
    [SerializeField] private IStatModifier[] addedDefense = new IStatModifier[0];
    [SerializeField] private IStatModifier[] damageMultipliers = new IStatModifier[0];
    [SerializeField] private IStatModifier[] defenseMultipliers = new IStatModifier[0];

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;

    /// <summary>Raised exactly once per life when health reaches 0. Cleared by Heal, so a revived
    /// player can die (and notify) again. Listeners: game-over flow, HUD flashes, audio.</summary>
    public event Action OnDied;

    bool died;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public float BaseDamage => baseDamage;
    public float BaseDefense => baseDefense;

    public IStatModifier[] AddedDamage => addedDamage;
    public IStatModifier[] AddedDefense => addedDefense;
    public IStatModifier[] DamageMultipliers => damageMultipliers;
    public IStatModifier[] DefenseMultipliers => defenseMultipliers;

    // NOTE: no constructor wiring. The old `OnDamageTaken += TakeDamage` self-subscription was a
    // landmine: every raise of OnDamageTaken would have called TakeDamage again (double damage,
    // infinite recursion once TakeDamage itself started raising it). Events are raised explicitly
    // by the mutators below instead.

    private float CalculateDamageReduction(float damage)
    {
        // Clamped at zero: a hit weaker than defense deals 0 — it must never heal the player.
        return Mathf.Max(0f, damage - baseDefense * Constants.ALPHA);
    }

    private float CalculateAttackDamage(float damage)
    {
        return baseDamage;
    }

    public void TakeDamage(float damage)
    {
        health -= CalculateDamageReduction(damage);
        if (health < 0f) health = 0f;
        OnDamageTaken?.Invoke(damage);
        CheckDeath();
    }

    public void Heal(float healAmount)
    {
        health += healAmount;
        if (health > maxHealth) health = maxHealth;
        if (health > 0f) died = false; // revived: allow a future death notification
        OnHealed?.Invoke(healAmount);
    }

    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        if (health > maxHealth) health = maxHealth;
        CheckDeath();
    }

    void CheckDeath()
    {
        if (health > 0f || died) return;
        died = true;
        OnDied?.Invoke();
    }
}
