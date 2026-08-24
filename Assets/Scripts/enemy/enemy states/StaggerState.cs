using System.Collections;
using UnityEngine;
using UnityEngine.AI;


/*
    Enemy interruption system:

    Interrupt types:
        Hit:
            normal damage / small flinch.
            should NOT always cancel attacks.

        PoiseBreak:
            poise reached zero.
            can cancel normal attacks and enter StaggerState.

        Death:
            health reached zero.
            interrupts everything except Death itself.

    Each EnemyState decides which interrupt types it accepts.

    Example:
        PatrolState  -> Hit yes, PoiseBreak yes, Death yes
        AttackState  -> Hit no,  PoiseBreak yes, Death yes
        StaggerState -> Hit no,  PoiseBreak no,  Death yes
        DieState     -> nothing can interrupt it
*/

public class StaggerState : EnemyState
{
    NavMeshAgent agent;
    Animator animator;
    Coroutine hitFlashCoroutine;

    public enum ReactionType { Hit, Stun }


    float hitDuration;
    float stunDuration;
    // It is just a variable that stores the currently running coroutine.

    Coroutine reactionCoroutine;

    ReactionType pondingReaction = ReactionType.Hit;
    ReactionType currentReaction = ReactionType.Hit;

    bool CanReactionBeInterrupted => currentReaction == ReactionType.Hit;

    public override bool CanBeInterrupted => false;


    public StaggerState(EnemyController enemyController) : base(enemyController)
    {
        agent = enemyController.Agent;
        animator = enemyController.Animator;
        hitDuration = enemyController.HitDuration;
        stunDuration = enemyController.StunDuration;
    }

    public override void Enter()
    {
        agent.isStopped = true;
        currentReaction = pondingReaction;
        PlayReaction();
    }

    public void SetReaction(ReactionType reactionType)
        => pondingReaction = reactionType;


    void PlayReaction()
    {
        RestartTimer();
        ReplayAnimation();
    }

    void RestartTimer()
    { 
        float duration = currentReaction == ReactionType.Stun ? stunDuration : hitDuration;
        if (reactionCoroutine != null)
            enemyController.StopCoroutine(reactionCoroutine);
        reactionCoroutine = enemyController.StartCoroutine(DurationCoroutine(duration));
    }

    void ReplayAnimation()
    {
        string clip = currentReaction == ReactionType.Stun ? "GetStun" : "GetHit";
        animator.Play(Animator.StringToHash(clip), 0, 0);
    }

    public void ReceiveHit(ReactionType incoming)
    {
        if (currentReaction == ReactionType.Stun)
        {
            if (hitFlashCoroutine != null)
                enemyController.StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = enemyController.StartCoroutine(HitFlash());
            return;
        }

        currentReaction = incoming;
        PlayReaction();
    }

    IEnumerator HitFlash()
    {
        animator.Play(Animator.StringToHash("GetHit"), 0, 0);
        yield return new WaitForSeconds(hitDuration);
        animator.Play(Animator.StringToHash("GetStun"), 0, 0);
    }

    public override void Tick()
    {
    }

    public override void Exit()
    {
        agent.isStopped = false;
        if (reactionCoroutine != null)
        {
            enemyController.StopCoroutine(reactionCoroutine);
            reactionCoroutine = null;
        }
        if (hitFlashCoroutine != null)
        {
            enemyController.StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }
    }

    IEnumerator DurationCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        Debug.Log($"StaggerState timer done — health:{enemyController.EnemyEntity.Health}");

        // any state that can be interrupted
        enemyController.SetState<PatrolState>();
    }
}