using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageHitboxHelper : MonoBehaviour
{
    public event Action<GameObject, IEntity> OnHitboxTriggered;
    [SerializeField] private string[] tagsToHandle;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Collider hitboxCollider;
    private readonly HashSet<int> hitTargetIDs = new HashSet<int>();
    private float lastHitTime = 0f;
    [SerializeField] private float clearAfterSeconds = 1f;

    private bool isActive = true;
    public bool IsActive { get { return isActive; } }

    void Awake()
    {
        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<Collider>();
        }

        if (hitboxCollider == null)
        {
            isActive = false;
            Debug.LogWarning("Hitbox Collider is not assigned. Disabling DamageHitboxHelper on " + gameObject.name, this);
        }
    }

    void Start()
    {
        if (!isActive) return;
        DisableHitbox();
    }

    void OnEnable()
    {
        if (!isActive) return;
        EnableHitbox();
    }

    void OnDisable()
    {
        if (!isActive) return;
        DisableHitbox();
    }

    public void ResetHitTargets()
    {
        hitTargetIDs.Clear();
    }

    public void EnableHitbox()
    {
        if (!isActive) return;
        hitTargetIDs.Clear();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }
    }

    public void DisableHitbox()
    {
        if (!isActive) return;
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
        hitTargetIDs.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        Debug.Log($"Hitbox triggered by {other.gameObject.name} on layer {LayerMask.LayerToName(other.gameObject.layer)}", this);

        // 1. Instant bitwise layer check
        if (targetLayers.value != 0 && ((1 << other.gameObject.layer) & targetLayers.value) == 0)
        {
            Debug.Log($"Hitbox triggered by {other.gameObject.name} but its layer is not in the target layers. Ignoring.", this);
            return;
        }

        // 2. Optional tag check (if tags are specified)
        if (tagsToHandle != null && tagsToHandle.Length > 0 && !HasMatchingTag(other))
        {
            Debug.Log($"Hitbox triggered by {other.gameObject.name} but its tag is not in the target tags. Ignoring.", this);
            return;
        }

        // 3. Resolve target entity and root object
        Transform targetRoot = other.transform.root != null ? other.transform.root : other.transform;
        IEntity entity = null;
        GameObject targetGameObject = null;

        if (other.TryGetComponent<IEntityProvider>(out var provider) ||
            (provider = other.GetComponentInParent<IEntityProvider>()) != null ||
            (targetRoot != null && targetRoot.TryGetComponent(out provider)))
        {
            entity = provider.Entity;
            targetGameObject = (provider as Component)?.gameObject ?? targetRoot.gameObject;
        }
        else if (other.TryGetComponent<IEntity>(out var directEntity) ||
                 (directEntity = other.GetComponentInParent<IEntity>()) != null ||
                 (targetRoot != null && targetRoot.TryGetComponent(out directEntity)))
        {
            entity = directEntity;
            targetGameObject = (directEntity as Component)?.gameObject ?? targetRoot.gameObject;
        }

        if (entity == null || targetGameObject == null)
        {
            return;
        }

        // 4. O(1) deduplication check per entity
        int targetId = targetGameObject.GetInstanceID();
        if (!hitTargetIDs.Add(targetId))
        {
            Debug.Log($"Hitbox triggered by {other.gameObject.name} but this target has already been hit. Ignoring.", this);
            return;
        }
        lastHitTime = Time.time;

        // 5. Invoke hit event
        OnHitboxTriggered?.Invoke(targetGameObject, entity);
    }

    private bool HasMatchingTag(Collider col)
    {
        for (int i = 0; i < tagsToHandle.Length; i++)
        {
            string expectedTag = tagsToHandle[i];
            if (col.CompareTag(expectedTag)) return true;
            if (expectedTag == "Enemy" && col.CompareTag("Ranged Enemy")) return true;
        }
        return false;
    }

    void Update()
    {
        if (hitTargetIDs.Count > 0)
        {
            // Clear the hit target IDs after a certain time
            if (Time.time - lastHitTime > clearAfterSeconds)
            {
                hitTargetIDs.Clear();
            }
        }
    }
}
