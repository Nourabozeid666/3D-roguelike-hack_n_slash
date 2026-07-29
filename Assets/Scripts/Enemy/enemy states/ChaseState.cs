using UnityEngine;
using UnityEngine.AI;

public class ChaseState : EnemyState
{
    Animator animator;
    NavMeshAgent agent;
    public ChaseState(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
        agent = enemyController.Agent;
    }

    public override void Enter()
    {
        agent.isStopped = false;
        agent.speed = enemyController.ChaseSpeed;
        agent.stoppingDistance = enemyController.AttackRange;
        //animator.SetBool("isRunning", true);
    }

    public override void Exit()
    {
        agent.speed = enemyController.PatrolSpeed;
        agent.stoppingDistance = enemyController.PatrolRange;
        //animator.SetBool("isRunning", false);
    }

    public override void Tick()
    {
    }
}

