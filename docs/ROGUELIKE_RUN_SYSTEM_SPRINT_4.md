# Roguelike Run System — Sprint 4 Investigation (Enemy/Spawn Integration)

> **Status:** PLANNING / INVESTIGATION ONLY — Sprint 4 is **NOT implemented**. This document reports what Sprint 4 actually requires given the current repository, what is `READY NOW`, and what is `[BLOCKED — TEAM DEPENDENCY]`.
> Evidence labels: `[EXISTS]` / `[PARTIAL]` / `[MISSING]` / `[PROPOSED]` / `[TEAM DECISION]` / `[BLOCKED]` / `[FUTURE]`
> Base plan: `docs/ROGUELIKE_RUN_SYSTEM_PLAN.md` (§8 Sprint 4 = "Enemy/Spawn Integration").
> Prior sprints: 1 (`a473850`), 2 (`2e1d63e`), 3 (`cc11a51`, `RunController`).

> ## ⚠️ SUPERSEDED (2026-08-16) — Sprint 4 enemy/spawn integration IMPLEMENTED
> The integration this document described as "NOT implemented / BLOCKED — team dependency" has been
> built on branch `fix/enemy-spawn-integration`: `SpawnSystem` (`Populate`, `ApplyFloorScaling`,
> `AliveCount`, `FloorCleared`) exists, `EnemyController : IEnemySpawned` surfaces `EnemyEntity.OnDied`
> (death-only contract), health scaling flows through the existing `EnemyEntity.SetMaxHealth`, and floor
> scaling flows through the **`ISpawnStatConfig`** seam owned by `SpawnSystem`. The current contract
> is in **`docs/ENEMY_SPAWN_INTEGRATION.md`**; the Sprint 4 Spawning report is
> `docs/ROGUELIKE_SPAWNING_SPRINT_4.md`. Lines below saying `needs EnemyEntity setters` / death hook
> `5+ future` are stale. Remaining real-enemy blocker: the production prefab (`TreeEntAsh.prefab`) is
> still unfinished (see the integration doc) — not a code gap.

---

## # Goal

From `ROGUELIKE_RUN_SYSTEM_PLAN.md §8`:

> **Sprint 4 — Enemy/Spawn Integration. Goal:** Run requests spawns by budget; spawn system reports.
> **Dependencies:** Sprint 3; `[TEAM DECISION]` SpawnSystem owner; Enemy dev `[EXISTS:36-43]`.
> **Blockers:** owner decision; `DieState` crash fix (`[PARTIAL — crashes]`) if we need clean enemy death.

Concretely, Sprint 4 = build the **cost-based enemy spawner** that the design comment at `EnemyController.cs:36-43` already intends (archetype cost, floor budget, cost scaling), wire it to the run budget in `RunData`, and give the Run System a way to know **how many enemies are still alive** (needed by Sprint 5 floor-clear). Floor layout/procedural generation is **NOT** Sprint 4 (`FloorGenerator` is Sprint 11+; first floors reuse the existing arena).

---

## # Current Repository Evidence

| Item | Status | Evidence |
|---|---|---|
| Run state machine + RunData | `[EXISTS]` (Sprints 1–2) | `RunStateMachine.cs`, `RunData.cs` |
| RunController thin hub | `[EXISTS]` (Sprint 3, `cc11a51`) | `RunController.cs` — `Data.enemyBudget`, `Data.floor`, `BeginFloor()` |
| Enemy entity with `OnDied` event | `[EXISTS]` | `EnemyEntity.cs:22` `OnDied`; poise loop `:38-62` |
| Enemy hub wiring death → DieState | `[EXISTS]` | `EnemyController.cs:103` `OnDied += HandleDied` → `SetState<DieState>()` |
| Cost-based spawn **intent** | `[EXISTS]` (comment only) | `EnemyController.cs:36-43` (archetype cost, budget, level gating) |
| EnemyEntity stat accessors | `[EXISTS — read-only]` | `EnemyEntity.cs:15-18` — no setters except `SetMaxHealth` `:31` |
| Enemy death | `[PARTIAL — crashes]` | `DieState.cs:11-18` — `agent`/`animator` never assigned → NRE on `Enter` |
| Damage to player | `[PARTIAL — commented out]` | `DealDamage.cs:14-18` — `TakeDamage` call commented |
| SpawnSystem / SpawnPoint / EnemyArchetype / SpawnTable | `[MISSING]` | grep `SpawnSystem|SpawnPoint|EnemyArchetype|FloorGenerator` → **no hits** |
| Alive count / clear reporting | `[MISSING]` | grep `AliveCount` → no hits |
| Spawn point markers in scene | `[MISSING]` | TestingScene has 2 enemies, no `SpawnPoint` components |
| Full enemy prefab | `[PARTIAL]` | `Assets\prefabs\TreeEntAsh.prefab` (full wrapper) but carries 2 missing-script components; scene uses raw FBX for the active enemy |
| Player tag | `[MISSING]` | TagManager has only `Enemy`; `Pickup`/spawn interactions later need `Player` |
| GameManager bootstrap | `[EXISTS:7-11]` | cursor lock only; Core dev territory |

