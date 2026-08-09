using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cost-based spawner: fills its child SpawnPoints with affordable archetypes until the
/// floor budget is spent, tracks the spawned enemies, and reports when they are cleared.
/// Owned by the Roguelike side. No singleton, no static state, no EventBus.
/// </summary>
public class SpawnSystem : MonoBehaviour
{
    [SerializeField] SpawnTable table;

    readonly List<GameObject> alive = new();

    public int AliveCount() => alive.Count;

    public bool IsFloorCleared => alive.Count == 0;

    /// <summary>
    /// REPORT-ONLY integration seam: raised exactly when the last alive spawned enemy is removed
    /// by a death (alive.Count hits 0). NOT raised by ClearAlive()/Populate() resetting the list.
    /// SpawnSystem never touches RunState — whoever owns the Run decides what happens next.
    /// </summary>
    public event System.Action FloorCleared;

    /// <summary>Clear previous spawns, then spend <paramref name="budget"/> spawning enemies for <paramref name="floor"/>.</summary>
    public void Populate(float budget, int floor)
    {
        ClearAlive();

        if (table == null || table.Archetypes.Count == 0) return;

        SpawnPoint[] points = GetComponentsInChildren<SpawnPoint>(true);
        if (points.Length == 0) return;

        int cheapest = CheapestCost(table.Archetypes);
        if (cheapest == int.MaxValue) return;

        float remaining = budget;
        var available = new List<SpawnPoint>(points);
        while (remaining >= cheapest)
        {
            EnemyArchetype archetype = PickAffordable(table.Archetypes, remaining);
            if (archetype == null || archetype.Cost <= 0) break;

            if (available.Count == 0) available.AddRange(points);
            SpawnPoint point = PickRandomPoint(available);
            alive.Add(InstantiateEnemy(archetype, point, floor));
            remaining -= archetype.Cost;
        }
    }

    static int CheapestCost(IReadOnlyList<EnemyArchetype> archetypes)
    {
        int cheapest = int.MaxValue;
        foreach (EnemyArchetype a in archetypes)
            if (a != null && a.Cost < cheapest) cheapest = a.Cost;
        return cheapest;
    }

    static EnemyArchetype PickAffordable(IReadOnlyList<EnemyArchetype> archetypes, float budget)
    {
        int affordableCount = 0;
        for (int i = 0; i < archetypes.Count; i++)
            if (archetypes[i] != null && archetypes[i].Cost <= budget) affordableCount++;
        if (affordableCount == 0) return null;

        int pick = Random.Range(0, affordableCount);
        for (int i = 0; i < archetypes.Count; i++)
        {
            if (archetypes[i] != null && archetypes[i].Cost <= budget && pick-- == 0)
                return archetypes[i];
        }
        return null;
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
}
