# Roguelike Spawning — Sprint 4 (SpawnSystem Foundation)

> **Status:** PLANNING + IMPLEMENTATION — SpawnSystem foundation implemented against a **temporary test enemy**. Code-level verification done (external dotnet harness). Unity Play Mode verification **NOT run** (no Unity Editor on this machine) — deferred.
> Evidence labels: `[EXISTS]` / `[PARTIAL]` / `[MISSING]` / `[PROPOSED]` / `[TEAM DECISION]` / `[BLOCKED]` / `[FUTURE]` / `[WAITING FOR ENEMY SYSTEM]`
> Base design: `docs/ROGUELIKE_SYSTEM.md §4.5` (cost-based spawning).
> Prior investigation: `docs/ROGUELIKE_RUN_SYSTEM_SPRINT_4.md` (verdict: BLOCKED on ownership + Enemy dev).
> Prior sprints: 1 (`a473850`), 2 (`2e1d63e`), 3 (`cc11a51`, `RunController`).

> ## ⚠️ SUPERSEDED (2026-08-16) — Enemy ↔ SpawnSystem integration landed
> The enemy-facing contract rows below are superseded by the integration branch
> `fix/enemy-spawn-integration`. `IEnemySpawned` is now a **death-only** contract
> (`event Action OnDied`) surfaced by `EnemyController` from `EnemyEntity.OnDied` (explode path
> included); health is applied via the existing `EnemyEntity.SetMaxHealth`; floor scaling moved to
> the separate **`ISpawnStatConfig`** seam (`BaseMaxHealth` / `BaseDamage` / `ConfigureForSpawn`),
> owned by `SpawnSystem`. The current contract is documented in **`docs/ENEMY_SPAWN_INTEGRATION.md`**.
> Everything below that still says `IEnemySpawned.ApplyFloorScaling`, `IEnemySpawned.Died`, or
> `[WAITING FOR ENEMY SYSTEM]` for the death hook / setters is stale.

---

## Goal

Build the first **working SpawnSystem foundation** for the Roguelike: a cost-based spawner that takes a floor budget, selects affordable archetypes, fills `SpawnPoint`s, instantiates enemies, tracks alive enemies, applies per-floor scaling, and reports when the floor is cleared.

**Team decision (confirmed):** SpawnSystem is owned by the **Roguelike side**. Because the real Enemy System is still being completed by another team member, Sprint 4 builds and validates against a **temporary test enemy** (`TestEnemy`), kept isolated under `Assets/Scripts/Roguelike/Spawning/Testing/`. No `Assets/Scripts/Enemy/` file is created or modified.

---

## Responsibilities

### SpawnSystem SHOULD own

- Selecting affordable enemy archetypes (cost <= remaining budget).
- Spending a spawn budget (from `RunData.enemyBudget`; total spent <= budget).
- Choosing spawn points (scene `SpawnPoint` markers).
- Instantiating enemies at those points.
- Applying floor scaling through a spawn→enemy contract.
- Tracking spawned/alive enemies (`AliveCount()`).
- Reporting when the spawned enemies are cleared (`IsFloorCleared`).

### SpawnSystem MUST NOT own

- Enemy AI / enemy states (Enemy dev).
- Player logic (Player dev).
- Combat logic (Weapon/Combat owner).
- Run state transitions (Run owns flow; SpawnSystem is driven by callers).
- Floor/room generation (`FloorGenerator` — Sprint 11+).
- Upgrade logic (later).
- Player death handling (later).

---

## Architecture

Folder structure (all new code under `Assets/Scripts/Roguelike/`, parallel-safe per `ROGUELIKE_SPRINT_PLAN.md`):

