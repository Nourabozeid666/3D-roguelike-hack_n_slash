using System;
using UnityEngine;

[Serializable]
public class RunData
{
    [Header("Progress")]
    public int floor = 1;
    public int clearedRooms = 0;

    [Header("Spawning")]
    public float enemyBudget = 10f;         // spawn budget for current floor
    public float enemyBudgetGrowth = 1.4f;  // multiplier per floor
    public float enemyStatGrowth = 1.12f;   // health/damage multiplier per floor

    public void StartNewRun()
    {
        floor = 1;
        clearedRooms = 0;
        enemyBudget = 10f;
    }

    public void AdvanceFloor()
    {
        floor++;
        clearedRooms++;
        enemyBudget *= enemyBudgetGrowth;
    }
}
