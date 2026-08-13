using UnityEngine;

[CreateAssetMenu(fileName = "SacrificeAttack", menuName = "Attacks/SacrificeAttack")]
public class SacrificeAttackConfig : ScriptableObject
{
    //the time the enemy needs to finish before getting locked in the state in the same place
    public float lockedInTime = 1f;
    // the distance that the enemy will be starting to chase the player if he is further away from
    public float maxAttackRange = 4f;
    public float fuseDuration = 2.5f;
    // the max radius frrom the enenmy to a distance to damage the player
    public float explosionRadius = 10f;
    public float explosionDamage = 40f;
}
