# Roguelike Spawning — Sprint 5 (Floor Unlock + Composition Selection)

> **Status:** IMPLEMENTED — floor-based enemy unlocking (`SpawnTable.unlockInterval`) and cached
> ranked composition selection (`EnemyCompositionSelector` / `EnemyComposition`) landed on top of the
> Sprint 4 SpawnSystem foundation. Code-level verification done (external dotnet harness, 218 checks).
> Unity Play Mode verification **NOT run** (no Unity Editor on this machine) — deferred.
> Evidence labels: `[EXISTS]` / `[PARTIAL]` / `[MISSING]` / `[PROPOSED]` / `[TEAM DECISION]` / `[BLOCKED]` / `[FUTURE]` / `[WAITING FOR ENEMY SYSTEM]`
> Base design: `docs/ROGUELIKE_SYSTEM.md §4.5` + new **§4.5a** (floor unlock & composition selection).
> Prior sprint: 4 (`af63693`, SpawnSystem foundation + Run↔Spawn floor-clear integration).

---

## Goal

Replace the greedy random "spend until the budget runs out" fill with:
1. **Floor-based enemy availability** — new archetypes unlock over the run by floor, configured by a
   single data-driven interval (default 3). No hardcoded enemy types or floor numbers.
2. **Composition selection** — a floor's enemies are a *composition* chosen from a precomputed/cached
   ranked set: budget is a MAXIMUM (not exact), the target enemy count is satisfied first, budget use
   is optimized, and variety/randomness only apply between equally-ranked compositions.

---

## Changes (all under `Assets/Scripts/Roguelike/Spawning/`)

| File | Change |
|---|---|
| `SpawnTable.cs` | + `unlockInterval` (default 3), `UnlockInterval` (guarded ≥ 1), `AvailableForFloor(floor)` — pool prefix whose unlock floor `1 + i*interval` is reached |
| `EnemyArchetype.cs` | + `displayName` (editor/debug summaries only) |
| `EnemyComposition.cs` | **NEW** — immutable snapshot: chosen `EnemyArchetype[]`, `TotalCost`, `DistinctTypes`, `Count`. Reused across spawns (no per-enemy allocation) |
| `EnemyCompositionSelector.cs` | **NEW** — pure instance-based solver + cache keyed on `(floor, target, budget)`; enumerates combinations-with-repetition once per key, ranks (best budget use → most distinct types → random), no per-spawn search |
| `SpawnSystem.cs` | `Populate` now: `AvailableForFloor(floor)` → target count (`floor(budget/cheapest)`) → cached ranked compositions → pick → spawn. + `SpawnFallback` (deterministic cheapest fill, never silently invalid), `LastCompositionInfo`, `CachedCompositionKeys` |
| `SpawnTestDebugDisplay.cs` (TEST-ONLY) | HUD + one `Comp: ...` line reading `LastCompositionInfo` |

Untouched: `RunData`/`RunController`/`RunStateMachine` (target count is derived from the existing
budget, not a new data field), the real Enemy System, Player, Combat, production UI, scenes.

## Design decisions

- **Unlock is index-based on the SpawnTable list, not per-archetype floor fields.** One
  `unlockInterval` config; index `i` unlocks at floor `1 + i*interval`. Add new enemies to the END of
  the list. Fully data-driven, matches "interval 3 → floors 1–3 E1, 4–6 E1+E2, 7–9 +E3".
- **Target count is derived, not stored.** `floor(budget / cheapest available cost)` is deterministic
  and always achievable (`target × cheapest ≤ budget`), so a valid composition always exists and all
  prior budget/count expectations hold (budget 10 → 3, 14 → 4, 19.6 → 6). The derivation is one named
  method (`TargetCountFor`) where an explicit target-count design can slot in later.
- **Budget is a maximum.** Compositions must satisfy `total ≤ budget`; "best budget use" picks the
  highest total ≤ budget (e.g. `3+3+4=10` beats `3+3+3=9` at budget 10).
