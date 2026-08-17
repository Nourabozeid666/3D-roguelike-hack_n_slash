using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

class Program
{
    static int checks;
    static readonly List<string> failures = new();

    static void Main()
    {
        HeightFixScenario();
        TouchKillScenario();
        NonPlayerTouchScenario();
        FloorClearingScenario();
        BudgetScenario();
        DistinctSpawnPointScenario();
        DebugDisplayScenario();
        RunInfoScenario();
        DriverIntegrationScenario();
        FloorUnlockScenario();
        CompositionRankingScenario();
        CompositionSystemScenario();
        CompositionCacheScenario();
        CompositionProgressionScenario();
        SaveServiceScenario();
        SaveInvalidScenario();
        SaveMainMenuScenario();
        SaveBootstrapScenario();
        PlacementPipelineScenario();
        RandomZoneSpawnScenario();
        WavePlanScenario();
        WaveFloorIntegrationScenario();
        WaveCompositionPreservedScenario();
        PlayerHudDataScenario();
        HudChainScenario();
        UpgradeSelectScenario();
        UpgradeOfferDataScenario();
        GameOverScenario();
        GameOverRunTimeScenario();
        SpawnStatConfigScenario();
        DeathIdempotencyScenario();
        RuntimeDamageConfigScenario();
        OnDiedLethalDamageScenario();
        ExplosionDeathFlowScenario();

        Console.WriteLine(failures.Count == 0
            ? $"[SpawnIntegration] ALL {checks} CHECKS PASSED"
            : $"[SpawnIntegration] {failures.Count}/{checks} FAILED: {string.Join(" | ", failures)}");

        Environment.ExitCode = failures.Count == 0 ? 0 : 1;
    }

    // 1. Feet-pivot height fix + root trigger collider.
    static void HeightFixScenario()
    {
        var go = new GameObject("TestEnemy");
        var te = go.AddComponent<TestEnemy>();
        Invoke(te, "Awake");

        Check(go.transform.childCount == 1, "height: runtime Body child created");

        Transform body = go.transform.GetChild(0);
        var bodyCol = body.GetComponent<CapsuleCollider>();
        Check(bodyCol != null, "height: Body has CapsuleCollider");
        float footOffset = bodyCol.height * 0.5f + bodyCol.center.y;
        Check(Mathf.Approximately(body.localPosition.y, footOffset), "height: Body lifted by collider-derived footOffset");
        Check(Mathf.Approximately(footOffset, 1f), "height: footOffset == 1 for default capsule (height 2, center 0)");
        Check(bodyCol.isTrigger, "touch: Body collider is trigger (no physics push)");

        var rootCol = go.GetComponent<Collider>();
        Check(rootCol != null, "touch: root trigger collider added at runtime");
        Check(rootCol is CapsuleCollider, "touch: root collider is a CapsuleCollider");
        Check(rootCol.isTrigger, "touch: root collider is trigger");
        var rootCap = (CapsuleCollider)rootCol;
        Check(rootCap.radius == bodyCol.radius && Mathf.Approximately(rootCap.height, bodyCol.height), "touch: root collider dims match Body");
        Check(Mathf.Approximately(rootCap.center.y, footOffset), "touch: root collider center at footOffset");
    }

    // 2. OnTriggerEnter with a Player-tagged ancestor kills (mirrors PlayerObj child under Player root).
    static void TouchKillScenario()
    {
        var playerRoot = new GameObject("Player");
        playerRoot.tag = "Player";
        var playerChild = new GameObject("PlayerObj");
        playerChild.transform.SetParent(playerRoot.transform, false);
        Check(playerChild.tag == "Untagged", "touch: PlayerObj collider object is Untagged (as in TestingScene)");
        var playerCol = playerChild.AddComponent<CapsuleCollider>();

        var go = new GameObject("TestEnemy");
        var te = go.AddComponent<TestEnemy>();
        Invoke(te, "Awake");
        bool died = false;
        te.OnDied += () => died = true;

        Invoke(te, "OnTriggerEnter", playerCol);
        Check(died, "touch: OnTriggerEnter from Player hierarchy kills the enemy");
    }

    // 3. Non-Player touch does not kill.
    static void NonPlayerTouchScenario()
    {
        var other = new GameObject("Enemy");
        other.tag = "Enemy";
        var col = other.AddComponent<CapsuleCollider>();

        var go = new GameObject("TestEnemy");
        var te = go.AddComponent<TestEnemy>();
        Invoke(te, "Awake");
        bool died = false;
        te.OnDied += () => died = true;

        Invoke(te, "OnTriggerEnter", col);
        Check(!died, "touch: non-Player collider does NOT kill");
    }

    // 4. SpawnSystem integration: 3 capsules, AliveCount 3 -> 2 -> 1 -> 0, IsFloorCleared.
    static void FloorClearingScenario()
    {
        UnityEngine.Object.Clones.Clear();

        var prefab = new GameObject("TestEnemy");
        prefab.AddComponent<TestEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);

