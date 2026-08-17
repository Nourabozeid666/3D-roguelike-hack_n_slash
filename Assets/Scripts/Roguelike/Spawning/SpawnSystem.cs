using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Composition-based spawner for a floor, with two placement strategies and high-floor wave pacing:
///
/// PLACEMENT — FixedPoints (designer-placed SpawnPoint children, controlled/testing) or RandomZone
/// (a data-driven SpawnZone region resolved through SpawnPlacementValidator's pipeline: bounds ->
/// ground/NavMesh -> blocking layers -> player distance -> enemy distance, bounded by MaxAttempts).
///
/// WAVES — the floor composition is selected ONCE per floor (cached, budget never recomputed) and
/// sliced into waves by SpawnPacingConfig/WavePlan. Floors below the threshold spawn everything at
/// once. On a wave floor, killing the current wave releases the next; FloorCleared fires only when
/// NOTHING is alive AND no unspawned composition entries remain (a dead wave alone is not a clear).
///
/// REPORT-ONLY seam: this class raises the FloorCleared event and exposes IsFloorCleared; it never
/// touches RunState — the Run owner (RunBootstrap / test driver) decides what happens next. No
/// singleton, no static state, no EventBus.
///
/// ENEMY CONTRACT: every spawned enemy that implements IEnemySpawned is tracked via its OnDied
/// event (the Enemy system's authoritative death notification, bridged by EnemyController). Floor
/// scaling is owned here: SpawnSystem reads each enemy's base stats through ISpawnStatConfig,
/// computes the floor-scaled absolute values, and applies them through ConfigureForSpawn before
/// the enemy initializes. The Enemy side never sees floors, growth rates or multipliers.
/// </summary>
public class SpawnSystem : MonoBehaviour
{
    [SerializeField] SpawnTable table;

    [Header("Placement")]
    [SerializeField] SpawnStrategy strategy = SpawnStrategy.FixedPoints;
    [Tooltip("Required when strategy == RandomZone: the region + validation rules for general-purpose spawning.")]
    [SerializeField] SpawnZone zone;
    [Tooltip("Optional. When set, RandomZone candidates must stay at least SpawnZone.MinPlayerDistance from it. When null, the player-distance rule is skipped.")]
    [SerializeField] Transform playerReference;

    [Header("High-floor pacing / waves")]
    [SerializeField] SpawnPacingConfig pacingConfig = new();

    readonly List<GameObject> alive = new();
    readonly List<Vector3> occupiedPositions = new();
    readonly EnemyCompositionSelector compositions = new();
    readonly SpawnPlacementValidator placement = new();

    WavePlan pacing;
    List<SpawnPoint> remainingPoints = new();
    SpawnPoint[] pointPool = System.Array.Empty<SpawnPoint>();
    int currentFloor = 1;
    float currentBudget;
    int floorVersion;

    public int AliveCount() => alive.Count;

    /// <summary>
    /// A floor is cleared only when NOTHING is alive AND the whole floor composition has been
    /// released. On a multi-wave floor the current wave being dead is NOT a clear — the remaining
    /// waves are released first. The FloorCleared event fires under exactly this condition.
    /// </summary>
    public bool IsFloorCleared => alive.Count == 0 && !HasUnspawnedRemaining();

    /// <summary>
    /// REPORT-ONLY integration seam: raised exactly when the last alive enemy of the FINAL wave dies
    /// (alive hits 0 with nothing left to release). NOT raised for a cleared intermediate wave, and
    /// NOT raised by ClearAlive()/Populate() resetting the list. SpawnSystem never touches RunState.
    /// </summary>
    public event System.Action FloorCleared;

    /// <summary>
    /// Read-only summary of the last Populate: floor, available (unlocked) types, target count,
    /// chosen composition, total cost and budget. Debug/test info only.
    /// </summary>
    public string LastCompositionInfo { get; private set; } = string.Empty;

    /// <summary>Number of distinct cached (floor, target, budget) composition keys. Test-observable,
    /// so tests can assert a floor's composition is computed once and reused across waves.</summary>
    public int CachedCompositionKeys => compositions.CachedKeyCount;

