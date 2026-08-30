using UnityEngine;
using UnityEngine.AI;

public class ChaseState : EnemyState
{
    private const float MinSafeDistance = 6f; // Back up if player gets closer than this
    private const float DestinationRefreshInterval = 0.25f;

    private readonly Animator animator;
    private readonly NavMeshAgent agent;
    private float refreshTimer;

    public ChaseState(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
        agent = enemyController.Agent;
    }

    public override void Enter()
    {
        animator.Play(Animator.StringToHash("Run"), 0, 0);
        agent.isStopped = false;
        agent.speed = enemyController.ChaseSpeed;
        agent.stoppingDistance = 0f;
        refreshTimer = 0f;
    }

    public override void Exit()
    {
    }

    public override void Tick()
    {
        Transform target = enemyController.TargetTransform;
        if (target == null) return;

        Vector3 toPlayer = target.position - enemyController.transform.position;
        float distance = toPlayer.magnitude;

        // 1. IN RANGE (6m - 12m): Stop running and attack!
        if (distance >= MinSafeDistance && distance <= enemyController.AttackRange)
        {
            agent.isStopped = true;
            enemyController.SetState<AttackState>();
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = DestinationRefreshInterval;

        // 2. TOO CLOSE (< 6m): Backpedal away from player!
        if (distance < MinSafeDistance)
        {
            Vector3 fleeDirection = -toPlayer.normalized;
            Vector3 retreatPoint = enemyController.transform.position + fleeDirection * 4f;

            if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }
        // 3. TOO FAR (> 12m): Move closer to player to get into throwing range!
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }
}