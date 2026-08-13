using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ExplodeState : EnemyState
{
    NavMeshAgent agent;
    Animator animator;
    GameObject explosionParticles;

    public override bool CanBeInterrupted => false;

    public ExplodeState(EnemyController enemyController) : base(enemyController)
    {
        agent = enemyController.Agent;
        animator = enemyController.Animator;
        explosionParticles = enemyController.ExplosionParticles;
    }

    public override void Enter()
    {
        agent.isStopped = true;
        animator.Play(Animator.StringToHash("Explode"), 0, 0);
        enemyController.StartCoroutine(DestroyAfterDelay());
        explosionParticles?.SetActive(false);
    }

    public override void Tick()
    {

    }

    public override void Exit()
    {

    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(1.25f);
        explosionParticles?.SetActive(true);
        yield return new WaitForSeconds(.75f);
        Object.Destroy(enemyController.gameObject);
    }
}