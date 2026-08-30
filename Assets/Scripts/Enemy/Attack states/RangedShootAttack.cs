// Suggested path: Assets/Scripts/enemy/Attack states/RangedShootAttack.cs
using UnityEngine;

internal class RangedShootAttack : CombatActionState
{
    private readonly Animator animator;
    private readonly RangedAttackConfig config;
    private readonly Transform firePoint;
    private readonly Transform target; // CONFIRM: swap enemyController.Target for whatever field/property
                                        // your EnemyController actually uses to reference the player

    private int shotsFired;
    private float elapsed;
    private float shotTimer;

    public RangedShootAttack(EnemyController enemyController, RangedAttackConfig config, Transform firePoint)
        : base(enemyController)
    {
        this.config = config;
        this.firePoint = firePoint;
        animator = enemyController.Animator;
        target = enemyController.TargetTransform;
    }

    public override bool CanBeInterrupted => false;

    public override void Enter()
    {
        if (config == null || config.ProjectilePrefab == null || firePoint == null)
        {
            IsFinished = true;
            return;
        }

        shotsFired = 0;
        elapsed = 0f;
        shotTimer = 0f;
        IsFinished = false;

        animator.Play(config.AnimationHash, 0, 0f);

        // First shot fires immediately on entry. If you want the throw to land on a specific
        // animation frame instead (windup before release), delay this into Tick() using elapsed
        // time against a "releaseTime" field on the config — same idea as the shotTimer below.
        FireProjectile();
        shotsFired++;
    }

    public override void Tick()
    {
        if (IsFinished) return;

        elapsed += Time.deltaTime;

        if (shotsFired < config.ProjectileCount)
        {
            shotTimer += Time.deltaTime;
            if (shotTimer >= config.DelayBetweenShots)
            {
                FireProjectile();
                shotsFired++;
                shotTimer = 0f;
            }
        }

        if (elapsed >= config.Duration && shotsFired >= config.ProjectileCount)
            IsFinished = true;
    }

    private void FireProjectile()
    {
        GameObject go = ProjectilePoolManager.Instance != null
            ? ProjectilePoolManager.Instance.Get(config.ProjectilePrefab, firePoint.position, firePoint.rotation)
            : Object.Instantiate(config.ProjectilePrefab, firePoint.position, firePoint.rotation); // fallback if no manager is in the scene yet

        Projectile projectile = go.GetComponent<Projectile>();
        if (projectile == null) return;

        // Straight-line motion only uses this as a direction; arc motion uses it as
        // the actual landing point, which is why we pass a position, not just a vector.
        Vector3 aimTarget = target != null
            ? target.position
            : firePoint.position + firePoint.forward * 20f;

        projectile.Launch(firePoint.position, aimTarget, config.ProjectileSpeed, config.Damage);
    }

    public override void Exit() { }
}
