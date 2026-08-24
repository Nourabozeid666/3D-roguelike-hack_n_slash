using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;
using static StaggerSeverity;

public class DebugService : MonoBehaviour
{
    public static DebugService Instance { get; private set; }
    
    [SerializeField] private GameObject debugSpherePrefab;
    [SerializeField] private int poolSize = 5;
    [SerializeField] private bool visualizationEnabled = true;
    
    private ObjectPool<GameObject> _spherePool;
    private List<GameObject> _activeSpheres = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePool();
        InputController.OnDebugInput1 += DebugEvent1SimulateTakeDamage;
    }

    private void InitializePool()
    {
        if (debugSpherePrefab == null)
        {
            debugSpherePrefab = Resources.Load<GameObject>("DebugSphere");
        }

        _spherePool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject sphere = Instantiate(debugSpherePrefab);
                sphere.transform.SetParent(transform);
                return sphere;
            },
            actionOnGet: (sphere) =>
            {
                sphere.SetActive(true);
            },
            actionOnRelease: (sphere) =>
            {
                sphere.SetActive(false);
            },
            actionOnDestroy: (sphere) =>
            {
                Destroy(sphere);
            },
            collectionCheck: false,
            defaultCapacity: poolSize,
            maxSize: poolSize * 2
        );

        // Pre-populate pool
        var temp = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            temp.Add(_spherePool.Get());
        }
        foreach (var sphere in temp)
        {
            _spherePool.Release(sphere);
        }
    }

    public void ShowDebugSphere(Vector3 position, float radius, float duration = 0.2f)
    {
        if (!visualizationEnabled) return;

        GameObject sphere = _spherePool.Get();
        sphere.transform.position = position;
        
        // Resize sphere to match radius (scale = radius * 2 for diameter)
        sphere.transform.localScale = Vector3.one * (radius * 2);
        
        _activeSpheres.Add(sphere);
        
        StartCoroutine(HideSphereAfterDuration(sphere, duration));
    }

    private IEnumerator HideSphereAfterDuration(GameObject sphere, float duration)
    {
        yield return new WaitForSeconds(duration);
        _activeSpheres.Remove(sphere);
        _spherePool.Release(sphere);
    }

    public void SetVisualizationEnabled(bool enabled)
    {
        visualizationEnabled = enabled;
    }

    void DebugEvent1SimulateTakeDamage()
    {
        Debug.Log("Debug Input 1 Triggered: Simulating TakeDamage on PlayerEntity");
        var playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            AttackEffectData effectData = new AttackEffectData
            {
                appliedStagger = StaggerTier.Normal,
                selfPoise = PoiseTier.Normal,
                multiplier = 1f,
                hitstopDuration = 0.1f,
                knockbackForce = new Vector3(0, 0, 5f)
            };
            playerController.Entity.TakeDamage(10f, effectData);
        }
    }
}
