using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TargetingSystem
{
    [NonSerialized] public CombatController _owner;

    [Header("Targeting Parameters")]
    [SerializeField] private float targetingRadius = 8f;
    [SerializeField] private float directionalConeAngle = 120f; // Half-angle when directional input is held (160 deg total)
    [SerializeField] private float neutralConeAngle = 80f;     // Half-angle when neutral (90 deg total)
    [SerializeField] private LayerMask enemyLayers = ~0;
    [SerializeField] private string[] enemyTag = {"Enemy", "Ranged Enemy"};
    [SerializeField] private bool filterByTag = true;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Scoring Weights")]
    [SerializeField, Range(0f, 1f)] private float angleWeight = 0.6f;
    [SerializeField, Range(0f, 1f)] private float distanceWeight = 0.4f;

    [Header("Lunge Adaptation")]
    [SerializeField] private bool adaptLungeDistance = true;
    [SerializeField] private float minMeleeDistance = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    // Running state
    [NonSerialized] private Transform currentTarget;
    [NonSerialized] private Vector3 lastIntendedDirection = Vector3.forward;
    [NonSerialized] private float lastTargetScore = 0f;

    public Transform CurrentTarget => currentTarget;
    public float TargetingRadius => targetingRadius;
    public float DirectionalConeAngle => directionalConeAngle;
    public float NeutralConeAngle => neutralConeAngle;

    public TargetingSystem(CombatController owner)
    {
        _owner = owner;
    }

    public Vector3 GetIntendedDirection(out bool hasDirectionalInput)
    {
        Vector3 moveInput = Vector3.zero;
        if (_owner != null && _owner._playerController != null)
        {
            moveInput = _owner._playerController.MoveDirectionToWorldSpace();
            moveInput.y = 0f;
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            hasDirectionalInput = true;
            return moveInput.normalized;
        }

        hasDirectionalInput = false;
        Transform model = (_owner != null && _owner.ReferencesContext != null && _owner.ReferencesContext.playerModel != null)
            ? _owner.ReferencesContext.playerModel
            : (_owner != null ? _owner.transform : null);

        if (model != null)
        {
            Vector3 fwd = model.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.01f)
            {
                return fwd.normalized;
            }
        }

        return Vector3.forward;
    }

    public Vector3 AcquireTarget(out Transform target)
    {
        Vector3 intendedDir = GetIntendedDirection(out bool hasDirectionalInput);
        lastIntendedDirection = intendedDir;

        target = FindBestTarget(intendedDir, hasDirectionalInput);
        currentTarget = target;

        if (_owner != null && _owner.CombatContext != null)
        {
            _owner.CombatContext.currentTargetPos = target;
        }

        if (target != null && _owner != null)
        {
            Vector3 toTarget = target.position - _owner.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                return toTarget.normalized;
            }
        }

        return intendedDir;
    }

    public Transform FindBestTarget(Vector3 intendedDirection, bool hasDirectionalInput)
    {
        if (_owner == null) return null;

        Vector3 playerPos = _owner.transform.position;
        float maxConeAngle = hasDirectionalInput ? directionalConeAngle : neutralConeAngle;

        Collider[] colliders = Physics.OverlapSphere(playerPos, targetingRadius, enemyLayers);
        if (colliders == null || colliders.Length == 0)
        {
            lastTargetScore = 0f;
            return null;
        }

        Transform bestCandidate = null;
        float bestScore = float.NegativeInfinity;
        HashSet<int> evaluatedEntityIds = new HashSet<int>();

        LayerMask walls = obstacleLayers.value != 0 
            ? obstacleLayers 
            : (_owner.ReferencesContext != null ? _owner.ReferencesContext.wallLayer : (LayerMask)0);
        Vector3 eyePos = playerPos + Vector3.up * 1f;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col.transform.IsChildOf(_owner.transform)) continue;

            // Optional tag filter
            if (filterByTag && enemyTag != null && enemyTag.Length > 0 && !string.IsNullOrEmpty(enemyTag[0]))
            {
                bool tagMatch = false;
                foreach (string tag in enemyTag)
                {
                    if (col.CompareTag(tag) && (col.transform.root != null || col.transform.root.CompareTag(tag)))
                    {
                        tagMatch = true;
                        break;
                    }
                }
                if (!tagMatch) continue;
            }

            // Entity resolution & Alive check
            Transform enemyRoot = col.transform.root != null ? col.transform.root : col.transform;
            EnemyController enemyController = col.GetComponentInParent<EnemyController>();
            if (enemyController == null && enemyRoot != null)
            {
                enemyController = enemyRoot.GetComponent<EnemyController>();
            }

            if (enemyController != null)
            {
                if (enemyController.EnemyEntity != null && enemyController.EnemyEntity.IsDead)
                {
                    continue;
                }
            }
            else
            {
                IEntityProvider provider = col.GetComponentInParent<IEntityProvider>();
                if (provider == null && enemyRoot != null) provider = enemyRoot.GetComponent<IEntityProvider>();
                if (provider != null && provider.Entity is EnemyEntity enemyEnt && enemyEnt.IsDead)
                {
                    continue;
                }
            }

            Transform targetTransform = enemyController != null ? enemyController.transform : enemyRoot;
            int targetId = targetTransform.gameObject.GetInstanceID();
            if (!evaluatedEntityIds.Add(targetId))
            {
                continue; // Already scored this enemy from another collider
            }

            // Direction and distance checks
            Vector3 toEnemy = targetTransform.position - playerPos;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;

            if (dist < 0.01f || dist > targetingRadius) continue;

            Vector3 toEnemyDir = toEnemy / dist;
            float angle = Vector3.Angle(intendedDirection, toEnemyDir);
            if (angle > maxConeAngle) continue;

            // Line of sight check
            if (requireLineOfSight && walls.value != 0)
            {
                Vector3 targetChest = targetTransform.position + Vector3.up * 1f;
                Vector3 rayDir = targetChest - eyePos;
                float rayDist = rayDir.magnitude;
                if (Physics.Raycast(eyePos, rayDir.normalized, rayDist, walls))
                {
                    continue; // Obstacle in the way
                }
            }

            // Scoring: alignment [0, 1] and distance proximity [0, 1]
            float angleAlignment = Mathf.Clamp01(Vector3.Dot(intendedDirection, toEnemyDir));
            float distanceFactor = 1f - Mathf.Clamp01(dist / targetingRadius);
            float score = (angleAlignment * angleWeight) + (distanceFactor * distanceWeight);

            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = targetTransform;
            }
        }

        lastTargetScore = bestScore;
        return bestCandidate;
    }

    public float CalculateAdaptedLungeDistance(float authoredLungeDistance, Transform target)
    {
        if (!adaptLungeDistance || target == null || _owner == null)
        {
            return authoredLungeDistance;
        }

        Vector3 diff = target.position - _owner.transform.position;
        diff.y = 0f;
        float currentDist = diff.magnitude;

        if (currentDist <= minMeleeDistance)
        {
            return 0f;
        }

        float effectiveDist = currentDist - minMeleeDistance;
        float distanceScale = Mathf.Clamp01(effectiveDist / 1.5f);
        return authoredLungeDistance * distanceScale;
    }

    public void DrawGizmos()
    {
        if (!drawDebugGizmos || _owner == null) return;

        Vector3 playerPos = _owner.transform.position;
        Vector3 intendedDir = GetIntendedDirection(out bool hasInput);
        float maxConeAngle = hasInput ? directionalConeAngle : neutralConeAngle;

        // Draw detection sphere
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireSphere(playerPos, targetingRadius);

        // Draw cone boundary rays
        Gizmos.color = hasInput ? Color.yellow : Color.cyan;
        Quaternion leftRot = Quaternion.AngleAxis(-maxConeAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(maxConeAngle, Vector3.up);
        Vector3 leftRay = leftRot * intendedDir * targetingRadius;
        Vector3 rightRay = rightRot * intendedDir * targetingRadius;

        Gizmos.DrawLine(playerPos, playerPos + intendedDir * targetingRadius);
        Gizmos.DrawLine(playerPos, playerPos + leftRay);
        Gizmos.DrawLine(playerPos, playerPos + rightRay);

        // Draw target indicator
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerPos + Vector3.up * 1f, currentTarget.position + Vector3.up * 1f);
            Gizmos.DrawWireSphere(currentTarget.position + Vector3.up * 1f, 0.4f);
        }
    }
}