```
Assets/Scripts/Roguelike/Spawning/
    SpawnSystem.cs            (MonoBehaviour spawner)
    SpawnPoint.cs             (MonoBehaviour marker)
    EnemyArchetype.cs         (ScriptableObject data)
    SpawnTable.cs             (ScriptableObject pool)
    IEnemySpawned.cs          (spawn→enemy death/scaling contract)

Assets/Scripts/Roguelike/Spawning/Testing/     ← TEST-ONLY, isolated
    TestEnemy.cs              (temporary RED CAPSULE test double)
    SpawnSystemTestDriver.cs  (Play-mode self-check driver)

Assets/prefabs/Roguelike/Spawning/Testing/     ← TEST-ONLY assets
    TestEnemy.prefab

Assets/Data/Roguelike/Spawning/Testing/        ← TEST-ONLY data assets
    TestEnemyArchetype.asset
    TestSpawnTable.asset

Assets/Scenes/Roguelike/
    SpawnTest.unity           ← TEST-ONLY isolated scene
```

Flow:

```
RunController (Sprint 3, [EXISTS])  ── StartRun() → FloorStart
   │
   │  driver reads RunData.enemyBudget (10) + RunData.floor (1)
   ▼
SpawnSystem.Populate(budget, floor)
   │  1. ClearAlive()
   │  2. cheap archetype cost
   │  3. while remaining >= cheapest → PickAffordable + PickRandomPoint
   │  4. Instantiate(prefab, point.Position, point.Rotation)
   │  5. IEnemySpawned.ApplyFloorScaling(healthScale, damageScale)
   │  6. subscribe IEnemySpawned.Died → remove from alive
   ▼
driver calls RunController.BeginFloor()   → FloorStart → FloorActive
   │
   ▼
SpawnSystem.AliveCount() / IsFloorCleared   → floor-clear reporting (Sprint 5 consumes)
```

---

## Contracts

| Contract | Provider → Consumer | Signature | Sprint | Status |
|---|---|---|---|---|
| Spawn request | Run → Spawn | `Populate(budget: float, floor: int)` | 4 | **READY NOW** |
| Alive count | Spawn → Run | `AliveCount() : int` | 4 | **READY NOW** |
| Floor cleared report | Spawn → Run | `IsFloorCleared : bool` (`AliveCount() == 0`) | 4 | **READY NOW** |
| Floor-ready transition | Runner → Run | `RunController.BeginFloor()` | 4 | **READY NOW** (`[EXISTS]` `RunController.cs:12`) |
| Floor scaling (test) | Spawn → TestEnemy | `ISpawnStatConfig.ConfigureForSpawn(maxHealth, baseDamage)` | 4+ | **READY NOW** (test double; superseded signature, see banner) |
| Floor scaling (real) | Spawn → EnemyEntity | health `SetMaxHealth`; damage deferred (attack SO configs are the runtime source — see `docs/ENEMY_SPAWN_INTEGRATION.md §7`) | 4/5 | **IMPLEMENTED (health)** (integration branch; no new EnemyEntity setter) |
| Death hook (real) | Enemy → Spawn | `EnemyEntity.OnDied` surfaced via `IEnemySpawned.OnDied` | 4/5 | **IMPLEMENTED** (integration branch; `EnemyController : IEnemySpawned`) |
| Floor clear transition | Spawn → Run | `AliveCount() == 0` → `FloorCleared` | 5 | **NOT Sprint 4** |

Contract stability rule (from `ROGUELIKE_SPRINT_PLAN.md §10`): signature changes require a team heads-up before merge.

---

## Temporary Test Enemy

**Why:** the real Enemy System (`EnemyController`, `EnemyEntity`, states, prefab) is still being completed. Sprint 4 must not block on it or duplicate it.

**What `TestEnemy` is:** a tiny test double — a RED CAPSULE (built at runtime if no visual child exists). It implements `IEnemySpawned` and has ONLY:

- `baseHealth` / `baseDamage` serialized fields (its own base stats — the real enemy keeps stats on `EnemyEntity`).
- `ISpawnStatConfig` (`BaseMaxHealth` / `BaseDamage` / `ConfigureForSpawn`) — stores the floor-scaled absolute stats from SpawnSystem (replaces the old `ApplyFloorScaling`, see banner).
- `Die()` — fires `OnDied`, then destroys itself (simulates enemy removal/death).
- `Health` / `Damage` read-outs for assertions.
- A red wireframe gizmo.

