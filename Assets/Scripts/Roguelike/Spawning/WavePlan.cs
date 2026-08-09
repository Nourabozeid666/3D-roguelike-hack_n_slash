using UnityEngine;

/// <summary>
/// Deterministic wave plan for ONE floor's already-selected composition. The composition is never
/// copied, reselected or re-budgeted: waves are contiguous slices of the SAME
/// <see cref="EnemyComposition"/>'s entry array, released in order through a single advancing cursor,
/// so every enemy spawns exactly once (no duplicates) and the original composition is preserved.
///
/// A floor with waves off, or below the configured threshold, is a single wave containing the whole
/// composition (WaveCount == 1). Waves advance only when the previous wave is fully dead
/// (SpawnSystem.OnEnemyDied -> SpawnCurrentWave), and the floor is only cleared once the final slice
/// has been released AND nothing is alive.
/// </summary>
public class WavePlan
{
    readonly int waveSize;
    readonly int totalCount;
    int spawnedIndex;
    int wavesReleased;

    public WavePlan(EnemyComposition composition, SpawnPacingConfig config, int floor)
    {
        Composition = composition;
        totalCount = composition != null ? composition.Count : 0;

        bool split = composition != null && config != null && config.UsesWavesOn(floor);
        waveSize = split ? config.WaveSize : totalCount;
        UsesWaves = split && waveSize > 0 && waveSize < totalCount;
        WaveCount = UsesWaves ? (totalCount + waveSize - 1) / waveSize : 1;
    }

    /// <summary>The exact composition object this plan slices. Never rebuilt per wave.</summary>
    public EnemyComposition Composition { get; }

    public bool UsesWaves { get; }

    /// <summary>Total enemies in the composition (== TotalCount of the original).</summary>
    public int TotalCount => totalCount;

    /// <summary>Number of waves the composition is divided into (1 when waves are off).</summary>
    public int WaveCount { get; }

    /// <summary>1-based wave currently being released (0 before the first wave spawns).</summary>
    public int CurrentWave => wavesReleased;

    /// <summary>True while unspawned composition entries remain.</summary>
    public bool HasRemaining => spawnedIndex < totalCount;

    /// <summary>Unspawned composition entries still to release.</summary>
    public int RemainingCount => totalCount - spawnedIndex;

    /// <summary>How many enemies the NEXT wave will release (bounded by the remaining composition).</summary>
    public int PeekNextWaveSize() => Mathf.Min(waveSize, totalCount - spawnedIndex);

    /// <summary>Return the next composition entry and advance the cursor. Null when exhausted.</summary>
    public EnemyArchetype NextEntry()
    {
        if (spawnedIndex >= totalCount) return null;
        return Composition.Entries[spawnedIndex++];
    }

    /// <summary>Record that the current wave's slice has been fully released.</summary>
    public void MarkWaveReleased()
    {
        wavesReleased++;
    }
}
