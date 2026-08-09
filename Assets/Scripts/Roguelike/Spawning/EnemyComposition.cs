using System.Collections.Generic;

/// <summary>
/// Immutable snapshot of one way to spend a floor budget on exactly <c>Count</c> enemies drawn from
/// the available archetype pool (total cost is guaranteed &lt;= the floor budget). Built once by
/// <see cref="EnemyCompositionSelector"/> per cached (floor, target, budget) key and reused across
/// Populate calls, so spawn-time selection allocates nothing per enemy.
/// </summary>
public class EnemyComposition
{
    readonly EnemyArchetype[] entries;

    public EnemyComposition(EnemyArchetype[] entries)
    {
        this.entries = entries;
        int total = 0;
        var distinct = new HashSet<EnemyArchetype>();
        foreach (EnemyArchetype a in entries)
        {
            total += a.Cost;
            distinct.Add(a);
        }
        TotalCost = total;
        DistinctTypes = distinct.Count;
    }

    public IReadOnlyList<EnemyArchetype> Entries => entries;

    public int Count => entries.Length;

    /// <summary>Total spawn cost of this composition (never exceeds the floor budget).</summary>
    public int TotalCost { get; }

    /// <summary>How many distinct archetype types this composition uses (variety metric).</summary>
    public int DistinctTypes { get; }
}
