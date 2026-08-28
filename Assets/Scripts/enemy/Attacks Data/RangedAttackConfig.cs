using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Attacks/EnemyAttack/RangedAttack")]
public class RangedAttackConfig : EnemyAttackConfig
{
    [SerializeField] private AnimationClip throwClip;     // Drag your Throw animation here
    [SerializeField] private GameObject projectilePrefab; // Drag your Rock Prefab here
    [SerializeField] private float damage = 15f;          // Damage dealt
    [SerializeField] private float projectileSpeed = 12f; // How fast the rock flies
    [SerializeField] private float releaseTime = 0.4f;    // Seconds into animation when rock is released

    // Helper properties:
    public int AnimationHash => throwClip != null ? Animator.StringToHash(throwClip.name) : 0;
    public float Duration => throwClip != null ? throwClip.length : 0f;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float Damage => damage;
    public float ProjectileSpeed => projectileSpeed;
    public float ReleaseTime => releaseTime;
}