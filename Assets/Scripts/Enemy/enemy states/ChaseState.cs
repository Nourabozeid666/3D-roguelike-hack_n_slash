using UnityEngine;
using UnityEngine.AI;

public class ChaseState : EnemyState
{
    private readonly Animator animator;
    private readonly NavMeshAgent agent;

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
        agent.stoppingDistance = enemyController.IsRangedEnemy ? 0.5f : Mathf.Max(0.5f, enemyController.AttackRange - 1f);
    }

    public override void Exit()
    {
    }

    public override void Tick()
    {
        Transform target = enemyController.TargetTransform;
        if (target == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        Vector3 lookAtVector = new Vector3(target.position.x, enemyController.transform.position.y, target.position.z);
        enemyController.transform.LookAt(lookAtVector);

        if (enemyController.IsRangedEnemy)
        {
            UpdateRangedMovement(target);
        }
        else
        {
            UpdateMeleeMovement(target);
        }
    }

    private void UpdateMeleeMovement(Transform target)
    {
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    private void UpdateRangedMovement(Transform target)
    {
        float distance = Vector3.Distance(enemyController.transform.position, target.position);
        float attackRange = enemyController.AttackRange;
        // The enemy retreats if the player gets closer than 65% of its attack range
        float retreatDistance = attackRange * 0.65f;

        if (distance < retreatDistance)
        {
            // Move away from the player (kite)
            Vector3 fleeDirection = (enemyController.transform.position - target.position).normalized;
            fleeDirection.y = 0f;
            if (fleeDirection.sqrMagnitude < 0.001f)
                fleeDirection = -enemyController.transform.forward;

            Vector3 retreatPosition = enemyController.transform.position + fleeDirection * 5f;

            if (NavMesh.SamplePosition(retreatPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
            else
            {
                // Fallback: try perpendicular escape if directly behind is blocked
                Vector3 flank = Vector3.Cross(fleeDirection, Vector3.up).normalized;
                Vector3 flankPosition = enemyController.transform.position + flank * 5f;
                if (NavMesh.SamplePosition(flankPosition, out NavMeshHit flankHit, 5f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(flankHit.position);
                }
            }
        }
        else if (distance > attackRange)
        {
            // Player is too far away, move closer to get in attack range
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
        else
        {
            // In the sweet spot (between retreatDistance and attackRange): hold ground and aim
            agent.isStopped = true;
        }
    }
}