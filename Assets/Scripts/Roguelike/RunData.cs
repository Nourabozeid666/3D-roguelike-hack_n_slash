using System;
using UnityEngine;

[Serializable]
public class RunData
{
    [Header("Progress")]
    public int floor = 1;
    public int clearedRooms = 0;
    public int currentRegionIndex = 0;
    public int currentRoundInRegion = 1;
    public int totalRoundsPerRegion = 10;

    [Header("Spawning")]
    public float enemyBudget = 10f;         // spawn budget for current floor
    public float enemyBudgetGrowth = 1.4f;  // multiplier per floor
    public float enemyStatGrowth = 1.12f;   // health/damage multiplier per floor

    public bool IsFinalRoundOfRegion => currentRoundInRegion >= totalRoundsPerRegion;
    public bool IsFinalRegion => currentRegionIndex >= 1; // Region 2 is the final region
    public bool IsRunComplete => IsFinalRegion && IsFinalRoundOfRegion;

    public void StartNewRun()
    {
        floor = 1;
        clearedRooms = 0;
        currentRegionIndex = 0;
        currentRoundInRegion = 1;
        totalRoundsPerRegion = 10;
        enemyBudget = 10f;
    }

    public void AdvanceFloor()
    {
        floor++;
        clearedRooms++;
        currentRoundInRegion++;
        enemyBudget *= enemyBudgetGrowth;
    }

    public void AdvanceRegion(float newRegionBaseBudget, float newBudgetGrowth, float newStatGrowth)
    {
        floor++;
        clearedRooms++;
        currentRegionIndex++;
        currentRoundInRegion = 1;
        enemyBudget = newRegionBaseBudget;
        enemyBudgetGrowth = newBudgetGrowth;
        enemyStatGrowth = newStatGrowth;
    }
}
