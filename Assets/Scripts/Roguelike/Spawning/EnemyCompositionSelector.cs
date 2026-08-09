using System;
using System.Collections.Generic;

/// <summary>
/// Pure, instance-based composition selector for a floor's spawn. Given the currently unlocked
/// archetype pool, a target enemy count and the floor budget (a MAXIMUM spend, not exact), it
/// enumerates every valid composition exactly once, ranks them by the stated priorities and caches
/// the ranked result keyed on (floor, target, budget). No recursive search runs per spawn and no
/// brute-force retry loop exists - a Populate just does a cache lookup and picks a candidate.
///
/// Ranking priorities (docs/ROGUELIKE_SYSTEM.md §4.5a):
///   1. exactly <c>target</c> enemies and total cost &lt;= budget (every cached candidate satisfies this)
///   2. best budget use: highest total cost without exceeding the budget
///   3. variety only among equally-ranked compositions: most distinct archetype types
///   4. controlled randomness only between the final equally-ranked candidates
///
/// If no valid composition exists the returned list is empty and the caller applies its documented
/// deterministic fallback (SpawnSystem.SpawnFallback) - never a silently invalid spawn.
/// No static state: each SpawnSystem owns one selector for the whole run.
/// </summary>
public class EnemyCompositionSelector
{
    /// <summary>Cache key. The pool is a pure function of the floor (SpawnTable.AvailableForFloor),
    /// so the floor identifies it; budget is quantized to 1/1000 so costs (ints) can never straddle
    /// a bucket boundary.</summary>
    public readonly struct Key : IEquatable<Key>
    {
        public readonly int floor;
        public readonly int target;
        public readonly int budgetCents;

        public Key(int floor, int target, float budget)
        {
            this.floor = floor;
            this.target = target;
            this.budgetCents = (int)(budget * 1000f + 0.5f);
        }

        public bool Equals(Key other) =>
            floor == other.floor && target == other.target && budgetCents == other.budgetCents;

        public override bool Equals(object obj) => obj is Key other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = floor;
                hash = hash * 397 ^ target;
                hash = hash * 397 ^ budgetCents;
                return hash;
            }
        }
    }

    readonly Dictionary<Key, List<EnemyComposition>> cache = new();

    /// <summary>Number of distinct cached (floor, target, budget) keys - lets tests assert cache reuse.</summary>
    public int CachedKeyCount => cache.Count;

    /// <summary>
    /// Valid, ranked compositions for (pool, target, budget): best budget use first, then most
    /// distinct archetype types. Returns an empty list when no valid composition exists (the caller
    /// applies its documented fallback). The returned list is the same cached instance on repeated
    /// calls with the same key.
    /// </summary>
    public List<EnemyComposition> Get(int floor, IReadOnlyList<EnemyArchetype> pool, int target, float budget)
    {
        var key = new Key(floor, target, budget);
        if (!cache.TryGetValue(key, out List<EnemyComposition> ranked))
        {
            ranked = Build(pool, target, budget);
            cache[key] = ranked;
        }
        return ranked;
    }

    static List<EnemyComposition> Build(IReadOnlyList<EnemyArchetype> pool, int target, float budget)
    {
        if (pool == null || pool.Count == 0 || target <= 0) return new List<EnemyComposition>();

        var usable = new List<EnemyArchetype>(pool.Count);
        foreach (EnemyArchetype a in pool)
            if (a != null && a.Cost > 0) usable.Add(a);
        if (usable.Count == 0) return new List<EnemyComposition>();

        var found = new List<EnemyComposition>();
        Enumerate(usable, budget, new int[target], 0, 0, 0, found);
        if (found.Count == 0) return new List<EnemyComposition>();

        // 2. Best budget use = highest total cost <= budget.
        int maxCost = 0;
        foreach (EnemyComposition c in found) if (c.TotalCost > maxCost) maxCost = c.TotalCost;

        var bestCost = new List<EnemyComposition>();
        foreach (EnemyComposition c in found) if (c.TotalCost == maxCost) bestCost.Add(c);

        // 3. Variety only among equally-ranked compositions = most distinct archetype types.
        int maxDistinct = 0;
        foreach (EnemyComposition c in bestCost) if (c.DistinctTypes > maxDistinct) maxDistinct = c.DistinctTypes;

        var varied = new List<EnemyComposition>();
        foreach (EnemyComposition c in bestCost) if (c.DistinctTypes == maxDistinct) varied.Add(c);

        return varied;
    }

    /// <summary>
    /// Combinations WITH repetition of exactly <c>indices.Length</c> picks from the pool (indices are
    /// non-decreasing, so each multiset is enumerated once). Prunes the moment a partial composition
    /// already exceeds the budget (adding more enemies only ever increases cost). Bounded input: pool
    /// size and target are small, and this runs once per cached key, never per spawn.
    /// </summary>
    static void Enumerate(List<EnemyArchetype> pool, float budget, int[] indices, int slot, int startIndex, int partialCost, List<EnemyComposition> found)
    {
        if (slot == indices.Length)
        {
            var entries = new EnemyArchetype[indices.Length];
            for (int i = 0; i < indices.Length; i++) entries[i] = pool[indices[i]];
            found.Add(new EnemyComposition(entries));
            return;
        }

        for (int i = startIndex; i < pool.Count; i++)
        {
            int nextCost = partialCost + pool[i].Cost;
            if (nextCost > budget) continue;
            indices[slot] = i;
            Enumerate(pool, budget, indices, slot + 1, i, nextCost, found);
        }
    }
}
