using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining the ordered regions for the game run.
/// By default configures Region 1 (10 rounds in TestingScene) and Region 2 (10 rounds in basic game scene).
/// </summary>
[CreateAssetMenu(fileName = "RunRegionSettings", menuName = "Roguelike/Run Region Settings")]
public class RunRegionSettings : ScriptableObject
{
    [SerializeField]
    private List<RegionConfig> regions = new()
    {
        new RegionConfig
        {
            regionName = "Region 1: The Arena",
            sceneName = "TestingScene",
            totalRounds = 10,
            startingBudget = 10f,
            budgetGrowthPerRound = 1.3f,
            statGrowthPerRound = 1.1f
        },
        new RegionConfig
        {
            regionName = "Region 2: The Infernal Depths",
            sceneName = "basic game scene",
            totalRounds = 10,
            startingBudget = 25f,
            budgetGrowthPerRound = 1.35f,
            statGrowthPerRound = 1.12f
        }
    };

    public IReadOnlyList<RegionConfig> Regions => regions;

    public RegionConfig GetRegion(int index)
    {
        if (regions == null || regions.Count == 0) return null;
        int clamped = Mathf.Clamp(index, 0, regions.Count - 1);
        return regions[clamped];
    }

    public bool IsFinalRegion(int index)
    {
        if (regions == null || regions.Count == 0) return true;
        return index >= regions.Count - 1;
    }
}
