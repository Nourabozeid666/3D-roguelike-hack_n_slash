using UnityEngine;
using System;

[Serializable]
public class PlayerEntity : IEntity
{
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseDefense = 5f;

    [SerializeField] private float[] addedDamage = new float[0];
    [SerializeField] private float[] addedDefense = new float[0];
    [SerializeField] private float[] damageMultipliers = new float[0];
    [SerializeField] private float[] defenseMultipliers = new float[0];

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public float BaseDamage => baseDamage;
    public float BaseDefense => baseDefense;

    public float[] AddedDamage => addedDamage;
    public float[] AddedDefense => addedDefense;
    public float[] DamageMultipliers => damageMultipliers;
    public float[] DefenseMultipliers => defenseMultipliers;

    public PlayerEntity()
    {
        OnDamageTaken += TakeDamage;
    }
    private float CalculateDamageReduction(float damage)
    {
        return damage - (baseDefense * Constants.ALPHA);
    }

    private float CalculateAttackDamage(float damage)
    {
        return baseDamage;
    }

    public void TakeDamage(float damage)
    {
        health -= CalculateDamageReduction(damage);
        if (health < 0) health = 0;
    }

    public void Heal(float healAmount)
    {
        health += healAmount;
        if (health > maxHealth) health = maxHealth;
    }

    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        if (health > maxHealth) health = maxHealth;
    }
}
