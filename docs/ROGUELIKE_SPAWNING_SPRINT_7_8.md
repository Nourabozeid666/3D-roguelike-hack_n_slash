# Roguelike Spawning — Sprint 7 & 8 (Spawn Zones + High-Floor Wave Pacing)

> **Status:** IMPLEMENTED on `feature/roguelike-system`. Sprint 7 adds a data-driven rectangle
> `SpawnZone` placement strategy (`FixedPoints`/`RandomZone`) with a bounded validation pipeline;
> Sprint 8 adds high-floor wave pacing over a once-selected floor composition with correct
> "no alive AND no unspawned" FloorCleared semantics. Code-level verification done (external dotnet
> harness, **331 checks — ALL PASS**). Unity Play Mode verification **NOT run** (no Unity Editor on
> this machine) — deferred.
> Evidence labels: `[EXISTS]` / `[PARTIAL]` / `[MISSING]` / `[PROPOSED]` / `[TEAM DECISION]` / `[BLOCKED]` / `[FUTURE]` / `[WAITING FOR ENEMY SYSTEM]`
> Base design: `docs/ROGUELIKE_SYSTEM.md §4.5` / `§4.5a`. Prior sprints: 4–5 (spawn system +
> composition selection), 6 (run save persistence).

---

## Goal

1. **Spawn zones (Sprint 7)** — replace the single placement path (designer-placed `SpawnPoint`
   children) with a choice of strategy: FixedPoints (unchanged, for controlled/testing scenes) or
   RandomZone (a serialized rectangle region whose candidates are validated before spawn). Validation
   is a bounded pipeline: inside-bounds → ground/NavMesh → blocking geometry → distance from the
   player → distance from already-placed enemies. No hardcoded layers, no hardcoded floors, no
   infinite retry loops, no spawning into invalid geometry.
2. **Wave pacing (Sprint 8)** — on high floors, a floor's once-selected composition is sliced into
   waves: killing the current wave releases the next after a delay. `FloorCleared` fires only when
   NOTHING is alive AND no unspawned composition entries remain — a dead intermediate wave is NOT a
   clear. Composition selection and budget are never recomputed mid-floor.

---

## Changes (all under `Assets/Scripts/Roguelike/Spawning/`)

| File | Change |
|---|---|
| `SpawnStrategy.cs` | **NEW** — `enum SpawnStrategy { FixedPoints, RandomZone }`. |
| `SpawnZone.cs` | **NEW** — serialized rectangle: `size` (20×2×20), `centerOffset`, `blockingLayers` (mask), `useNavMeshValidation` (default true), `groundSampleRadius` (1), `minPlayerDistance` (5), `minEnemyDistance` (2), `maxAttempts` (20), `footprintRadius` (0.5). Props with guards; `RandomPoint()` deterministic; `OnDrawGizmosSelected` cyan wire cube. |
| `SpawnPlacementValidator.cs` | **NEW** — pure, statics-free validation pipeline, testable without Unity physics/NavMesh: `Contains`, `PassesDistanceRules`, `TryFindLocation` (bounded while over `zone.MaxAttempts`). |
| `SpawnPacingConfig.cs` | **NEW** — `waveStartFloor` (default int.MaxValue = off), `waveSize`, `waveDelaySeconds`. |
| `WavePlan.cs` | **NEW** — pure slice/cursor over one `EnemyComposition`: contiguous entries, `PeekNextWaveSize()`, `NextEntry()`, `MarkWaveReleased()`, `WaveCount` (ceil division). |
| `SpawnSystem.cs` | **modified** — strategy field, zone + player reference, pacing config; `Populate` selects the composition ONCE and builds a `WavePlan`; placement goes through `TryResolvePlacement` (zone or point path); `OnEnemyDied` releases the next wave (coroutine, floor-version-guarded) or fires `FloorCleared`; pool-refill restored so "more enemies than points" still spawns all. |
| `SpawnTestDebugDisplay.cs` (TEST-ONLY) | HUD + live `Wave: x/y` and `Budget: n` lines. |

Untouched: `RunData`/`RunController`/`RunStateMachine`/`RunBootstrap` (SpawnSystem stays REPORT-ONLY —
it raises `FloorCleared` and never touches `RunState`), the real Enemy System, Player, Combat,
production UI, scenes.

## Design decisions

- **Placement lives in `SpawnZone`, not the archetype.** "Footprint radius" (how much room a spawn
  needs) is a placement concern, so it is a `SpawnZone` field; `EnemyArchetype` has no such field.
- **Validation is a pipeline, never fabricated.** If NavMesh validation is on but no validator result
  exists, the candidate is rejected — the code never invents a "walkable" position. No validator
  (zone disables the rule) = pass-through, an explicit opt-out.
