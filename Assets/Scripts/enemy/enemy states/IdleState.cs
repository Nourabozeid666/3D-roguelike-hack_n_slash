using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class IdleState : EnemyState
{
    NavMeshAgent agent;
    Animator animator;
    EnemyController _owner;

    public IdleState(EnemyController enemyController) : base(enemyController)
    {
        agent = enemyController.Agent;
        animator = enemyController.Animator;
        _owner = enemyController;
    }

    public override void Enter()
    {
        Debug.Log("Entered idle state");
        animator.SetBool("isIdle", true);
    }

    public override void Tick()
    {
    }
    public override void Exit()
    {
        Debug.Log("Existed idle state");
        animator.SetBool("isIdle", false);
    }

}
