using UnityEngine;
using System;
using System.Collections.Generic;
using static StaggerSeverity;
[Serializable]
public class EnemyEntity : IEntity
{
    [SerializeField] float currentHealth = 100;
    [SerializeField] float maxHealth = 100;
    [SerializeField] float baseDamage = 10;
    [SerializeField] float baseDefence = 0;

    [Header("-------------Poise-------------")]
    [SerializeField] PoiseTier currentPoise = PoiseTier.Normal;
    [SerializeField] Severity staggerSeverity = Severity.None;
    [SerializeField] bool isDead = false;
    [SerializeField] List<IStatModifier> modifiers = new List<IStatModifier>();
    

    private float timeSinceLastPoiseDamage;

    public float Health => currentHealth;
    public float MaxHealth => maxHealth;
    public float BaseDamage => baseDamage;
    public float BaseDefense => baseDefence;
    // public float CurrentPoise => currentPoise;
    // public float MaxPoise => maxPoise;
    public PoiseTier CurrentPoise => currentPoise;
    public Severity StaggerSeverity => staggerSeverity;
    public bool IsDead => isDead;
    public List<IStatModifier> Modifiers => modifiers;
    public event Action<float, AttackEffectData> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action<float> OnMaxHealthChanged;
    public event Action OnDied;
    public event Action<Severity> OnStaggered;
    //public event Action Died;

    public void Initialize()
        => Initialize(null, null);

    public void Initialize(EnemyArchetypeConfig config)
        => Initialize(config, null);

    /// <summary>
    /// Single authoritative initialization. Authored stats come from the shared archetype config
    /// (read-only, never mutated). scaledMaxHealthOverride - provided by the SpawnSystem stat seam
    /// (ISpawnStatConfig.ConfigureForSpawn) - is applied LAST so the per-instance floor-scaled value
    /// wins over the authored base, and the enemy spawns at full scaled health.
    /// </summary>
    public void Initialize(EnemyArchetypeConfig config, float? scaledMaxHealthOverride)
    {
        if (config == null && !scaledMaxHealthOverride.HasValue)
        {
            Debug.LogWarning($"EnemyEntity.Initialize called with no archetype config - using default field values.", null);
            currentHealth = maxHealth;
            // currentPoise = maxPoise;
            return;
        }

        if (config != null)
        {
            // maxPoise = config.maxPoise;
            baseDamage = config.baseDamage;
            baseDefence = config.baseDefense;
            if (!scaledMaxHealthOverride.HasValue)
                maxHealth = config.maxHealth;
        }

        if (scaledMaxHealthOverride.HasValue)
            maxHealth = scaledMaxHealthOverride.Value;

        currentHealth = maxHealth;
        // currentPoise = maxPoise;
    }

    // public void TickPoiseRegen(float deltaTime)
    // {
    //     if (currentPoise >= maxPoise)
    //         return;

    //     timeSinceLastPoiseDamage += deltaTime;

    //     if (timeSinceLastPoiseDamage < poiseRegenDelay)
    //         return;

    //     currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRegenRate * deltaTime);
    // }

    public void SetMaxHealth(float newMaxHealth)
    {
        this.maxHealth = newMaxHealth;
        if (this.currentHealth > maxHealth)
            this.currentHealth = maxHealth;
        OnMaxHealthChanged?.Invoke(newMaxHealth);
    }
    
    public void Heal(float healAmount)
    {
        return; // TODO: implement healing for enemies if needed
    }

    public void TakeDamage(float damage, AttackEffectData effectData = null)
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
        Severity staggerSeverity = GetStaggerSeverity(currentPoise, effectData.appliedStagger);
        if (staggerSeverity != Severity.None)
        {
            // currentPoise = maxPoise;
            OnStaggered?.Invoke(staggerSeverity);
            return;
        }

        OnDamageTaken?.Invoke(damage, effectData);
        Debug.Log($"TakeDamage - dmg:{damage} poise:{effectData?.appliedStagger ?? 0f} | HP:{currentHealth} | Poise:{currentPoise}");
    }
    public void Kill()
    {
        if (currentHealth <= 0)
            return;
        currentHealth = 0;
        OnDied?.Invoke();
    }
}
