using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DieState : EnemyState
{
    NavMeshAgent agent;
    Animator animator;

    public override bool CanBeInterrupted => false;
    public DieState(EnemyController enemyController) : base(enemyController)
    {
        agent = enemyController.Agent;
        animator = enemyController.Animator;
    }

    public override void Enter()
    {
        agent.isStopped = true;
        animator.Play(Animator.StringToHash("Death"), 0, 0);
        enemyController.StartCoroutine(UpdateCoroutine());
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
    }

    IEnumerator UpdateCoroutine()
    {
        yield return new WaitForSeconds(3f);
        Object.Destroy(enemyController.gameObject);
    }
}