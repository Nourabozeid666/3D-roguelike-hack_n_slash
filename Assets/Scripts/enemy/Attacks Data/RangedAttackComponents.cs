using UnityEngine;

public class RangedAttackComponents : MonoBehaviour
{
    [SerializeField] private RangedAttackConfig config;
    [SerializeField] private Transform firePoint; // Child Transform placed at the enemy's hand

    public RangedAttackConfig Config => config;
    public Transform FirePoint => firePoint;
}