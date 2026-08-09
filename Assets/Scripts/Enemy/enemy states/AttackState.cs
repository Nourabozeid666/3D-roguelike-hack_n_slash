using UnityEngine;
using UnityEngine.AI;

public enum AttackType { 
    Melee, Ranged, Sacrifice, Strong, Combo, Exploding 
}

public abstract class CombatActionState : IEstate
{
    protected EnemyController enemyController;
    protected CombatActionState(EnemyController enemyController) => this.enemyController = enemyController;
    public abstract void Enter();
    public abstract void Exit();
    public abstract void Tick();
}

public class AttackState : EnemyState
{
    NavMeshAgent agent;

    EnemyStateMachine<CombatActionState> combatActions = new();
    public CombatActionState CurrentCombatAction => combatActions.CurrentState;
    public AttackState(EnemyController enemyController) : base(enemyController)
    {
        agent = enemyController.Agent;

        //add states to all the attacks you need in the game
        AddState(new MeleeAttack(enemyController));
        AddState(new RangedShootAttack(enemyController));
        AddState(new SacrificeAttack(enemyController));
        AddState(new StrongAttack(enemyController));
        AddState(new ComboAttack(enemyController));
        AddState(new ExplodingAttack(enemyController));
    }
    public override void Enter()
    {
        agent.isStopped = true;

<<<<<<< Updated upstream
        combatActions.SetState<MeleeAttack>();
=======
<<<<<<< Updated upstream
        //animator.SetBool("isWalking", false);
        //animator.SetBool("isRunning", false);
=======
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
>>>>>>> Stashed changes
        //animator.SetTrigger("isShooting");
>>>>>>> Stashed changes
    }

    public override void Exit()
    {
        agent.isStopped = false;
        combatActions.Exit();
    }

    public override void Tick()
    {
<<<<<<< Updated upstream
        Vector3 lookAtVector = new Vector3( enemyController.TargetTransform.position.x, enemyController.transform.position.y, enemyController.TargetTransform.position.z);
        enemyController.transform.LookAt(lookAtVector);
        combatActions.Tick();
=======
        //animator.SetTrigger("isShooting");
<<<<<<< Updated upstream
        Vector3 lookAtVector = new Vector3(enemyController.TargetTransform.position.x, enemyController.transform.position.y, enemyController.TargetTransform.position.z);
        enemyController.transform.LookAt(lookAtVector);
=======
        enemyController.transform.LookAt(enemyController.TargetTransform.position);
>>>>>>> Stashed changes
>>>>>>> Stashed changes
    }

     void AddState(CombatActionState combatAction)
    {
        this.combatActions.AddState(combatAction);
    }
}
