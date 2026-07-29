using Unity.IO.LowLevel.Unsafe;
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
        agent.isStopped = false;
        agent.speed = enemyController.PatrolSpeed;
        agent.stoppingDistance = enemyController.PatrolRange;

        Transform waypoint = patrolRoute.WayPoints[waypointIndex];
        agent.SetDestination(waypoint.position);
    }

    //runs and checks conditions while the state is active.
    public override void Tick()
    {
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            return;
        }
        waypointIndex = (waypointIndex + 1) % patrolRoute.WayPoints.Count;
        Debug.Log("New waypoint: " + waypointIndex);
        Transform waypoint = patrolRoute.WayPoints[waypointIndex];
        agent.SetDestination(waypoint.position);
    }
    // Clean up anything the current state started or changed before another state takes control.
    public override void Exit()
    {
        //animator.SetBool("isWalking", false);
    }
}
