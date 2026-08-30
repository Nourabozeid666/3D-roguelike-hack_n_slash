// Suggested path: Assets/Scripts/enemy/Attack states/Projectile.cs
// Requires: a Collider on this prefab set to "Is Trigger", plus a component
// implementing IProjectileMotion (LinearMotion, ArcMotion, ...) on the same object.
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f; // safety net in case HasFinished() never fires (e.g. bullets)

    // Set by ProjectilePoolManager.Get() right after this instance is pulled from its pool,
    // so the projectile knows which pool to return itself to.
    public GameObject SourcePrefab { get; set; }

    private IProjectileMotion motion;
    private float damage;
    private float elapsedTime;

    private void Awake()
    {
        motion = GetComponent<IProjectileMotion>();
    }

    public void Launch(Vector3 startPosition, Vector3 targetPosition, float speed, float projectileDamage)
    {
        damage = projectileDamage;
        elapsedTime = 0f;
        transform.position = startPosition;
        motion.Initialize(startPosition, targetPosition, speed);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        transform.position = motion.Evaluate(elapsedTime);

        if (motion.HasFinished(elapsedTime) || elapsedTime >= lifeTime)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        if (player.Entity == null || player.Entity.Health <= 0f) return;

        player.Entity.TakeDamage(damage);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (ProjectilePoolManager.Instance != null && SourcePrefab != null)
            ProjectilePoolManager.Instance.Release(gameObject, SourcePrefab);
        else
            Destroy(gameObject); // fallback if no pool manager exists in the scene yet
    }
}