---

## # Existing Dependencies

Already available and owned by the Run System:

1. `RunData.enemyBudget` (10f) and `RunData.floor` — the spawn budget/floor inputs (`RunData.cs:12,8`).
2. `RunStateMachine` `FloorStart → FloorActive` — the transition a spawner completes after filling the floor (`RunStateMachine.cs:8`).
3. `RunController.BeginFloor()` — public hook a spawner (or GameManager) calls once floor setup finishes (`RunController.cs:12`).
4. `EnemyEntity.Initialize()` + `SetMaxHealth` — minimal stat override surface (`EnemyEntity.cs:25-36`).
5. `EnemyController` — enemy prefab wrapper: NavMeshAgent, Animator, FSM, `EnemyEntity` (`EnemyController.cs:94-120`).
6. `EnemyEntity.OnDied` — already used for death; reusable for future drops/clear counting (Sprint 5+).
7. ScriptableObject precedent — `PatrolRoute` + `routeA.asset` (the pattern for `EnemyArchetype`/`SpawnTable` assets).

---

## # Missing Dependencies

| Dependency | Owner | Why needed |
|---|---|---|
| `SpawnSystem` class + scene component | `[TEAM DECISION]` — plan says "SpawnSystem (or delegate to owner)" | the actual spawner |
| `EnemyArchetype` ScriptableObject | `[TEAM DECISION]` | cost + scaling + prefab reference per enemy type |
| `SpawnTable` ScriptableObject | `[TEAM DECISION]` | archetype pool + budget curve |
| `SpawnPoint` marker component | `[TEAM DECISION]` | where enemies spawn in the arena |
| `EnemyEntity` setters (`SetBaseDamage`, `SetBaseDefense`, `SetMaxPoise`) | **Enemy dev** | spawn-time per-floor stat scaling (`ROGUELIKE_SYSTEM.md §6 #4`) |
| `DieState` crash fix | **Enemy dev** | clean enemy death so spawned enemies are killable and floors can clear |
| Clean enemy prefab (no missing scripts) | **Enemy dev / content** | archetype `prefab` field must point at a working wrapper |
| SpawnPoint markers placed in the scene | **Unity Editor work** | `[MISSING]` — not code |
| `.asset` data files (archetypes, spawn table) | **Unity Editor work** | `[MISSING]` — not code |

---

## # Team Ownership

| System | Owner | Run System must NOT |
|---|---|---|
| Enemy (entity, states, death) | **Enemy dev** | edit `EnemyController`/`EnemyEntity`/enemy states; only request the setters |
| SpawnSystem | **`[TEAM DECISION]`** — the run plan lists this as unresolved; the older `ROGUELIKE_SPRINT_PLAN.md §7` assigned the cost-based spawner to "Me (Roguelike)". **Conflict between docs** → needs a team answer before writing it | own enemy prefabs / placement logic if another owner is named |
| Weapon/Combat | `[TEAM DECISION]` | own the swing/damage logic |
| GameManager bootstrap | **Core dev** | modify `GameManager.cs` without coordination |
| Player | **Player dev** | touch player movement/stats |

**Key conflict:** `ROGUELIKE_RUN_SYSTEM_PLAN.md §8` marks SpawnSystem owner `[TEAM DECISION]`, while the older `ROGUELIKE_SPRINT_PLAN.md §7` names "Me" as owner. This must be resolved before writing any spawn code.

---

## # Architecture

Per `ROGUELIKE_SYSTEM.md §4.5` (design sketch, `[PROPOSED]`), adapted to what exists today:

```
RunController (Sprint 3, [EXISTS])          ← owns RunData (budget/floor) + state machine
   │  Data.enemyBudget, Data.floor
   ▼
SpawnSystem (MonoBehaviour)  [NEW — owner?]
   ├── SpawnTable (SO)  [NEW] ── List<EnemyArchetype>
   │                              └── EnemyArchetype (SO) [NEW]: cost, prefab, per-floor growth
   ├── SpawnPoint (MonoBehaviour marker)  [NEW]
   ├── Populate(budget, floor)  → instantiate enemies at SpawnPoints until budget spent
   ├── ApplyFloorScaling(enemy, archetype, floor)   ← needs EnemyEntity setters (Enemy dev)
   └── AliveCount() : int          ← reports to Run System (floor-clear, Sprint 5)
        │
        ▼
   calls RunController.BeginFloor()  once floor is populated   (FloorStart → FloorActive)
```

