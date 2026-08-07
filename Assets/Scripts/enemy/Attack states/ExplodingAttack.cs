using UnityEngine;

public class ExplodingAttack : CombatActionState
{
    Animator animator;
    public ExplodingAttack(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
    }

    public override void Enter()
    {
        animator.Play(Animator.StringToHash("Attack1"), 0, 0);
    }

    public override void Exit()
    {
    }

    public override void Tick()
    {
    }
}