- **Player-distance rule is skipped when no player reference exists** (effective min 0), never
  fabricated.
- **Bounded by `MaxAttempts` (zone default 20, min 1).** A candidate that fails the whole pipeline
  after `MaxAttempts` logs a `Debug.LogWarning`, skips that one enemy, and the floor continues with
  the entries that did find a location — no hang, no spawn into invalid geometry.
- **Composition is selected ONCE per floor.** `EnemyCompositionSelector`'s cache is keyed on
  `(floor, target, budget)`; a floor's waves slice that same composition. `WavePlan` is pure slicing
  over contiguous entries, so no composition or budget is recomputed mid-floor.
- **Wave floor threshold is data-driven.** `waveStartFloor` default `int.MaxValue` = waves off;
  `waveSize` 0 = single wave. Kill of a non-final wave starts `SpawnNextWaveCoroutine(floorVersion)`;
  the floor version guards a stale coroutine from spawning into a replaced floor.
- **`IsFloorCleared` semantics.** `alive.Count == 0 && !HasUnspawnedRemaining()`; `FloorCleared` fires
  only on the last-alive death of the FINAL wave. `ClearAlive()`/`Populate()` never raise it.
- **No second state machine.** SpawnSystem never owns `RunState`; the existing driver/bootstrap
  bridge (`FloorCleared → CompleteFloor → StartNextFloor → Populate`) is unchanged.
- **Pool refill preserved from Sprint 4.** FixedPoints draws without replacement within a wave, but
  when the pool empties (more enemies than points) it refills so every affordable enemy still spawns.

---

## Tests (external dotnet harness, `%TEMP%\opencode\spawn_integration_test\`)

Compiles the **actual** `Spawning/*.cs` sources; was 261 checks (Sprint 6) and is now **331**.

| # | Scenario | Result |
|---|---|---|
| 19 | Placement pipeline: inside-bounds pass, outside-bounds fail, min-player-distance reject, already-occupied reject, blocking-layer reject, NavMesh-off pass-through, bounded attempts | **PASS** |
| 20 | RandomZone end-to-end: `Populate` via zone places each enemy through the pipeline, distances hold | **PASS** |
| 21 | WavePlan slicing: sizes, cursor, `MarkWaveReleased`, `WaveCount` ceil division, single wave off-threshold | **PASS** |
| 22 | Wave floor integration: kill wave 1 → next wave released after delay, low floor spawns everything at once, FloorCleared only after the final wave dies (fired exactly once) | **PASS** |
| 23 | Composition preserved across waves: wave contents match the original composition slice, cache count stays 1, budget never recomputed, `LastCompositionInfo` unchanged | **PASS** |
| — | Sprint 4–6 regression rows (budget, distinct points, composition selection, driver integration 1→2→3, save/bootstrap) | **PASS** |

## Unity Play Mode (deferred; no Editor here)

Expected with the existing cost-3 test archetype: identical to Sprint 4–6 on FixedPoints scenes; a
scene with a `SpawnZone` + `SpawnStrategy.RandomZone` spawns through the pipeline and the HUD shows
`Wave: x/y` + `Budget: n`.

---

## Definition of Done

- [x] Rectangle SpawnZone with serialized validation rules; `RandomZone` strategy wired into `Populate`.
- [x] Bounded pipeline: bounds → ground/NavMesh (seam) → blocking layers (mask, never hardcoded) → player distance (skipped without reference) → enemy distance → PASS/FAIL, `MaxAttempts` bounded, graceful skip.
- [x] `FixedPoints` unchanged for controlled/testing scenes; pool refill keeps >points affordable spawns working.
- [x] Composition selected once per floor; waves slice it via pure `WavePlan`; budget never recomputed mid-floor.
- [x] `IsFloorCleared` = nothing alive AND nothing unspawned; `FloorCleared` fires only after the final wave.
- [x] No hardcoded enemy types/floors/layers; no infinite loops; no second state machine; no team-file changes.
- [x] Dotnet harness 331/331 PASS.
- [ ] Play Mode verification (deferred — no Editor).

---

## Risks / Blockers

| Risk / Block | Mitigation |
|---|---|
| Real Enemy System still not ready | Unchanged: isolated `TestEnemy`, archetype `prefab` swap later |
| NavMesh seam depends on a baked/available NavMesh | Validation is opt-in per zone (`useNavMeshValidation`); harness injects a deterministic fake; Unity Play Mode deferred |
| Mid-wave quit/save | Documented (Sprint 6 note): floor-start checkpoints only; resume repopulates the floor fresh from wave 1 |
| Unity Editor unavailable | Play Mode deferred; harness + static diff verification only |
