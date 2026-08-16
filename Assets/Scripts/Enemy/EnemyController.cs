using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*
    basic states for a grunt enemy:
        attackState  
        RetreatState
        { 
            SacrificeAttack
            RangedShootAttack
            MeleeAttack
        }
        DefendState { use sheild - run away}
        stagerState{
        // poise damage: for every attack
        // if the poise > poise damage no interruption
        // if the poise <= poise damage the stager bar gets less by the the poise damage
        // get it sepatated
            standard and grunt :
             only take damage and get back for getting hurt
            boss : 
             does not feel any thing unless the stager bar is full and then empty it and make him hurt for some seconds
        }
        
    basic attackState for Melee archetypes
    {
    #Standard
        Shielder (strong attack - weak attack - defand)
        fast (assassin) (attack - run away)
    }

    spownEnemySystem:
       *ScriptableObject to store the spown points
       *system to spown a certain amout of enemies with spown times  
       *controls what eneies are we spownning, Enemies will be with high cost. And actually it's going to be prevented from being in the first 
        levels, so they are going to be open after a certain amount of levels.
       *It has a specific cost for how many levels the player have get through. And the enemies, each of them is going to have a certain amount
        of this cost. So we're going to combine the cost of all enemies to get to the number that we need to. And if it's larger, we're going to 
        get to minus it by a bit.
 */

/* 
 * EnemyEntity owns the numbers and decides what happened, EnemyController is the only one holding both the 
 * entity and the state machine so it's the one that translates "what happened" into "which state, doing what"
 * and StagerState just plays a clip and holds a timer.
 */
public class EnemyController : MonoBehaviour, IEnemySpawned, ISpawnStatConfig
{
    // IEnemySpawned death contract: raised whenever the EnemyEntity dies (forwards its
    // authoritative OnDied), so SpawnSystem can decrement alive tracking / raise FloorCleared.
    public event Action OnDied;

    private EnemyStateMachine<EnemyState> EStateMachine;

    private NavMeshAgent agent;
    private Animator animator;

    [SerializeField] Transform targetTransform;

    [SerializeField] PatrolRoute patrolRoute;

    [Header("------------chasing the player------------")]
    [SerializeField] private float detectionDistance;
    [SerializeField] private float loseTargetDistance;
    [SerializeField] private float attackRange;

    [SerializeField] private float patrolSpeed;
    [SerializeField] private float chaseSpeed;

    [SerializeField] private float waypointStoppingDistance;
    [SerializeField] private float viewHalfAngle;

    [Header("----------------------------")]
    [SerializeField] Text _debugText;
    [SerializeField] private EnemyEntity enemyEntity;

    [Header("-------------Attack components-------------")]
    [SerializeField] private EnemyAttackConfig enemyAttackConfig;
    [SerializeField] private GameObject explosionParticles;

    [Header("-------------Poise-------------")]
    [SerializeField] float maxPoise = 100f;
    [SerializeField] float currentPoise;
    [SerializeField] float poiseRegenDelay = 1.5f;  // seconds without poise damage before it starts climbing back
    [SerializeField] float poiseRegenRate = 20f;

    private bool hasTarget;

    // agent = the private field that stores the component
    // Agent = the public read-only property that returns the field

    public Dictionary<System.Type, EnemyState> EnemyStates =>
        EStateMachine.EnemyStates;


    public EnemyEntity EnemyEntity => enemyEntity;
    public PatrolRoute PatrolRoute => patrolRoute;
    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float AttackRange => attackRange;
    public float WaypointStoppingDistance => waypointStoppingDistance;
    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;
    public Transform TargetTransform => targetTransform;
    public EnemyAttackConfig EnemyAttackConfig => enemyAttackConfig;
    public GameObject ExplosionParticles => explosionParticles;

    // ISpawnStatConfig: SpawnSystem reads the enemy's serialized/prefab base stats and pushes the
    // floor-scaled absolute values in before Initialize() runs. This is the ONLY place the Enemy
    // side learns about what stats to use; it never sees floors or multipliers.
    public float BaseMaxHealth => enemyEntity != null ? enemyEntity.MaxHealth : 0f;
    public float BaseDamage => enemyEntity != null ? enemyEntity.BaseDamage : 0f;

