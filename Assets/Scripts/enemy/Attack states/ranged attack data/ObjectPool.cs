using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private readonly GameObject prefab;// what are we gonna store (bullets /or rocks)
    private readonly Transform perantHolder;
    private readonly Queue<GameObject> available;

    public ObjectPool( GameObject prefab, Transform perantHolder, int objectCount = 10)
    {
        this.prefab = prefab;
        this.perantHolder = perantHolder;
        this.available = new Queue<GameObject>();

        for (int i = 0; i < objectCount; i++)
        {
            GameObject instance = GameObject.Instantiate( prefab, perantHolder);
            instance.SetActive(false);
            available.Enqueue(instance);
        }
    }

    public GameObject Get(Vector3 parentPosition, Quaternion parentRotation)
    {
        GameObject instance;
        if (available.Count > 0)
        {
            instance = available.Dequeue(); 
        }
        else {
            instance = GameObject.Instantiate(prefab, perantHolder);
        }

        instance.transform.SetPositionAndRotation(parentPosition, parentRotation);
        instance.SetActive(true);
        return instance;
    }

    public void Release(GameObject instance)
    {
        instance.SetActive(false);
        instance.transform.SetParent(perantHolder);
        available.Enqueue(instance);
    }
}