**What `TestEnemy` does NOT do:** enemy AI, states, combat, navigation, damage systems, player targeting, real `EnemyEntity`, `EnemyController`.

**Distinction:** this is a **test double, not a replacement for the real Enemy System**. When the real enemy lands, `SpawnSystem` needs zero changes: swap the archetype `prefab` to a real enemy prefab — the real `IEnemySpawned` / `ISpawnStatConfig` surface now exists on `EnemyController` (integration branch; only the production prefab swap remains, see `docs/ENEMY_SPAWN_INTEGRATION.md`).

---

## Data (ScriptableObjects)

### `EnemyArchetype`
- `prefab` : `GameObject` — **adaptation:** the design (`ROGUELIKE_SYSTEM.md:425`) typed this as `EnemyController`; `GameObject` keeps `SpawnSystem` decoupled from the in-progress Enemy System so the test prefab works now and a real enemy prefab drops in later without an archetype change.
- `cost : int` — spawn cost (design intent `EnemyController.cs:39-43`: high cost = stronger, gated from early floors).
- `healthGrowthPerFloor : float` / `damageGrowthPerFloor : float` — per-floor growth multipliers (`ROGUELIKE_SYSTEM.md:431-432`).
- **Skipped:** `baseStats`/`EnemyEntityStats` (`ROGUELIKE_SYSTEM.md:434-435`) — Enemy-owned type; base stats for the test live on `TestEnemy`, for the real enemy on `EnemyEntity`. `[WAITING FOR ENEMY SYSTEM]` to re-add if needed.

### `SpawnTable`
- `archetypes : List<EnemyArchetype>` — the pool to pick from.
- **Skipped:** `baseBudget` (`ROGUELIKE_SYSTEM.md:553`) — `RunData.enemyBudget` is the single source of truth for the budget; two budget fields would be competing data.

### `SpawnPoint`
- MonoBehaviour marker at a transform position/rotation.
- **Skipped:** `isElite` (`ROGUELIKE_SYSTEM.md:455`) — unused by anything yet; deferred.
- Yellow wire gizmo (editor).

---

## SpawnSystem (MonoBehaviour)

`SpawnSystem` is a MonoBehaviour because it `Instantiate`s prefabs, reads scene `SpawnPoint`s, and lives on a scene GameObject.

Public API (exact, per this plan):

```csharp
public void Populate(float budget, int floor)   // clear + fill until budget spent
public int  AliveCount()                         // currently tracked alive enemies
public bool IsFloorCleared                       // AliveCount() == 0
```

Internal flow: `ClearAlive()` → cheap-cost check → loop `PickAffordable` + `PickRandomPoint` + `InstantiateEnemy` → `ApplyFloorScaling` (via `ISpawnStatConfig`; superseded signature, see banner) → subscribe `OnDied`.

Constraints honored: **no singleton, no global static manager, no EventBus, no `GameManager` change, no `RunController` change.** `SpawnSystem` never references `RunController` (thin orchestration rule, `ROGUELIKE_RUN_SYSTEM_SPRINT_3.md §8`). The `SpawnSystemTestDriver` is the temporary glue that drives the real `RunController`.

---

## Unity Editor Work

Everything that must exist in the Editor (or be hand-authored as test assets):

| Item | Where | Status |
|---|---|---|
| `SpawnSystem` component | on a scene GameObject in `SpawnTest.unity` | **CREATED (hand-authored scene)** |
| `SpawnPoint` GameObjects | children of the SpawnSystem GameObject | **CREATED (5 points)** |
| `SpawnTable` asset | `Assets/Data/Roguelike/Spawning/Testing/TestSpawnTable.asset` | **CREATED** |
| `EnemyArchetype` asset | `Assets/Data/Roguelike/Spawning/Testing/TestEnemyArchetype.asset` (cost 3) | **CREATED** |
| `TestEnemy` prefab | `Assets/prefabs/Roguelike/Spawning/Testing/TestEnemy.prefab` | **CREATED** |
| Inspector refs | SpawnSystem→table→archetype→prefab | **CREATED (hand-authored GUIDs)** |

