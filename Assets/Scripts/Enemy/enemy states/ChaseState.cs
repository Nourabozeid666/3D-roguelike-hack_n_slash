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
        animator.Play(Animator.StringToHash("Run"), 0, 0);
        agent.isStopped = false;
        agent.speed = enemyController.ChaseSpeed;
        agent.stoppingDistance = enemyController.AttackRange;
    }

    public override void Exit()
    {
    }

    public override void Tick()
    {
    }
}

