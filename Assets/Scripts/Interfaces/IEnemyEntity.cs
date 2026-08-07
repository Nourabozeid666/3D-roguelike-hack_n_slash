using System;

public interface IEnemyEntity
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    float BaseDamage { get; }
    float BaseDefense { get; }

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action OnDied;

    void TakeDamage(float damage, float poiseDamage = 0f);

    //void Heal(float healAmount);
    void SetMaxHealth(float maxHealth);
}
