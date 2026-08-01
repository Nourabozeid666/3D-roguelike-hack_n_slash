using System;

public interface IEnemyEntity
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    float BaseDamage { get; }
    float BaseDefense { get; }
    AttackState AttackState { get; }

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action<float> OnDied;

    void ApplyAttack(AttackState AttackState);
    void TakeDamage(float damage);
    //void Heal(float healAmount);
    void SetMaxHealth(float maxHealth);
}
