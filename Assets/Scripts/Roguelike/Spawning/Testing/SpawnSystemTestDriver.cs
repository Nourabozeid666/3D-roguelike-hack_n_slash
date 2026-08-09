using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TEST-ONLY Play Mode driver for TestingScene.unity (and SpawnTest.unity). It drives the REAL
/// RunController and SpawnSystem: runs the Sprint 4 checks AND the SpawnSystem <-> Run System
/// integration (floors auto-advance when the current floor's spawned enemies are cleared), logging
/// PASS/FAIL. It is not production code and uses FindObjectsOfType freely because it is a test
/// harness, not a system.
///
/// Integration contract (docs/ROGUELIKE_SPAWNING_SPRINT_4.md):
///   SpawnSystem only REPORTS "current floor cleared" via its FloorCleared event; it never touches
///   RunState. This driver is the test-only bridge: on that event it asks the REAL RunController to
///   CompleteFloor() (FloorActive -> FloorCleared), then after a short visible pause StartNextFloor()
///   (FloorCleared -> FloorStart + RunData.AdvanceFloor()), asks SpawnSystem to Populate the new
///   floor's budget, then BeginFloor() (FloorStart -> FloorActive). The state machine's guarded
///   transitions + the report-only event (fired only on a real all-clear) prevent restart loops.
/// </summary>
public class SpawnSystemTestDriver : MonoBehaviour
{
    [SerializeField] SpawnSystem spawnSystem;

    /// <summary>How long the run stays in FloorCleared before the next floor populates, so the
    /// cleared state is visible in the HUD (default 1s). Test-only tuning.</summary>
    [SerializeField] float floorClearPauseSeconds = 1f;

    /// <summary>The real RunController this driver drives, exposed so the debug HUD can read
    /// live run state/floor (SpawnTestDebugDisplay).</summary>
    public RunController Run { get; } = new();

    readonly List<string> failures = new();
    int checks;

    void OnDestroy()
    {
        if (spawnSystem != null) spawnSystem.FloorCleared -= OnFloorCleared;
    }

    IEnumerator Start()
    {
        yield return null;

        if (RunSession.EnterFromMenu)
        {
            // The scene was entered from the Main Menu: RunBootstrap owns the run. Deferring here
            // keeps the automated checks for direct scene opens (Editor Play Mode) while letting the
            // real Continue/New Run flow use the same scene without two run owners fighting.
            Debug.Log("[SpawnSystemTest] Scene entered from the Main Menu — automated checks skipped (RunBootstrap owns the run).");
            yield break;
        }

        if (spawnSystem == null)
        {
            Fail("spawnSystem reference is null");
            LogSummary();
            yield break;
        }

        RunController run = Run;

        // Live integration seam: SpawnSystem reports, the RunController decides.
        spawnSystem.FloorCleared += OnFloorCleared;

        // 1. Run starts and reaches FloorStart.
        Check(run.StartRun(), "StartRun() -> FloorStart");
        Check(run.CurrentState == RunState.FloorStart, "Run state is FloorStart after StartRun");
        Check(!run.CompleteFloor(), "CompleteFloor() rejected outside FloorActive (guarded)");
        Check(!run.StartNextFloor(), "StartNextFloor() rejected outside FloorCleared (guarded)");

        // 2. Budget comes from the real RunData.
        float budget = run.Data.enemyBudget;
        int floor = run.Data.floor;
        Check(budget == 10f, "RunData.enemyBudget == 10 on floor 1");
        Check(floor == 1, "RunData.floor == 1");

        // 3. Empty before populate.
        Check(spawnSystem.AliveCount() == 0, "AliveCount() == 0 before populate");

        // 4. Populate spawns multiple enemies within budget (cost-3 archetype, budget 10 -> 3).
        spawnSystem.Populate(budget, floor);
        int spawned = spawnSystem.AliveCount();
        Check(spawned > 0, "Populate spawned enemies");
        Check(spawned == 3, "budget respected: 3 x cost-3 <= 10 (AliveCount=" + spawned + ")");

        // 5. Spawned enemies are at SpawnPoints.
        SpawnPoint[] points = spawnSystem.GetComponentsInChildren<SpawnPoint>(true);
        Check(points.Length > 0, "spawn points present");
        TestEnemy[] enemies = FindObjectsOfType<TestEnemy>();
        Check(enemies.Length == spawned, "one TestEnemy per alive entry");
        foreach (TestEnemy e in enemies)
        {
            bool atPoint = false;
            foreach (SpawnPoint p in points)
                if (Vector3.Distance(e.transform.position, p.Position) < 0.01f) { atPoint = true; break; }
            Check(atPoint, "TestEnemy spawned at a SpawnPoint");
        }

        // 5b. Each enemy sits on its own SpawnPoint (fix for stacked/overlapping spawns).
        bool distinct = true;
        for (int i = 0; i < enemies.Length && distinct; i++)
            for (int j = i + 1; j < enemies.Length && distinct; j++)
                if (Vector3.Distance(enemies[i].transform.position, enemies[j].transform.position) < 0.01f)
                    distinct = false;
        Check(distinct, "no two enemies overlap on the same SpawnPoint");

        // 6. Real Run transition after spawns are placed.
        Check(run.BeginFloor(), "BeginFloor() -> FloorActive");
        Check(run.CurrentState == RunState.FloorActive, "Run state is FloorActive after populate");

        // 7. Floor 1 => no scaling.
        foreach (TestEnemy e in enemies)
            Check(Mathf.Approximately(e.Health, 10f), "floor 1 health unscaled");

        // 8. INTEGRATION: clearing the whole floor auto-advances Floor 1 -> 2 -> 3 via the live
        //    SpawnSystem.FloorCleared event. The transient FloorCleared state (AliveCount 0 +
        //    FloorCleared) is observable during the short pause before the next floor populates.
        yield return ClearFloorAndVerifyAdvance(run, expectedFloor: 2);
        yield return ClearFloorAndVerifyAdvance(run, expectedFloor: 3);

        // 9. Manual-play floor: reset the run to a clean Floor 1 and leave enemies for the player.
        //    The live FloorCleared bridge stays subscribed, so manual kills keep advancing floors.
        run.Reset();
        Check(run.CurrentState == RunState.Lobby, "manual play: Reset() -> Lobby");
        Check(run.StartRun(), "manual play: StartRun() -> FloorStart");
        spawnSystem.Populate(run.Data.enemyBudget, run.Data.floor);
        Check(run.Data.floor == 1 && Mathf.Approximately(run.Data.enemyBudget, 10f), "manual play: reset to Floor 1, budget 10");
        Check(spawnSystem.AliveCount() == 3, "manual play: floor 1 populated with 3 enemies");
        Check(run.BeginFloor(), "manual play: BeginFloor() -> FloorActive");
        Check(run.CurrentState == RunState.FloorActive, "manual play: run active on floor 1");

        LogSummary();
    }

