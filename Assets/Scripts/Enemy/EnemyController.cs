using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/*
    First:
    Prove the architecture with Debug.Log. !
    
    Then:
    Add real Patrol behavior. !
    
    Then:
    Supply waypoints. !
    
    Then:
    Add detection and Chase.
    
    Then:
    Add animations.
 */

// any thing that any state might need
public class EnemyController : MonoBehaviour
{
    private EnemyStateMachine stateMachine;

    private NavMeshAgent agent;
    private Animator animator;

    [SerializeField] Transform targetTransform;

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

    public float PatrolRange => patrolRange;
    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float AttackRange => attackRange;
    public float WaypointStoppingDistance => waypointStoppingDistance;
    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;
    public Transform TargetTransform => targetTransform;

    // start state here
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        stateMachine = new EnemyStateMachine();

        // for each state i need to initialise them all first
        // apply changes to the constractor after finishing with each state
        // -------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        AddState(new IdleState(this));
        AddState(new ChaseState(this));
        AddState(new AttackState(this));

        // set the first state the enemy will enter
        SetState<IdleState>();

        agent.stoppingDistance = PatrolRange;
    }

    // update state here
    void Update()
    {
        stateMachine.Tick();

        SeeThePlayer();

        _debugText.text =
            "Current State: " +
            stateMachine.CurrentState.GetType().ToString()
            + "\nPrevious State: " +
            (
                stateMachine.PreviousState != null
                    ? stateMachine.PreviousState.GetType().ToString()
                    : "None"
            );
    }

    //void Rotate(Vector2 direction)
    //{
    //    Vector3 groundVelocity = new Vector3(context.rb.velocity.x, 0, context.rb.velocity.z);
    //    Quaternion targetRotation = groundVelocity != Vector3.zero ? Quaternion.LookRotation(groundVelocity) : context.playerModel.rotation;
    //    if (targetRotation != null && targetRotation != context.playerModel.rotation && direction != Vector2.zero)
    //    {
    //        context.playerModel.rotation = Quaternion.Slerp(context.playerModel.rotation, targetRotation, 0.1f);
    //    }
    //}

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

        if (distance < attackRange)
        {
            SetState<AttackState>();
            return;
        }

        if (distance >= loseTargetDistance)
        {
            hasTarget = false;

            SetState<IdleState>();
            return;
        }

        SetState<ChaseState>();

        transform.LookAt(targetTransform.position);

        agent.SetDestination(targetTransform.position);
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