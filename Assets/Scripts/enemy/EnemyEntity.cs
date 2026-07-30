using System;
// An abstract class is intended to be used as a base for other classes and cannot be instantiated directly.

[Serializable]
public class EnemyEntity : IEntity
{
    public float Health => throw new NotImplementedException();

    public float MaxHealth => throw new NotImplementedException();

    public float BaseDamage => throw new NotImplementedException();

    public float BaseDefense => throw new NotImplementedException();

    public float[] AddedDamage => throw new NotImplementedException();

    public float[] AddedDefense => throw new NotImplementedException();

    public float[] DamageMultipliers => throw new NotImplementedException();

    public float[] DefenseMultipliers => throw new NotImplementedException();

    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;

    public EnemyEntity()
    {

    }

    public void Heal(float healAmount)
    {
        throw new NotImplementedException();
    }

    public void SetMaxHealth(float maxHealth)
    {
        throw new NotImplementedException();
    }

    public void TakeDamage(float damage)
    {
        throw new NotImplementedException();
    }
}

