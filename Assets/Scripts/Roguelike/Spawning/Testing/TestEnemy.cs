using System;
using UnityEngine;

/// <summary>
/// TEMPORARY TEST DOUBLE for the Roguelike SpawnSystem. NOT a real enemy.
/// A red capsule whose only job is to validate spawning, tracking, death/removal and AliveCount.
/// It deliberately has NO enemy AI, states, combat, navigation, damage systems, or EnemyEntity.
/// Lives under Spawning/Testing/ (outside Assets/Scripts/Enemy/) to stay isolated from the real
/// Enemy System. When the real enemy lands, the archetype prefab is swapped and this is deleted.
/// </summary>
public class TestEnemy : MonoBehaviour, IEnemySpawned
{
    [SerializeField] float baseHealth = 10f;
    [SerializeField] float baseDamage = 1f;

    public event Action Died;

    public float Health { get; private set; }
    public float Damage { get; private set; }

    bool dead;

    void Awake()
    {
        EnsureVisual();
        Health = baseHealth;
        Damage = baseDamage;
    }

    public void ApplyFloorScaling(float healthScale, float damageScale)
    {
        Health = baseHealth * healthScale;
        Damage = baseDamage * damageScale;
    }

    /// <summary>Simulate death/removal: notify the SpawnSystem, then destroy this object.</summary>
    public void Die()
    {
        if (dead) return;
        dead = true;
        Died?.Invoke();
        Destroy(gameObject);
    }

    void EnsureVisual()
    {
        if (transform.childCount > 0) return;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(transform, false);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { color = Color.red };
        body.GetComponent<MeshRenderer>().sharedMaterial = mat;

        CapsuleCollider bodyCollider = body.GetComponent<CapsuleCollider>();
        if (bodyCollider == null) return;

        // A runtime capsule's pivot is its center. Lift it so the capsule BOTTOM sits on this
        // object's origin: the origin then means "feet on the ground", and every scene places
        // its SpawnPoints at that scene's floor height. Offset is data-driven from the collider.
        float footOffset = bodyCollider.height * 0.5f + bodyCollider.center.y;
        bodyCollider.isTrigger = true;
        body.transform.localPosition = new Vector3(0f, footOffset, 0f);

        EnsureTouchCollider(bodyCollider, footOffset);
    }

    void EnsureTouchCollider(CapsuleCollider bodyCollider, float footOffset)
    {
        if (GetComponent<Collider>() != null) return;
        CapsuleCollider trigger = gameObject.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.radius = bodyCollider.radius;
        trigger.height = bodyCollider.height;
        trigger.center = new Vector3(0f, footOffset, 0f);
    }

    /// <summary>
    /// Touch-to-kill for tests only: die when the Player touches this enemy.
    /// OnTriggerEnter (not OnCollisionEnter) is used because it is a pure detection event with
    /// no physics response, so touching a test enemy never pushes or bounces the Player, and it
    /// only needs the Player's existing Rigidbody + collider. The Player capsule collider sits on
    /// a child (PlayerObj) whose own tag is Untagged, so the check walks up the hierarchy to find
    /// the root GameObject tagged "Player".
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other.transform)) Die();
    }

    static bool IsPlayer(Transform current)
    {
        while (current != null)
        {
            if (current.CompareTag("Player")) return true;
            current = current.parent;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
