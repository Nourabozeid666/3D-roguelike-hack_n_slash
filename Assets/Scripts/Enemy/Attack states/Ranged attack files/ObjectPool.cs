// Suggested path: Assets/Scripts/Core/Pooling/ObjectPool.cs
// Plain C# class (not a MonoBehaviour) - recycles instances of a single prefab.
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly Queue<GameObject> available = new Queue<GameObject>();

    public ObjectPool(GameObject prefab, Transform parent, int prewarmCount = 0)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < prewarmCount; i++)
        {
            GameObject instance = CreateNew();
            Release(instance);
        }
    }

    private GameObject CreateNew()
    {
        GameObject instance = Object.Instantiate(prefab, parent);
        instance.SetActive(false);
        return instance;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject instance = available.Count > 0 ? available.Dequeue() : CreateNew();
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    public void Release(GameObject instance)
    {
        instance.SetActive(false);
        instance.transform.SetParent(parent);
        available.Enqueue(instance);
    }
}