    /// <summary>
    /// Kill every enemy currently alive, verify the transient FloorCleared state (AliveCount 0 +
    /// RunState.FloorCleared — only reachable via a real all-clear), then wait for the bridge to
    /// populate the next floor and verify the advanced run. The wait matches floorClearPauseSeconds
    /// (+ a frame buffer) so Unity Play Mode sees the same FloorCleared state the checks assert.
    /// </summary>
    IEnumerator ClearFloorAndVerifyAdvance(RunController run, int expectedFloor)
    {
        int prevFloor = run.Data.floor;
        float prevBudget = run.Data.enemyBudget;
        TestEnemy[] enemies = FindObjectsOfType<TestEnemy>();

        int alive = enemies.Length;
        foreach (TestEnemy e in enemies)
        {
            e.Die();
            alive--;
            Check(spawnSystem.AliveCount() == alive, $"floor {prevFloor}: AliveCount decremented to {alive}");
        }

        Check(spawnSystem.AliveCount() == 0, $"floor {prevFloor}: AliveCount == 0 after all enemies die");
        Check(spawnSystem.IsFloorCleared, $"floor {prevFloor}: SpawnSystem reports floor cleared");
        Check(run.CurrentState == RunState.FloorCleared, $"floor {prevFloor}: Run entered FloorCleared (no restart loop)");

        yield return new WaitForSeconds(floorClearPauseSeconds + 0.1f);

        int expectedCount = (int)(run.Data.enemyBudget / 3f);              // cost-3 test archetype
        float expectedHealth = Mathf.Pow(1.12f, expectedFloor - 1) * 10f;  // archetype growth 0.12, base 10
        Check(run.CurrentFloor == expectedFloor, $"advance: floor {prevFloor} -> {expectedFloor} (FloorCleared -> FloorStart + AdvanceFloor)");
        Check(run.CurrentState == RunState.FloorActive, $"advance: next floor active on {expectedFloor}");
        Check(Mathf.Approximately(run.Data.enemyBudget, prevBudget * 1.4f), $"advance: enemyBudget scaled x1.4 ({prevBudget} -> {run.Data.enemyBudget})");
        Check(spawnSystem.AliveCount() == expectedCount, $"advance: floor {expectedFloor} populated with {expectedCount} enemies");
        Check(!spawnSystem.IsFloorCleared, $"advance: floor {expectedFloor} is live (not instantly cleared)");
        foreach (TestEnemy e in FindObjectsOfType<TestEnemy>())
            Check(Mathf.Approximately(e.Health, expectedHealth), $"advance: floor {expectedFloor} health scaled to {expectedHealth}");
    }

    /// <summary>
    /// Test-only bridge: SpawnSystem reports "current floor cleared"; this asks the REAL RunController
    /// to handle it. The next floor starts AFTER a short visible pause so the FloorCleared state is
    /// observable, using the state machine's FloorCleared -> FloorStart -> FloorActive chain.
    /// </summary>
    void OnFloorCleared()
    {
        if (!spawnSystem.IsFloorCleared) return;          // report only a real all-clear
        if (!Run.CompleteFloor()) return;                 // FloorActive -> FloorCleared (guarded)
        StartCoroutine(AdvanceToNextFloor());
    }

    IEnumerator AdvanceToNextFloor()
    {
        yield return new WaitForSeconds(floorClearPauseSeconds);
        Run.StartNextFloor();                             // FloorCleared -> FloorStart + AdvanceFloor
        spawnSystem.Populate(Run.Data.enemyBudget, Run.Data.floor);
        Run.BeginFloor();                                 // FloorStart -> FloorActive
    }

    void Check(bool condition, string label)
    {
        checks++;
        if (!condition) Fail(label);
        else Debug.Log("PASS: " + label);
    }

    void Fail(string label)
    {
        failures.Add(label);
        Debug.LogWarning("FAIL: " + label);
    }

    void LogSummary()
    {
        Debug.Log(failures.Count == 0
            ? $"[SpawnSystemTest] ALL {checks} CHECKS PASSED"
            : $"[SpawnSystemTest] {failures.Count}/{checks} FAILED: " + string.Join(" | ", failures));
    }
}
