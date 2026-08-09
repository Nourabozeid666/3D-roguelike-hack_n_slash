using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StaggerState : EnemyState
{
    NavMeshAgent agent;
    Animator animator;
    public enum ReactionType { Hit, Stun }
    ReactionType pondingReation = ReactionType.Hit;

    [SerializeField] float hitDuration = 0.5f;
    [SerializeField] float stunDuration = 4f;

    public override bool CanBeInterrupted => false;
    public StaggerState(EnemyController enemyController) : base(enemyController)
    {
        agent = enemyController.Agent;
        animator = enemyController.Animator;
    }

    public override void Enter()
    {
        agent.isStopped = true;
        PlayReaction();
    }

    public void SetReaction(ReactionType reactionType)
        => pondingReation = reactionType;

    void PlayReaction()
    {
        if (pondingReation == ReactionType.Stun)
        {
            animator.Play(Animator.StringToHash("GetStun"), 0, 0);
            enemyController.StartCoroutine(DurationCoroutine(stunDuration));
        }
        else
        {
            animator.Play(Animator.StringToHash("GetHit"), 0, 0);
            enemyController.StartCoroutine(DurationCoroutine(hitDuration));
        }
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        agent.isStopped = false;
    }

    IEnumerator DurationCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        // any state that can be interrupted
        enemyController.SetState<PatrolState>();
    }
}