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
        agent.stoppingDistance = enemyController.AttackRange - 1f; 
    }

    public override void Exit()
    {
    }

    public override void Tick()
    {
        Transform target = enemyController.TargetTransform;
        if (target == null) return;

        agent.SetDestination(target.position);
    }
}