Design intent (from `EnemyController.cs:36-43`): high-cost enemies are gated from early floors; total spent cost ≈ budget. First floors reuse the existing arena — no `FloorGenerator`.

---

## # Required Contracts

Interfaces/seams between systems (documented, not yet implemented):

| Contract | Provider → Consumer | Signature | Sprint |
|---|---|---|---|
| Spawn request | Run → Spawn | `StartFloor(budget: float, floorN: int)` | 4 |
| Floor-ready signal | Spawn → Run | `RunController.BeginFloor()` (FloorStart → FloorActive) | 4 |
| Alive count | Spawn → Run | `AliveCount() : int` | 4 (consumed 5) |
| Stat scaling | Spawn → EnemyEntity | health `SetMaxHealth` (damage deferred — see `docs/ENEMY_SPAWN_INTEGRATION.md §7`) | 4 | **IMPLEMENTED (health)** (see banner) |
| Floor clear | Enemy/Spawn → Run | "all enemies dead" (`AliveCount() == 0`) | 5 | **IMPLEMENTED** (`SpawnSystem.FloorCleared`) |
| Death hook | Enemy → Run | `EnemyEntity.OnDied` via `IEnemySpawned.OnDied` | 5+ | **IMPLEMENTED** (see banner) |

Contract stability rule (from `ROGUELIKE_SPRINT_PLAN.md §10`): signature changes require a team heads-up before merge.

---

## # Implementation Tasks

Split by ownership and readiness:

