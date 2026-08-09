# Roguelike UI — Sprint 11 (Player HUD + Upgrade Cards + Game Over)

> **Status:** IMPLEMENTED on `feature/player-ui-foundation` (NOT merged to `main`).
> Base plan: `docs/UI_MENU_SYSTEM_PLAN.md`, `docs/ROGUELIKE_SPRINT_PLAN.md` (Sprint 11 = HUD / upgrade screen / game-over).
> Verification: dotnet integration harness **395 checks** (was 331) + static YAML/code review. **Play Mode was NOT run** (no local Unity Editor) — see §6.

---

## 1. Goal

Give the run its player-facing UI, fully wired end-to-end but fed by **clearly-temporary mock sources** behind real interfaces:

1. **Player HUD** — health (current/max), XP (current/required), level, floor.
2. **Upgrade selection screen** — a reusable upgrade card, shown as a **list-driven row** (one card per offer, never hardcoded), with pick-one-then-lock rules.
3. **Game Over screen** — title, floor reached, enemies defeated, run time (`M:SS` / `H:MM:SS`), and RETRY/NEW RUN + MAIN MENU buttons.

Explicitly **out of scope** (owned elsewhere): real player stats/XP progression, real damage/death → run-end, real loot/upgrade effects, the real menu-scene return target. Where gameplay is missing, the UI consumes a **mock** through the same interface a real system will implement later.

---

## 2. Architecture (event-driven, source → presenter → view)

```
 Gameplay system later ──►  IPlayerHudSource / IUpgradeSource / IGameOverSource
   (today: Mock*Source)             │  event Changed
                                    ▼
                     Presenter (plain C#, harness-tested)
                 PlayerHudPresenter / UpgradeSelectPresenter / GameOverPresenter
                                    │  render / state calls
                                    ▼
                     View (Unity legacy UI, runtime-built)
        PlayerHudController / UpgradeSelectController / GameOverScreenController
```

- **Event-driven only.** Views render on `Changed` events; nothing polls every frame and no view is refreshed from `Start()` alone. Identical snapshots are deduped (one value = one render).
- **Plain-C# contracts are harness-testable.** `PlayerHudData`, `UpgradeCardData`, `GameOverData` are `struct`s with `IEquatable`/operators and zero Unity APIs; presenters are pure C# classes. The dotnet harness drives mocks against recording fake views.
- **Real data plugs in later** behind the same interfaces (`MockPlayerHudSource` → real stat source, etc.). The views only ever call `Present(...)`, so no UI code changes.
- No singletons, no EventBus, no FindObjectOfType hubs in production code (the test-only demo driver may use `GetComponent`).

### UI construction

