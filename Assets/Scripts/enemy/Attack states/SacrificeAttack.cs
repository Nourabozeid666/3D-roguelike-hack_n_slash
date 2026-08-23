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
    float timeToStartExplosionState;
    float explosionRadius;
    float explosionDamage;
    float explosionDuration;
    float timeBeforeExplosion;
    private readonly GameObject explosionParticles;

    private readonly SacrificeAttackConfig config;

    public SacrificeAttack(EnemyController enemyController, SacrificeAttackConfig config, GameObject explosionParticles)
        : base(enemyController)
    {
        this.config = config;
        agent = enemyController.Agent;
        animator = enemyController.Animator;
        target = enemyController.TargetTransform;
        this.explosionParticles = explosionParticles;

        lockedInTime = config.LockedInTime;
        maxAttackRange = config.MaxAttackRange;
        timeToStartExplosionState = config.TimeToStartExplosionState;
        explosionRadius = config.ExplosionRadius;

        // Scale explosion damage proportionally to the runtime melee damage so floor-scaled
        // enemies deal floor-scaled explosion damage. Uses RuntimeDamage (written by SpawnSystem
        // through ConfigureForSpawn) relative to the shared SO base; shared assets are never mutated.
        float ratio = config.BaseDamage > 0f ? enemyController.RuntimeDamage / config.BaseDamage : 1f;
        explosionDamage = config.ExplosionDamage * ratio;

        explosionDuration = config.ExplosionDuration;
        timeBeforeExplosion = config.TimeBeforeExplosion;
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

        if (fuseTimer >= timeToStartExplosionState)
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
        ExplodeState explodeState = enemyController.GetState<ExplodeState>() as ExplodeState;

        explodeState?.SetExplosionParticles(explosionParticles,explosionDuration,timeBeforeExplosion);
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
