# Roguelike Run System — Sprint 6 (Run Save Persistence + Main Menu/Pause UI)

> **Status:** IMPLEMENTED on `feature/roguelike-system`.
> Base plan: `docs/ROGUELIKE_RUN_SYSTEM_PLAN.md`. Prior sprints: 1–3 (state machine/RunData/RunController), 4–5 (spawn system + floor integration).
> Verification: dotnet integration harness (261 checks) + static YAML/diff inspection. **Play Mode was NOT run** (no local Unity Editor) — see §6.

---

## # Goal

Add **run save persistence** and a player-facing **Main Menu / Pause** flow driven by that real save data: Continue vs New Run, a saved-floor label on the Continue button, and a production scene bootstrap that owns the run when the scene is entered from the menu. Progression is RUN → FLOOR → CLEAR → NEXT FLOOR (no rooms system exists; `clearedRooms` is kept for backward compatibility).

---

## # Design

### # Persistence split (who owns what)

| Layer | File | Responsibility |
|---|---|---|
| DTO | `Assets/Scripts/Roguelike/SaveData.cs` | Versioned, serializable save schema (`version=1`). |
| I/O | `Assets/Scripts/Roguelike/RunSaveService.cs` | File `run_save_v1.json` under `Application.persistentDataPath`; validate/treat-as-no-save; injectable path for tests. |
| Mapping | `RunController.Capture()` / `RunController.TryRestore(SaveData)` | `RunData` ↔ `SaveData`, pure data, no I/O. |
| WHEN to save | `Assets/Scripts/Roguelike/RunBootstrap.cs` | Save points: START of every floor (incl. floor 1). |
| Menu flag | `Assets/Scripts/Roguelike/RunSession.cs` | `EnterFromMenu` static bool handed from MainMenu to the scene bootstrap. |
| UI | `MainMenuController.cs`, `PauseController.cs` | Continue/New Run + disabled state; quit-to-menu safety. |

### # Save points (explicit, never continuous)

- **Floor start checkpoint** — written BEFORE the floor is played (including floor 1 via the initial checkpoint), so quitting at any time resumes that floor from its start and a run is never silently lost.
- **New Run** — `RunSaveService.Delete()` discards the save, then the scene bootstrap starts a fresh floor-1 run and writes the floor-1 checkpoint.
- **Opening the game NEVER writes.** `MainMenuController.Start()` only reads (`TryLoad` for the Continue state/label). A resumed run never rewrites the file on open.

### # Resume semantics

- `RunState` is **NOT persisted** — it is derived on resume. A resumed run always enters `FloorStart` for the saved floor; the bootstrap populates the floor via `SpawnSystem.Populate(enemyBudget, floor)` and calls `BeginFloor()`.
- Mid-floor enemy state is **NOT persisted** (real Enemy System not integrated yet). A resumed floor is repopulated fresh.
- Corrupt/invalid save → treated as no save; the bad file is deleted; Continue is disabled; New Run is available. Nothing is ever fabricated.

### # Scene entry flow

`MainMenuController` sets `RunSession.EnterFromMenu = true` before `SceneManager.LoadScene(gameSceneName)`. In the game scene, `RunBootstrap` (production MonoBehaviour on the SpawnSystem GameObject) checks the flag:
- `true` (entered from the menu) → `RunBootstrap` owns the run: `TryLoad` + `TryRestore` (Continue) or `StartRun()` + initial checkpoint (New Run), then populate + `BeginFloor()`. It bridges `SpawnSystem.FloorCleared` (report-only) → `Run.CompleteFloor()` → coroutine pause → `Run.StartNextFloor()` → checkpoint save → populate → `BeginFloor()`.
- `false` (scene opened directly, e.g. Editor Play Mode) → the test-only `SpawnSystemTestDriver` owns the run exactly as before; `RunBootstrap` stays out of the way (and the driver defers when the flag is true, so the two never fight).

No singleton, no EventBus.

---

## # Files Changed

| File | Change |
|---|---|
| `Assets/Scripts/Roguelike/SaveData.cs` (+ `.meta`) | **new** — serializable save DTO. |
| `Assets/Scripts/Roguelike/RunSaveService.cs` (+ `.meta`) | **new** — JSON file persistence, validation, corrupt handling. |
| `Assets/Scripts/Roguelike/RunSession.cs` (+ `.meta`) | **new** — `EnterFromMenu` menu→scene handoff. |
| `Assets/Scripts/Roguelike/RunBootstrap.cs` (+ `.meta`) | **new** — production run owner + floor-start save points. |
| `Assets/Scripts/Roguelike/RunController.cs` | **modified** — `Capture()` / `TryRestore(SaveData)`. |
| `Assets/Scripts/UI/MainMenuController.cs` | **modified** — Continue/New Run/Settings/Quit, refresh from real save, floor label, disabled state. |
| `Assets/Scripts/UI/PauseController.cs` | **modified** — documented quit-to-menu safety (checkpoint already on disk). |
| `Assets/Scripts/Roguelike/Spawning/Testing/SpawnSystemTestDriver.cs` | **modified** — defers when `RunSession.EnterFromMenu` is true. |
| `Assets/Scripts/Roguelike/Spawning/Testing/SpawnTestDebugDisplay.cs` | **modified** — HUD reads driver OR bootstrap. |
| `Assets/Scenes/MainMenu.unity` | **modified** — ContinueButton, Play→New Run, wiring. |
| `Assets/Scenes/TestingScene.unity` | **modified** — `RunBootstrap` on the SpawnSystem GameObject. |

---

## # Verification

### # Harness

`%TEMP%\opencode\spawn_integration_test\` — the existing dotnet harness extended with save scenarios and Unity stubs (`Application.persistentDataPath/Quit`, `JsonUtility`, `Time`, `Cursor`, `SceneManager.LoadScene`, `Button`/`UnityEvent`). **ALL 261 CHECKS PASSED**, including:

- Save service round-trips (floor 1, floor 7 budget `10·1.4⁶`), delete → no save.
- Invalid saves rejected and removed: corrupt JSON, wrong version, `floor < 1`, `clearedRooms >= floor`, zero budget — valid saves still accepted afterwards.
- Main Menu: Continue disabled + plain label with no save; Continue without a save does NOT load the scene; New Run deletes the save, sets `EnterFromMenu`, loads the game scene; with a floor-7 save the button enables and shows `CONTINUE — FLOOR 7`; Continue loads the scene.
- RunBootstrap: no save → floor 1 + initial checkpoint → populated + live; FloorCleared bridge → floor 2 checkpoint at its start; fresh bootstrap after a quit resumes floor 2 (clearedRooms 1 restored, repopulated fresh, file not rewritten on open).

### # Not verified (no local Unity Editor)

Play Mode, actual scene load flow, Canvas/button layout, and the legacy-UI serialized references must be confirmed in the Editor.

---

## # Out of Scope / Later

- Rooms, RoomManager, SpawnZone/NavMesh/obstacle validation, high-floor wave/batch spawning (later milestones).
- Persisting mid-floor enemy state (needs the real Enemy System).
- EventBus / singletons (deliberately avoided).
- `Assets/Scripts/Enemy/*`, Player, Combat — untouched.
