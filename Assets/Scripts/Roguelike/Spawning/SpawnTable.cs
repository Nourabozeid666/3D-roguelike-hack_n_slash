using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool of enemy archetypes the SpawnSystem may pick from. The pool unlocks over a run by floor:
/// the archetype at list index i becomes available starting at floor 1 + i * unlockInterval
/// (default interval 3: index 0 -> floors 1-3, index 1 -> floors 4-6, index 2 -> floors 7-9, ...).
/// Unlocking only EXPANDS the pool; a newly unlocked enemy is never guaranteed to spawn (the
/// composition selection may legitimately skip it). Add new enemies to the END of the list.
/// The spawn budget comes from RunData.enemyBudget (single source of truth), not this asset.
/// </summary>
[CreateAssetMenu(fileName = "SpawnTable", menuName = "Roguelike/Spawn Table")]
public class SpawnTable : ScriptableObject
{
    [SerializeField] List<EnemyArchetype> archetypes = new();

    [Tooltip("New enemies unlock every N floors: the archetype at list index i becomes available at floor 1 + i*N (default 3: index 0 -> floor 1, index 1 -> floor 4, ...). Add new enemies to the end of the list.")]
    [SerializeField] int unlockInterval = 3;

    public IReadOnlyList<EnemyArchetype> Archetypes => archetypes;

    /// <summary>The unlock interval, guarded to be at least 1.</summary>
    public int UnlockInterval => Mathf.Max(1, unlockInterval);

    /// <summary>
    /// The archetypes unlocked on <paramref name="floor"/>: a prefix of the pool whose unlock floor
    /// has been reached (index i unlocks at floor 1 + i * UnlockInterval, i.e. i &lt;= (floor-1)/interval).
    /// Returns the whole pool once everything is unlocked, so no per-floor list is built then.
    /// </summary>
    public IReadOnlyList<EnemyArchetype> AvailableForFloor(int floor)
    {
        int unlocked = (floor - 1) / UnlockInterval + 1;
        if (unlocked >= archetypes.Count) return Archetypes;
        if (unlocked <= 0) return new List<EnemyArchetype>();

        var available = new List<EnemyArchetype>(unlocked);
        for (int i = 0; i < unlocked; i++)
            if (archetypes[i] != null) available.Add(archetypes[i]);
        return available;
    }
}
