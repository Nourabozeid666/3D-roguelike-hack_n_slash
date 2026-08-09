using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Composition-based spawner: for a floor it picks the enemies that are UNLOCKED on that floor
/// (SpawnTable.AvailableForFloor), derives the target enemy count the floor budget can afford,
/// selects a valid composition from the cached/ranked candidates (budget is a MAXIMUM spend, not
/// exact), spawns it onto the child SpawnPoints, tracks the alive enemies and reports when they are
/// cleared. Owned by the Roguelike side. No singleton, no static state, no EventBus.
/// </summary>
public class SpawnSystem : MonoBehaviour
{
    [SerializeField] SpawnTable table;

    readonly List<GameObject> alive = new();
    readonly EnemyCompositionSelector compositions = new();

    public int AliveCount() => alive.Count;

    public bool IsFloorCleared => alive.Count == 0;

    /// <summary>
    /// REPORT-ONLY integration seam: raised exactly when the last alive spawned enemy is removed
    /// by a death (alive.Count hits 0). NOT raised by ClearAlive()/Populate() resetting the list.
    /// SpawnSystem never touches RunState — whoever owns the Run decides what happens next.
    /// </summary>
    public event System.Action FloorCleared;

    /// <summary>
    /// Read-only summary of the last Populate: floor, available (unlocked) types, target count,
    /// chosen composition, total cost and budget. Debug/test info only — no production system reads
    /// it (see the test HUD in SpawnTestDebugDisplay).
    /// </summary>
    public string LastCompositionInfo { get; private set; } = string.Empty;

    /// <summary>Number of distinct cached (floor, target, budget) composition keys. Test-observable,
    /// so tests can assert a floor's composition is computed once and reused.</summary>
    public int CachedCompositionKeys => compositions.CachedKeyCount;

    /// <summary>Clear previous spawns, then spend <paramref name="budget"/> (a MAXIMUM, not exact)
    /// spawning a valid composition of enemies for <paramref name="floor"/>.</summary>
    public void Populate(float budget, int floor)
    {
        ClearAlive();
        LastCompositionInfo = string.Empty;

        if (table == null) return;

        IReadOnlyList<EnemyArchetype> available = table.AvailableForFloor(floor);
        if (available.Count == 0) return;

        SpawnPoint[] points = GetComponentsInChildren<SpawnPoint>(true);
        if (points.Length == 0) return;

        int target = TargetCountFor(budget, available);
        if (target <= 0) return;

        List<EnemyComposition> candidates = compositions.Get(floor, available, target, budget);
        if (candidates.Count == 0)
        {
            SpawnFallback(budget, available, points, floor);
            LastCompositionInfo = BuildSummary(floor, available, target, null, budget);
            return;
        }

        // 4. Controlled randomness only between the final equally-ranked candidates.
        EnemyComposition chosen = candidates[Random.Range(0, candidates.Count)];
        SpawnComposition(chosen, points, floor);
        LastCompositionInfo = BuildSummary(floor, available, target, chosen, budget);
    }

    /// <summary>
    /// Deterministic target enemy count: the most enemies the floor budget can buy at the cheapest
    /// available cost (floor(budget / cheapest)). Always achievable — `target * cheapest &lt;= budget` —
    /// so a valid composition always exists for the pools used by a run. A future explicit
    /// target-count design can replace this single derivation point.
    /// </summary>
    static int TargetCountFor(float budget, IReadOnlyList<EnemyArchetype> available)
    {
        int cheapest = int.MaxValue;
        foreach (EnemyArchetype a in available)
            if (a != null && a.Cost > 0 && a.Cost < cheapest) cheapest = a.Cost;
        if (cheapest == int.MaxValue) return 0;
        return (int)(budget / cheapest);
    }

    void SpawnComposition(EnemyComposition composition, SpawnPoint[] points, int floor)
    {
        var remainingPoints = new List<SpawnPoint>(points);
        for (int i = 0; i < composition.Count; i++)
        {
            EnemyArchetype archetype = composition.Entries[i];
            if (archetype == null || archetype.Cost <= 0) continue;

            if (remainingPoints.Count == 0) remainingPoints.AddRange(points);
            SpawnPoint point = PickRandomPoint(remainingPoints);
            if (point == null) break;
            alive.Add(InstantiateEnemy(archetype, point, floor));
        }
    }

    /// <summary>
    /// Deterministic documented fallback, only reachable if a caller ever passes a target the budget
    /// cannot hold (not reachable with TargetCountFor above, which is always achievable): spawn the
    /// largest affordable count of the cheapest archetype, never exceeding the budget, and log it —
    /// never a silently invalid composition.
    /// </summary>
    void SpawnFallback(float budget, IReadOnlyList<EnemyArchetype> available, SpawnPoint[] points, int floor)
    {
        Debug.LogWarning("[SpawnSystem] no valid composition for this floor; spawning deterministic cheapest fill");

        EnemyArchetype cheapest = null;
        foreach (EnemyArchetype a in available)
            if (a != null && a.Cost > 0 && (cheapest == null || a.Cost < cheapest.Cost)) cheapest = a;
        if (cheapest == null) return;

        var remainingPoints = new List<SpawnPoint>(points);
        float remaining = budget;
        while (remaining >= cheapest.Cost)
        {
            if (remainingPoints.Count == 0) remainingPoints.AddRange(points);
            SpawnPoint point = PickRandomPoint(remainingPoints);
            if (point == null) break;
            alive.Add(InstantiateEnemy(cheapest, point, floor));
            remaining -= cheapest.Cost;
        }
    }

    /// <summary>
    /// Draw a SpawnPoint without replacement so a single Populate pass never stacks two enemies
    /// on the same point (which made overlapping enemies look like one). If more enemies than
    /// points are affordable, the pool resets and points get reused.
    /// </summary>
    static SpawnPoint PickRandomPoint(List<SpawnPoint> available)
    {
        if (available.Count == 0) return null;
        int index = Random.Range(0, available.Count);
        SpawnPoint point = available[index];
        available.RemoveAt(index);
        return point;
    }

    GameObject InstantiateEnemy(EnemyArchetype archetype, SpawnPoint point, int floor)
    {
        GameObject enemy = Instantiate(archetype.Prefab, point.Position, point.Rotation);
        IEnemySpawned spawned = enemy.GetComponent<IEnemySpawned>();
        if (spawned != null)
        {
            ApplyFloorScaling(spawned, archetype, floor);
            spawned.Died += () => OnEnemyDied(enemy);
        }
        return enemy;
    }

    static void ApplyFloorScaling(IEnemySpawned spawned, EnemyArchetype archetype, int floor)
    {
        float healthScale = Mathf.Pow(archetype.HealthGrowthPerFloor + 1f, floor - 1);
        float damageScale = Mathf.Pow(archetype.DamageGrowthPerFloor + 1f, floor - 1);
        spawned.ApplyFloorScaling(healthScale, damageScale);
    }

    void OnEnemyDied(GameObject enemy)
    {
        alive.Remove(enemy);
        if (alive.Count == 0) FloorCleared?.Invoke();
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
