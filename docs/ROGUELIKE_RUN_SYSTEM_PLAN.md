# Roguelike Run System — Execution Plan

> Planning-only document. No Unity assets are created or modified by this plan.
> Evidence labels: `[EXISTS]` / `[PARTIAL]` / `[MISSING]` / `[PROPOSED]` / `[TEAM DECISION]` / `[BLOCKED]` / `[FUTURE]`
> Source-of-truth docs: `docs/ROGUELIKE_SYSTEM.md`, `docs/ROGUELIKE_IMPLEMENTATION_PLAN.md`, `docs/ROGUELIKE_SPRINT_PLAN.md`, `docs/ARCHITECTURE.md`.

> **Status note (cleanup branch `fix/roguelike-spawn-cleanup`):** the `WeaponData` data container this plan references was **removed as obsolete** — the merged Combat system owns weapons via `Assets/Scripts/Combat/Objects/WeaponObject.cs` + `AttackData.cs`. See `docs/ROGUELIKE_IMPLEMENTATION_PLAN.md` for the same note.

---

## 1. RUN SYSTEM RESPONSIBILITY

### What the Run System owns

The Run System owns the **run structure and progression flow** — not how the character moves, not how enemies think.

| Owns | Detail | Status |
|---|---|---|
| Run lifecycle | start → floors → death/victory → lobby | `[PROPOSED]` — `ROGUELIKE_SYSTEM.md:32-54` |
| Run state | which state the run is in (`RunState`) | `[MISSING]` → **Sprint 1** |
| Floor progression | advancing floor N → N+1 | `[MISSING]` → Sprint 3 |
| Floor lifecycle | start → active → clear per floor | `[MISSING]` → Sprint 3 |
| Current floor number | floor index tracked in `RunData` | `[MISSING]` → Sprint 2 |
| Run start / floor start triggers | explicit transition requests | `[MISSING]` → Sprint 1 + 3 |
| Floor clear detection | knows *when* a floor is done (delegates *what* clears to Enemy/Spawn) | `[MISSING]` → Sprint 5 |
| Run end | run over → summary/lobby | `[MISSING]` → Sprint 5 |

### What the Run System does NOT own

| System | Owned by | Run System must NOT |
|---|---|---|
| Player | Player dev | touch `PlayerController` / `PlayerEntity` movement or combat code |
| Enemy | Enemy dev | touch `EnemyController` / enemy states / `EnemyEntity` |
| Weapon/Combat | `[TEAM DECISION]` | own the swing/attack logic (only `WeaponData` data container) |
| Upgrade (selection/application) | `[TEAM DECISION]` — likely Roguelike trigger + team stats | own the stat-application math until team decides |
| Spawn | `[TEAM DECISION]` — `EnemyController.cs:36-43` states cost-based spawn **intent**; `SpawnSystem` is `[MISSING]` | own enemy prefabs / placement logic |
| UI | UI dev (`[MISSING]`) | build panels; Run provides data + events only |
| Save / Meta | `[MISSING]` — proposed Roguelike | build until Sprint 9–10 |

**Ownership rule:** Run owns the *flow and state*, other systems own the *behavior*. Unclear ownership is marked `[TEAM DECISION]` and must be resolved before the related sprint (see §7).

---

## 2. RUN STATE MACHINE

### Verification against repository docs

`ROGUELIKE_SYSTEM.md:39-51` proposes exactly this flow:

```
LobbyState ──Start──► FloorStartState
                            │ generate floor N + spawn budget
                            ▼
                      FloorActiveState ◄────┐
                       (player died)   (room cleared)   │
                            │               │            │
                     RunEndState     FloorClearedState  │ (upgrade picked)
                            │               └───────────┘
                      return to Meta Lobby
```

Names are preserved verbatim: **Lobby, FloorStart, FloorActive, FloorCleared, RunEnd**.

### States

| State | Meaning | Created in |
|---|---|---|
| `Lobby` | pre-run / between runs | **Sprint 1** (enum value) |
| `FloorStart` | floor is being set up | Sprint 1 |
| `FloorActive` | combat in progress | Sprint 1 |
| `FloorCleared` | room cleared, upgrade choice pending | Sprint 1 |
| `RunEnd` | run over (death or victory) | Sprint 1 |

