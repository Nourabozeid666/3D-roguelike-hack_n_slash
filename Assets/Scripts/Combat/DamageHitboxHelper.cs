using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageHitboxHelper : MonoBehaviour
{
    public event Action<GameObject, IEntity> OnHitboxTriggered;
    public event Action OnEnableHitBox;
    public event Action OnDisableHitBox;
    [SerializeField] private string[] tagsToHandle;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Collider hitboxCollider;
    private readonly HashSet<int> hitTargetIDs = new HashSet<int>();

    private bool isActive = true;
    public bool IsActive { get { return isActive; } }
    void Start()
    {
        if (hitboxCollider == null)
        {
            isActive = false;
            Debug.LogWarning("Hitbox Collider is not assigned. Disabling DamageHitboxHelper. " + gameObject.name);
        }
        else
        {
            hitboxCollider.enabled = false;

        }
    }

    void OnEnable()
    {
        if (!isActive) return;
        OnEnableHitBox += EnableHitbox;
        OnDisableHitBox += DisableHitbox;
    }

    void OnDisable()
    {
        if (!isActive) return;
        OnEnableHitBox -= EnableHitbox;
        OnDisableHitBox -= DisableHitbox;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // 1. Instant bitwise layer check
        if (targetLayers.value != 0 && ((1 << other.gameObject.layer) & targetLayers.value) == 0)
            return;

        // 2. Optional tag check (if tags are specified)
        if (tagsToHandle != null && tagsToHandle.Length > 0 && !HasMatchingTag(other))
            return;

        // 3. Resolve target root to prevent hitting multiple child colliders on the same entity
        Transform targetRoot = other.transform.root != null ? other.transform.root : other.transform;
        int targetId = targetRoot.GetInstanceID();

        // 4. O(1) deduplication check
        if (!hitTargetIDs.Add(targetId))
            return;

        // 5. Direct generic entity resolution (zero reflection)
        if (other.TryGetComponent<IEntityProvider>(out var provider) ||
            targetRoot.TryGetComponent(out provider))
        {
            OnHitboxTriggered?.Invoke(targetRoot.gameObject, provider.Entity);
            return;
        }

        if (other.TryGetComponent<IEntity>(out var entity) ||
            targetRoot.TryGetComponent(out entity))
        {
            OnHitboxTriggered?.Invoke(targetRoot.gameObject, entity);
            return;
        }
    }

    private bool HasMatchingTag(Collider col)
    {
        for (int i = 0; i < tagsToHandle.Length; i++)
        {
            if (col.CompareTag(tagsToHandle[i])) return true;
        }
        return false;
    }

    void EnableHitbox()
    {
        if (!isActive) return;
        hitTargetIDs.Clear();
        hitboxCollider.enabled = true;
    }

    void DisableHitbox()
    {
        if (!isActive) return;
        hitboxCollider.enabled = false;
        hitTargetIDs.Clear();
    }
}
