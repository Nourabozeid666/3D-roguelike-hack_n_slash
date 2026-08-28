using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ProjectilePoolManager : MonoBehaviour
{
    // 1. The Global Hotline: Anyone in the game can call ProjectilePoolManager.Instance
    //"This belongs to the Class itself, not to a specific object. You can access it
    //from anywhere just by typing the Class Name."
    public static ProjectilePoolManager Instance {  get; private set; }

    private readonly Dictionary<GameObject,ObjectPool> poolsType = new Dictionary<GameObject, ObjectPool>();

    [Tooltip("How many copies to create upfront when a new weapon is first used.")]
    [SerializeField] int objectCount = 10;

    private void Awake()
    {
        // Enforce only ONE manager in the scene and it is this specific game object
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
            return;
        }
        Instance = this;
    }

    public ObjectPool GetOrCreatePool(GameObject prefab) {
        //The out keyword is a way for a function to fill a variable for you and hand it back
        /*
         The slow/messy way:
        if (pools.ContainsKey(prefab))      // 1st search in the dictionary
        {
            ObjectPool pool = pools[prefab]; // 2nd search in the dictionary (wasteful)
        }
        */
        if (!poolsType.TryGetValue(prefab,out ObjectPool poolType))
        {
            //In C#, the $ symbol before quotes lets you insert variables directly into text using
            //{curly_brackets} (called String Interpolation).
            GameObject container = new GameObject($"Pool_{prefab.name}");
            //gonna be a child of the game manager 
            container.transform.SetParent(this.transform);
            poolType = new ObjectPool( prefab, container.transform, objectCount);
            poolsType.Add(prefab, poolType);
        }
        return poolType;
    }

    public GameObject Get(GameObject prefab, Vector3 parentPosition, Quaternion parentRotation)
    {
        ObjectPool poolType = GetOrCreatePool(prefab);
        GameObject instance = poolType.Get(parentPosition, parentRotation);

        //give the rock its parent so we can return it later
        Projectile projectile = instance.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile.SourcePrefab = prefab;
        }
        return instance;
    }

    public void Release(GameObject prefab, GameObject instance) {
        if(poolsType.TryGetValue(prefab, out ObjectPool poolType))
            poolType.Release(prefab);
        else
            Destroy(prefab);
    }
}