### Valid transitions

```
Lobby ──Start──► FloorStart ──► FloorActive ──► FloorCleared ──► FloorStart (N+1)
                                        │               │
                                        │ (player died) │ (victory / end run)
                                        ▼               ▼
                                     RunEnd ──(return to lobby)──► Lobby
```

| From | To | Trigger | Requesting system |
|---|---|---|---|
| Lobby | FloorStart | `StartRun()` | Run System (via UI) |
| FloorStart | FloorActive | floor setup finished | Run System (FloorStart logic) |
| FloorActive | FloorCleared | floor clear reported | Spawn/Enemy system reports clear |
| FloorActive | RunEnd | player died | Player system reports death |
| FloorCleared | FloorStart | next floor (N+1) | Run System (after upgrade pick) |
| FloorCleared | RunEnd | victory / end run | Run System |
| RunEnd | Lobby | return to lobby | Run System (via UI) |

### Invalid transitions (rejected)

- Lobby → anything except FloorStart (e.g. Lobby → FloorActive, Lobby → RunEnd)
- FloorStart → anything except FloorActive
- FloorActive → Lobby, FloorActive → FloorStart (can't restart mid-floor)
- FloorCleared → Lobby, FloorCleared → FloorActive
- RunEnd → FloorStart / FloorActive / FloorCleared (must go through Lobby)
- Any self-transition (e.g. Lobby → Lobby)

### Implementation rule (Sprint 1)

The state machine is a **plain C# class with no MonoBehaviour, no UnityEngine dependency, no singletons, no events** — a transition table + `CurrentState`. This keeps it deterministic and unit-testable without Unity (`ROGUELIKE_SYSTEM.md:11` proposes a generic FSM clone of `EnemyStateMachine`; we start simpler with a hardcoded enum + transition table, and can generalize later).

---

## 3. RUN DATA

Separate **static configuration** from **runtime run state**.

| Field | Kind | Value | Where defined | Sprint |
|---|---|---|---|---|
| `floor` | runtime | int (current floor) | `ROGUELIKE_SYSTEM.md:182` `RunData.floor` | 2 |
| `clearedRooms` | runtime | int | `ROGUELIKE_SYSTEM.md:183` | 2 |
| `enemyBudget` | runtime | float | `ROGUELIKE_SYSTEM.md:194` | 8 |
| `enemyBudgetGrowth` | static config | float 1.4 | `ROGUELIKE_SYSTEM.md:195` | 8 |
| `enemyStatGrowth` | static config | float 1.12 | `ROGUELIKE_SYSTEM.md:196` | 8 |
| run seed | runtime | — | **NOT in current architecture** — do NOT invent | `[PROPOSED]` only if procedural needs it (Sprint 11+) |

**Do NOT add fields "because roguelikes commonly have them."** Only repository/design-supported fields are included. The run seed is explicitly `[PROPOSED]` and deferred.

`RunData` as a `[Serializable]` bag (mirroring `PlayerContext`) is **Sprint 2**, not Sprint 1.

---

## 4. FLOOR LIFECYCLE

```
Run Start → Floor Start → Floor Active → Floor Clear → Next Floor (repeat)
```

| Step | What | Who | Status |
|---|---|---|---|
| Run Start | transition Lobby → FloorStart | Run System | Sprint 1 (transition only) |
| Floor Start | generate floor arena + spawn budget | FloorGenerator + SpawnSystem | `[MISSING]` → Sprint 3/4 |
| Floor Active | combat runs; floor marked active | Run System (state) | Sprint 1 (state only) |
| Floor Clear | all enemies dead / objective met | Spawn/Enemy system **reports** | `[MISSING]` → Sprint 5 |
| Next Floor | FloorCleared → FloorStart (N+1) | Run System | Sprint 5 |

**Contracts needed (documented, not yet implemented):**
- Spawn/Enemy → Run: **"floor cleared"** signal (all enemies dead).
- Player → Run: **"player died"** signal (health ≤ 0).
- Run → FloorGenerator/SpawnSystem: **"start floor N"** request.
- Run → Upgrade/UI: **"upgrade opportunity"** event.

These are only *interfaces/contracts*; the external systems themselves are built in later sprints.

---

## 5. SYSTEM CONTRACTS

Only contracts actually required by the design, marked `[PROPOSED]` (not built in Sprint 1).

| Provider | → | Consumer | Contract | Sprint |
|---|---|---|---|---|
| Spawn/Enemy | → | Run | `FloorCleared` (all enemies dead) | 5 |
| Player | → | Run | `PlayerDied` (health ≤ 0) | 5 |
| Run | → | Spawn | `StartFloor(budget, floorN)` | 4 |
| Run | → | Upgrade/UI | `UpgradeOpportunity` (after FloorCleared) | 6 |
| Upgrade | → | Run | `UpgradeApplied` → next floor | 6 |
| Run | → | UI | `FloorChanged(n)`, `RunEnded(summary)` | 9+ |
| GameManager | → | Run | bootstrap: instantiate/start run | 3 (`GameManager.cs` is cursor-lock only `[EXISTS:7-11]`) |

`GameManager` currently only locks the cursor (`Assets\Scripts\GameManager.cs:7-11`) — it is the future bootstrap point, **not modified in this sprint**.

---

## 6. DEPENDENCY GRAPH

```
GameManager (bootstrap)  [EXISTS, untouched]        ← Core dev territory
    ↓ (Sprint 3+)
Run State Machine (Sprint 1)  ◄── INDEPENDENT ── can be built & tested now
    ↓
Run Data / runtime state (Sprint 2)                 ← depends on Sprint 1
    ↓
Floor Lifecycle (Sprint 3)                          ← depends on 2
    ↓
Spawn / Enemy Integration (Sprint 4)                ← depends on 3 + Enemy dev
    ↓
Floor Clear (Sprint 5)                              ← depends on 4
    ↓
Reward / Upgrade (Sprint 6-7)                       ← depends on 5
    ↓
Next Floor / scaling (Sprint 8)                     ← depends on 7
    ↓
Meta progression (Sprint 9) / Save (Sprint 10)      ← depends on 8
    ↓
Procedural floors (Sprint 11+)                      ← depends on 10 + NavMesh spike
```

**Independent now:** Sprint 1 (Run State Machine) — zero external dependencies, testable with plain C#. Marked `◄── INDEPENDENT`.

---

## 7. TEAM RESPONSIBILITY MATRIX

| Feature | Owner | Run Dependency | Run Responsibility | External Responsibility |
|---|---|---|---|---|
| Run state | Me (Roguelike) | none | define `RunState`, transitions | — |
| Floor state | Me (Roguelike) | none | floor lifecycle states | — |
| Enemy death | Enemy dev | `OnDied` event `[EXISTS EnemyEntity.cs:22]` | subscribe later for clear detection | Enemy dev owns the event |
| Floor clear detection | Me (Roguelike) | spawn/enemy clear report | listen for "all dead" | Spawn system reports count |
| Enemy spawning | `[TEAM DECISION]` | `EnemyController.cs:36-43` intent | request spawn with budget | who owns `SpawnSystem`? |
| Player death | Player dev | health ≤ 0 `[EXISTS]` | listen for death → RunEnd | Player dev exposes health |
| Upgrade trigger | Me (Roguelike) | FloorCleared state | emit `UpgradeOpportunity` | UI dev builds panel |
| Upgrade application | `[TEAM DECISION]` | stats model | `[PROPOSED]` stat application | `PlayerEntity` owns stats |
| UI | UI dev | `[MISSING]` | provide data/events | UI dev builds panels |
| Save | Me (Roguelike) | `[MISSING]` | Sprint 10 | — |
| Meta progression | Me (Roguelike) | `[MISSING]` | Sprint 9 | — |

Unresolved ownership is explicitly `[TEAM DECISION]`:
- Who owns `SpawnSystem` / cost-based spawning.
- Who owns upgrade application to stats.
- Who owns `WeaponData` consumption (combat).

---

## 8. SPRINT ROADMAP (Run-System-centered)

Each sprint: Goal / Scope / Tasks / Files / Dependencies / Owner / Collaboration / Tests / Definition of Done / Out of Scope / Blockers.

### Sprint 1 — Run State Machine Foundation
- **Goal:** smallest testable representation of the run lifecycle.
- **Scope:** `RunState` enum (Lobby, FloorStart, FloorActive, FloorCleared, RunEnd), transition table, `CurrentState`, explicit transition method, reset-to-Lobby.
- **Tasks:** define enum; define valid/invalid transitions; implement machine; test all transitions.
- **Files:** `Assets\Scripts\Roguelike\RunState.cs`, `Assets\Scripts\Roguelike\RunStateMachine.cs` **[NEW]**.
- **Dependencies:** none.
- **Owner:** Me.
- **Collaboration:** none required.
- **Tests:** §10 list (10 tests), run outside Unity.
- **DoD:** enum + valid transitions + invalid rejected + current state tracked + tests pass + no Player/Enemy/Combat changes + no Unity assets + no RunManager.
- **Out of scope:** RunManager, RunData, floors, spawn, upgrades, UI, saving.
- **Blockers:** none.

### Sprint 2 — Run Data / Runtime State
- **Goal:** `RunData` bag for runtime run state.
- **Scope:** `[Serializable] RunData` (floor, clearedRooms, budget/growth from `ROGUELIKE_SYSTEM.md:194-196`), `StartNewRun()`.
- **Files:** `Assets\Scripts\Roguelike\RunData.cs` **[NEW]**.
- **Dependencies:** Sprint 1.
- **Owner:** Me.
- **Tests:** run-reset, floor advance.
- **DoD:** data bag exists, compiles, no assets.
- **Out of scope:** anything but the bag.
- **Blockers:** none.

### Sprint 3 — Floor Lifecycle
- **Goal:** floor states drive progression (FloorStart → FloorActive → FloorCleared).
- **Scope:** state behaviors, current-floor tracking.
- **Dependencies:** Sprint 2; `GameManager` bootstrap coordination (Core dev).
- **Owner:** Me + Core dev (bootstrap only).
- **Tests:** run start → floor 1 active.
- **Blockers:** none.

### Sprint 4 — Enemy/Spawn Integration
- **Goal:** Run requests spawns by budget; spawn system reports.
- **Scope:** `SpawnSystem` (or delegate to owner), budget plumbing.
- **Dependencies:** Sprint 3; **`[TEAM DECISION]` SpawnSystem owner**; Enemy dev `[EXISTS:36-43]`.
- **Blockers:** owner decision; `DieState` crash fix (`[PARTIAL — crashes]`) if we need clean enemy death.

### Sprint 5 — Floor Clear / Transition Flow
- **Goal:** "all enemies dead" → FloorCleared → next floor (or RunEnd).
- **Dependencies:** Sprint 4; Player death signal.
- **Blockers:** `DealDamage.cs:14-18` damage commented out (`[PARTIAL — commented out]`) blocks "player can die".

### Sprint 6 — Upgrade Trigger Integration
- **Goal:** FloorCleared emits `UpgradeOpportunity`.
- **Dependencies:** Sprint 5; UI dev panel `[MISSING]`.
- **Blockers:** UI dev availability.

### Sprint 7 — Rewards / Economy Integration
- **Goal:** drops/currency on kill (`OnDied` `[EXISTS EnemyEntity.cs:22]`).
- **Dependencies:** Sprint 5; `[TEAM DECISION]` economy model.

### Sprint 8 — Enemy Scaling
- **Goal:** per-floor budget/stat growth (`ROGUELIKE_SYSTEM.md:195-196`).
- **Dependencies:** Sprint 7; `[TEAM DECISION]` stats owner.

### Sprint 9 — Meta Progression
- **Goal:** persistent unlocks across runs.
- **Dependencies:** Sprint 8.

### Sprint 10 — Save System
- **Goal:** persist meta data (PlayerPrefs/JSON per `ROGUELIKE_SYSTEM.md:846-868`).
- **Dependencies:** Sprint 9.

### Sprint 11+ — Procedural Floor Integration
- **Goal:** runtime floor generation.
- **Dependencies:** Sprint 10; **highest risk: runtime NavMesh rebuild** (`ROGUELIKE_SYSTEM.md:991`, `ROGUELIKE_SPRINT_PLAN.md` §14).
- **Blockers:** NavMesh spike first.

---

## 9. UNITY EDITOR DEFERRED WORK (C# CODE FIRST, EDITOR LATER)

Must be done in the Unity Editor **later** — NOT now:

- `RunManager` GameObject with the state machine wired (Sprint 3+).
- `SpawnSystem`/`FloorGenerator` components + scene references (Sprint 4+).
- Upgrade/GameOver UI panels (Sprint 6/9) — UI dev.
- Scene transitions and any scene-level wiring.
- Data assets (weapon/archetype/room `.asset`) — other sprints.
- NavMesh runtime rebuild.

This sprint is **code-only**; nothing above is touched.

---

## 10. TESTING STRATEGY

The state machine is **pure C#** (no `UnityEngine`), so it is tested outside Unity with a small console harness compiled by `dotnet`.

| # | Test | Expected |
|---|---|---|
| 1 | Initial state is Lobby | `CurrentState == Lobby` |
| 2 | Lobby → FloorStart succeeds | returns true, state updates |
| 3 | FloorStart → FloorActive succeeds | returns true, state updates |
| 4 | FloorActive → FloorCleared succeeds | returns true, state updates |
| 5 | FloorCleared → FloorStart succeeds | returns true, state updates |
| 6 | FloorCleared → RunEnd succeeds (documented end-run path) | returns true, state updates |
| 7 | Invalid transitions fail (e.g. Lobby → FloorActive, FloorActive → FloorStart, RunEnd → FloorActive) | returns false |
| 8 | Current state updates after each successful transition | assert after each call |
| 9 | Failed transition does not change current state | state unchanged after failure |
| 10 | Reset returns to Lobby | Reset() → `CurrentState == Lobby` |

Project test status: `com.unity.test-framework` is in `Packages\manifest.json:12` `[EXISTS]` but **no test assembly / folder exists** `[MISSING]`. Decision: Sprint 1 uses an external dotnet harness (no Unity test infra created). A Unity Test Runner suite can be added when the team standardizes test infrastructure `[TEAM DECISION]`.

---

## 11. RISKS

| Risk | Mitigation |
|---|---|
| RunManager becomes a God Object | keep it a thin hub (state + data); delegate behavior to other systems |
| Mixing Run and Spawn responsibilities | Run requests, Spawn/Enemy reports; no spawn code inside Run |
| Mixing Run and Upgrade responsibilities | Run emits events; upgrade application lives elsewhere |
| Scene lifecycle coupling | state machine is plain C#, decoupled from scenes |
| Player/Enemy ownership conflicts | `[TEAM DECISION]`; Run only subscribes to existing events |
| Event contract ambiguity | contracts documented in §5 before implementation |
| Runtime state vs static data confusion | §3 separation; data bags only for runtime state |
| Unity Editor dependencies | Sprint 1 is pure C#; Editor work deferred to §9 |
| No test infra in project | external dotnet harness for Sprint 1 |

---

## 12. BEGINNER-FRIENDLY EXECUTION ORDER

1. Create `RunState.cs` — a plain C# enum with the 5 documented states.
2. Create `RunStateMachine.cs` — plain C# class with a transition table, `CurrentState`, `TryTransition`, `Reset`.
3. Verify: compile + run the 10 tests from §10 with `dotnet` (no Unity needed).
4. Report. Do NOT open Unity yet.

---

# WHAT TO DO FIRST

1. **Write `RunState.cs`** — enum: `Lobby, FloorStart, FloorActive, FloorCleared, RunEnd`.
2. **Write `RunStateMachine.cs`** — transition table (Lobby→FloorStart→FloorActive→FloorCleared→FloorStart / RunEnd, FloorCleared→RunEnd, RunEnd→Lobby), `CurrentState` property, `TryTransition(RunState)` returning bool (no change on failure), `Reset()` → Lobby.
3. **Verify with dotnet** — compile the two `.cs` files with a tiny console harness and run the 10 tests in §10; all must pass.
4. **Stop.** No RunManager, no RunData, no Unity assets, no Player/Enemy/Combat/UI changes.

Sprint 1 is the smallest possible foundation: **an enum + a transition table.**