All test content is isolated. No production/player/enemy setup was modified. `TestingScene.unity` is untouched by this sprint.

---

## Sprint 4 Follow-Up — SpawnSystem Integration Test (additive)

### 1. TestEnemy spawn height fix (`TestEnemy.cs`)

**Root cause:** the runtime red capsule is a `PrimitiveType.Capsule` whose pivot is its center. It was
parented at the object origin, so an enemy placed at a `SpawnPoint` sat half-buried in the floor.

**Fix (no hardcoded `y += 1`):** `EnsureVisual()` lifts the `Body` child so the capsule **BOTTOM** sits
on the object origin — `footOffset = collider.height * 0.5f + collider.center.y`, data-driven from the
capsule's own collider. The object origin now means "feet on the ground", and each scene places its
SpawnPoints at that scene's floor height:

- `SpawnTest.unity`: floor top is y=0 → SpawnPoints stay at y=0 (**unchanged**).
- `TestingScene.unity`: Ground top is y=0.5 → new SpawnPoints are at y=0.5.

### 2. Touch-to-kill for the test enemy only

`TestEnemy` now dies when the Player touches it (test-only; the real enemy has its own combat).

**Chosen: `OnTriggerEnter`, not `OnCollisionEnter`, because:**
- Trigger events are pure detection — no physics response, so touching a test enemy never pushes or
  bounces the Player, and it needs only the Player's existing Rigidbody + collider.
- `OnCollisionEnter` would require solid-solid contact and impose a physical response the test double
  must not add.

**Detection:** the Player's capsule collider sits on the child `PlayerObj` (tag `Untagged`), so the
check walks the hierarchy to the root GameObject tagged `Player`.

### 3. Additive `TestingScene.unity` setup (user-approved)

Appended ONLY new objects with brand-new fileIDs (`4000000001`…`4000000033`); nothing existing was
modified — the team UI blocks (fileIDs 3001–3254) and all scene content are untouched:

- Root `SpawnSystem` GameObject (fileID `4000000001`) carrying `SpawnSystem` (table ref →
  `TestSpawnTable.asset`) + `SpawnSystemTestDriver` (Play Mode self-check), mirrored from `SpawnTest.unity`.
- 3 `SpawnPoint` children at floor height y=0.5 near the Player: `(4, 0.5, 7)`, `(7, 0.5, 5)`, `(5, 0.5, 3)`.
- New root registered in `SceneRoots` (last entry).

### 4. Run → Spawn floor-clear integration (`IMPLEMENTED`)

The Run System has `FloorActive → FloorCleared → FloorStart → FloorActive` transitions
(`RunStateMachine.cs:9-10`). The floor-clear report is now wired end-to-end (test-only):

- **`SpawnSystem.FloorCleared`** (`SpawnSystem.cs:24`) — **report-only** event, fired exactly when the
  last alive spawned enemy is removed by a death (`OnEnemyDied`, `alive.Count` hits 0). It is NOT
  raised by `ClearAlive()`/`Populate()` resetting the list, and `SpawnSystem` never touches `RunState`.
- **`RunController.CompleteFloor()`** (`RunController.cs:23`) — `FloorActive → FloorCleared`, guarded
  (returns false outside `FloorActive`, so an automatic restart loop is impossible).
- **`RunController.StartNextFloor()`** (`RunController.cs:33`) — `FloorCleared → FloorStart` +
  `RunData.AdvanceFloor()` (`floor++`, `clearedRooms++`, `enemyBudget *= 1.4`), guarded.
- **Integration owner** = `SpawnSystemTestDriver` (test-only bridge). On the event it calls
  `Run.CompleteFloor()` → (short visible pause `floorClearPauseSeconds`, default 1s) →
  `Run.StartNextFloor()` → `spawnSystem.Populate(Run.Data.enemyBudget, Run.Data.floor)` →
  `Run.BeginFloor()`. The pause is a test-only tuning knob so the `FloorCleared` state is visible in
  the HUD before the next floor populates.

