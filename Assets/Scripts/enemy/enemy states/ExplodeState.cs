using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ExplodeState : EnemyState
{
    NavMeshAgent agent;
    Animator animator;
    GameObject explosionParticles;
    float explosionDuration;
    float timeBeforeExplosion;
    public override bool CanBeInterrupted => false;

    public ExplodeState(EnemyController enemyController) : base(enemyController)
    {
        agent = enemyController.Agent;
        animator = enemyController.Animator;
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
        yield return new WaitForSeconds(timeBeforeExplosion);
        explosionParticles?.SetActive(true);
        yield return new WaitForSeconds(explosionDuration);
        Object.Destroy(enemyController.gameObject);
    }

    public void SetExplosionParticles(GameObject particles, float explosionDuration, float timeBeforeExplosion)
    {
        explosionParticles = particles;
        this.explosionDuration = explosionDuration;
        this.timeBeforeExplosion = timeBeforeExplosion;
    }

}