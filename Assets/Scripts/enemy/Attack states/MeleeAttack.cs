using UnityEngine;
using UnityEngine.AI;

internal class MeleeAttack : CombatActionState
{
    Animator animator;
    NavMeshAgent agent;
    public MeleeAttack(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
        agent = enemyController.Agent;
    }

    public override void Enter()
    {
        Debug.Log("melee state entered ----------------");
    }

    public override void Tick()
    {
    }
    public override void Exit()
    {
        Debug.Log("melee state exited");
    }
}