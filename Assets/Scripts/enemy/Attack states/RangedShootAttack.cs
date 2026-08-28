using UnityEngine;

internal class RangedShootAttack : CombatActionState
{
    private readonly Animator animator;
    private readonly RangedAttackConfig config;
    private readonly Transform firePoint;
    private readonly Transform target;

    private float elapsed;
    private bool hasFired;

    public RangedShootAttack(EnemyController enemyController, RangedAttackConfig config, Transform firePoint)
        : base(enemyController)
    {
        this.config = config;
        this.firePoint = firePoint;
        animator = enemyController.Animator;
        target = enemyController.TargetTransform; // Matches your EnemyController TargetTransform
    }

    public override bool CanBeInterrupted => true;

    public override void Enter()
    {
        if (config == null || config.ProjectilePrefab == null || firePoint == null)
        {
            IsFinished = true;
            return;
        }

        elapsed = 0f;
        hasFired = false;
        IsFinished = false;

        animator.Play(config.AnimationHash, 0, 0f);
    }

    public override void Tick()
    {
        if (IsFinished) return;

        elapsed += Time.deltaTime;

        if (!hasFired && elapsed >= config.ReleaseTime)
        {
            FireProjectile();
            hasFired = true;
        }

        if (elapsed >= config.Duration)
        {
            IsFinished = true;
        }
    }

    private void FireProjectile()
    {
        GameObject fire = ProjectilePoolManager.Instance != null
            ? ProjectilePoolManager.Instance.Get(config.ProjectilePrefab, firePoint.position, firePoint.rotation)
            : Object.Instantiate(config.ProjectilePrefab, firePoint.position, firePoint.rotation);

        Projectile proj = fire.GetComponent<Projectile>();
        if (proj == null) 
            return;

        Vector3 aimTarget = target != null
            ? target.position
            : firePoint.position + firePoint.forward * 20f;

        proj.Launch(firePoint.position, aimTarget, config.ProjectileSpeed, config.Damage);
    }

    public override void Exit()
    {
    }
}