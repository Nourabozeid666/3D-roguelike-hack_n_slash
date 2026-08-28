using UnityEngine;

public class Projectile : MonoBehaviour
{
    IProjectileMotion motion;
    float elapsedTime;
    float damage;
    public GameObject SourcePrefab { get; set; } //rock or the bullet
    [SerializeField] private float lifeTime = 5f;


    void Awake()
    {
        motion = GetComponent<IProjectileMotion>();
    }

    public void Launch(Vector3 startPos, Vector3 target, float projectileSpeed, float projectileDamage)
    {
        elapsedTime = 0f;
        damage = projectileDamage;
        transform.position = startPos;

        if (motion != null) 
            motion.Intialization(startPos, target, projectileSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        elapsedTime += Time.deltaTime;
        transform.position = motion.Evaluate(elapsedTime);

        if (motion.HasFinished(elapsedTime) || elapsedTime >= lifeTime)
            ReturnToPool();
        
    }
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            return;

        if (player.Entity == null || player.Entity.Health <= 0f) 
            return;

        player.Entity.TakeDamage(damage);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if(ProjectilePoolManager.Instance != null && SourcePrefab != null)
        {
            ProjectilePoolManager.Instance.Release(gameObject,SourcePrefab);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
