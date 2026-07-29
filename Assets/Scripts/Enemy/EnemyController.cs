using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/*
    basic states for a grent enemy:
        spownState == idle
        chaseState
        attackState  
        RetreatState
        { 
            SacrificeAttack
            rangedShootingAttack
            simpleAttack
        }
        stagerState
        dieState

    basic attackState for Melee archetypes
    {
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

public class EnemyController : MonoBehaviour
{
    private EnemyStateMachine stateMachine;

    private NavMeshAgent agent;
    private Animator animator;

    [SerializeField] Transform targetTransform;
    [SerializeField] PatrolRoute patrolRoute;

    [Header("-----chasing the player-----")]
    [SerializeField] private float detectionDistance;
    [SerializeField] private float loseTargetDistance;
    [SerializeField] private float attackRange;
    [SerializeField] private float patrolRange;

    [SerializeField] private float patrolSpeed;
    [SerializeField] private float chaseSpeed;

    [SerializeField] private float waypointStoppingDistance;
    [SerializeField] private float viewHalfAngle;

    [Header("----------------------------")]
    [SerializeField] Text _debugText;

    private bool hasTarget;

    // agent = the private field that stores the component
    // Agent = the public read-only property that returns the field

    public Dictionary<System.Type, EnemyState> EnemyStates =>
        stateMachine.EnemyStates;

    public EnemyState PreviousState =>
        stateMachine.PreviousState;

    public PatrolRoute PatrolRoute => patrolRoute;
    public float PatrolRange => patrolRange;
    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float AttackRange => attackRange;
    public float WaypointStoppingDistance => waypointStoppingDistance;
    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;
    public Transform TargetTransform => targetTransform;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        stateMachine = new EnemyStateMachine();

        // for each state i need to initialise them all first
        // apply changes to the constractor after finishing with each state
        // -------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        AddState(new SpownState(this));
        AddState(new ChaseState(this));
        AddState(new AttackState(this));
        AddState(new PatrolState(this));

        // set the first state the enemy will enter
        SetState<SpownState>();

        agent.stoppingDistance = PatrolRange;
    }

    // update state here
    void Update()
    {
        stateMachine.Tick();

        SeeThePlayer();

        _debugText.text =
            "Current State: " +
            (
                stateMachine.CurrentState != null
                    ? stateMachine.CurrentState.GetType().ToString()
                    : "None"
            ) +
            "\nPrevious State: " +
            (
                stateMachine.PreviousState != null
                    ? stateMachine.PreviousState.GetType().ToString()
                    : "None"
            );
    }

    void AddState(EnemyState state)
    {
        stateMachine.AddState(state);
    }

    void SeeThePlayer()
    {
        if (targetTransform == null || agent == null)
            return;

        Vector3 direction =
            targetTransform.position - transform.position;

        float distance = direction.magnitude;

        float angle =
            Vector3.Angle(direction, transform.forward);

        if (!hasTarget)
        {
            bool playerViewed =
                angle <= viewHalfAngle &&
                distance <= detectionDistance;

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

    // exit state here
    public void SetState<T>() where T : EnemyState
    {
        stateMachine.SetState<T>();
    }

    public EnemyState GetState<T>() where T : EnemyState
    {
        return stateMachine.GetState<T>();
    }
}