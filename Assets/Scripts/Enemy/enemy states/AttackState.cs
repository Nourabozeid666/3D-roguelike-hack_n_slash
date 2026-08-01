using UnityEngine;
using UnityEngine.AI;


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
    Animator animator;
    NavMeshAgent agent;

    EnemyStateMachine<CombatActionState> combatActions = new();
    public CombatActionState CurrentCombatAction => combatActions.CurrentState;
    public AttackState(EnemyController enemyController) : base(enemyController)
    {
        //animator = enemyController.Animator;
        agent = enemyController.Agent;
        AddState(new MeleeAttack(enemyController));
        AddState(new RangedShootAttack(enemyController));
        AddState(new SacrificeAttack(enemyController));
        AddState(new StrongAttack(enemyController));
        
    }
    public override void Enter()
    {
        agent.isStopped = true;
        //add states to all the attacks you need in the game
        combatActions.SetState<MeleeAttack>();
    }

    public override void Exit()
    {
        agent.isStopped = false;
        //animator.ResetTrigger("isShooting");
        combatActions.Exit();
    }

    public override void Tick()
    {
        //animator.SetTrigger("isShooting");
        Vector3 lookAtVector = new Vector3( enemyController.TargetTransform.position.x, enemyController.transform.position.y, enemyController.TargetTransform.position.z);
        enemyController.transform.LookAt(lookAtVector);
        combatActions.Tick();
    }
     void AddState(CombatActionState combatAction)
    {
        this.combatActions.AddState(combatAction);
    }

}