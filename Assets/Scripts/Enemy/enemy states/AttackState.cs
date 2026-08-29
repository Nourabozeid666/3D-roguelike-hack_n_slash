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
    public virtual bool IsEligible => true; // �Can this attack start right now?�, eligible only when its cooldown is finished.

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
    DamageHitboxHelper hitboxHelper;
    public AttackState(EnemyController enemyController, DamageHitboxHelper hitboxHelper) : base(enemyController)
    {
        agent = enemyController.Agent;
        this.hitboxHelper = hitboxHelper;

        //add states to all the attacks you need in the game
        // -------------------------------------------------need adds for each attack state--------------------------------------------------------------

        //AddState(new RangedShootAttack(enemyController));
        //var sacrificeParts = enemyController.GetComponent<SacrificeAttackComponents>();
        //AddState(new StrongAttack(enemyController));

        var comboParts = enemyController.GetComponent<ComboAttackComponents>();
        if (comboParts != null)
            AddState(new ComboAttack(enemyController, comboParts.Config, hitboxHelper));
        var sacrificeParts = enemyController.GetComponent<SacrificeAttackComponents>();
        if (sacrificeParts != null)
            AddState(new SacrificeAttack(enemyController, sacrificeParts.Config, sacrificeParts.ExplosionParticles));
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
        if (combatActions.EnemyStates.ContainsKey(typeof(SacrificeAttack)))
            combatActions.SetState<SacrificeAttack>();
        else if(combatActions.EnemyStates.ContainsKey(typeof(ComboAttack)))
            combatActions.SetState<ComboAttack>();
    }

    public override void Exit()
    {
        combatActions.Exit();
    }

    bool CheckTargetDistance()
    {
        float distanceToTarget = Vector3.Distance(enemyController.transform.position, enemyController.TargetTransform.position); 
        return distanceToTarget > enemyController.AttackRange;
    }

    public override void Tick()
    {
        Vector3 lookAtVector = new Vector3( enemyController.TargetTransform.position.x, enemyController.transform.position.y, enemyController.TargetTransform.position.z);
        enemyController.transform.LookAt(lookAtVector);
        combatActions.Tick();
        if (combatActions.CurrentState != null && combatActions.CurrentState.IsFinished || CheckTargetDistance())
        {
            enemyController.SetState<ChaseState>();
        }
    }

     void AddState(CombatActionState combatAction)
    {
        this.combatActions.AddState(combatAction);
    }
}