        var pointPositions = new List<Vector3>();
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), pointPositions);
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), pointPositions);
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), pointPositions);

        Check(sys.AliveCount() == 0, "clear: AliveCount == 0 before populate");

        sys.Populate(9f, 1);
        Check(sys.AliveCount() == 3, "clear: 3 spawned (budget 9, cost-3 archetype)");
        Check(!sys.IsFloorCleared, "clear: not cleared while 3 alive");
        Check(UnityEngine.Object.Clones.Count == 3, "clear: exactly 3 enemies instantiated");

        foreach (GameObject clone in UnityEngine.Object.Clones)
        {
            bool atPoint = pointPositions.Any(p => Vector3.Distance(clone.transform.position, p) < 0.01f);
            Check(atPoint, $"clear: enemy spawned at a SpawnPoint ({clone.transform.position})");

            var te = clone.GetComponent<TestEnemy>();
            Check(Mathf.Approximately(te.Health, 10f), "clear: floor 1 health unscaled (base 10)");

            var body = clone.transform.GetChild(0);
            var bodyCol = body.GetComponent<CapsuleCollider>();
            float bottom = body.transform.position.y - bodyCol.height * 0.5f;
            Check(Mathf.Approximately(bottom, clone.transform.position.y),
                $"clear: capsule bottom ({bottom}) == spawn feet ({clone.transform.position.y}) -> on floor");
        }

        int expected = 3;
        foreach (GameObject clone in UnityEngine.Object.Clones)
        {
            clone.GetComponent<TestEnemy>().Die();
            expected--;
            Check(sys.AliveCount() == expected, $"clear: AliveCount decremented to {expected} after Die()");
        }

        Check(sys.AliveCount() == 0, "clear: AliveCount == 0 after all die");
        Check(sys.IsFloorCleared, "clear: SpawnSystem.IsFloorCleared == true after all die");

        sys.Populate(9f, 1);
        Check(sys.AliveCount() == 3, "clear: repopulate after clear works");
    }

    // 5. Budget respected: budget 10 -> exactly 3 (3x3 <= 10, 4x3 > 10).
    static void BudgetScenario()
    {
        UnityEngine.Object.Clones.Clear();

        var prefab = new GameObject("TestEnemy");
        prefab.AddComponent<TestEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), new List<Vector3>());

        sys.Populate(10f, 1);
        Check(sys.AliveCount() == 3, "budget: RunData-style budget 10 -> 3 enemies, total 9 <= 10");
    }

    // 6. Fix: a single Populate pass must not stack two enemies on the same SpawnPoint.
    static void DistinctSpawnPointScenario()
    {
        UnityEngine.Object.ResetWorld();

        var prefab = new GameObject("TestEnemy");
        prefab.AddComponent<TestEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(-9.7f, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, -18.56f), new List<Vector3>());

        for (int pass = 0; pass < 50; pass++)
        {
            foreach (GameObject c in UnityEngine.Object.Clones) UnityEngine.Object.Destroy(c);
            UnityEngine.Object.Clones.Clear();
            sys.Populate(9f, 1);
            Check(sys.AliveCount() == 3, "distinct: 3 spawned in pass " + pass);
            var positions = UnityEngine.Object.Clones
                .Select(c => c.transform.position)
                .Distinct()
                .Count();
            Check(positions == 3, $"distinct: 3 unique positions in pass {pass} (got {positions})");
            foreach (GameObject c in UnityEngine.Object.Clones) c.GetComponent<TestEnemy>().Die();
        }
    }

    // 7. Test-only debug display tracks Spawned/Alive/Dead/FloorCleared from real state.
    static void DebugDisplayScenario()
    {
        UnityEngine.Object.ResetWorld();
        UnityEngine.SceneManagement.SceneManager.activeSceneName = "TestingScene";

        var prefab = new GameObject("TestEnemy");
        prefab.AddComponent<TestEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), new List<Vector3>());

        var displayGo = new GameObject("SpawnTestDebugDisplay");
        var display = displayGo.AddComponent<SpawnTestDebugDisplay>();
        SetField(display, "spawnSystem", sys);
        Invoke(display, "Awake");
        Invoke(display, "Start");

        sys.Populate(9f, 1);
        Invoke(display, "Update");
        Check(sys.AliveCount() == 3, "display: 3 alive after populate");
        Check(GetField<int>(display, "spawned") == 3, "display: spawned == 3 from observed TestEnemies");
        Check(GetField<int>(display, "dead") == 0, "display: dead == 0 before any death");

        string hudAfterSpawn = GetProperty<string>(display, "CurrentHudText");
        Check(hudAfterSpawn.Contains("SPAWN TEST"), "display: HUD header present");
        Check(hudAfterSpawn.Contains("Scene: TestingScene"), "display: HUD shows active scene name");
        Check(hudAfterSpawn.Contains("Spawned: 3"), "display: HUD shows Spawned: 3");
        Check(hudAfterSpawn.Contains("Alive: 3"), "display: HUD shows Alive: 3");
        Check(hudAfterSpawn.Contains("Dead: 0"), "display: HUD shows Dead: 0");
        Check(hudAfterSpawn.Contains("Floor Cleared: NO"), "display: HUD shows Floor Cleared NO while alive");
        Check(hudAfterSpawn.Contains("Run: n/a"), "display: Run state n/a when no driver present");
        Check(hudAfterSpawn.Contains("Comp: floor 1"), "display: HUD shows composition info line after populate");

        foreach (GameObject c in UnityEngine.Object.Clones) c.GetComponent<TestEnemy>().Die();
        Invoke(display, "Update");
        Check(GetField<int>(display, "dead") == 3, "display: dead == 3 after all enemies die");
        Check(GetField<bool>(display, "wasCleared"), "display: wasCleared set when IsFloorCleared == true");
        string hudAfterClear = GetProperty<string>(display, "CurrentHudText");
        Check(hudAfterClear.Contains("Alive: 0"), "display: HUD shows Alive: 0 after all die");
        Check(hudAfterClear.Contains("Dead: 3"), "display: HUD shows Dead: 3 after all die");
        Check(hudAfterClear.Contains("Floor Cleared: YES"), "display: HUD shows Floor Cleared YES after all die");
    }

    // 8. HUD reads live Run state/floor from the driver's real RunController.
    static void RunInfoScenario()
    {
        UnityEngine.Object.ResetWorld();
        UnityEngine.SceneManagement.SceneManager.activeSceneName = "TestingScene";

        var prefab = new GameObject("TestEnemy");
        prefab.AddComponent<TestEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), new List<Vector3>());

        var driverGo = new GameObject("SpawnSystemTestDriver");
        var driver = driverGo.AddComponent<SpawnSystemTestDriver>();
        SetField(driver, "spawnSystem", sys);

        var displayGo = new GameObject("SpawnTestDebugDisplay");
        var display = displayGo.AddComponent<SpawnTestDebugDisplay>();
        SetField(display, "spawnSystem", sys);
        Invoke(display, "Awake");
        Invoke(display, "Start");

        Check(driver.Run != null, "runinfo: driver exposes a live RunController");
        Check(driver.Run.CurrentState == RunState.Lobby, "runinfo: initial RunState is Lobby");

        driver.Run.StartRun();
        Invoke(display, "Update");
        Check(driver.Run.CurrentState == RunState.FloorStart, "runinfo: StartRun -> FloorStart");
        string hudStart = GetProperty<string>(display, "CurrentHudText");
        Check(hudStart.Contains("Run state: FloorStart"), "runinfo: HUD shows FloorStart after StartRun");
        Check(hudStart.Contains("Floor: 1"), "runinfo: HUD shows Floor 1 from RunData");
        Check(!hudStart.Contains("Run: 1"), "runinfo: HUD does NOT invent a run number (no such field)");

        sys.Populate(9f, 1);
        driver.Run.BeginFloor();
        Invoke(display, "Update");
        Check(driver.Run.CurrentState == RunState.FloorActive, "runinfo: BeginFloor -> FloorActive");
        string hudActive = GetProperty<string>(display, "CurrentHudText");
        Check(hudActive.Contains("Run state: FloorActive"), "runinfo: HUD shows FloorActive after BeginFloor");
        Check(hudActive.Contains("Spawned: 3"), "runinfo: HUD shows Spawned: 3 after populate");
        Check(hudActive.Contains("Alive: 3"), "runinfo: HUD shows Alive: 3 after populate");
    }

    // 9. Run <-> Spawn integration: pumps the REAL driver's Start() coroutine against the real
    //    SpawnSystem + RunController. Floors auto-advance 1 -> 2 -> 3 on clear via the live
    //    FloorCleared bridge, then the driver resets to a manual-play Floor 1 whose live bridge
    //    still advances on kill.
    static void DriverIntegrationScenario()
    {
        UnityEngine.Object.ResetWorld();
        UnityEngine.SceneManagement.SceneManager.activeSceneName = "TestingScene";
        UnityEngine.MonoBehaviour.PendingCoroutines.Clear();

        var prefab = new GameObject("TestEnemy");
        prefab.AddComponent<TestEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), new List<Vector3>());

        var driverGo = new GameObject("SpawnSystemTestDriver");
        var driver = driverGo.AddComponent<SpawnSystemTestDriver>();
        SetField(driver, "spawnSystem", sys);

        // Pump the driver's Start() coroutine (real code). The stub's StartCoroutine queues the
        // bridge's advance coroutine; the pump drains it each time Start() yields, so the floor
        // advance runs exactly where Unity would run it (after the FloorCleared pause).
        var driverRun = (System.Collections.IEnumerator)InvokeReturn(driver, "Start");
        while (driverRun.MoveNext()) UnityEngine.MonoBehaviour.RunPendingCoroutines();
        UnityEngine.MonoBehaviour.RunPendingCoroutines();

        Check(driver.Run.CurrentState == RunState.FloorActive, "integration: driver leaves run active after scripted checks");
        Check(driver.Run.CurrentFloor == 1, "integration: manual-play floor is 1 after reset");
        Check(driver.Run.Data.floor == 1 && driver.Run.Data.enemyBudget == 10f, "integration: manual-play RunData reset to floor 1 / budget 10");
        Check(sys.AliveCount() == 3, "integration: manual-play floor 1 populated with 3 enemies");

        // The live bridge stays subscribed: killing the manual-play floor auto-advances to floor 2.
        foreach (TestEnemy te in UnityEngine.Object.FindObjectsOfType<TestEnemy>()) te.Die();
        UnityEngine.MonoBehaviour.RunPendingCoroutines();
        Check(driver.Run.CurrentFloor == 2, "integration: live FloorCleared bridge advances manual play to floor 2");
        Check(driver.Run.CurrentState == RunState.FloorActive, "integration: floor 2 active after live bridge");
        Check(Mathf.Approximately(driver.Run.Data.enemyBudget, 14f), "integration: floor 2 budget scaled x1.4 (10 -> 14)");
        Check(sys.AliveCount() == 4, "integration: floor 2 populated with 4 enemies (budget 14, cost 3)");
        Check(!sys.IsFloorCleared, "integration: floor 2 live (not instantly cleared)");
    }

    // 10. Floor-based pool unlock: index i unlocks at floor 1 + i*interval (default 3).
    static void FloorUnlockScenario()
    {
        UnityEngine.Object.ResetWorld();

        var table = MakeTable(new[] { 3, 7 }, 3);
        Check(table.Archetypes.Count == 2, "unlock: pool has 2 archetypes");
        Check(table.AvailableForFloor(1).Count == 1, "unlock: floor 1 -> 1 type unlocked");
        Check(table.AvailableForFloor(3).Count == 1, "unlock: floor 3 -> still 1 type (interval 3)");
        Check(table.AvailableForFloor(4).Count == 2, "unlock: floor 4 -> 2 types unlocked");
        Check(table.AvailableForFloor(6).Count == 2, "unlock: floor 6 -> still 2 types");
        Check(table.AvailableForFloor(7).Count == 2, "unlock: floor 7 -> 2 types (only 2 exist)");
        Check(table.AvailableForFloor(1)[0].Cost == 3, "unlock: floor 1 pool is the first archetype (E1)");

        var fast = MakeTable(new[] { 3, 7 }, 1);
        Check(fast.AvailableForFloor(1).Count == 1, "unlock: interval 1 -> 1 type at floor 1 (index 0)");
        Check(fast.AvailableForFloor(2).Count == 2, "unlock: interval 1 -> 2 types at floor 2 (one per floor)");
    }

    // 11. Composition ranking (solver level): budget is a MAXIMUM; satisfy the target count; prefer
    //     best budget use; variety only among equally-ranked; controlled randomness last.
    static void CompositionRankingScenario()
    {
        UnityEngine.Object.ResetWorld();

        // Budget-max: 3+3+3 = 9 is the only valid way to spend budget 10 on exactly 3 enemies.
        var pool3 = MakePool(new[] { 3 });
        var sel = new EnemyCompositionSelector();
        var c3 = sel.Get(1, pool3, 3, 10f);
        Check(c3.Count == 1, "rank: single cost-3 pool, budget 10, target 3 -> 1 candidate");
        Check(c3[0].TotalCost == 9, "rank: 3+3+3 = 9 <= 10 (budget is max, not exact)");
        Check(c3[0].Count == 3, "rank: exactly 3 enemies (target count satisfied)");

        // Invalid: 3+3+7 = 13 must be rejected with budget 12.
        var pool37 = MakePool(new[] { 3, 7 });
        var ci = sel.Get(4, pool37, 3, 12f);
        Check(ci.Count == 1, "rank: pool {3,7}, budget 12, target 3 -> only 3+3+3 survives");
        Check(ci[0].TotalCost == 9, "rank: 3+3+7=13 rejected (>12); selected total 9");

        // Best budget use: 3+3+4 = 10 beats 3+3+3 = 9.
        var pool34 = MakePool(new[] { 3, 4 });
        var cb = sel.Get(4, pool34, 3, 10f);
        Check(cb.Count == 1, "rank: pool {3,4}, budget 10, target 3 -> one winner");
        Check(cb[0].TotalCost == 10, "rank: best budget use 10 (3+3+4) beats 9 (3+3+3)");
        Check(cb[0].DistinctTypes == 2, "rank: winner uses both archetypes (3,3,4)");

        // Variety tie-break: budget 12, target 3 -> (3,4,5)=12 and (4,4,4)=12 tie on cost; the
        // most distinct types wins (3,4,5) without ever exceeding the budget.
        var pool345 = MakePool(new[] { 3, 4, 5 });
        var cv = sel.Get(1, pool345, 3, 12f);
        Check(cv.Count == 1, "rank: pool {3,4,5}, budget 12, target 3 -> one winner after variety tie-break");
        Check(cv[0].TotalCost == 12, "rank: variety winner totals 12 (<= 12)");
        Check(cv[0].DistinctTypes == 3, "rank: variety winner uses 3 distinct types (3,4,5) beats (4,4,4)");

        // Controlled randomness only between equally-ranked candidates: budget 13 -> (3,5,5)=13 and
        // (4,4,5)=13 both have 2 distinct types; both are offered and each stays valid.
        var cr = sel.Get(1, pool345, 3, 13f);
        Check(cr.Count == 2, "rank: two equally-ranked candidates for budget 13");
        foreach (var cand in cr)
        {
            Check(cand.TotalCost == 13, "rank: random-pool candidate totals 13");
            Check(cand.DistinctTypes == 2, "rank: random-pool candidate uses 2 distinct types");
        }
    }

    // 12. Composition selection through Populate: unlock + best-budget composition end-to-end.
    static void CompositionSystemScenario()
    {
        UnityEngine.Object.ResetWorld();

        var table = MakeTable(new[] { 3, 4 }, 3);
        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), new List<Vector3>());

        // Floor 1: only E1 (cost 3) unlocked -> target 3 -> 3xE1 = 9 <= 10.
        sys.Populate(10f, 1);
        Check(sys.AliveCount() == 3, "sys: floor 1 spawns 3 enemies (target 3)");
        Check(sys.LastCompositionInfo.Contains("cost 9/10"), "sys: floor 1 total 9 <= 10");
        Check(SpawnedNames() == "E1,E1,E1", "sys: floor 1 composition is all E1 (E2 locked)");

        // Floor 4: E1 + E2 unlocked -> target 3 -> best budget use 3+3+4 = 10.
        sys.Populate(10f, 4);
        Check(sys.AliveCount() == 3, "sys: floor 4 spawns 3 enemies");
        Check(sys.LastCompositionInfo.Contains("cost 10/10"), "sys: floor 4 best budget use 10");
        Check(SpawnedNames() == "E1,E1,E2", "sys: floor 4 composition 3+3+4");
    }

    // 13. Composition cache: one key per (floor, target, budget); repeated Populates reuse it.
    static void CompositionCacheScenario()
    {
        UnityEngine.Object.ResetWorld();

        var pool = MakePool(new[] { 3, 4 });
        var sel = new EnemyCompositionSelector();
        var first = sel.Get(1, pool, 3, 10f);
        Check(sel.CachedKeyCount == 1, "cache: first Get caches one key");
        var second = sel.Get(1, pool, 3, 10f);
        Check(ReferenceEquals(first, second), "cache: same key returns the same cached instance");
        Check(sel.CachedKeyCount == 1, "cache: repeat Get does not add a key");
        sel.Get(4, pool, 3, 10f);
        Check(sel.CachedKeyCount == 2, "cache: a different floor adds a key (pool changed)");
        sel.Get(1, pool, 3, 10f);
        Check(sel.CachedKeyCount == 2, "cache: revisiting floor 1 reuses its key");

        var table = MakeTable(new[] { 3, 4 }, 3);
        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), new List<Vector3>());

        sys.Populate(10f, 1);
        int keysAfterFirst = sys.CachedCompositionKeys;
        sys.Populate(10f, 1);
        Check(sys.CachedCompositionKeys == keysAfterFirst, "cache: two Populates on same floor reuse the cached key");
        Check(sys.CachedCompositionKeys == 1, "cache: one key after two same-floor Populates");
    }

    // 14. Floor progression through the unlock boundary with the real budget growth: floors 1-3 use
    //     only E1; floor 4 unlocks E2 but does NOT force it; E2 appears only when the budget has room.
    static void CompositionProgressionScenario()
    {
        UnityEngine.Object.ResetWorld();

        var table = MakeTable(new[] { 3, 4 }, 3);
        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        for (int i = 0; i < 9; i++)
            AddPoint(sysGo, "SpawnPoint (" + i + ")", new Vector3(i, 0.5f, i), new List<Vector3>());

        float budget = 10f;
        for (int floor = 1; floor <= 3; floor++)
        {
            sys.Populate(budget, floor);
            int expected = (int)(budget / 3f);
            Check(sys.AliveCount() == expected, $"progression: floor {floor} spawns {expected} enemies (budget {budget})");
            Check(sys.LastCompositionInfo.Contains("available: E1") && !sys.LastCompositionInfo.Contains("E2"),
                $"progression: floor {floor} pool has only E1 (E2 locked)");
            foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
            budget *= 1.4f;
        }
        Check(sys.AliveCount() == 0, "progression: floors 1-3 cleared");

        // Floor 4: budget 10*1.4^3 = 27.44, pool {E1,E2}, target 9 -> best composition is 9xE1 (27);
        // E2 is unlocked but a 28 (any E2) would exceed 27.44, so it is NOT forced in.
        sys.Populate(budget, 4);
        Check(sys.AliveCount() == 9, "progression: floor 4 spawns 9 enemies (target 9)");
        Check(sys.LastCompositionInfo.Contains("available: E1,E2"), "progression: floor 4 pool unlocked E2");
        Check(SpawnedNames() == "E1,E1,E1,E1,E1,E1,E1,E1,E1", "progression: floor 4 does not force E2 (9xE1 = 27 <= 27.44)");

        // E2 appears when the budget has room: 8xE1 + 1xE2 = 28 <= 28.
        foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
        sys.Populate(28f, 4);
        Check(sys.AliveCount() == 9, "progression: floor 4 @ budget 28 spawns 9 enemies");
        Check(SpawnedNames() == "E1,E1,E1,E1,E1,E1,E1,E1,E2", "progression: budget 28 admits one E2 (8xE1+1xE2 = 28)");
    }

    // 15. Save service: real JSON file on disk via persistentDataPath; New Run / floor-start writes.
    static void SaveServiceScenario()
    {
        string dir = Path.Combine(Application.persistentDataPath, "save_unit");
        Directory.CreateDirectory(dir);
        var saves = new RunSaveService(Path.Combine(dir, "run_save_v1.json"));

        saves.Delete();
        Check(!saves.HasSave(), "save: no save after Delete");

        Check(saves.Save(new SaveData { floor = 1, clearedRooms = 0, enemyBudget = 10f, enemyBudgetGrowth = 1.4f, enemyStatGrowth = 1.12f }), "save: Save returns true");
        Check(saves.HasSave(), "save: HasSave true after Save");
        SaveData loaded;
        Check(saves.TryLoad(out loaded), "save: TryLoad succeeds");
        Check(loaded.floor == 1 && Mathf.Approximately(loaded.enemyBudget, 10f), "save: loaded floor/budget match saved");

        float budget7 = 10f * (float)Math.Pow(1.4f, 6);
        Check(saves.Save(new SaveData { floor = 7, clearedRooms = 3, enemyBudget = budget7, enemyBudgetGrowth = 1.4f, enemyStatGrowth = 1.12f }), "save: floor 7 Save");
        Check(saves.TryLoad(out loaded) && loaded.floor == 7 && loaded.clearedRooms == 3, "save: floor 7 round-trips");
        Check(Mathf.Approximately(loaded.enemyBudget, budget7), "save: floor 7 budget round-trips");

        saves.Delete();
        Check(!saves.HasSave(), "save: deleted again");
    }

    // 16. Corrupt/invalid saves: treated as no save, bad file removed, nothing fabricated.
    static void SaveInvalidScenario()
    {
        string dir = Path.Combine(Application.persistentDataPath, "save_invalid");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "run_save_v1.json");
        var saves = new RunSaveService(path);

        File.WriteAllText(path, "{\"not\":\"json");
        SaveData s;
        Check(!saves.TryLoad(out s) && s == null, "invalid: corrupt json -> TryLoad false, data null");
        Check(!File.Exists(path), "invalid: corrupt file removed");
        Check(!saves.HasSave(), "invalid: no save after corrupt");

        File.WriteAllText(path, "{\"version\":999,\"floor\":1,\"clearedRooms\":0,\"enemyBudget\":10,\"enemyBudgetGrowth\":1.4,\"enemyStatGrowth\":1.12}");
        Check(!saves.TryLoad(out s), "invalid: wrong version rejected");
        Check(!File.Exists(path), "invalid: wrong-version file removed");

        File.WriteAllText(path, "{\"version\":1,\"floor\":0,\"clearedRooms\":0,\"enemyBudget\":10,\"enemyBudgetGrowth\":1.4,\"enemyStatGrowth\":1.12}");
        Check(!saves.TryLoad(out s), "invalid: floor 0 rejected");
        Check(!File.Exists(path), "invalid: floor 0 file removed");

        File.WriteAllText(path, "{\"version\":1,\"floor\":2,\"clearedRooms\":2,\"enemyBudget\":10,\"enemyBudgetGrowth\":1.4,\"enemyStatGrowth\":1.12}");
        Check(!saves.TryLoad(out s), "invalid: clearedRooms >= floor rejected");
        Check(!File.Exists(path), "invalid: bad clearedRooms file removed");

        File.WriteAllText(path, "{\"version\":1,\"floor\":1,\"clearedRooms\":0,\"enemyBudget\":0,\"enemyBudgetGrowth\":1.4,\"enemyStatGrowth\":1.12}");
        Check(!saves.TryLoad(out s), "invalid: zero budget rejected");
        Check(!File.Exists(path), "invalid: zero-budget file removed");

        File.WriteAllText(path, "{\"version\":1,\"floor\":3,\"clearedRooms\":1,\"enemyBudget\":19.6,\"enemyBudgetGrowth\":1.4,\"enemyStatGrowth\":1.12}");
        Check(saves.TryLoad(out s) && s.floor == 3, "invalid: valid floor 3 save still accepted after all rejections");
    }

    // 17. Main Menu: Continue/New Run driven by the REAL save; disabled when there is no save.
    static void SaveMainMenuScenario()
    {
        UnityEngine.Object.ResetWorld();
        RunSession.EnterFromMenu = false;
        UnityEngine.SceneManagement.SceneManager.lastLoadedScene = null;

        // Point persistentDataPath at a dedicated folder so the controller's default-path service
        // and the test's service resolve to the SAME file (no cross-scenario contamination).
        Application.persistentDataPath = Path.Combine(Path.GetTempPath(), "opencode", "pd_menu");
        Directory.CreateDirectory(Application.persistentDataPath);
        var saves = new RunSaveService();
        saves.Delete();

        var continueBtn = MakeUiButton("ContinueButton", "CONTINUE");
        var newRunBtn = MakeUiButton("NewRunButton", "NEW RUN");
        var settingsBtn = MakeUiButton("SettingsButton", "SETTINGS");
        var quitBtn = MakeUiButton("QuitButton", "QUIT");
        var panel = new GameObject("SettingsPanel");

        var go = new GameObject("MainMenuController");
        var ctrl = go.AddComponent<MainMenuController>();
        SetField(ctrl, "continueButton", continueBtn.GetComponent<Button>());
        SetField(ctrl, "newRunButton", newRunBtn.GetComponent<Button>());
        SetField(ctrl, "settingsButton", settingsBtn.GetComponent<Button>());
        SetField(ctrl, "quitButton", quitBtn.GetComponent<Button>());
        SetField(ctrl, "settingsPanel", panel);
        Invoke(ctrl, "Awake");
        Invoke(ctrl, "Start");

        Check(!continueBtn.GetComponent<Button>().interactable, "menu: Continue disabled with no save");
        Check(continueBtn.GetComponentInChildren<Text>().text == "CONTINUE", "menu: Continue label plain when no save");

        ctrl.Continue();
        Check(UnityEngine.SceneManagement.SceneManager.lastLoadedScene == null, "menu: Continue without save does NOT load the scene");

        ctrl.StartNewRun();
        Check(UnityEngine.SceneManagement.SceneManager.lastLoadedScene == "TestingScene", "menu: New Run loads the game scene");
        Check(RunSession.EnterFromMenu, "menu: New Run sets EnterFromMenu for the scene bootstrap");
        RunSession.EnterFromMenu = false;
        UnityEngine.SceneManagement.SceneManager.lastLoadedScene = null;
        Check(!saves.HasSave(), "menu: New Run deleted the save");

        saves.Save(new SaveData { floor = 7, clearedRooms = 3, enemyBudget = 10f * (float)Math.Pow(1.4f, 6), enemyBudgetGrowth = 1.4f, enemyStatGrowth = 1.12f });
        ctrl.Refresh();
        Check(continueBtn.GetComponent<Button>().interactable, "menu: Continue enabled when a save exists");
        Check(continueBtn.GetComponentInChildren<Text>().text == "CONTINUE — FLOOR 7", "menu: Continue label shows real saved floor 7");

        ctrl.Continue();
        Check(UnityEngine.SceneManagement.SceneManager.lastLoadedScene == "TestingScene", "menu: Continue with save loads the game scene");
        RunSession.EnterFromMenu = false;
        UnityEngine.SceneManagement.SceneManager.lastLoadedScene = null;
    }

    // 18. RunBootstrap (production run owner): Continue resumes the saved floor, New Run starts floor 1
    //     with a checkpoint, and the FloorCleared bridge writes a checkpoint for each new floor start.
    static void SaveBootstrapScenario()
    {
        UnityEngine.Object.ResetWorld();
        UnityEngine.SceneManagement.SceneManager.activeSceneName = "TestingScene";
        UnityEngine.MonoBehaviour.PendingCoroutines.Clear();
        RunSession.EnterFromMenu = false;

        Application.persistentDataPath = Path.Combine(Path.GetTempPath(), "opencode", "pd_bootstrap");
        Directory.CreateDirectory(Application.persistentDataPath);
        var saves = new RunSaveService();
        saves.Delete();

        // No save -> New Run: floor 1 + initial checkpoint, then populated and live.
        var sys = BuildSystem();
        var bootGo = new GameObject("RunBootstrap");
        var boot = bootGo.AddComponent<RunBootstrap>();
        SetField(boot, "spawnSystem", sys);
        SetField(boot, "floorClearPauseSeconds", 0f);

        RunSession.EnterFromMenu = true;
        Invoke(boot, "Awake");
        Invoke(boot, "Start");
        RunSession.EnterFromMenu = false;

        Check(boot.Run.CurrentFloor == 1, "bootstrap: no save -> fresh run floor 1");
        Check(boot.Run.CurrentState == RunState.FloorActive, "bootstrap: floor 1 live after begin");
        Check(sys.AliveCount() == 3, "bootstrap: floor 1 populated (budget 10, cost-3 pool)");
        SaveData loaded;
        Check(saves.TryLoad(out loaded) && loaded.floor == 1, "bootstrap: initial checkpoint saved at floor 1");

        // Kill floor 1 -> bridge completes the floor -> floor 2 checkpoint written before it is played.
        foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
        UnityEngine.MonoBehaviour.RunPendingCoroutines();
        Check(boot.Run.CurrentFloor == 2, "bootstrap: FloorCleared bridge advanced to floor 2");
        Check(boot.Run.CurrentState == RunState.FloorActive, "bootstrap: floor 2 live after advance");
        Check(sys.AliveCount() == 4, "bootstrap: floor 2 populated (budget 14, cost-3 pool)");
        Check(saves.TryLoad(out loaded) && loaded.floor == 2, "bootstrap: floor 2 checkpoint saved at its start");

        // Quit + Continue: a fresh bootstrap resumes floor 2 from the save (state derived, repopulated).
        UnityEngine.Object.ResetWorld();
        UnityEngine.MonoBehaviour.PendingCoroutines.Clear();
        var sys2 = BuildSystem();
        var boot2Go = new GameObject("RunBootstrap2");
        var boot2 = boot2Go.AddComponent<RunBootstrap>();
        SetField(boot2, "spawnSystem", sys2);
        SetField(boot2, "floorClearPauseSeconds", 0f);

        RunSession.EnterFromMenu = true;
        Invoke(boot2, "Awake");
        Invoke(boot2, "Start");
        RunSession.EnterFromMenu = false;

        Check(boot2.Run.CurrentFloor == 2, "bootstrap: Continue resumes saved floor 2");
        Check(boot2.Run.CurrentState == RunState.FloorActive, "bootstrap: resumed run live after begin");
        Check(sys2.AliveCount() == 4, "bootstrap: resumed floor 2 repopulated fresh");
        Check(boot2.Run.Data.clearedRooms == 1, "bootstrap: clearedRooms 1 restored from save");
        Check(saves.HasSave(), "bootstrap: save file still present after resume (never rewritten on open)");
    }

    // 19. Placement pipeline (Sprint 7): SpawnPlacementValidator is pure + deterministic — bounds,
    //     blocking, NavMesh/ground, player/enemy distance, bounded max-attempts. Never hangs.
    static void PlacementPipelineScenario()
    {
        UnityEngine.Object.ResetWorld();

        var zone = MakeZone(new Vector3(10, 2, 10), Vector3.zero, navMesh: false);
        var validator = new SpawnPlacementValidator();

        // Candidate in/out of the rectangular zone.
        Check(validator.Contains(zone, zone.Center), "placement: zone center is inside bounds");
        Check(validator.Contains(zone, zone.Center + new Vector3(4.9f, 0, 4.9f)), "placement: near-corner point inside bounds");
        Check(!validator.Contains(zone, zone.Center + new Vector3(5.1f, 0, 0)), "placement: beyond +x edge is outside");
        Check(!validator.Contains(zone, zone.Center + new Vector3(0, 0, -5.1f)), "placement: beyond -z edge is outside");
        Check(!validator.Contains(zone, zone.Center + new Vector3(0, 1.1f, 0)), "placement: above size.y is outside");

        // Blocking layer: every candidate blocked -> fail after a bounded number of attempts.
        SetField(zone, "maxAttempts", 7);
        Vector3 loc;
        Check(!validator.TryFindLocation(zone, Vector3.zero, 0f, null, null, p => true, out loc),
            "placement: all-blocked -> rejected after bounded attempts (no infinite retry)");
        Check(!validator.TryFindLocation(zone, Vector3.zero, 0f, null, null, p => true, out loc),
            "placement: all-blocked again -> still rejected (repeatable)");

        // Invalid NavMesh / no ground validator -> rejected, never a fabricated result.
        SetField(zone, "useNavMeshValidation", true);
        Check(!validator.TryFindLocation(zone, Vector3.zero, 0f, null, null, null, out loc),
            "placement: navmesh on but no ground validator -> rejected");
        Check(!validator.TryFindLocation(zone, Vector3.zero, 0f, null, NeverGround, null, out loc),
            "placement: ground validator always fails -> rejected");
        SetField(zone, "useNavMeshValidation", false);

        // Valid candidate accepted inside bounds.
        Check(validator.TryFindLocation(zone, Vector3.zero, 0f, null, null, null, out loc),
            "placement: valid candidate accepted");
        Check(validator.Contains(zone, loc), "placement: accepted location is inside the zone");

        // Too close to the player.
        Check(!validator.PassesDistanceRules(new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0), 1f, null, 0f),
            "placement: too close to player rejected");
        Check(validator.PassesDistanceRules(new Vector3(2f, 0, 0), new Vector3(0, 0, 0), 1f, null, 0f),
            "placement: beyond min player distance accepted");
        Check(validator.PassesDistanceRules(new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0), 0f, null, 0f),
            "placement: min player distance 0 skips the rule");

        // Too close to an already-placed enemy.
        var occupied = new List<Vector3> { new Vector3(0, 0, 0) };
        Check(!validator.PassesDistanceRules(new Vector3(0.9f, 0, 0), Vector3.zero, 0f, occupied, 1f),
            "placement: too close to a spawned enemy rejected");
        Check(validator.PassesDistanceRules(new Vector3(2f, 0, 0), Vector3.zero, 0f, occupied, 1f),
            "placement: beyond min enemy distance accepted");

        // End-to-end impossible rule + maxAttempts 1 -> fails fast, no hang.
        SetField(zone, "maxAttempts", 1);
        Check(!validator.TryFindLocation(zone, zone.Center, 1000f, occupied, null, null, out loc),
            "placement: impossible player distance with maxAttempts 1 -> rejected fast");
    }

    // 20. SpawnSystem with strategy RandomZone (Sprint 7): zone placement spawns inside bounds with
    //     distance rules, composition/budget untouched, and invalid zones fail gracefully (blocking
    //     layers, no NavMesh, impossible player distance) — never invalid spawns, never a hang.
    static void RandomZoneSpawnScenario()
    {
        UnityEngine.Object.ResetWorld();
        UnityEngine.MonoBehaviour.PendingCoroutines.Clear();
        Physics.ResetBlockers();
        NavMesh.FakeValid = true;

        var table = MakeTable(new[] { 3 }, 3);
        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);

        Check(sys.Strategy == SpawnStrategy.FixedPoints, "zone: default strategy is FixedPoints");

        var zone = MakeZone(new Vector3(20, 2, 20), Vector3.zero, navMesh: false);
        SetField(sys, "strategy", SpawnStrategy.RandomZone);
        SetField(sys, "zone", zone);

        sys.Populate(9f, 1);
        Check(sys.AliveCount() == 3, "zone: RandomZone spawns 3 enemies (budget 9, cost-3)");
        foreach (GameObject c in LiveClones())
            Check(InZone(zone, c.transform.position), $"zone: enemy inside zone bounds ({c.transform.position})");
        int distinct = LiveClones().Select(c => c.transform.position).Distinct().Count();
        Check(distinct == 3, "zone: 3 distinct spawn positions (enemy distance rule applied)");
        Check(sys.LastCompositionInfo.Contains("cost 9/9"), "zone: composition/budget unchanged (cost 9 <= 9)");
        Check(sys.CachedCompositionKeys == 1, "zone: one cached composition key (selection unchanged)");

        // Blocking geometry covering the whole zone -> graceful skip (no invalid spawn).
        foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
        UnityEngine.MonoBehaviour.RunPendingCoroutines();
        Physics.ResetBlockers();
        var blockedZone = MakeZone(new Vector3(20, 2, 20), Vector3.zero, navMesh: false);
        SetField(blockedZone, "blockingLayers", (LayerMask)8);
        SetField(blockedZone, "footprintRadius", 1f);
        SetField(sys, "zone", blockedZone);
        Physics.BlockedRadius = 12f;
        Physics.BlockedPositions.Add(new Vector3(0, 0.5f, 0));
        sys.Populate(9f, 1);
        Check(sys.AliveCount() == 0, "zone: blocking geometry -> all candidates skipped (no invalid spawn)");

        // Invalid NavMesh (nothing walkable) -> graceful skip.
        NavMesh.FakeValid = false;
        NavMesh.FakeArea = new Bounds { center = Vector3.zero, size = new Vector3(0.01f, 0.01f, 0.01f) };
        var navZone = MakeZone(new Vector3(20, 2, 20), Vector3.zero, navMesh: true);
        SetField(sys, "zone", navZone);
        sys.Populate(9f, 1);
        Check(sys.AliveCount() == 0, "zone: no walkable NavMesh -> all candidates skipped");
        NavMesh.FakeValid = true;

        // Impossible min player distance -> graceful skip (bounded attempts).
        var playerGo = new GameObject("Player");
        playerGo.transform.position = new Vector3(0, 0.5f, 0);
        SetField(sys, "playerReference", playerGo.transform);
        var playerZone = MakeZone(new Vector3(20, 2, 20), Vector3.zero, navMesh: false);
        SetField(playerZone, "minPlayerDistance", 1000f);
        SetField(playerZone, "maxAttempts", 3);
        SetField(sys, "zone", playerZone);
        sys.Populate(9f, 1);
        Check(sys.AliveCount() == 0, "zone: impossible min player distance -> all candidates skipped (bounded)");
    }

    // 21. WavePlan (Sprint 8): pure slicing of one composition — waves on/off by floor, partial last
    //     wave, deterministic cursor, no duplicates, no infinite release.
    static void WavePlanScenario()
    {
        var e1 = new EnemyArchetype();
        var entries12 = new EnemyArchetype[12];
        for (int i = 0; i < 12; i++) entries12[i] = e1;
        var comp12 = new EnemyComposition(entries12);

        var cfg = new SpawnPacingConfig();
        SetField(cfg, "waveStartFloor", 3);
        SetField(cfg, "waveSize", 4);

        var plan = new WavePlan(comp12, cfg, 5);
        Check(plan.UsesWaves, "waves: floor 5 uses waves");
        Check(plan.WaveCount == 3, "waves: 12 / 4 = 3 waves");
        Check(plan.PeekNextWaveSize() == 4, "waves: first wave size 4");
        Check(plan.CurrentWave == 0, "waves: no wave released before the first spawn");

        var planLow = new WavePlan(comp12, cfg, 2);
        Check(!planLow.UsesWaves, "waves: floor 2 below threshold -> no waves");
        Check(planLow.WaveCount == 1, "waves: floor 2 is a single wave");
        Check(planLow.PeekNextWaveSize() == 12, "waves: floor 2 releases the whole composition at once");

        var cfg0 = new SpawnPacingConfig();
        SetField(cfg0, "waveStartFloor", 3);
        SetField(cfg0, "waveSize", 0);
        var plan0 = new WavePlan(comp12, cfg0, 5);
        Check(!plan0.UsesWaves && plan0.WaveCount == 1, "waves: waveSize 0 disables splitting");

        var entries10 = new EnemyArchetype[10];
        for (int i = 0; i < 10; i++) entries10[i] = e1;
        var comp10 = new EnemyComposition(entries10);
        var plan10 = new WavePlan(comp10, cfg, 5);
        Check(plan10.WaveCount == 3, "waves: 10 / 4 = 3 waves (4+4+2)");
        Check(plan10.TotalCount == 10, "waves: total equals the composition count");

        int released = 0;
        while (plan10.HasRemaining)
        {
            int size = plan10.PeekNextWaveSize();
            for (int i = 0; i < size; i++) { plan10.NextEntry(); released++; }
            plan10.MarkWaveReleased();
        }
        Check(released == 10, "waves: every composition entry released exactly once (no duplicates)");
        Check(!plan10.HasRemaining && plan10.NextEntry() == null, "waves: exhausted plan returns null (no infinite release)");
        Check(plan10.CurrentWave == 3, "waves: 3 waves released");
    }

    // 22. SpawnSystem wave flow end-to-end (Sprint 8): floor 5, composition 12, wave size 4.
    //     Non-final cleared waves release the next; FloorCleared fires exactly once after the final
    //     wave; low floors spawn all at once.
    static void WaveFloorIntegrationScenario()
    {
        UnityEngine.Object.ResetWorld();
        UnityEngine.MonoBehaviour.PendingCoroutines.Clear();

        var table = MakeTable(new[] { 3 }, 3);
        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        for (int i = 0; i < 12; i++)
            AddPoint(sysGo, "SpawnPoint (" + i + ")", new Vector3(i, 0.5f, i), new List<Vector3>());

        var pacing = new SpawnPacingConfig();
        SetField(pacing, "waveStartFloor", 3);
        SetField(pacing, "waveSize", 4);
        SetField(pacing, "waveDelaySeconds", 0.5f);
        SetField(sys, "pacingConfig", pacing);

        int cleared = 0;
        sys.FloorCleared += () => cleared++;

        sys.Populate(36f, 5);
        Check(sys.AliveCount() == 4, "waves: floor 5 releases wave 1 of 3 (4 enemies, not all 12)");
        Check(sys.WaveCount == 3, "waves: plan has 3 waves");
        Check(sys.CurrentWave == 1, "waves: current wave is 1 after populate");
        Check(sys.RemainingInComposition == 8, "waves: 8 composition entries remain");
        Check(!sys.IsFloorCleared, "waves: floor NOT cleared after wave 1 spawned");
        Check(sys.CachedCompositionKeys == 1, "waves: composition selected once (one cached key)");
        Check(sys.CurrentBudget == 36f, "waves: budget untouched by pacing");
        Check(cleared == 0, "waves: FloorCleared not fired on spawn");

        // Kill wave 1 -> next wave deferred (not spawned synchronously inside the death event).
        foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
        Check(sys.AliveCount() == 0 && sys.CurrentWave == 1, "waves: wave 1 dead, next wave deferred");
        UnityEngine.MonoBehaviour.RunPendingCoroutines();
        Check(sys.AliveCount() == 4, "waves: wave 2 released after the delay/pump");
        Check(sys.CurrentWave == 2, "waves: current wave is 2");
        Check(sys.RemainingInComposition == 4, "waves: 4 entries remain");
        Check(!sys.IsFloorCleared, "waves: a cleared intermediate wave is NOT a floor clear");
        Check(cleared == 0, "waves: FloorCleared still 0 after wave 2 spawned");

        // Kill wave 2 -> final wave released.
        foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
        UnityEngine.MonoBehaviour.RunPendingCoroutines();
        Check(sys.AliveCount() == 4, "waves: wave 3 (final) released");
        Check(sys.CurrentWave == 3 && sys.RemainingInComposition == 0, "waves: wave 3 is final, nothing left");
        Check(cleared == 0, "waves: FloorCleared still 0 while wave 3 is alive");

        // Kill the final wave -> floor truly cleared, exactly once.
        foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
        UnityEngine.MonoBehaviour.RunPendingCoroutines();
        Check(sys.AliveCount() == 0, "waves: alive == 0 after the final wave dies");
        Check(sys.IsFloorCleared, "waves: floor cleared only after the final wave is dead");
        Check(cleared == 1, "waves: FloorCleared fired exactly once");

        // Next (low) floor spawns everything at once again.
        sys.Populate(10f, 1);
        Check(sys.AliveCount() == 3, "waves: low floor spawns its whole composition at once");
        Check(sys.WaveCount == 1 && sys.CurrentWave == 1, "waves: low floor is a single wave");
    }

    // 23. Waves preserve the ONCE-selected composition: pool {E1,E2}, budget 28 -> 8xE1 + 1xE2, split
    //     4/4/1 across waves; no reselection, no budget recompute, no duplicates.
    static void WaveCompositionPreservedScenario()
    {
        UnityEngine.Object.ResetWorld();
        UnityEngine.MonoBehaviour.PendingCoroutines.Clear();

        var table = MakeTable(new[] { 3, 4 }, 3);
        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        for (int i = 0; i < 9; i++)
            AddPoint(sysGo, "SpawnPoint (" + i + ")", new Vector3(i, 0.5f, i), new List<Vector3>());

        var pacing = new SpawnPacingConfig();
        SetField(pacing, "waveStartFloor", 3);
        SetField(pacing, "waveSize", 4);
        SetField(sys, "pacingConfig", pacing);

        sys.Populate(28f, 5);
        var wave1 = LiveClones().Select(c => c.name).OrderBy(n => n).ToList();
        Check(string.Join(",", wave1) == "E1,E1,E1,E1", "preserve: wave 1 = first 4 entries (E1 x4)");

        foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
        UnityEngine.MonoBehaviour.RunPendingCoroutines();
        var wave2 = LiveClones().Select(c => c.name).OrderBy(n => n).ToList();
        Check(string.Join(",", wave2) == "E1,E1,E1,E1", "preserve: wave 2 = next 4 entries (E1 x4)");

        foreach (GameObject c in LiveClones().ToList()) c.GetComponent<TestEnemy>().Die();
        UnityEngine.MonoBehaviour.RunPendingCoroutines();
        var wave3 = LiveClones().Select(c => c.name).OrderBy(n => n).ToList();
        Check(string.Join(",", wave3) == "E2", "preserve: wave 3 = final entry (E2 x1)");

        Check(sys.CachedCompositionKeys == 1, "preserve: composition never re-selected across waves");
        Check(sys.CurrentBudget == 28f, "preserve: budget never recomputed");
        Check(sys.RemainingInComposition == 0, "preserve: all composition entries released");
        Check(sys.LastCompositionInfo.Contains("cost 28/28"), "preserve: composition info is still the original 28/28");
    }

    /// <summary>SpawnZone at (0, 0.5, 0) with the given size/offset and validation toggles.</summary>
    static SpawnZone MakeZone(Vector3 size, Vector3 centerOffset, bool navMesh = false, int blocking = 0)
    {
        var zoneGo = new GameObject("SpawnZone");
        zoneGo.transform.position = new Vector3(0, 0.5f, 0);
        var zone = zoneGo.AddComponent<SpawnZone>();
        SetField(zone, "size", size);
        SetField(zone, "centerOffset", centerOffset);
        SetField(zone, "useNavMeshValidation", navMesh);
        if (blocking != 0) SetField(zone, "blockingLayers", (LayerMask)blocking);
        return zone;
    }

    static bool InZone(SpawnZone zone, Vector3 p)
    {
        Vector3 min = zone.Center - zone.Size * 0.5f;
        Vector3 max = zone.Center + zone.Size * 0.5f;
        return p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z;
    }

    static bool NeverGround(Vector3 candidate, out Vector3 snapped)
    {
        snapped = candidate;
        return false;
    }

    static List<EnemyArchetype> MakePool(int[] costs)
    {
        var pool = new List<EnemyArchetype>();
        for (int i = 0; i < costs.Length; i++)
        {
            var prefab = new GameObject("E" + (i + 1));
            prefab.AddComponent<TestEnemy>();
            var a = new EnemyArchetype();
            SetField(a, "prefab", prefab);
            SetField(a, "cost", costs[i]);
            SetField(a, "displayName", "E" + (i + 1));
            SetField(a, "healthGrowthPerFloor", 0.12f);
            SetField(a, "damageGrowthPerFloor", 0.08f);
            pool.Add(a);
        }
        return pool;
    }

    static SpawnTable MakeTable(int[] costs, int unlockInterval)
    {
        var table = new SpawnTable();
        SetField(table, "archetypes", MakePool(costs));
        SetField(table, "unlockInterval", unlockInterval);
        return table;
    }

    /// <summary>SpawnSystem with a cost-3 pool + 4 points (enough for floor 1 budget 10 and the
    /// floor-2 budget 14 at 4 enemies).</summary>
    static SpawnSystem BuildSystem()
    {
        var table = MakeTable(new[] { 3 }, 3);
        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        for (int i = 0; i < 4; i++)
            AddPoint(sysGo, "SpawnPoint (" + i + ")", new Vector3(i, 0.5f, i), new List<Vector3>());
        return sys;
    }

    /// <summary>Button GO with a child Text (mirrors the legacy-UI MainMenu hierarchy the controller
    /// reads via GetComponentInChildren).</summary>
    static GameObject MakeUiButton(string name, string label)
    {
        var go = new GameObject(name);
        go.AddComponent<Button>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.text = label;
        return go;
    }

    static string SpawnedNames()
        => string.Join(",", LiveClones().Select(c => c.name).OrderBy(n => n));

    static List<GameObject> LiveClones()
        => UnityEngine.Object.Clones.Where(c => !c.IsDestroyed).ToList();

    static T GetField<T>(object target, string name)
    {
        var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) throw new Exception("No field " + name + " on " + target.GetType().Name);
        return (T)f.GetValue(target);
    }

    static T GetProperty<T>(object target, string name)
    {
        var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p == null) throw new Exception("No property " + name + " on " + target.GetType().Name);
        return (T)p.GetValue(target);
    }

    static void AddPoint(GameObject parent, string name, Vector3 position, List<Vector3> outPositions)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = position;
        go.AddComponent<SpawnPoint>();
        outPositions.Add(position);
    }

    static void Check(bool condition, string label)
    {
        checks++;
        if (condition) Console.WriteLine("PASS: " + label);
        else { failures.Add(label); Console.WriteLine("FAIL: " + label); }
    }

    static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) throw new Exception("No field " + name + " on " + target.GetType().Name);
        f.SetValue(target, value);
    }

    static void Invoke(object target, string methodName, params object[] args)
    {
        var m = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) throw new Exception("No method " + methodName + " on " + target.GetType().Name);
        m.Invoke(target, args);
    }

    static object InvokeReturn(object target, string methodName, params object[] args)
    {
        var m = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) throw new Exception("No method " + methodName + " on " + target.GetType().Name);
        return m.Invoke(target, args);
    }

    // 24. PlayerHudData contract: defaults, ratio math (clamping), equality operators/dedupe support.
    static void PlayerHudDataScenario()
    {
        var d = PlayerHudData.Default();
        Check(d.currentHealth == 100 && d.maxHealth == 100, "huddata: Default() is 100/100 HP");
        Check(d.xp == 0 && d.xpRequired == 100, "huddata: Default() is 0/100 XP");
        Check(d.level == 1 && d.floor == 1, "huddata: Default() level 1 floor 1");

        Check(Mathf.Approximately(d.HealthRatio, 1f), "huddata: full health => ratio 1");
        var low = new PlayerHudData(50, 100, 0, 100, 1, 1);
        Check(Mathf.Approximately(low.HealthRatio, 0.5f), "huddata: 50/100 => ratio 0.5");
        var over = new PlayerHudData(250, 100, 0, 100, 1, 1);
        Check(Mathf.Approximately(over.HealthRatio, 1f), "huddata: over-heal clamps ratio to 1");
        var under = new PlayerHudData(-5, 100, 0, 100, 1, 1);
        Check(Mathf.Approximately(under.HealthRatio, 0f), "huddata: negative health clamps ratio to 0");
        var noMax = new PlayerHudData(10, 0, 0, 100, 1, 1);
        Check(Mathf.Approximately(noMax.HealthRatio, 0f), "huddata: no max health => ratio 0");
        Check(Mathf.Approximately(low.XpRatio, 0f), "huddata: 0/100 => xp ratio 0");

        var halfXp = new PlayerHudData(100, 100, 50, 100, 1, 1);
        Check(Mathf.Approximately(halfXp.XpRatio, 0.5f), "huddata: 50/100 => xp ratio 0.5");
        var noReq = new PlayerHudData(100, 100, 10, 0, 1, 1);
        Check(Mathf.Approximately(noReq.XpRatio, 0f), "huddata: no xp required => xp ratio 0");

        Check(d == new PlayerHudData(100, 100, 0, 100, 1, 1), "huddata: equality via ==");
        Check(d != halfXp, "huddata: inequality via !=");
        Check(d.Equals(new PlayerHudData(100, 100, 0, 100, 1, 1)), "huddata: IEquatable.Equals");
        Check(d.GetHashCode() == new PlayerHudData(100, 100, 0, 100, 1, 1).GetHashCode(), "huddata: equal snapshots hash equally");
    }

    // 25. HUD chain: bind renders current, source change re-renders, identical snapshots dedupe.
    static void HudChainScenario()
    {
        var view = new RecordingHudView();
        var presenter = new PlayerHudPresenter(view);
        var source = new MockPlayerHudSource();

        presenter.Bind(source);
        Check(view.PresentCount == 1, "hud: bind renders the current snapshot once");
        Check(view.Last == PlayerHudData.Default(), "hud: bind renders the Default() snapshot");

        source.SetPlayerHud(new PlayerHudData(80, 100, 25, 100, 2, 1));
        Check(view.PresentCount == 2, "hud: source change re-renders");
        Check(view.Last.currentHealth == 80 && view.Last.level == 2, "hud: re-render carries the new data");

        source.SetPlayerHud(new PlayerHudData(80, 100, 25, 100, 2, 1));
        Check(view.PresentCount == 2, "hud: identical snapshot dedupes (no extra render)");

        source.SetPlayerHud(new PlayerHudData(80, 100, 30, 100, 2, 1));
        Check(view.PresentCount == 3, "hud: any changed field re-renders");

        presenter.Unbind(source);
        source.SetPlayerHud(new PlayerHudData(50, 100, 30, 100, 2, 1));
        Check(view.PresentCount == 3, "hud: unbind stops re-renders");
    }

    // 26. Upgrade selection rules: list-driven offers, first pick locks the rest, re-offer resets.
    static void UpgradeSelectScenario()
    {
        var view = new RecordingUpgradeSelectView();
        var presenter = new UpgradeSelectPresenter(view);
        var source = new MockUpgradeSource();
        var picked = new List<UpgradeCardData>();
        presenter.CardSelected += c => picked.Add(c);

        presenter.Bind(source);
        Check(view.ShowCount == 0, "upg: bind alone never shows the screen");

        source.SetUpgrades(MockUpgradeSource.CreateDefaultCards());
        Check(view.ShowCount == 1, "upg: an offer shows the screen once");
        Check(view.LastOffers.Count == 3, "upg: three cards offered");
        Check(view.LastOffers[0].id == "upg_damage", "upg: card order preserved (damage first)");
        Check(view.LastOffers[1].title == "Vitality", "upg: card order preserved (vitality second)");
        Check(view.LastOffers[2].valueText == "+15% speed", "upg: card order preserved (haste third)");
        Check(presenter.SelectedIndex == -1, "upg: nothing selected before a pick");
        Check(!presenter.SelectionResolved, "upg: not resolved before a pick");
        Check(presenter.SelectedCard == null, "upg: no selected card before a pick");

        presenter.Select(1);
        Check(picked.Count == 1 && picked[0].id == "upg_vitality", "upg: CardSelected carries the picked card");
        Check(presenter.SelectedIndex == 1, "upg: selected index recorded");
        Check(presenter.SelectionResolved, "upg: resolved after the first pick");
        Check(view.StateCalls.Count == 3, "upg: one SetCardState per card after a pick");
        Check(view.StateCalls[0] == "0:False:False", "upg: unpicked card 0 locked + deselected");
        Check(view.StateCalls[1] == "1:True:True", "upg: picked card 1 enabled + selected");
        Check(view.StateCalls[2] == "2:False:False", "upg: unpicked card 2 locked + deselected");

        presenter.Select(0);
        Check(picked.Count == 1, "upg: a second pick is ignored after resolution");
        presenter.Select(-1);
        presenter.Select(99);
        Check(picked.Count == 1, "upg: out-of-range picks are ignored");

        source.SetUpgrades(new[] { new UpgradeCardData("upg_bag", "Bigger Pouch", "Carry more.", "+2 slots", "bag") });
        Check(view.ShowCount == 2, "upg: a new offer re-shows the screen");
        Check(view.LastOffers.Count == 1, "upg: re-offer is list-driven (one card)");
        Check(presenter.SelectedIndex == -1, "upg: re-offer resets the selection");
        Check(!presenter.SelectionResolved, "upg: re-offer resets the resolved state");

        presenter.Dismiss();
        Check(presenter.Dismissed, "upg: dismiss marks the offer closed");
        presenter.Select(0);
        Check(picked.Count == 1, "upg: picks are ignored after dismiss");
    }

    // 27. MockUpgradeSource default offers are valid, distinct, fully described placeholders.
    static void UpgradeOfferDataScenario()
    {
        var offers = MockUpgradeSource.CreateDefaultCards();
        Check(offers.Count == 3, "offerdata: three default offers");
        Check(offers.All(c => c.IsValid), "offerdata: every default offer has an id");
        Check(offers.Select(c => c.id).Distinct().Count() == 3, "offerdata: offer ids are distinct");
        Check(offers.All(c => !string.IsNullOrEmpty(c.title) && !string.IsNullOrEmpty(c.description) && !string.IsNullOrEmpty(c.valueText)), "offerdata: offers carry title/description/value");
        Check(offers.All(c => !string.IsNullOrEmpty(c.iconKey)), "offerdata: offers carry an icon key");
        Check(offers[0].iconKey == "sword" && offers[1].iconKey == "heart" && offers[2].iconKey == "boots", "offerdata: icon keys are stable for the view mapping");
    }

    // 28. Game over chain: bind never renders, a run end renders + shows, identical summaries dedupe.
    static void GameOverScenario()
    {
        var view = new RecordingGameOverView();
        var presenter = new GameOverPresenter(view);
        var source = new MockGameOverSource();

        presenter.Bind(source);
        Check(view.PresentCount == 0, "over: bind alone never renders the screen");

        source.SetGameOver(new GameOverData(3, 27, 95f));
        Check(view.PresentCount == 1, "over: a run end renders once");
        Check(view.Last.floorReached == 3 && view.Last.enemiesDefeated == 27, "over: summary carries floor + kills");

        source.SetGameOver(new GameOverData(3, 27, 95f));
        Check(view.PresentCount == 1, "over: identical summary dedupes at the source");

        source.SetGameOver(new GameOverData(4, 30, 120f));
        Check(view.PresentCount == 2, "over: a changed summary re-renders");

        presenter.Unbind(source);
        source.SetGameOver(new GameOverData(5, 40, 130f));
        Check(view.PresentCount == 2, "over: unbind stops renders");
    }

    // 29. GameOverData.RunTimeText formats M:SS (H:MM:SS past an hour) and clamps negatives.
    static void GameOverRunTimeScenario()
    {
        Check(new GameOverData(0, 0, 0f).RunTimeText() == "0:00", "time: zero run reads 0:00");
        Check(new GameOverData(0, 0, 95f).RunTimeText() == "1:35", "time: 95s reads 1:35");
        Check(new GameOverData(0, 0, 599f).RunTimeText() == "9:59", "time: 599s reads 9:59");
        Check(new GameOverData(0, 0, 600f).RunTimeText() == "10:00", "time: 600s reads 10:00");
        Check(new GameOverData(0, 0, 3600f + 125f).RunTimeText() == "1:02:05", "time: past an hour reads H:MM:SS");
        Check(new GameOverData(0, 0, -10f).RunTimeText() == "0:00", "time: negative clamps to 0:00");
    }

    // 30. ISpawnStatConfig seam (enemy integration): SpawnSystem reads the enemy's BASE stats and
    //     pushes floor-scaled ABSOLUTE values in through ConfigureForSpawn before initialization.
    //     TestEnemy stores them as its working stats; the raw base is never overwritten.
    static void SpawnStatConfigScenario()
    {
        UnityEngine.Object.ResetWorld();

        var prefab = new GameObject("TestEnemy");
        prefab.AddComponent<TestEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), new List<Vector3>());

        sys.Populate(9f, 2);
        Check(sys.AliveCount() == 3, "scale: floor 2 spawns 3 enemies");
        foreach (GameObject c in UnityEngine.Object.Clones)
        {
            var te = c.GetComponent<TestEnemy>();
            Check(Mathf.Approximately(te.BaseMaxHealth, 10f), "scale: BaseMaxHealth reads the raw base (10)");
            Check(Mathf.Approximately(te.BaseDamage, 1f), "scale: BaseDamage reads the raw base (1)");
            Check(Mathf.Approximately(te.Health, 10f * 1.12f), "scale: floor 2 health scaled 10 * 1.12^1");
            Check(Mathf.Approximately(te.Damage, 1f * 1.08f), "scale: floor 2 damage scaled 1 * 1.08^1");
        }
    }

    // 31. Death idempotency (enemy integration): a double death notification must decrement exactly
    //     once per enemy and raise FloorCleared exactly once; TestEnemy.Die() is a no-op when dead.
    static void DeathIdempotencyScenario()
    {
        UnityEngine.Object.ResetWorld();

        var prefab = new GameObject("DoubleNotifyEnemy");
        prefab.AddComponent<DoubleNotifyEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (2)", new Vector3(7, 0.5f, 5), new List<Vector3>());
        AddPoint(sysGo, "SpawnPoint (3)", new Vector3(5, 0.5f, 3), new List<Vector3>());

        int cleared = 0;
        sys.FloorCleared += () => cleared++;

        sys.Populate(9f, 1);
        Check(sys.AliveCount() == 3, "idem: 3 spawned");

        // Double-notify one enemy: AliveCount must drop by exactly 1, floor not cleared yet.
        var first = UnityEngine.Object.Clones[0].GetComponent<DoubleNotifyEnemy>();
        first.FireTwice();
        Check(sys.AliveCount() == 2, "idem: double-notify decrements once (3 -> 2)");
        Check(cleared == 0, "idem: floor NOT cleared while others live");

        // Double-notify the rest: floor clears exactly once despite the duplicate notifications.
        foreach (GameObject c in UnityEngine.Object.Clones)
            c.GetComponent<DoubleNotifyEnemy>().FireTwice();
        Check(sys.AliveCount() == 0, "idem: AliveCount 0 after all enemies double-notify");
        Check(sys.IsFloorCleared, "idem: floor cleared");
        Check(cleared == 1, "idem: FloorCleared fired exactly once despite double-notifies");

        // TestEnemy.Die() itself is guarded: the second call is a no-op.
        var teSys = BuildSystem();
        teSys.Populate(9f, 1);
        var te = UnityEngine.Object.FindObjectsOfType<TestEnemy>()[0];
        te.Die();
        int afterFirst = teSys.AliveCount();
        te.Die();
        Check(teSys.AliveCount() == afterFirst, "idem: TestEnemy.Die() twice decrements once");
    }

    // 32. Runtime damage config (enemy integration): a stub mimicking EnemyController stores the
    //     floor-scaled damage in a per-instance runtimeDamage field (exposed as RuntimeDamage); attacks
    //     read this instead of the shared SO base. ConfigureForSpawn writes both health and damage.
    static void RuntimeDamageConfigScenario()
    {
        UnityEngine.Object.ResetWorld();

        var prefab = new GameObject("RuntimeDamageEnemy");
        prefab.AddComponent<RuntimeDamageEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0.12f);
        SetField(archetype, "damageGrowthPerFloor", 0.08f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());

        sys.Populate(3f, 1);
        Check(sys.AliveCount() == 1, "rdamage: floor 1 spawns 1 enemy");
        var rde = UnityEngine.Object.Clones[0].GetComponent<RuntimeDamageEnemy>();
        Check(Mathf.Approximately(rde.BaseDamage, 5f), "rdamage: BaseDamage reads the raw base (5)");
        Check(Mathf.Approximately(rde.RuntimeDamage, 5f), "rdamage: floor 1 RuntimeDamage == base (no scaling)");
        Check(Mathf.Approximately(rde.RuntimeDamage, rde.Damage), "rdamage: RuntimeDamage matches working Damage");

        // Floor 3: damageScale = 1.08^2 = 1.1664
        UnityEngine.Object.ResetWorld();
        sys.Populate(3f, 3);
        Check(sys.AliveCount() == 1, "rdamage: floor 3 spawns 1 enemy");
        rde = UnityEngine.Object.Clones[0].GetComponent<RuntimeDamageEnemy>();
        Check(Mathf.Approximately(rde.RuntimeDamage, 5f * 1.08f * 1.08f), "rdamage: floor 3 RuntimeDamage == 5 * 1.08^2");
        Check(Mathf.Approximately(rde.Damage, 5f * 1.08f * 1.08f), "rdamage: floor 3 working Damage matches RuntimeDamage");
    }

    // 33. OnDied-once on lethal TakeDamage (enemy integration): a stub mimicking EnemyEntity fires
    //     OnDied exactly once when TakeDamage reduces health to zero; a second TakeDamage is a no-op.
    static void OnDiedLethalDamageScenario()
    {
        UnityEngine.Object.ResetWorld();

        var prefab = new GameObject("LethalEnemy");
        prefab.AddComponent<LethalEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0f);
        SetField(archetype, "damageGrowthPerFloor", 0f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());

        int deaths = 0;
        sys.FloorCleared += () => deaths++;

        sys.Populate(3f, 1);
        Check(sys.AliveCount() == 1, "lethal: 1 spawned");

        var le = UnityEngine.Object.Clones[0].GetComponent<LethalEnemy>();
        le.TakeDamage(4f);
        Check(sys.AliveCount() == 1, "lethal: partial damage keeps enemy alive");
        Check(deaths == 0, "lethal: no death on partial damage");

        le.TakeDamage(1f);
        Check(sys.AliveCount() == 0, "lethal: lethal damage decrements alive");
        Check(deaths == 1, "lethal: FloorCleared fired exactly once");

        // Second TakeDamage is a no-op (dead guard).
        le.TakeDamage(10f);
        Check(sys.AliveCount() == 0, "lethal: post-death TakeDamage is a no-op");
        Check(deaths == 1, "lethal: post-death TakeDamage does not fire again");
    }

    // 34. Explosion death flow (enemy integration): a stub mimicking SacrificeAttack calls Kill()
    //     which fires OnDied exactly once; SpawnSystem receives it and decrements alive.
    static void ExplosionDeathFlowScenario()
    {
        UnityEngine.Object.ResetWorld();

        var prefab = new GameObject("ExplodingEnemy");
        prefab.AddComponent<ExplodingEnemy>();

        var archetype = new EnemyArchetype();
        SetField(archetype, "prefab", prefab);
        SetField(archetype, "cost", 3);
        SetField(archetype, "healthGrowthPerFloor", 0f);
        SetField(archetype, "damageGrowthPerFloor", 0f);

        var table = new SpawnTable();
        SetField(table, "archetypes", new List<EnemyArchetype> { archetype });

        var sysGo = new GameObject("SpawnSystem");
        var sys = sysGo.AddComponent<SpawnSystem>();
        SetField(sys, "table", table);
        AddPoint(sysGo, "SpawnPoint (1)", new Vector3(4, 0.5f, 7), new List<Vector3>());

        int deaths = 0;
        sys.FloorCleared += () => deaths++;

        sys.Populate(3f, 1);
        Check(sys.AliveCount() == 1, "explode: 1 spawned");

        var ee = UnityEngine.Object.Clones[0].GetComponent<ExplodingEnemy>();
        Check(Mathf.Approximately(ee.RuntimeDamage, 5f), "explode: RuntimeDamage is set by ConfigureForSpawn");

        // Mimic SacrificeAttack: scale explosion proportionally and then Kill().
        float ratio = ExplodingEnemy.ConfigBaseDamage > 0f ? ee.RuntimeDamage / ExplodingEnemy.ConfigBaseDamage : 1f;
        float scaledExplosion = ExplodingEnemy.ConfigExplosionDamage * ratio;
        Check(Mathf.Approximately(scaledExplosion, 40f), "explode: explosion damage unscaled on floor 1");

        ee.Kill();
        Check(sys.AliveCount() == 0, "explode: Kill() decrements alive");
        Check(deaths == 1, "explode: FloorCleared fired exactly once");

        // Double Kill is a no-op.
        ee.Kill();
        Check(sys.AliveCount() == 0, "explode: double Kill() is a no-op");
        Check(deaths == 1, "explode: double Kill() does not fire again");
    }

    sealed class RecordingHudView : IPlayerHudView
    {
        public int PresentCount;
        public PlayerHudData Last;

        public void Present(in PlayerHudData data)
        {
            PresentCount++;
            Last = data;
        }
    }

    sealed class RecordingUpgradeSelectView : IUpgradeSelectView
    {
        public int ShowCount;
        public List<UpgradeCardData> LastOffers = new();
        public List<string> StateCalls = new();

        public void ShowSelection(IReadOnlyList<UpgradeCardData> cards)
        {
            ShowCount++;
            LastOffers = cards.ToList();
        }

        public void SetCardState(int index, bool enabled, bool selected)
            => StateCalls.Add($"{index}:{enabled}:{selected}");
    }

    sealed class RecordingGameOverView : IGameOverView
    {
        public int PresentCount;
        public GameOverData Last;

        public void Present(in GameOverData data)
        {
            PresentCount++;
            Last = data;
        }
    }
}

