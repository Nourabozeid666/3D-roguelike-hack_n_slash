using System;
using UnityEngine;

public class DamageHitboxHelper : MonoBehaviour
{
    public event Action<GameObject, IEntity> OnHitboxTriggered;
    public event Action OnEnableHitBox;
    public event Action OnDisableHitBox;
    [SerializeField] private string[] tagsToHandle;
    [SerializeField] private Type[] componentsToHandle = { typeof(IEntity), typeof(IEntityProvider), typeof(IEnemyEntity) };
    [SerializeField] private Collider hitboxCollider;
    private bool isActive = true;
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

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        foreach (var tag in tagsToHandle)
        {
            if (other.CompareTag(tag))
            {
                var entity = other.TryGetComponent<IEntity>(out var componentInstance);
                if (componentInstance != null)
                {
                    OnHitboxTriggered?.Invoke(other.gameObject, componentInstance);
                    return;
                }
            }
        }

        foreach (var componentType in componentsToHandle)
        {
            var component = other.TryGetComponent(componentType, out var componentInstance);
            if (componentInstance != null && componentInstance is IEntity entity)
            {
                OnHitboxTriggered?.Invoke(other.gameObject, entity);
                return;
            }
        }
    }

    void EnableHitbox()
    {
        if (!isActive) return;
        hitboxCollider.enabled = true;
    }

    void DisableHitbox()
    {
        if (!isActive) return;
        hitboxCollider.enabled = false;
    }
}