    public SpawnStrategy Strategy => strategy;
    public int CurrentFloor => currentFloor;
    public float CurrentBudget => currentBudget;

    /// <summary>1-based wave currently released (0 before any wave / floor populated).</summary>
    public int CurrentWave => pacing != null ? pacing.CurrentWave : 0;

    /// <summary>Total waves for the current floor's composition (1 when waves are off).</summary>
    public int WaveCount => pacing != null ? pacing.WaveCount : 0;

    /// <summary>Unspawned composition entries still to release on the current floor.</summary>
    public int RemainingInComposition => pacing != null ? pacing.RemainingCount : 0;

    /// <summary>
    /// Start a floor: clear previous spawns, select the floor's composition ONCE (cached), build the
    /// wave plan, and release the first wave. The budget is a MAXIMUM spend, not exact.
    /// </summary>
    public void Populate(float budget, int floor)
    {
        floorVersion++;
        ClearAlive();
        pacing = null;
        occupiedPositions.Clear();
        currentFloor = floor;
        currentBudget = budget;
        LastCompositionInfo = string.Empty;

        if (table == null) return;

        IReadOnlyList<EnemyArchetype> available = table.AvailableForFloor(floor);
        if (available.Count == 0) return;

        if (!HasPlacementSource()) return;

        int target = TargetCountFor(budget, available);
        if (target <= 0) return;

        bool usedFallback = false;
        List<EnemyComposition> candidates = compositions.Get(floor, available, target, budget);
        EnemyComposition chosen;
        if (candidates.Count == 0)
        {
            // Deterministic documented fallback (never a silently invalid composition).
            chosen = BuildFallbackComposition(budget, available);
            if (chosen == null || chosen.Count == 0) return;
            usedFallback = true;
        }
        else
        {
            // Controlled randomness only between the final equally-ranked candidates.
            chosen = candidates[Random.Range(0, candidates.Count)];
        }

        pacing = new WavePlan(chosen, pacingConfig, floor);
        LastCompositionInfo = BuildSummary(floor, available, target, usedFallback ? null : chosen, budget);

        SpawnCurrentWave();
    }

    /// <summary>
    /// Deterministic target enemy count: the most enemies the floor budget can buy at the cheapest
    /// available cost (floor(budget / cheapest)). Always achievable.
    /// </summary>
    static int TargetCountFor(float budget, IReadOnlyList<EnemyArchetype> available)
    {
        int cheapest = int.MaxValue;
        foreach (EnemyArchetype a in available)
            if (a != null && a.Cost > 0 && a.Cost < cheapest) cheapest = a.Cost;
        if (cheapest == int.MaxValue) return 0;
        return (int)(budget / cheapest);
    }

    /// <summary>
    /// Release the current wave from the SAME composition plan: a fresh board, then the next
    /// PeekNextWaveSize() entries, then mark the wave released. Never re-selects a composition and
    /// never recomputes the budget.
    /// </summary>
    void SpawnCurrentWave()
    {
        if (pacing == null || !pacing.HasRemaining) return;

        occupiedPositions.Clear(); // previous wave is fully dead; the board is fresh for this wave
        int size = pacing.PeekNextWaveSize();
        for (int i = 0; i < size; i++)
            SpawnOne(pacing.NextEntry());
        pacing.MarkWaveReleased();
    }

    void SpawnOne(EnemyArchetype archetype)
    {
        if (archetype == null || archetype.Cost <= 0) return;

        if (!TryResolvePlacement(out Vector3 position, out Quaternion rotation))
        {
            // Fail gracefully: no valid location after MaxAttempts. Log a useful diagnostic and skip
            // this enemy — never spawn inside invalid geometry, never hang. The floor can still
            // complete with the entries that did find a location.
            Debug.LogWarning($"[SpawnSystem] Floor {currentFloor}: skipped one {archetype.DisplayName} — no valid spawn location after MaxAttempts (strategy {strategy}).");
            return;
        }

        alive.Add(InstantiateEnemy(archetype, position, rotation));
        if (strategy == SpawnStrategy.RandomZone) occupiedPositions.Add(position);
    }

