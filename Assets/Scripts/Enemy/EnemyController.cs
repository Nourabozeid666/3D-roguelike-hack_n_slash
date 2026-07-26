using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyController : MonoBehaviour//, IDamageable
{
    [Header("Enemy Stats")]
    public float health = 250f;
    public float damage = 5f;

    [Header("Enemy Behaviour")]

    BehaviourTree tree;
    [SerializeField] GameObject player;
    NavMeshAgent agent;
    [SerializeField] Animator animator;
    [SerializeField] GameObject enemyModel;
    [SerializeField] Rigidbody rb;

    [Header("Attack Settings")]
    float attackCooldownTimer = 0f;
    string IdleAnimation = "CombatIdle";
    string AttackAnimation = "Attack1Light";
    string HitAnimation = "GetHit";
    int hashIdleAnimation;
    int hashAttackAnimation;
    int hashHitAnimation;
    [SerializeField] float attackCooldown = 1.5f;

    [Header("Stun Settings")]
    [SerializeField] float stunDuration = 0.75f;
    bool wasDamaged = false;
    float stunTimer = 0f;

    public enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        Stunned
    }
    public EnemyState currentState = EnemyState.Idle;

    Node.Status treeStatus = Node.Status.running;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        hashIdleAnimation = Animator.StringToHash(IdleAnimation);
        hashAttackAnimation = Animator.StringToHash(AttackAnimation);
        hashHitAnimation = Animator.StringToHash(HitAnimation);
        animator.Play(hashIdleAnimation);
    }

    void Start()
    {
        tree = new BehaviourTree();
        Selector Root = new Selector("Root");
        Repeater Loop = new Repeater("Chase-Attack Loop");
        Leaf GetHit = new Leaf("GetHit?", CheckIfDamaged);
        Sequence Fight = new Sequence("Fight");
        Leaf Chase = new Leaf("Chase", ChasePlayer);
        Leaf Attack = new Leaf("Attack", AttackPlayer);

        Fight.AddChild(Chase);
        Fight.AddChild(Attack);
        Loop.AddChild(Fight);
        Root.AddChild(GetHit);
        Root.AddChild(Loop);
        tree.AddChild(Root);

        tree.PrintTree();
    }

    public Node.Status ChasePlayer()
    {
        return GoToLocation(player.transform.position);
    }

    public Node.Status AttackPlayer()
    {
        if (currentState == EnemyState.Stunned)
            return Node.Status.running;

        if (wasDamaged)
            return Node.Status.failure;

        if (currentState != EnemyState.Attacking)
        {
            agent.isStopped = true;
            rb.isKinematic = false;
            currentState = EnemyState.Attacking;
            attackCooldownTimer = attackCooldown;
            animator.Play(hashAttackAnimation, 0, 0f);
            //Deal damage to player
            // CombatSystem combatSystem = player.GetComponent<CombatSystem>();
            // if (combatSystem != null)
            // {
            //     player.GetComponent<CombatSystem>()?.TakeDamage(damage);
            //     if (combatSystem.IsBlocking())
            //     {
            //         rb.AddForce(-transform.forward * 100f, ForceMode.Impulse);
            //     }
            // }
            return Node.Status.running;
        }
        if (player == null)
        {
            // Task.Delay(1000).ContinueWith(_ => ReloadScene());
            return Node.Status.success;
        }

        // Look at player while attacking
        Vector3 directionToPlayer = player.transform.position - transform.position;
        directionToPlayer.y = 0;
        enemyModel.transform.rotation = Quaternion.LookRotation(directionToPlayer);

        attackCooldownTimer -= Time.deltaTime;

        // If player moved beyond attack range before cooldown ended, return success to chase
        if (Vector3.Distance(transform.position, player.transform.position) > 2f && attackCooldownTimer <= 0f)
        {
            currentState = EnemyState.Idle;
            animator.CrossFadeInFixedTime(hashIdleAnimation, 0.1f);
            return Node.Status.success;
        }

        return Node.Status.running;
    }

    Node.Status GoToLocation(Vector3 destination)
    {
        if (currentState == EnemyState.Stunned)
            return Node.Status.running;

        if (wasDamaged)
            return Node.Status.failure;

        float distanceToTarget = Vector3.Distance(transform.position, destination);
        if (currentState == EnemyState.Idle)
        {
            agent.isStopped = false;
            rb.isKinematic = true; // Disable Rigidbody to prevent physics interference with NavMeshAgent
            agent.SetDestination(destination);
            currentState = EnemyState.Chasing;
            return Node.Status.running;
        }
        else if (distanceToTarget < 2)
        {
            rb.isKinematic = false;
            currentState = EnemyState.Idle;
            return Node.Status.success;
        }
        else if (Vector3.Distance(agent.destination, destination) >= 0.5f)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
            currentState = EnemyState.Chasing;
            return Node.Status.running;
        }
        return Node.Status.running;
    }

    Node.Status CheckIfDamaged()
    {
        if (!wasDamaged)
            return Node.Status.failure;

        if (stunTimer <= 0f)
        {
            currentState = EnemyState.Stunned;
            attackCooldownTimer = 0f;
            stunTimer = stunDuration;
            agent.isStopped = true;
            rb.isKinematic = false;
            animator.CrossFadeInFixedTime(hashIdleAnimation, 0.1f);
        }

        stunTimer -= Time.deltaTime;

        if (stunTimer > 0f)
            return Node.Status.running;

        wasDamaged = false;
        stunTimer = 0f;
        agent.isStopped = false;
        currentState = EnemyState.Idle;
        return Node.Status.failure;
    }

    void Update()
    {
        treeStatus = tree.Process();
    }

    public void TakeDamage(float damage)
    {
        print("Enemy took " + damage + " damage");
        health -= damage;
        wasDamaged = true;
        animator.Play(hashHitAnimation, 0, 0f);
        if (health <= 0)
        {
            Die();
        }
    }

    public float GetCurrentHealth()
    {
        return health;
    }

    void Die()
    {
        //Play death animation, drop loot, etc.
        // animator.CrossFadeInFixedTime(hashDeathAnimation, 0.1f);
        Destroy(gameObject);
        Task.Delay(1000).ContinueWith(_ => ReloadScene());
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
