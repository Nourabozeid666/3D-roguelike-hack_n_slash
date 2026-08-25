using UnityEngine;

internal class ComboAttack : CombatActionState
{
    private readonly Animator animator;
    private readonly ComboAttackConfig config; // all the combos that we are gonna use
    private readonly DamageHitboxHelper hitbox; //deal damage through the attack

    ComboSequence activeSequence;  // Which combo recipe did I pick for this attack?
    int hitIndex;  // Which swing in the combo am I currently on? (0, 1, 2...)
    float elapsedInHit;  // How many seconds have passed since this swing started?

    public override bool CanBeInterrupted => true;

    public ComboAttack( EnemyController enemyController, ComboAttackConfig config, DamageHitboxHelper hitbox) : base(enemyController)
    {
        animator = enemyController.Animator;
        this.config = config;
        this.hitbox = hitbox;
    }


    void PlayCurrentHit()
    {
        var hit = activeSequence.Hits[hitIndex];
        elapsedInHit = 0;
        // hitbox.SetDamage(hit.Damage);
        animator.Play(hit.AnimationHash,0,0f);
    }
    public override void Enter()
    {
        activeSequence = config.Sequences[Random.Range(0,config.Sequences.Count)];
        hitIndex = 0;
        IsFinished = false;
        PlayCurrentHit();
        hitbox.OnHitboxTriggered += OnHitboxTriggered;
    }

    void OnHitboxTriggered(GameObject gameObject, IEntity entity)
    {
        Debug.Log($"Enemy hit {entity} for {activeSequence.Hits[hitIndex].Damage} damage!");
        var hit = activeSequence.Hits[hitIndex];
        entity.TakeDamage(hit.Damage);
    }

    public override void Tick()
    {
        elapsedInHit += Time.deltaTime;

        // Uses the clip's actual duration automatically!
        if (elapsedInHit < activeSequence.Hits[hitIndex].Duration)
            return;

        
        if (hitIndex >= activeSequence.Hits.Count - 1)
            IsFinished = true;
        else
        {
            hitIndex++;
            PlayCurrentHit();
        }
    }

    public override void Exit()
    {
    }
}