The whole player UI is built at **runtime** with legacy uGUI under its own Screen-Space-Overlay canvas (`PlayerUI`, CanvasScaler 1920×1080 match-height, sorting order 10 — below the debug HUD's 100). This mirrors the existing `SpawnTestDebugDisplay` approach, keeps the scene edit to a single GameObject, and keeps bars/cards data-driven. `PlayerUiKit` provides the shared Image/Text/Button/outline helpers.

- HUD: health + XP bars use `Image.Type.Filled` / `FillMethod.Horizontal` driven by `PlayerHudData.HealthRatio` / `XpRatio`; `LVL n` + `FLOOR n` text, bottom-left panel.
- Upgrade card: icon (tinted by `iconKey`, glyph = first letter), title, value text, description; click → `Clicked(index)`. Disabled cards fade via `CanvasGroup` and can't be picked; the picked card gets a highlight border.
- Game over: dim overlay + centered panel with the three stat lines and the two buttons; `Present` fills them and shows the screen.

---

## 3. Files changed

### New — `Assets/Scripts/UI/` (all with `.meta`)

| File | Kind | Note |
|---|---|---|
| `PlayerHudData.cs` | plain C# contract | defaults 100/100, 0/100, lv 1, floor 1; `HealthRatio`/`XpRatio` clamp to 0..1; `IEquatable` |
| `UpgradeCardData.cs` | plain C# contract | id/title/description/valueText/iconKey; `IsValid` |
| `GameOverData.cs` | plain C# contract | `RunTimeText()` → `M:SS` / `H:MM:SS` |
| `IPlayerHudSource.cs` | interface + `MockPlayerHudSource` | event `Changed`; mock dedupes identical snapshots |
| `IUpgradeSource.cs` | interface + `MockUpgradeSource` | 3 placeholder offers (Sharpened Edge / Vitality / Swift Boots) |
| `IGameOverSource.cs` | interface + `MockGameOverSource` | event `Changed`; mock dedupes |
| `PlayerHudPresenter.cs` | plain C# presenter + `IPlayerHudView` | bind renders once; re-render on change; dedupe; unbind |
| `UpgradeSelectPresenter.cs` | plain C# presenter + `IUpgradeCardView`/`IUpgradeSelectView` | pick-one-locks-rest; `CardSelected`; re-offer resets; dismiss |
| `GameOverPresenter.cs` | plain C# presenter + `IGameOverView` | renders/shows only on `Changed`, never on bind |
| `PlayerUiKit.cs` | Unity helper | runtime legacy-UI factory |
| `PlayerHudController.cs` | Unity view | `IPlayerHudView` |
| `UpgradeCardController.cs` | Unity view | `IUpgradeCardView` |
| `UpgradeSelectController.cs` | Unity view | `IUpgradeSelectView`, list-driven card row |
| `GameOverScreenController.cs` | Unity view | `IGameOverView` |
| `PlayerUiBootstrap.cs` | Unity composition root | builds canvas + views, wires mocks → presenters → views, exposes state for test drivers |
| `PlayerUiDemoDriver.cs` | **TEST/DEMO ONLY** | keyboard driver (H/J/K/L/U/G/R) for `TestingScene` |

### Modified

| File | Change |
|---|---|
| `Assets/Scenes/TestingScene.unity` | **appended** a `PlayerUI` root GameObject (`4100000001`, layer 5, `PlayerUiBootstrap` MonoB)` + Transform (`4100000002`) and registered it in `SceneRoots`. No existing block edited. Script guid in the scene = `PlayerUiBootstrap.cs.meta`. |
| `tools/spawn-integration-test/spawn_integration_test.csproj` | added the 9 plain-C# UI files (contracts + interfaces/mocks + presenters) to the live-source compile list |
| `tools/spawn-integration-test/Program.cs` | added scenarios 24–29 (below) |

### Not modified (explicitly)

Enemy System, player combat/movement, `SpawnSystem` architecture, `RunController`/`RunBootstrap` architecture, `MainMenuController`/`PauseController`/`SettingsController`, `ProjectSettings/*`, `Packages/*`, prefabs. The 7 pre-existing untracked `.meta` files in `Assets/Scripts/Roguelike/Spawning/` remain untouched and were **not** staged.

---

## 4. Verification

### 4.1 Automated (dotnet harness)

`tools/spawn-integration-test` compiles the **real** source files and runs `dotnet run --project tools/spawn-integration-test/spawn_integration_test.csproj`:

```
[SpawnIntegration] ALL 395 CHECKS PASSED   (baseline was 331, +64)
```

New checks (scenarios 24–29):

- **huddata** — defaults, ratio math (half / clamp over-heal / clamp negative / no-max), `==`/`!=`/`Equals`/hash.
- **hud** — bind renders once with the current snapshot; source change re-renders and carries the data; identical snapshot dedupes; any-field change re-renders; unbind stops renders.
- **upg** — bind alone never shows the screen; offer shows a 3-card list, order preserved; first `Select` picks, records the index, locks the other two (`SetCardState` 0/2 disabled, 1 selected) and raises `CardSelected`; second / out-of-range picks ignored; new offer re-shows and resets; dismiss blocks picks.
- **offerdata** — the 3 default mock offers are valid, distinct, fully described, stable icon keys.
- **over** — bind alone never renders; a run end renders + shows; identical summary dedupes at the source; changed summary re-renders; unbind stops renders.
- **time** — `RunTimeText` `0:00` / `1:35` / `9:59` / `10:00` / `1:02:05`, negative clamps to `0:00`.

### 4.2 Static (Unity files)

The MonoBehaviours use only legacy uGUI (`Canvas`, `CanvasScaler`, `Image`, `Text`, `Button`, `CanvasGroup`, `GraphicRaycaster`, `Outline`) and the existing Input System (`Keyboard.current.*.wasPressedThisFrame`, same as `PauseController`). Scene YAML was modeled on existing `TestingScene` blocks; the `PlayerUiBootstrap` guid in the scene matches `PlayerUiBootstrap.cs.meta`. **Runtime verification (Play Mode) is deferred to the Unity Editor** — none of the view code is exercised by the harness by design.

---

## 5. How to try it (TestingScene)

`PlayerUiBootstrap` builds the whole UI on scene load (keyboard demo enabled). Keys: **H** damage, **J** heal, **K** gain XP / level up, **L** next floor, **U** offer upgrades, **G** game over, **R** retry (closes screens, resets mock HUD). The debug HUD (root `3002`) is untouched and renders above the production HUD.

---

## 6. Risks / open questions

| Item | Type | Note |
|---|---|---|
| View layer not runtime-verified | deferred | Play Mode pass in the Unity Editor required (same as prior UI sprints) |
| Mock numbers are placeholders | `[TEAM DECISION]` | XP curve / upgrade effects / enemy-kill totals live in real systems later; nothing here is gameplay-true |
| Game Over `MAIN MENU` button | `[TEAM DECISION]` | currently logs + is a no-op in `TestingScene`; the real menu target needs the build scene list / menu flow |
| Icon art | `[FUTURE]` | cards tint by `iconKey` + glyph letter; real sprites map to the same key later |

---

## 7. Definition of Done

- [x] Player HUD (HP current/max, XP current/required, level, floor) — data contract + presenter + Unity view.
- [x] Upgrade screen: `UpgradeCardData` contract, reusable card, **list-driven** 3-card row, pick-one-locks-rest.
- [x] Game Over screen (GAME OVER, floor reached, enemies defeated, run time; RETRY/NEW RUN + MAIN MENU).
- [x] TEMPORARY mocks behind real interfaces, clearly named; real systems swap in with no UI changes.
- [x] Event-driven refresh (no polling, no Start-only render); identical snapshots deduped.
- [x] Responsive layout (CanvasScaler 1920×1080 match-height, anchored panels).
- [x] Production/debug UI separated (production canvas at sorting 10; debug HUD at 100 untouched).
- [x] MainMenu/Pause/Settings reviewed but **not** rebuilt.
- [x] Harness extended: **395 checks PASSED** (baseline 331).
- [x] Report written (`docs/ROGUELIKE_UI_SPRINT_11.md`).
- [x] No Player/Enemy/Combat/SpawnSystem/RunController architecture changes; no `ProjectSettings/*` / `Packages/*` touched.
- [ ] Runtime (Play Mode) verification — deferred to the Unity Editor.
