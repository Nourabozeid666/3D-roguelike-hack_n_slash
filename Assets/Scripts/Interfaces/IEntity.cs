using System;

public interface IEntity
{
    float Health { get; }
    float MaxHealth { get; }
    float BaseDamage { get; }
    float BaseDefense { get; }

    // Temporarily float, will have its own class later
    IStatModifier[] AddedDamage { get; }
    IStatModifier[] AddedDefense { get; }
    IStatModifier[] DamageMultipliers { get; }
    IStatModifier[] DefenseMultipliers { get; }

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;

    void TakeDamage(float damage);
    void Heal(float healAmount);
    void SetMaxHealth(float maxHealth);
}
