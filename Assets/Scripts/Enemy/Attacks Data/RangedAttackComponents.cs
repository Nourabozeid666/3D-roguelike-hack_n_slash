// Suggested path: Assets/Scripts/Enemy/Attacks Data/RangedAttackComponents.cs
using UnityEngine;

public class RangedAttackComponents : MonoBehaviour
{
    [SerializeField] private RangedAttackConfig config;
    [SerializeField] private Transform firePoint; // muzzle / hand socket the projectile spawns from

    public RangedAttackConfig Config => config;
    public Transform FirePoint => firePoint;
}
