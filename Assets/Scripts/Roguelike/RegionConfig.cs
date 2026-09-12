using System;
using UnityEngine;

/// <summary>
/// Configuration for a single Region in the run.
/// Exposes scene name, total round count, and budget tuning so designers can edit them in the Inspector.
/// </summary>
[Serializable]
public class RegionConfig
{
    [Tooltip("Display name of the region (e.g. 'The Arena', 'The Infernal Depths').")]
    public string regionName = "Region 1";

    [Tooltip("The Unity scene name to load for this region. Kept open for designer editing.")]
    public string sceneName = "TestingScene";

    [Tooltip("Total rounds in this region before transitioning to the next region or the finale.")]
    public int totalRounds = 10;

    [Tooltip("SpawnTable asset used for this region's enemies.")]
    public SpawnTable spawnTable;

    [Tooltip("Starting enemy spawn budget for round 1 of this region.")]
    public float startingBudget = 10f;

    [Tooltip("Multiplier applied to enemy budget after each round.")]
    public float budgetGrowthPerRound = 1.3f;

    [Tooltip("Enemy stat multiplier (health/damage) per round.")]
    public float statGrowthPerRound = 1.1f;
}
