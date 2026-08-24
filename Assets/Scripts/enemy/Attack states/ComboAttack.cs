using UnityEngine;

internal class ComboAttack : CombatActionState
{
    private readonly Animator animator;
    private readonly ComboAttackConfig config; // all the combos that we are gonna use
    private readonly DealDamage hitbox; //deal damage through the attack

    ComboSequence activeSequence;  // Which combo recipe did I pick for this attack?
    int hitIndex;  // Which swing in the combo am I currently on? (0, 1, 2...)
    float elapsedInHit;  // How many seconds have passed since this swing started?

    public override bool CanBeInterrupted => true;

    public ComboAttack( EnemyController enemyController, ComboAttackConfig config, DealDamage hitbox) : base(enemyController)
    {
        animator = enemyController.Animator;
        this.config = config;
        this.hitbox = hitbox;
    }


    void PlayCurrentHit()
    {
        var hit = activeSequence.Hits[hitIndex];
        elapsedInHit = 0;
        animator.Play(hit.AnimationHash,0,0f);
    }
    public override void Enter()
    {
        activeSequence = config.Sequences[Random.Range(0,config.Sequences.Count)];
        hitIndex = 0;
        IsFinished = false;
        PlayCurrentHit();
    }

    public override void Tick()
    {
        elapsedInHit += Time.deltaTime;

        // Uses the clip's actual duration automatically!
        if (elapsedInHit < activeSequence.Hits[hitIndex].Duration)
            return;

        hitIndex++;
        if (hitIndex >= activeSequence.Hits.Count)
            IsFinished = true;
        else
            PlayCurrentHit();
    }

    public override void Exit()
    {
    }
}