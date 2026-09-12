// Suggested path: Assets/Scripts/enemy/Attack states/RangedShootAttack.cs
using UnityEngine;
using UnityEngine.AI;

internal class RangedShootAttack : CombatActionState
{
    private readonly Animator animator;
    private readonly RangedAttackConfig config;
    private readonly Transform firePoint;
    private readonly Transform target; 

    private int shotsFired;
    private float elapsed;
    private float shotTimer;
    NavMeshAgent agent;

    public RangedShootAttack(EnemyController enemyController, RangedAttackConfig config, Transform firePoint)
        : base(enemyController)
    {
        this.config = config;
        this.firePoint = firePoint;
        animator = enemyController.Animator;
        target = enemyController.TargetTransform;
        agent = enemyController.Agent;
    }

    public override bool CanBeInterrupted => true;

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
        agent.isStopped = true;
        animator.Play(config.AnimationHash, 0, 0f);
    }

    public override void Tick()
    {
        if (IsFinished) return;

        elapsed += Time.deltaTime;

        // Fire when the arm extends forward (e.g. 0.6s into the animation):
        if (shotsFired == 0 && elapsed >= 0.6f)
        {
            FireProjectile();
            shotsFired++;
        }

        if (elapsed >= config.Duration && shotsFired >= config.ProjectileCount)
            IsFinished = true;

        agent.isStopped = false;
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
