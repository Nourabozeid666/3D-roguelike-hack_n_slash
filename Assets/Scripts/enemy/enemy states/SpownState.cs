using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SpownState : EnemyState
{
    Animator animator;
    EnemyController owner;
    NavMeshAgent agent;
        
    public override bool CanBeInterrupted => false;
    public SpownState(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
        owner = enemyController;
        agent = enemyController.Agent;
    }

    public override void Enter()
    {
        animator.Play( Animator.StringToHash("Idle1"), 0, 0);
        // set animation or visaul effects 
        agent.isStopped = true;
        owner.StartCoroutine(UpdateCoroutine());
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
        enemyController.SetState<PatrolState>();
    }
}
