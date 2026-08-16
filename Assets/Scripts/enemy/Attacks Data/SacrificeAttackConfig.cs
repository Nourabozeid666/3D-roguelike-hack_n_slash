using UnityEngine;

[CreateAssetMenu(fileName = "SacrificeAttack", menuName = "Attacks/EnemyAttack/SacrificeAttack")]
public class SacrificeAttackConfig : EnemyAttackConfig
{
    // make them all can not be changed from outside here <----------------------------------------------------------------------------------------
    //the time the enemy needs to finish before getting locked in the state in the same place
    [SerializeField] private float lockedInTime = 1f;
    public float LockedInTime
    {
        get { return lockedInTime; }
    }

    // the distance that the enemy will be starting to chase the player if he is further away from
    [SerializeField] float maxAttackRange = 4f;
    public float MaxAttackRange
    {
        get { return maxAttackRange; }
    }

    [SerializeField] float fuseDuration = 2.5f;
    public float FuseDuration
    {
        get { return fuseDuration; }
    }

    // the max radius frrom the enenmy to a distance to damage the player
    [SerializeField] float explosionRadius = 10f;
    public float ExplosionRadius
    {
        get { return explosionRadius; }
    }

    [SerializeField] float explosionDamage = 40f;
    public float ExplosionDamage
    {
        get { return explosionDamage; }
    }
}

