using UnityEngine;
using UnityEngine.AI;

public enum AttackType { 
    Melee, Ranged, Sacrifice, Strong, Combo, Exploding 
}

public abstract class CombatActionState : IEstate
{
    protected EnemyController enemyController;

    public virtual bool CanBeInterrupted => true;
    public virtual bool IsFinished { get; protected set; }
    public virtual bool IsEligible => true; // “Can this attack start right now?”, eligible only when its cooldown is finished.

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
        AddState(new StrongAttack(enemyController));
        AddState(new ComboAttack(enemyController));
        AddState(new SacrificeAttack(enemyController, enemyController.SacrificeConfig));
    }

    public override bool CanBeInterrupted{
        get{
            if (CurrentCombatAction == null)
                return true;

            return CurrentCombatAction.CanBeInterrupted;
        }
    }

    public override void Enter()
    {
        combatActions.SetState<SacrificeAttack>();
    }

    public override void Exit()
    {
        combatActions.Exit();
    }

    public override void Tick()
    {
        Vector3 lookAtVector = new Vector3( enemyController.TargetTransform.position.x, enemyController.transform.position.y, enemyController.TargetTransform.position.z);
        enemyController.transform.LookAt(lookAtVector);
        combatActions.Tick();
    }

     void AddState(CombatActionState combatAction)
    {
        this.combatActions.AddState(combatAction);
    }
}