This deliberately deviates from the originally-proposed single `CompleteFloor()` call
(`FloorCleared → AdvanceFloor → FloorStart → FloorActive` in one shot): `SpawnSystem.Populate` must
run **between** `FloorStart` and `FloorActive`, and `RunController` stays decoupled from `SpawnSystem`
(plain C#, no MonoBehaviour refs), so the advance is split and the orchestrator inserts `Populate`.

#### 4a. Floor advance flow (verified end-to-end, cost-3 test archetype)

```
Floor 1: budget 10  → 3 enemies → kill all
  → SpawnSystem.FloorCleared (report) → Run.CompleteFloor() → [1s pause: HUD "Floor Cleared: YES"] 
  → Run.StartNextFloor() (FloorCleared→FloorStart + AdvanceFloor) → Populate(14, 2) → Run.BeginFloor()
Floor 2: budget 14  → 4 enemies → kill all → same chain
Floor 3: budget 19.6 → 6 enemies → kill all → same chain (repeatable; restart loop impossible:
  FloorCleared fires only on a real all-clear and every transition is guarded)
```

### 5. Root cause of "only 2 enemies visible" — SpawnPoint reuse (fixed in `SpawnSystem.cs`)

**User report:** in `TestingScene` Play Mode only 2 of the expected 3 enemies were visible.

**Investigation (all checkpoints checked):**
- **Scene used at runtime:** `TestingScene.unity` is the active test scene and IS the spawner used in
  Play Mode — it holds the real `SpawnSystem` (script `308e7766…`, table → `TestSpawnTable.asset`) +
  `SpawnSystemTestDriver`. `SpawnTest.unity` is NOT involved.
- **Driver/budget:** `RunData.enemyBudget == 10`, archetype cost 3 → 3 spawns is correct
  (`3×3 = 9 ≤ 10`, `4×3 = 12 > 10`). The driver asserts `spawned == 3`, so 3 WERE spawned.
- **SpawnPoints:** all 3 present, active (`m_IsActive: 1`), children of the SpawnSystem root, at
  `(-9.7, 0.5, 7)`, `(7, 0.5, 5)`, `(5, 0.5, -18.56)` (floor top y=0.5; no point inside a wall).
- **No instant destroy/disable:** no component destroys enemies on spawn; `AliveCount()` stayed 3.
- **ROOT CAUSE (visual/position):** `SpawnSystem.PickRandomPoint` sampled `points[Random.Range(0, length)]`
  **with replacement**, so a single `Populate` could place two enemies on the SAME `SpawnPoint`. Two
  identical capsules exactly overlapping read as ONE enemy → only 2 distinct enemies were visible
  while `AliveCount()` correctly reported 3.

**Fix (no count change 3→2):** `Populate` now draws `SpawnPoint`s **without replacement** — each point
is used at most once per pass, and the pool only refills when all points are used. 50/50 randomized
harness passes now always produce 3 unique positions (previously duplicate placement was observed at
`(7, 0.5, 5)`).

### 6. Test-only debug display + console logging (`SpawnTestDebugDisplay.cs`)

New TEST-ONLY component under `Assets/Scripts/Roguelike/Spawning/Testing/` (script guid
`63935cbb90b6457a807c62d6c24741c7`), added to `TestingScene.unity` additively (new fileIDs
`4000000041`–`4000000043`; team UI untouched). Displays live state — no hardcoded values:

- **HUD:** `SPAWN TEST / Scene: TestingScene / Run state: X / Floor: N / Spawned: N / Alive: N /
  Dead: N / Floor Cleared: YES|NO`. `Spawned`/`Dead` are derived from real `TestEnemy` instances +
  their `OnDied` events; `Alive`/`Floor Cleared` read `SpawnSystem.AliveCount()` / `IsFloorCleared`;
  `Run state`/`Floor` read the **real `RunController`** that `SpawnSystemTestDriver` drives
  (`driver.Run.CurrentState` / `driver.Run.Data.floor`). **Run number is NOT displayed**: no
  run-number field exists in the codebase (`RunData` has `floor`/`clearedRooms`/`enemyBudget` only),
  so it is honestly reported as unavailable instead of invented.
- Console logging: scene name, per-enemy SpawnPoint + world position + `AliveCount` on spawn, position
  + counters on death, and the `FloorCleared=true` transition. Counters reset when a new floor is
  populated after a clear.

`SpawnSystemTestDriver` gained a check that no two enemies overlap on the same `SpawnPoint`, and now
exposes its real `RunController` as `Run` (read-only property) so the HUD can read live run state.

#### 6a. HUD readability fix — Canvas Text instead of IMGUI (`OnGUI`)

**User report:** after the spawn fix, "the SpawnSystem IS spawning 3 enemies correctly now, but I cannot
properly see the new Spawn Debug HUD. It is either not visible, too small, misplaced, or not readable."

**Root cause:** the first HUD version used legacy **IMGUI** (`OnGUI` → `GUI.Box`). IMGUI draws in raw
screen pixels with the tiny default GUI-skin font, independent of the scene's real UI. The existing
Run/State debug texts (`PlayerCurrentState`, `EnemyCurrentState`) are **Canvas-based legacy UI Text**
— a `Canvas` (`m_RenderMode: 0` Screen Space Overlay, sortingOrder 0) with `CanvasScaler`
(`m_UiScaleMode: 1` ScaleWithScreenSize, reference 1920×1080, match height) and bold Arial text
(fontSize 211 @ scale 0.17). The IMGUI box was effectively invisible next to the proper UI.

**Fix:** `SpawnTestDebugDisplay` now **builds its own Canvas + legacy `UnityEngine.UI.Text` at runtime**
(`Awake`) matching the existing debug UI style — Screen-Space-Overlay Canvas, `CanvasScaler`
(ScaleWithScreenSize, 1920×1080, match height, `sortingOrder: 100` so it draws above the scene's
overlay canvases), Arial bold (built-in `LegacyRuntime.ttf`), fontSize 34, top-left anchored with a
translucent black `Image` panel behind it for readability. Nothing is hardcoded — the text is rebuilt
every `Update` from live state. Production UI is untouched; the HUD is fully test-only.

---

## Tests

### CODE VERIFIED (run — external dotnet harness with Unity stubs)

Ran outside the repo (temp harness), compiling the **actual** `Spawning/*.cs` sources:

| # | Test | Result |
|---|---|---|
| 1 | Budget: `RunData.enemyBudget` (10) with cost-3 archetype → exactly 3 spawns, total spent 9 ≤ 10 | **PASS** |
| 2 | No spawn beyond budget (cheapest-cost guard) | **PASS** |
| 3 | Affordable archetype selection (cost ≤ remaining) | **PASS** |
| 4 | Spawn points used (each spawned at a `SpawnPoint` position) | **PASS** |
| 5 | `AliveCount()` matches spawns | **PASS** |
| 6 | Death/removal: `Die()` → `AliveCount()` decrements → `IsFloorCleared` | **PASS** |
| 7 | Floor scaling: floor 2, growth 0.12 → health = base × 1.12 | **PASS** |
| 8 | Repopulate after clear works | **PASS** |
| 9 | No two enemies on the same SpawnPoint in one Populate (50 randomized passes) | **PASS** |
| 10 | Test-only debug display: Spawned/Alive/Dead/FloorCleared from live state | **PASS** |
| 11 | HUD text (Canvas) shows scene, live counts, and `Floor Cleared YES/NO` transitions | **PASS** |
| 12 | HUD reads real `RunController` state/floor; run number NOT invented (no such field) | **PASS** |
| 13 | Run↔Spawn integration: real driver's `Start()` pumped against real `SpawnSystem`+`RunController` — floors auto-advance 1→2→3 on clear (event → `CompleteFloor` → pause → `StartNextFloor`+`AdvanceFloor` → `Populate` → `BeginFloor`); guarded transitions reject out-of-state calls | **PASS** |
| 14 | Live bridge survives reset: manual-play Floor 1 kill → auto-advance to Floor 2 (budget 14, 4 enemies) | **PASS** |

### UNITY PLAY MODE VERIFIED (user-tested; no Unity Editor on this machine)

Earlier sprints were user-tested in Play Mode (`TestingScene.unity`) and reported issues that were
root-caused and fixed (§5 stacked spawns, §6a HUD readability). With the floor-advance integration
landed (harness-verified, rows 13–14), the user needs to confirm the live flow. Expected behaviour in
Play Mode (the driver auto-runs its checks at Start, then resets to a fresh Floor 1 for manual play):

```
[auto] checks: floors 1 -> 2 -> 3 clear and auto-advance; log: [SpawnSystemTest] ALL N CHECKS PASSED
[manual] Floor 1 populated (3 enemies) -> HUD Floor: 1 / Floor Cleared: NO
  → touch-kill all 3 → Alive: 0 / Dead: 3 / HUD "Floor Cleared: YES" + "Run state: FloorCleared"
  → ~1s pause → Floor 2 populates (4 enemies) → HUD Floor: 2
  → touch-kill all 4 → same chain → Floor 3 (6 enemies) → kill → Floor 4 ...
```

I cannot run Play Mode myself (no Unity Editor); all my verification is code-level (dotnet harness)
+ static scene/diff validation.

---

## Definition of Done

- [x] SpawnSystem owner confirmed `[TEAM DECISION]` resolved (Roguelike side).
- [x] `SpawnSystem` populates a floor from a budget and reports `AliveCount()` / `IsFloorCleared`.
- [x] Budget respected (no overspend); affordable selection works.
- [x] Spawn points used; enemies instantiated at their positions.
- [x] Floor scaling applied via contract (test enemy path).
- [x] Death/removal tracked; `AliveCount()` decreases; cleared report works.
- [x] Test enemy isolated under `Spawning/Testing/`; nothing under `Assets/Scripts/Enemy/`.
- [x] Code-level tests run and PASS (dotnet harness).
- [x] Run↔Spawn integration landed: `SpawnSystem.FloorCleared` report-only event + `RunController.CompleteFloor()`/`StartNextFloor()` + test-driver bridge; floors auto-advance 1→2→3 (harness-verified, rows 13–14); no restart loop.
- [ ] Play Mode verification (deferred — no Editor).
- [x] No Player/Enemy/Combat/GameManager code edited.

---

## Risks / Blockers

| Risk / Block | Mitigation |
|---|---|
| Real Enemy System not ready | Sprint 4 uses isolated `TestEnemy`; no `Enemy/` files touched |
| `EnemyEntity` setters missing | **RESOLVED (2026-08-16)** — health uses the existing `SetMaxHealth`; no new EnemyEntity setter was needed (damage deferred to combat side, see `docs/ENEMY_SPAWN_INTEGRATION.md §7`) |
| Real death notification missing | **RESOLVED (2026-08-16)** — `EnemyEntity.OnDied` surfaced via `IEnemySpawned.OnDied` on `EnemyController` (integration branch) |
| Spawn-ready real prefab missing (`TreeEntAsh.prefab` has 2 missing scripts + null refs) | `[WAITING FOR ENEMY SYSTEM]` — documented; archetype `prefab` swap is the only change |
| `DieState` crash (`DieState.cs:11-18`) | `[WAITING FOR ENEMY SYSTEM]` — only affects real-enemy killability |
| `DealDamage` damage commented out | `[WAITING FOR ENEMY SYSTEM]` — combat inert; out of scope |
| Unity Editor unavailable on this machine | Play Mode verification deferred; hand-authored assets + scene ready for an Editor session |
| `GameManager` bootstrap / Build Settings | not touched by this sprint |

---

## Future / Deferred

- Real-enemy wiring: archetype prefab swap (production prefab still blocked, see `docs/ENEMY_SPAWN_INTEGRATION.md`) — `IEnemySpawned`/`ISpawnStatConfig` on the real enemy is DONE (integration branch); the prefab swap remains.
- `FloorGenerator` / procedural floors — Sprint 11+.
- Drops / economy / upgrades — later sprints.
