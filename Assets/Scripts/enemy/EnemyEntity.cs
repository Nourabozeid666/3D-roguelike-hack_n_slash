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
    public float CurrentPoise => currentPoise;
    public float MaxPoise => maxPoise;

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

    public void SetBaseDamage(float newBaseDamage)
    {
        this.baseDamage = newBaseDamage;
    }

    public void TakeDamage( float damage, float poiseDamage = 0f)
    {
        if (damage <= 0f) 
            return;

        // One authoritative death transition: a dead enemy is dead. This guards against repeated
        // lethal hits (or a Kill() after death) re-firing OnDied and double-notifying listeners
        // such as SpawnSystem (which would double-decrement AliveCount / double-raise FloorCleared).
        if (currentHealth <= 0f)
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
    public void Kill()
    {
        if (currentHealth <= 0) 
            return;
        currentHealth = 0;
        OnDied?.Invoke();
    }
}
