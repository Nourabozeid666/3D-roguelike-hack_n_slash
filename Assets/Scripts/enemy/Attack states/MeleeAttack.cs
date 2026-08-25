using UnityEngine;
using UnityEngine.AI;

public class MeleeAttack : CombatActionState
{
    Animator animator;
    NavMeshAgent agent;
    public MeleeAttack(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
        agent = enemyController.Agent;
    }
    // capable of being a combo
    public override void Enter()
    {
        animator.Play(Animator.StringToHash("Attack1"), 0, 0);
    }

    public override void Tick()
    {

    }
    public override void Exit()
    {
    }
}