/// <summary>Test fake that implements IEnemySpawned and can fire its death notification twice, to
/// prove SpawnSystem's death handling is idempotent (a duplicate OnDied must never double-decrement
/// AliveCount or double-raise FloorCleared).</summary>
sealed class DoubleNotifyEnemy : MonoBehaviour, IEnemySpawned
{
    public event Action OnDied;

    public void FireTwice()
    {
        OnDied?.Invoke();
        OnDied?.Invoke();
    }
}

/// <summary>Test fake mimicking EnemyController: stores runtimeDamage per-instance (written by
/// ConfigureForSpawn), exposes it as RuntimeDamage so attacks can read the scaled value.</summary>
sealed class RuntimeDamageEnemy : MonoBehaviour, IEnemySpawned, ISpawnStatConfig
{
    [SerializeField] float baseHealth = 10f;
    [SerializeField] float baseDamage = 5f;

    public event Action OnDied;

    public float Health { get; private set; }
    public float Damage { get; private set; }
    public float RuntimeDamage { get; private set; }

    public float BaseMaxHealth => baseHealth;
    public float BaseDamage => baseDamage;

    public void ConfigureForSpawn(float maxHealth, float baseDamage)
    {
        Health = maxHealth;
        Damage = baseDamage;
        RuntimeDamage = baseDamage;
    }
}