- **Unlock never forces a new enemy in.** Example (harness-verified): pool `{3,4}`, interval 3 —
  floor 4 unlocks E2, but at budget 27.44 (10×1.4³) the best composition is 9×E1 (27); any E2 would
  push the total to 28 > 27.44. E2 appears at budget 28 (`8×E1 + 1×E2 = 28`).
- **No redesign of the Run System.** Progression is still RUN → FLOOR → CLEAR → NEXT FLOOR; the
  driver bridge and `Populate(budget, floor)` contract are unchanged. No room-based spawning.

---

## Tests (external dotnet harness, `%TEMP%\opencode\spawn_integration_test\`)

Compiles the **actual** `Spawning/*.cs` sources; was 167 checks (Sprint 4) and is now **218**.

| # | Scenario | Result |
|---|---|---|
| 10 | Floor unlock: interval 3 → floors 1/3 = 1 type, 4/6 = 2 types, 7 = all; interval 1 → one type per floor | **PASS** |
| 11 | Composition ranking (solver): budget-max (`3+3+3=9 ≤ 10`), invalid (`3+3+7=13 > 12` rejected), best budget use (`3+3+4=10` beats `9`), variety tie-break (`{3,4,5}` beats `{4,4,4}` at cost 12), controlled randomness only between two equally-ranked candidates at cost 13 | **PASS** |
| 12 | Populate-level: floor 1 all-E1 (E2 locked), floor 4 `3+3+4=10` — composition end-to-end | **PASS** |
| 13 | Cache: one key per `(floor, target, budget)`, repeat `Get` returns the same cached instance, two same-floor Populates reuse one key | **PASS** |
| 14 | Floor progression through unlock with real budget growth (10→14→19.6→27.44): floors 1–3 E1-only, floor 4 unlocks E2 but doesn't force it (9×E1=27 ≤ 27.44), E2 appears at budget 28 (`8×E1+1×E2=28`) | **PASS** |
| — | Sprint 4 regression rows 1–9 + 13–14 (budget, distinct points, scaling, driver integration 1→2→3, HUD) | **PASS** |

## Unity Play Mode (user-tested; no Editor here)

Expected behaviour with the existing single cost-3 test archetype: identical to Sprint 4 (floors
auto-advance 1→2→3, counts 3/4/6). With a multi-archetype table, the HUD's `Comp:` line shows the
live floor / available types / target / composition / cost.

---

## Definition of Done

- [x] Floor-based unlock, configurable interval (default 3), no hardcoded types/floors.
- [x] Unlocking only expands the pool; a new enemy is never guaranteed to spawn (verified at floor 4).
- [x] Budget is a maximum; target count satisfied first; best budget use; variety only among equivalent; controlled randomness last.
- [x] Precomputed/cached compositions keyed on (pool=floor, target, budget); no per-spawn search, no retry loops.
- [x] Documented deterministic fallback (never a silently invalid composition).
- [x] Dotnet harness 218/218 PASS (unlock, budget-max, invalid, best-budget, variety, cache-reuse, floor progression).
- [x] No `RunData`/`RunController`/Enemy/Player/Combat/production-UI/scene edits.
- [ ] Play Mode verification (deferred — no Editor).

---

## Risks / Blockers

| Risk / Block | Mitigation |
|---|---|
| Real Enemy System still not ready | Unchanged from Sprint 4: isolated `TestEnemy`, archetype `prefab` swap later |
| Target count is derived from budget, not a designed value | Single named derivation (`SpawnSystem.TargetCountFor`); an explicit target field slots in there |
| Composition enumeration growth | Combinations-with-repetition, bounded by pool size × target (≤ ~10k worst case), computed once per cached key, pruned when partial cost exceeds budget |
| Unity Editor unavailable | Play Mode deferred; harness + static diff verification only |
