using UnityEngine;

[CreateAssetMenu(fileName = "SacrificeAttack", menuName = "Attacks/EnemyAttack")]

public class EnemyAttackConfig : ScriptableObject
{
    [SerializeField] private float baseDamage = 10f;
    public float BaseDamage {
        get { return baseDamage; }
    }
}

