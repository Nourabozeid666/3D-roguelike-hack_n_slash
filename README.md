# 3D Roguelike Hack & Slash

## Current Project

A 3D roguelike hack-and-slash prototype built in Unity (URP, legacy uGUI, new Input System). The current focus is a run-based roguelike layer: floor progression, cost-based enemy spawning, run-scoped upgrades, and a player-facing UI — all wired behind real data contracts with temporary mock sources where the gameplay systems are not finished yet.

## Current Roguelike Systems

What is implemented so far (see `docs/` for details and sprint reports):

- **Run System** — implemented. `RunStateMachine` over `RunState` (`Lobby`, `FloorStart`, `FloorActive`, `FloorCleared`, `RunEnd`), `RunData` run-scoped data bag, `RunController`, and `RunBootstrap` (the production run owner when the game scene is entered from the menu).
- **Floor progression** — implemented. Clearing a floor advances to the next (`CompleteFloor` → `StartNextFloor`); budgets/stat scaling grow per floor.
- **Spawn System** — implemented. Cost-based budget spawning via `SpawnSystem` (`Populate(budget, floor)`), `SpawnTable` + `EnemyArchetype` (cost, per-floor scaling).
- **Enemy composition & floor-based unlocks** — implemented. `EnemyCompositionSelector` picks a composition for a floor's budget; archetypes unlock as floors increase.
- **Spawn Zones / placement validation** — implemented. `SpawnZone`, `SpawnPoint`, and `SpawnPlacementValidator` (nav-mesh / blocking / ground checks).
- **High-floor wave pacing** — implemented. `SpawnPacingConfig` + `WavePlan` release waves on higher floors while preserving the selected composition.
- **Save / Load / Continue / New Run** — implemented. `RunSaveService` (versioned JSON under `Application.persistentDataPath`), checkpoint at the start of every floor, corrupt saves treated as no-save; the Main Menu drives Continue (enabled when a save exists) / New Run.
- **Player UI foundation** — implemented (HUD, upgrade cards, game over — see below). Event-driven data → presenter → view; identical snapshots are deduped.
- **Upgrade Card UI foundation** — implemented (list-driven card row, pick-one-locks-rest).
- **Game Over UI foundation** — implemented (run summary + retry / main menu buttons).
- **Automated .NET CI** — implemented (GitHub Actions runs the integration harness on pushes to `main` and PRs targeting `main`).

**Status notes:**

- `prototype/test-only`: `TestEnemy` + `SpawnSystemTestDriver` + `SpawnTestDebugDisplay` are test-scene helpers, and `PlayerUiDemoDriver` is a test/demo keyboard driver. The `SpawnSystem` currently spawns `TestEnemy` archetypes in test scenes — the real Enemy System is **not** integrated with spawning yet.
- `waiting for another system`: real player stats (HP/XP/level), real upgrade effects, and real player-death → RunEnd are not implemented. The HUD, upgrade cards, and game over screen consume **clearly-named temporary mocks** (`MockPlayerHudSource`, `MockUpgradeSource`, `MockGameOverSource`) behind the same interfaces the real systems will implement later.
- No unfinished gameplay system is claimed as complete. Play Mode verification in the Unity Editor is still pending for the UI (verified via the harness + static review so far).

## Current UI

- **Player HUD** — bottom-left panel: health bar (current/max), XP bar (current/required), level and floor text.
- **Upgrade Cards** — runtime-built row of cards (icon, title, value, description); one card per offered upgrade, first pick locks the rest.
- **Game Over** — run summary (floor reached, enemies defeated, run time as `M:SS`/`H:MM:SS`) with RETRY / NEW RUN and MAIN MENU buttons.
- **Main Menu / Pause / Settings** — Main Menu (Continue / New Run / Settings / Quit; Continue and New Run are driven by real save data), Pause (Esc/Start, Resume / Settings / Main Menu), and session-only Settings (master volume / fullscreen / resolution).

> Note: the HUD, upgrade, and game-over screens currently render **temporary/mock data** where the real gameplay systems are not finished. Main Menu Continue / New Run uses the real save system.

## Development / Testing

The project has a .NET integration harness at `tools/spawn-integration-test` that compiles the **real** game sources from `Assets/Scripts/` (with Unity stubs) and runs the checks as a console program:

```
dotnet run --project tools/spawn-integration-test/spawn_integration_test.csproj
```

Current baseline: **`ALL 395 CHECKS PASSED`** (after the Player UI milestone). Do not weaken or remove existing checks.

## CI

GitHub Actions (`.github/workflows/dotnet-tests.yml`) sets up .NET 10, restores/builds the harness, and runs it on:

- pushes to `main`
- pull requests targeting `main`

The job fails (non-zero exit) if any harness check fails.

## Project Structure

- `Assets/` — Unity project assets (art, prefabs, materials, textures, scenes, scripts).
- `Assets/Scripts/` — flat C# (no namespaces): `Combat/`, `Data/`, `Enemy/`, `Interfaces/`, `Player/`, `Roguelike/` (run + spawning + save), `UI/` (menus + HUD + upgrade cards + game over).
- `Assets/Scenes/` — `TestingScene.unity` (main game scene), `MainMenu.unity` (title/menu), plus other demo/recovery scenes.
- `docs/` — design documents and sprint reports (`ROGUELIKE_SYSTEM.md`, `UI_MENU_SYSTEM_PLAN.md`, `ROGUELIKE_UI_SPRINT_11.md`, etc.).
- `tools/` — `spawn-integration-test/` (.NET integration harness).
- `.github/workflows/` — CI definitions.

## Current Limitations / Next Work

- Real Enemy System integration with the spawner and run (spawning currently uses `TestEnemy` in test scenes).
- Real player HP/XP/level and upgrade gameplay effects (HUD and upgrade cards run on mocks).
- Player death → RunEnd → Game Over flow (game over runs on a mock summary source).
- Unity Play Mode verification for UI scenes/menus (pending — no local Unity Editor; verified statically and via the harness).

## Development Notes

Gameplay systems owned by other team members (Enemy System, Player/Combat) should not be modified without coordination. UI/Roguelike systems currently consume temporary mocks where the real gameplay systems are pending; swap the real source in behind the existing interfaces (`IPlayerHudSource`, `IUpgradeSource`, `IGameOverSource`) rather than changing UI code. Keep the integration harness green.
