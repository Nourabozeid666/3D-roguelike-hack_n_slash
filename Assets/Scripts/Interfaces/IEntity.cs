using System;

public interface IEntity
{
    float Health { get; }
    float MaxHealth { get; }
    float BaseDamage { get; }
    float BaseDefense { get; }

    // Temporarily float, will have its own class later
    IStatModifier[] Modifiers { get; }

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action<float> OnMaxHealthChanged;
    public event Action OnDied;

    void TakeDamage(float damage);
    void Heal(float healAmount);
    void SetMaxHealth(float maxHealth);
}
