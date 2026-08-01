using UnityEngine;
using System;
[Serializable]
public class EnemyEntity : IEnemyEntity
{
    [SerializeField] float currentHealth;
    [SerializeField] float maxHealth;
    [SerializeField] float baseDamage;
    [SerializeField] float attackState;
    //[SerializeField] AttackState attackState;
    public float CurrentHealth => currentHealth;

    public float MaxHealth => maxHealth;

    public float BaseDamage => baseDamage;

    public float BaseDefense => BaseDefense;

    //need fixing  ------------------------------------------------------------------------------------------------------------
    public AttackState AttackState => throw new NotImplementedException();

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action<float> OnDied;

    //need fixing  ------------------------------------------------------------------------------------------------------------
    public void ApplyAttack(AttackState AttackState)
    {
        throw new NotImplementedException();
    }

    public void SetMaxHealth(float maxHealth)
    {
        this.currentHealth = maxHealth;
        if (this.currentHealth > maxHealth) 
            this.currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        this.currentHealth -= damage;
        if (this.currentHealth < 0) 
            this.currentHealth = 0;
    }
}
