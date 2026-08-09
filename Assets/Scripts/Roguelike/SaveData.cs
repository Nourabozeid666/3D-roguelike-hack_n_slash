using System;
using UnityEngine;

/// <summary>
/// Serialization DTO for a run's progress (the bytes that go to disk). Kept separate from the live
/// RunData because disk persistence needs a stable, versioned schema: adding a field here is a
/// save-format change, not a gameplay change. RunData remains the in-memory source of truth;
/// RunController.Capture()/TryRestore() map between the two (no duplicate run state at runtime).
///
/// Persisted fields: version, floor, clearedRooms, enemyBudget, enemyBudgetGrowth, enemyStatGrowth.
/// NOT persisted (derived on resume): RunState. A resumed run always enters FloorStart for the saved
/// floor; the scene bootstrap then populates the floor and calls BeginFloor().
/// </summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    [Header("Progress")]
    public int floor = 1;
    public int clearedRooms = 0;

    [Header("Spawning")]
    public float enemyBudget = 10f;
    public float enemyBudgetGrowth = 1.4f;
    public float enemyStatGrowth = 1.12f;
}
