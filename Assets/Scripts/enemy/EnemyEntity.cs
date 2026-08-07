using UnityEngine;
using System;
[Serializable]
public class EnemyEntity : IEnemyEntity
{
    [SerializeField] float currentHealth;
    [SerializeField] float maxHealth;
    [SerializeField] float baseDamage;
    [SerializeField] float baseDefence;

    [Header("-------------Poise-------------")]
    [SerializeField] float maxPoise = 100f;   // grunt: set this LOW (even 1). boss: set HIGH.
    [SerializeField] float currentPoise;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float BaseDamage => baseDamage;
    public float BaseDefense => baseDefence;

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action OnDied;
    public event Action OnStaggered;

    public void Initialize()
    {
        currentHealth = maxHealth;
        currentPoise = maxPoise;
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        this.maxHealth = newMaxHealth;
        if (this.currentHealth > maxHealth) 
            this.currentHealth = maxHealth;
    }

    public void TakeDamage( float damage, float poiseDamage = 0f)
    {
        if (damage <= 0f) 
            return;

        this.currentHealth -= damage;

        if (this.currentHealth <= 0)
        {
            this.currentHealth = 0;
            OnDied?.Invoke();
            return; // dead things don't also stagger
        }

        //for stagger
        this.currentPoise -= poiseDamage;
        if (this.currentPoise <= 0)
        {
            currentPoise = maxPoise;
            OnStaggered?.Invoke();
            return;
        }

        OnDamageTaken?.Invoke(damage);
    }

}
