using System;
using System.Collections.Generic;

public interface IEntity
{
    float Health { get; }
    float MaxHealth { get; }
    float BaseDamage { get; }
    float BaseDefense { get; }
    bool IsDead { get; }

    List<IStatModifier> Modifiers { get; }

    public event Action<float, AttackEffectData> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action<float> OnMaxHealthChanged;
    public event Action OnDied;

    void TakeDamage(float damage, AttackEffectData effectData = null);
    void Heal(float healAmount);
    void SetMaxHealth(float maxHealth);
}