    bool TryResolvePlacement(out Vector3 position, out Quaternion rotation)
    {
        if (strategy == SpawnStrategy.RandomZone)
            return TryResolveZonePlacement(out position, out rotation);
        return TryResolvePointPlacement(out position, out rotation);
    }

    bool TryResolvePointPlacement(out Vector3 position, out Quaternion rotation)
    {
        if (remainingPoints.Count == 0)
        {
            // Pool exhausted: refill so more enemies than points still spawn. Matches the original
            // documented behavior ("the pool resets and points get reused") and lets waves reuse
            // points after the previous wave is dead.
            if (pointPool.Length == 0)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }
            remainingPoints.AddRange(pointPool);
        }
        SpawnPoint point = PickRandomPoint(remainingPoints);
        position = point.Position;
        rotation = point.Rotation;
        return true;
    }

    bool TryResolveZonePlacement(out Vector3 position, out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        if (zone == null)
        {
            position = Vector3.zero;
            return false;
        }

        // No player reference -> the player-distance rule is skipped (effective min 0), never invented.
        float minPlayerDistance = playerReference != null ? zone.MinPlayerDistance : 0f;
        Vector3 playerPosition = playerReference != null ? playerReference.position : Vector3.zero;

        return placement.TryFindLocation(
            zone, playerPosition, minPlayerDistance, occupiedPositions,
            TryGroundOrNavMesh,
            IsBlocked,
            out position);
    }

    /// <summary>
    /// Ground/NavMesh validation seam. When the zone disables NavMesh validation this is a no-op
    /// pass-through; when enabled, the candidate must sample to a walkable NavMesh location (the
    /// accepted position snaps to the sample). SpawnSystem stays compilable and testable without a
    /// baked NavMesh because the harness injects a deterministic fake for this path.
    /// </summary>
    bool TryGroundOrNavMesh(Vector3 candidate, out Vector3 snapped)
    {
        snapped = candidate;
        if (zone == null || !zone.UseNavMeshValidation) return true;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, zone.GroundSampleRadius, NavMesh.AllAreas))
        {
            snapped = hit.position;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Blocking-geometry overlap using the zone's configured footprint radius + safety margin
    /// (a real volume, not a center-only point test). The effective clearance is the enemy's
    /// physical footprint plus a configurable buffer around obstacles. Layer numbers are never
    /// hardcoded — the mask comes from SpawnZone.BlockingLayers.
    /// </summary>
    bool IsBlocked(Vector3 candidate)
    {
        if (zone == null || zone.BlockingLayers.value == 0) return false;
        float radius = zone.FootprintRadius + zone.SafetyMargin;
        if (radius <= 0f) return false;
        return Physics.CheckSphere(candidate, radius, zone.BlockingLayers.value);
    }

    void OnEnemyDied(GameObject enemy)
    {
        // Idempotent death handling: a dead enemy must never decrement alive (or clear a floor)
        // twice. Remove returns false if the enemy was already removed (double OnDied, or the
        // floor was replaced by ClearAlive while a stale event was in flight), so we stop there.
        if (!alive.Remove(enemy)) return;

        if (alive.Count > 0) return;

        if (HasUnspawnedRemaining())
        {
            // Current wave fully dead but more of the composition remains: release the next wave.
            // NOT a floor clear. Bounded and floor-version-guarded so a stale coroutine can never
            // spawn into a replaced floor.
            StartCoroutine(SpawnNextWaveCoroutine(floorVersion));
            return;
        }

        FloorCleared?.Invoke();
    }

    bool HasUnspawnedRemaining() => pacing != null && pacing.HasRemaining;

    IEnumerator SpawnNextWaveCoroutine(int version)
    {
        float delay = pacingConfig != null ? pacingConfig.WaveDelaySeconds : 0f;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (version != floorVersion) yield break; // this floor was replaced (new run/floor)
        SpawnCurrentWave();
    }

    /// <summary>Draw a SpawnPoint without replacement so a single wave never stacks two enemies on
    /// the same point. If more enemies than points remain affordable, the pool resets (waves reuse
    /// points after the previous wave is dead).</summary>
    static SpawnPoint PickRandomPoint(List<SpawnPoint> available)
    {
        if (available.Count == 0) return null;
        int index = Random.Range(0, available.Count);
        SpawnPoint point = available[index];
        available.RemoveAt(index);
        return point;
    }

    bool HasPlacementSource()
    {
        if (strategy == SpawnStrategy.RandomZone) return zone != null;
        pointPool = GetComponentsInChildren<SpawnPoint>(true);
        remainingPoints = new List<SpawnPoint>(pointPool);
        return remainingPoints.Count > 0;
    }

    /// <summary>
    /// Deterministic documented fallback, only reachable if a caller ever passes a target the budget
    /// cannot hold (not reachable with TargetCountFor): the largest affordable count of the cheapest
    /// archetype, never exceeding the budget.
    /// </summary>
    static EnemyComposition BuildFallbackComposition(float budget, IReadOnlyList<EnemyArchetype> available)
    {
        EnemyArchetype cheapest = null;
        foreach (EnemyArchetype a in available)
            if (a != null && a.Cost > 0 && (cheapest == null || a.Cost < cheapest.Cost)) cheapest = a;
        if (cheapest == null) return null;

        int count = (int)(budget / cheapest.Cost);
        if (count <= 0) return null;

        var entries = new EnemyArchetype[count];
        for (int i = 0; i < count; i++) entries[i] = cheapest;
        return new EnemyComposition(entries);
    }

    GameObject InstantiateEnemy(EnemyArchetype archetype, Vector3 position, Quaternion rotation)
    {
        GameObject enemy = Instantiate(archetype.Prefab, position, rotation);

        // Death contract: surface the enemy's authoritative death notification so alive tracking
        // and FloorCleared work. Enemies that do not implement IEnemySpawned are not tracked.
        IEnemySpawned spawned = enemy.GetComponent<IEnemySpawned>();
        if (spawned != null)
            spawned.OnDied += () => OnEnemyDied(enemy);

        // Floor scaling (owned by SpawnSystem): read base stats, compute the scaled absolute
        // values, apply them through the enemy's config seam BEFORE its own initialization runs.
        ApplyFloorScaling(enemy, archetype, currentFloor);

        return enemy;
    }

    static void ApplyFloorScaling(GameObject enemy, EnemyArchetype archetype, int floor)
    {
        ISpawnStatConfig configurable = enemy.GetComponent<ISpawnStatConfig>();
        if (configurable == null) return;

        float healthScale = Mathf.Pow(archetype.HealthGrowthPerFloor + 1f, floor - 1);
        float damageScale = Mathf.Pow(archetype.DamageGrowthPerFloor + 1f, floor - 1);

        configurable.ConfigureForSpawn(
            configurable.BaseMaxHealth * healthScale,
            configurable.BaseDamage * damageScale);
    }

    void ClearAlive()
    {
        foreach (GameObject enemy in alive)
            if (enemy != null) Destroy(enemy);
        alive.Clear();
    }

    static string BuildSummary(int floor, IReadOnlyList<EnemyArchetype> available, int target, EnemyComposition chosen, float budget)
    {
        var sb = new StringBuilder();
        sb.Append("floor ").Append(floor).Append(" | available: ");
        for (int i = 0; i < available.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(Describe(available[i]));
        }
        sb.Append(" | target ").Append(target);
        if (chosen != null)
        {
            sb.Append(" | composition: ");
            for (int i = 0; i < chosen.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Describe(chosen.Entries[i]));
            }
            sb.Append(" | cost ").Append(chosen.TotalCost).Append('/').Append(budget);
        }
        else
        {
            sb.Append(" | fallback (no valid composition)");
        }
        return sb.ToString();
    }

    static string Describe(EnemyArchetype a)
    {
        string name = a != null ? a.DisplayName : string.Empty;
        return string.IsNullOrEmpty(name) ? "cost " + (a != null ? a.Cost : 0) : name;
    }
}
