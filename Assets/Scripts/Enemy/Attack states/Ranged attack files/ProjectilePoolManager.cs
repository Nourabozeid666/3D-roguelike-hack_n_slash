// Suggested path: Assets/Scripts/Core/Pooling/ProjectilePoolManager.cs
// Drop this on an empty GameObject in your scene (e.g. "Managers").
// One pool per distinct prefab is created automatically the first time it's requested,
// so bullets, rocks, or any future projectile type all just work without extra setup.
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    [SerializeField] private int prewarmCountPerPrefab = 10;

    private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // If this enemy system spans multiple scenes, uncomment:
        // DontDestroyOnLoad(gameObject);
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        ObjectPool pool = GetOrCreatePool(prefab);
        GameObject instance = pool.Get(position, rotation);

        Projectile projectile = instance.GetComponent<Projectile>();
        if (projectile != null)
            projectile.SourcePrefab = prefab;

        return instance;
    }

    public void Release(GameObject instance, GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out ObjectPool pool))
        {
            // No pool registered for this prefab - shouldn't normally happen, fall back safely.
            Destroy(instance);
            return;
        }
        pool.Release(instance);
    }

    private ObjectPool GetOrCreatePool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out ObjectPool pool))
        {
            Transform container = new GameObject($"Pool_{prefab.name}").transform;
            container.SetParent(transform);
            pool = new ObjectPool(prefab, container, prewarmCountPerPrefab);
            pools[prefab] = pool;
        }
        return pool;
    }
}