/// <summary>Test fake mimicking EnemyEntity: has health, TakeDamage with dead-guard, Kill() with
/// dead-guard, and fires OnDied exactly once. Used to verify the death flow from lethal damage.</summary>
sealed class LethalEnemy : MonoBehaviour, IEnemySpawned, ISpawnStatConfig
{
    float health = 5f;
    bool dead;

    public event Action OnDied;

    public float BaseMaxHealth => 5f;
    public float BaseDamage => 1f;

    public void ConfigureForSpawn(float maxHealth, float baseDamage)
    {
        health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (dead) return;
        if (damage <= 0f) return;
        health -= damage;
        if (health <= 0f)
        {
            health = 0f;
            dead = true;
            OnDied?.Invoke();
        }
    }
}

/// <summary>Test fake mimicking SacrificeAttack + EnemyEntity: has per-instance RuntimeDamage,
/// ConfigBaseDamage, ConfigExplosionDamage for proportional scaling, and Kill() with dead-guard.
/// Proves the explosion → Kill → OnDied → SpawnSystem path works correctly.</summary>
sealed class ExplodingEnemy : MonoBehaviour, IEnemySpawned, ISpawnStatConfig
{
    public const float ConfigBaseDamage = 5f;
    public const float ConfigExplosionDamage = 40f;

    float health = 10f;
    bool dead;

    public event Action OnDied;

    public float RuntimeDamage { get; private set; }
    public float BaseMaxHealth => 10f;
    public float BaseDamage => ConfigBaseDamage;

    public void ConfigureForSpawn(float maxHealth, float baseDamage)
    {
        health = maxHealth;
        RuntimeDamage = baseDamage;
    }

    public void Kill()
    {
        if (dead) return;
        dead = true;
        health = 0f;
        OnDied?.Invoke();
    }
}