    public void ConfigureForSpawn(float maxHealth, float baseDamage)
    {
        if (enemyEntity == null)
            enemyEntity = GetComponent<EnemyEntity>();
        enemyEntity.SetMaxHealth(maxHealth);
        enemyEntity.SetBaseDamage(baseDamage);
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        EStateMachine = new EnemyStateMachine<EnemyState>();

        // Null-safe init: never rely on the inspector slot being filled.
        if (enemyEntity == null)
            enemyEntity = GetComponent<EnemyEntity>();

        enemyEntity.Initialize();
        enemyEntity.OnStaggered += HandleStaggered;
        enemyEntity.OnDied += HandleDied;
        enemyEntity.OnDied += () => OnDied?.Invoke();
        enemyEntity.OnDamageTaken += HandleDamageTaken;

        // for each state i need to initialise them all first
        // apply changes to the constractor after finishing with each state
        // -------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        AddState(new SpownState(this));
        AddState(new ChaseState(this));
        AddState(new AttackState(this));
        AddState(new PatrolState(this));
        AddState(new StaggerState(this));
        AddState(new DieState(this));
        AddState(new ExplodeState(this));
        // set the first state the enemy will enter
        SetState<SpownState>();

        agent.stoppingDistance = waypointStoppingDistance;
    }

    private void HandleDamageTaken(float damage)
    {
        if (!EStateMachine.CurrentState.CanBeInterrupted)
            return;
        StaggerState staggerState = GetState<StaggerState>() as StaggerState;
        staggerState?.SetReaction(StaggerState.ReactionType.Hit);
        SetState<StaggerState>();
    }

    private void HandleStaggered()
    {
        if (!EStateMachine.CurrentState.CanBeInterrupted)
            return;
        StaggerState staggerState = GetState<StaggerState>() as StaggerState;
        staggerState?.SetReaction(StaggerState.ReactionType.Stun);
        SetState<StaggerState>();
    }

    // update state here
    void Update()
    {
        EStateMachine.Tick();

        SeeThePlayer();

        if (Keyboard.current.hKey.wasPressedThisFrame)
            enemyEntity.TakeDamage(10, 5); // small poise damage

        if (Keyboard.current.jKey.wasPressedThisFrame)
            enemyEntity.TakeDamage(10, 999); // guaranteed poise break

        // GetState<AttackState>() always hands back a plain EnemyState label (that's fixed in the method's return type).
        // "as AttackState" relabels it as AttackState specifically, so we can reach AttackState-only stuff like CurrentCombatAction.
        AttackState attackState = GetState<AttackState>() as AttackState;
        _debugText.text =
            "Current State: " +
            (
                EStateMachine.CurrentState != null
                    ? EStateMachine.CurrentState.GetType().ToString()
                    : "None"
            ) +
            "\nPrevious State: " +
            (
                EStateMachine.PreviousState != null
                    ? EStateMachine.PreviousState.GetType().ToString()
                    : "None"
            ) + "\nAttack State: " +
            (
                attackState?.CurrentCombatAction != null
                    ? attackState?.CurrentCombatAction.GetType().ToString()
                    : "None"
            );
    }

    void AddState(EnemyState state)
    {
        EStateMachine.AddState(state);
    }

    void SeeThePlayer()
    {
        if (targetTransform == null || agent == null)
            return;

        if (!EStateMachine.CurrentState.CanBeInterrupted)
            return;

        // an active attack manages its own positioning and knows when it's done
        if (EStateMachine.CurrentState is AttackState)
            return; 

        Vector3 direction = targetTransform.position - transform.position;
        float distance = direction.magnitude;
        float angle = Vector3.Angle(direction, transform.forward);

        if (!hasTarget)
        {
            bool playerViewed = angle <= viewHalfAngle && distance <= detectionDistance;
            if (!playerViewed)
                return;
            hasTarget = true;
        }

        if (distance <= attackRange)
        {
            SetState<AttackState>();
            return;
        }

        if (distance >= loseTargetDistance)
        {
            hasTarget = false;
            SetState<PatrolState>();
            return;
        }

        SetState<ChaseState>();
        Vector3 lookAtVector = new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z);
        transform.LookAt(lookAtVector);
        agent.SetDestination(lookAtVector);
    }

    private void HandleDied()
    {
        if (EStateMachine.CurrentState is ExplodeState)
            return; // already handling its own death, leave it alone
        SetState<DieState>();
    }

    // exit state here
    public void SetState<T>() where T : EnemyState
    {
        EStateMachine.SetState<T>();
    }

    public EnemyState GetState<T>() where T : EnemyState
    {
        return EStateMachine.GetState<T>();
    }
}
