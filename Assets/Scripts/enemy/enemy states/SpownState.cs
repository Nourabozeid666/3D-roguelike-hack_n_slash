using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SpownState : EnemyState
{
    Animator animator;
    NavMeshAgent agent;
        
    public override bool CanBeInterrupted => false;
    public SpownState(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
        agent = enemyController.Agent;
    }

    public override void Enter()
    {
        animator.Play( Animator.StringToHash("Idle"), 0, 0);
        agent.isStopped = true;
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
<<<<<<< Updated upstream
=======
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
>>>>>>> Stashed changes
        yield return new WaitForSeconds(3f);
        enemyController.SetState<PatrolState>();
    }
}
