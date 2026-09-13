using UnityEngine;
using UnityEngine.AI;
public class PatrolState : EnemyState
{
    int waypointIndex = 0;
    NavMeshAgent agent;
    PatrolRoute patrolRoute;
    Animator animator;

    // agent.SetDestination(wayPoint.position)
    public PatrolState(EnemyController enemyController) : base(enemyController)
    {
        agent = enemyController.Agent;
        patrolRoute = enemyController.PatrolRoute;
        animator = enemyController.Animator;
    }
    //prepares the behavior when the state begins.
    public override void Enter()
    {
        // Never touch an agent that is disabled or not placed on a NavMesh yet.
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.speed = enemyController.PatrolSpeed;
        agent.stoppingDistance = enemyController.WaypointStoppingDistance;
        animator.Play(Animator.StringToHash("Walk"), 0, 0);
        if (patrolRoute != null && patrolRoute.WayPoints != null && patrolRoute.WayPoints.Count > 0)
        {
            Transform waypoint = patrolRoute.WayPoints[waypointIndex % patrolRoute.WayPoints.Count];
            agent.SetDestination(waypoint.position);
        }
    }

    //runs and checks conditions while the state is active.
    public override void Tick()
    {
        // remainingDistance is only valid on an active agent that has been placed on a NavMesh.
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        if (agent.pathPending || agent.remainingDistance >= agent.stoppingDistance)
        {
            return;
        }
        if (patrolRoute == null || patrolRoute.WayPoints == null || patrolRoute.WayPoints.Count == 0)
            return;
        waypointIndex = (waypointIndex + 1) % patrolRoute.WayPoints.Count;
        Debug.Log(waypointIndex);
        Debug.Log("New waypoint: " + waypointIndex);
        Transform waypoint = patrolRoute.WayPoints[waypointIndex];
        agent.SetDestination(waypoint.position);
    }
    // Clean up anything the current state started or changed before another state takes control.
    public override void Exit()
    {
    }
}
