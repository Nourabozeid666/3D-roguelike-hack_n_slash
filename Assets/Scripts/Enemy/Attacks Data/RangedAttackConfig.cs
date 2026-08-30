// Suggested path: Assets/Scripts/enemy/Attacks Data/RangedAttackConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Attacks/EnemyAttack/RangedAttack")]
public class RangedAttackConfig : EnemyAttackConfig
{
    [SerializeField] private AnimationClip throwClip;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float projectileSpeed = 15f;

    [Tooltip("1 = single throw. Higher = a volley of shots fired during one Enter().")]
    [SerializeField] private int projectileCount = 1;

    [Tooltip("Only used when projectileCount > 1.")]
    [SerializeField] private float delayBetweenShots = 0.15f;

    [Tooltip("Minimum time before this enemy can re-enter this attack again.")]
    [SerializeField] private float cooldown = 1.5f;

    // Generates hash from the clip name for animator.Play()
    public int AnimationHash => throwClip != null ? Animator.StringToHash(throwClip.name) : 0;

    // Pulled from the animation length, same convention as ComboHit.Duration
    public float Duration => throwClip != null ? throwClip.length : 0f;

    public GameObject ProjectilePrefab => projectilePrefab;
    public float Damage => damage;
    public float ProjectileSpeed => projectileSpeed;
    public int ProjectileCount => Mathf.Max(1, projectileCount);
    public float DelayBetweenShots => delayBetweenShots;
    public float Cooldown => cooldown;
}