**READY NOW — code-only, no team dependency (small):**
- (Optional) `EnemyArchetype.cs` + `SpawnTable.cs` + `SpawnPoint.cs` class skeletons under `Assets\Scripts\Roguelike\Spawning\` — **only if SpawnSystem ownership is confirmed as Roguelike**. Writing them before the ownership decision would duplicate or pre-empt another owner's work.

**BLOCKED — TEAM DEPENDENCY:**
- `SpawnSystem.cs` (`Populate`, `ApplyFloorScaling`, `AliveCount`) — **IMPLEMENTED** (integration branch; the owner decision, EnemyEntity setters and a working test prefab resolved it). A working REAL enemy prefab remains the only open item (`TreeEntAsh.prefab` unfinished — see `docs/ENEMY_SPAWN_INTEGRATION.md`).
- EnemyEntity scaling setters — **Enemy dev**.
- `DieState` fix — **Enemy dev** (required for killable spawns / floor clear verification).

**UNITY EDITOR WORK (deferred):**
- Scene: place `SpawnPoint` markers in the arena.
- Create archetype + spawn-table `.asset` files; point archetypes at `TreeEntAsh.prefab` (or a cleaned prefab).
- Put the `SpawnSystem` component on a scene GameObject and assign its references.
- Wire the spawner to `RunController.BeginFloor()` (or via the eventual `RunManager` wrapper).

**NOT Sprint 4:** `FloorGenerator`, procedural layouts, NavMesh runtime rebuild (Sprint 11+).

---

## # Unity Editor Requirements

`[REQUIRES UNITY EDITOR]` — a substantial part of Sprint 4 cannot be done from code alone:

- SpawnSystem is a **MonoBehaviour that `Instantiate`s prefabs** — it must live on a scene GameObject.
- `SpawnPoint` markers must be placed in the arena.
- `.asset` data files (archetypes, spawn table) must be authored in the Editor (CreateAssetMenu pattern like `PatrolRoute`).
- A clean enemy prefab must exist (TreeEntAsh has 2 missing-script components `[PARTIAL]`).
- Inspector wiring (archetype→prefab refs, table→archetype refs) is unavoidable.

The pure-C# portion (the `RunController` side, budget exposure, `BeginFloor`) is already `[EXISTS]` — no RunController change is required for Sprint 4.

---

## # Tests

Project has no Unity test assembly `[MISSING]`; spawn code depends on `UnityEngine` (`Instantiate`, `MonoBehaviour`) → **cannot be tested with the external dotnet harness**. Verification is Play Mode:

| # | Test | Pass criteria |
|---|---|---|
| 1 | Spawner spends the budget | floor spawns enemies whose total cost ≈ `enemyBudget` (10) |
| 2 | Cheap archetypes only on early floors | high-cost enemies absent at floor 1 (`EnemyController.cs:39-40` intent) |
| 3 | `AliveCount()` matches spawns | count = instantiated enemies still alive |
| 4 | Per-floor scaling applied | spawned enemy stats reflect `floor` growth (needs EnemyEntity setters) |
| 5 | Floor-ready transition | after populate, Run state is `FloorActive` (`BeginFloor()`) |
| 6 | Killable spawns | enemy death does not crash (`DieState` fix required) |

---

## # Definition of Done

Sprint 4 is complete only when:

- [ ] SpawnSystem owner confirmed `[TEAM DECISION]` resolved.
- [ ] `SpawnSystem` populates a floor from a budget and reports `AliveCount()`.
- [ ] Run transitions `FloorStart → FloorActive` once spawns are placed.
- [ ] Spawned enemies scale per floor (EnemyEntity setters provided by Enemy dev).
- [ ] Kills work without crashing (DieState fix).
- [ ] Play Mode tests 1–6 pass.
- [ ] No Player/Enemy code edited by Roguelike dev; no `GameManager.cs` change.

---

## # Risks

| Risk | Mitigation |
|---|---|
| SpawnSystem owner never confirmed | `[TEAM DECISION]`; resolve before writing spawn code |
| Doc conflict: run plan says `[TEAM DECISION]`, old sprint plan says "Me" | team call; both plans cited here |
| `EnemyEntity` setters never merged (Enemy dev) | floor scaling blocked; document exact signatures needed |
| `DieState` crash unfixed | spawned enemies die with NRE; floor clear unverifiable |
| Clean enemy prefab missing (2 missing scripts) | archetype `prefab` ref needs a working wrapper |
| Unity Editor work piles up | Sprint 4 cannot ship code-only; schedule an Editor session |
| RunController becomes the integration point for everything | Sprint 4 keeps spawn logic OUT of RunController — it only exposes budget + `BeginFloor` |

---

## # Blockers

- **`[BLOCKED — TEAM DEPENDENCY]` SpawnSystem ownership** — `ROGUELIKE_RUN_SYSTEM_PLAN.md §8`; unresolved → who writes `SpawnSystem.cs`.
- **`[BLOCKED — TEAM DEPENDENCY]` EnemyEntity stat setters** — `SetBaseDamage/SetBaseDefense/SetMaxPoise` (Enemy dev; only `SetMaxHealth` exists).
- **`[BLOCKED — TEAM DEPENDENCY]` DieState crash fix** — `DieState.cs:11-18` (Enemy dev).
- **`[REQUIRES UNITY EDITOR]`** spawn points, archetype/table assets, prefab cleanup, scene wiring.

---

## # Ready Now

- Nothing requires writing `SpawnSystem` before the ownership decision. The Run side is complete:
  - `RunData.enemyBudget` + `floor` `[EXISTS]` — the spawner reads these.
  - `RunController.BeginFloor()` `[EXISTS]` — the spawner calls this when done.
  - No `RunController` modification needed for Sprint 4.
- Optional pure-C# skeletons (`EnemyArchetype`/`SpawnTable`/`SpawnPoint`) are **only** writable if ownership lands on Roguelike — recommend waiting.

**Recommendation: do not start Sprint 4 implementation now.** It is gated on (1) a team ownership decision, (2) two Enemy-dev fixes, and (3) Unity Editor work.

---

## # Future / Deferred Work

- `FloorGenerator` / procedural floors — Sprint 11+ (`[FUTURE]`, NavMesh risk `ROGUELIKE_SYSTEM.md:991`).
- Floor-clear detection (`AliveCount() == 0` → FloorCleared) — Sprint 5 (depends on 4 + player-death signal).
- Drops/currency on `OnDied` — Sprint 7.
- Upgrade trigger — Sprint 6.
- `Player` tag, `Pickup`, economy — later.

---

## # Recommended Execution Order

1. **Team decisions first** — resolve SpawnSystem owner (conflict between the two plan docs), approve the required `EnemyEntity` setter signatures, approve archetype cost model (values from `EnemyController.cs:36-43` intent).
2. **Enemy dev** fixes `DieState` + adds the setters (small, unblocks everything).
3. **Unity Editor session** — place `SpawnPoint`s, clean `TreeEntAsh.prefab` (or build a clean grunt prefab), author archetype/table assets.
4. **Then implement** `SpawnSystem.cs` (`Populate`, `ApplyFloorScaling`, `AliveCount`) + wire `BeginFloor()`.
5. Run Play Mode tests 1–6.

**Until then, the Run System is at a natural pause.** Alternative independent work: none in the Run System chain that does not depend on a team decision or an Editor session — the serial roadmap (§8) forbids skipping. Recommend pausing Roguelike dev until the ownership decision + Enemy fixes land.

---

## Sprint 4 summary

- **Goal:** cost-based spawner (`EnemyController.cs:36-43` intent), budget plumbing, `AliveCount()`.
- **READY NOW:** nothing new — Run side already exposes budget + `BeginFloor()`; no `RunController` change needed.
- **BLOCKED — TEAM DEPENDENCY:** SpawnSystem owner decision, `EnemyEntity` setters, `DieState` fix.
- **REQUIRES UNITY EDITOR:** spawn points, archetype/table assets, prefab cleanup, scene wiring.
- **Verdict: Sprint 4 is NOT ready to implement right now.**

**Do NOT implement Sprint 4 yet.**
