using UnityEngine;
using UnityEngine.AI;

internal class SacrificeAttack : CombatActionState
{
    public enum Phase
    { Aiming, Locked, Exploding }
    private readonly NavMeshAgent agent;
    private readonly Animator animator;

    // the values will be taking from a scriptable object
    float lockedInTime;
    float maxAttackRange;
    float fuseDuration;
    float explosionRadius;
    float explosionDamage;

    private readonly SacrificeAttackConfig config;

    public SacrificeAttack(EnemyController enemyController, SacrificeAttackConfig config) : base(enemyController)
    {
        this.config = config;
        agent = enemyController.Agent;
        animator = enemyController.Animator;
        target = enemyController.TargetTransform;

        lockedInTime = config.lockedInTime;
        maxAttackRange = config.maxAttackRange;
        fuseDuration = config.fuseDuration;
        explosionRadius = config.explosionRadius;
        explosionDamage = config.explosionDamage;
    }


    private float fuseTimer;
    private Transform target;

    private Phase currentPhase;

    public override bool CanBeInterrupted => currentPhase != Phase.Exploding;

    public override void Enter()
    {
        currentPhase = Phase.Aiming;
        fuseTimer = 0f;
        IsFinished = false;
        animator.Play(Animator.StringToHash("Attack1"), 0, 0f);
        agent.isStopped = false;
    }

    public override void Tick()
    {
        if (currentPhase == Phase.Exploding)
            return;

        float distanceFromThePlayer = Vector3.Distance(enemyController.transform.position, target.position);
        if(distanceFromThePlayer > maxAttackRange)
        {
            enemyController.SetState<ChaseState>();
            return;
        }

        if (currentPhase == Phase.Aiming)
        {
            UpdateCircleAroundTarget();
        }

        fuseTimer += Time.deltaTime;

        if(fuseTimer >= lockedInTime && currentPhase == Phase.Aiming)
        {
            currentPhase = Phase.Locked;
            agent.isStopped = true;
        }

        if (fuseTimer >= fuseDuration)
            ExplodingInAction();
    }

    private void UpdateCircleAroundTarget()
    {
        Vector3 frontOfPlayer = target.position + target.forward ;
        agent.SetDestination(frontOfPlayer);
    }

    private void ExplodingInAction()
    {
        if (currentPhase == Phase.Exploding)
        {
            return;
        }

        currentPhase = Phase.Exploding;

        //“Which colliders are currently inside this area?”
        Collider[] hits = Physics.OverlapSphere(this.enemyController.transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            PlayerController player = hit.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.Entity.TakeDamage(explosionDamage);
            }
        }

        enemyController.SetState<ExplodeState>();
        enemyController.EnemyEntity.Kill();
    }

    public override void Exit()
    {
        fuseTimer = 0f;
        IsFinished = true;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }
}