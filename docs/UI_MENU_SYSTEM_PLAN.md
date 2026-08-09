# UI / Menu System — Plan & Implementation

> **Status:** FOUNDATION IMPLEMENTED (Main Menu, Pause Menu, Settings) — static verification complete, runtime verification deferred to the Editor.
> Evidence labels (same convention as the rest of `docs/`): `[EXISTS]` / `[PARTIAL]` / `[MISSING]` / `[PROPOSED]` / `[TEAM DECISION]` / `[BLOCKED]` / `[FUTURE]`
> Related docs: `UNITY_PROJECT_INVENTORY.md`, `PROJECT_ANALYSIS.md`, `ARCHITECTURE.md`, `ROGUELIKE_SYSTEM.md`, `ROGUELIKE_SPRINT_PLAN.md`, `ROGUELIKE_RUN_SYSTEM_*.md`.

---

## 1. Goal

Give the prototype its basic app shell:

1. **Main Menu** — title screen with Play / Settings / Quit.
2. **Pause Menu** — opened in-game, with Resume / Settings / Main Menu.
3. **Settings foundation** — master volume, fullscreen, resolution; **session-only** (no save system).

Explicitly **out of scope** for this task:

- **No save system** — the Settings section below marks persistence as `[PROPOSED]`/`[TEAM DECISION]` only.
- No HUD, upgrade screen, or game-over screen (those belong to the Roguelike / UI system owner — `ROGUELIKE_SYSTEM.md §5`, `ROGUELIKE_SPRINT_PLAN.md §7 Sprint 11`).
- No changes to Player / Enemy / Combat / Roguelike (Run) systems or prefabs.
- No audio assets, no mixers, no menu art, no background 3D scene.

---

## 2. What already exists in the project (`[VERIFIED]`)

Evidence gathered by static inspection of `Assets/`, `Assets/Scenes/TestingScene.unity`, and `Packages/manifest.json`.

| Area | Status | Evidence |
|---|---|---|
| Engine / packages | `[EXISTS]` | Unity 6000.3.20f1; URP 17.3.0; `com.unity.ugui 2.0.0`; `com.unity.inputsystem 1.19.0`; `com.unity.cinemachine 3.1.7` |
| Input = New Input System only | `[EXISTS]` | `ProjectSettings.asset → activeInputHandler: 1`; `Assets/InputSystem.inputactions` (maps `PlayerMovement`, `Combat`); generated wrapper `Assets/InputSystem.cs`; `Assets/Scripts/InputController.cs` bridges it via **static events** |
| Game scene | `[EXISTS]` | `Assets/Scenes/TestingScene.unity` (single game scene; also `Player_v.3/Showcase.unity`, `ShaderGraph_Dissolve/.../URP Samples.unity`, `_Recovery/0.unity` — not part of the flow) |
| Scene canvas + EventSystem | `[EXISTS]` | `TestingScene.unity` has a `Canvas` (Screen Space Overlay, CanvasScaler 1920×1080, GraphicRaycaster) hosting two debug `Text` labels, and an `EventSystem` with `InputSystemUIInputModule` |
| Any menu / pause / settings / UI script | `[MISSING]` | grep for UI/menu/settings/quit across `Assets/Scripts` → no hits |
| Scene transitions | `[MISSING]` | no `SceneManager` usage anywhere |
| Main Menu scene | `[MISSING]` | no `Assets/Scenes/MainMenu.unity` (must be created) |
| Save system / PlayerPrefs usage | `[MISSING]` | `ROGUELIKE_SYSTEM.md §4.8` defines `SaveSystem` `[PROPOSED]`, not implemented |
| Audio mixer / master volume | `[MISSING]` | no AudioMixer assets; only `AudioListener` (present on Main Camera) — master volume via `AudioListener.volume` |
| Build scene list | `[BLOCKED]` | `ProjectSettings/EditorBuildSettings.asset` still lists `Assets/Scenes/SampleScene.unity` (guid `99c9720a...`, file no longer exists). Not touched by this task `[TEAM DECISION]` |
| `Player` tag | `[BLOCKED]` | `TagManager.asset` only defines the `Enemy` tag; the Player GameObject's `Player` tag is invalid (carried over from inventory task) — unrelated to menus, not fixed here |

**Build-settings note:** `SceneManager.LoadScene("TestingScene")` works in the Editor and in any build **only if** the scene is in `EditorBuildSettings`. The list is currently stale/broken, so the "Play" target is documented as temporary (`[TEAM DECISION]` — the team must register `MainMenu` + `TestingScene` in Build Settings before any build).

---

## 3. System classification (for the plan)

| System | Status | Notes |
|---|---|---|
| Main Menu | `[MISSING]` → this task creates `Assets/Scenes/MainMenu.unity` + `Assets/Scripts/UI/MainMenuController.cs` |
| Pause Menu | `[MISSING]` → this task adds a pause canvas to `TestingScene` + `Assets/Scripts/UI/PauseController.cs` |
| Settings (volume/fullscreen/resolution) | `[MISSING]` → this task adds `Assets/Scripts/UI/SettingsController.cs`; no persistence |
| Input (pause binding) | `[PARTIAL]` → the Input System package exists and is wired, but has **no Pause/UI action**. This task reads `Keyboard.current.escapeKey` / `Gamepad.startButton` directly through the existing system (no asset/wrapper changes). Adding a `UI/Pause` action to `Assets/InputSystem.inputactions` = `[PROPOSED]` `[TEAM DECISION]` |
| Save / settings persistence | `[MISSING]` → `[PROPOSED]` only, see §8 |
| HUD / upgrade / game-over | `[MISSING]` → owned by Roguelike/UI sprint 11 (`ROGUELIKE_SPRINT_PLAN.md §7`), NOT this task |

---

## 4. Architecture decisions & rules compliance

Rules from the task, and how this plan satisfies them:

| Rule | Implementation |
|---|---|
| No singletons / global statics | No `static`, no `Instance`, no static state. All state is instance fields of small MonoBehaviours |
| No service locator / event bus | No `FindObjectOfType` hubs, no central `EventBus`. Controllers only talk to what the Inspector wires via `[SerializeField]` |
| No giant UIManager / GameManager | Three tiny focused components: `MainMenuController`, `PauseController`, `SettingsController` — one concern each |
| No duplicate InputSystem | Reuses the existing `UnityEngine.InputSystem` package. The pause binding reads `Keyboard.current` / `Gamepad.current` directly (no second wrapper, no `.inputactions` change) |
| No duplicate SceneManager wrapper | Direct `SceneManager.LoadScene(...)` calls inside the two controllers that actually navigate (no `SceneLoader` helper) |
| Reuse existing input system for pause | `PauseController.Update` polls `Keyboard.current.escapeKey` / `Gamepad.current.startButton` via `wasPressedThisFrame` — works at `Time.timeScale == 0` (Input System runs unscaled) |
| Don't touch Player/Enemy/Combat/Roguelike | Only `Assets/Scripts/UI/*` (new), `Assets/Scenes/MainMenu.unity` (new), and an **additive** pause-canvas appended to `TestingScene` (existing objects untouched) |
| Small focused components | `MainMenuController` = menu buttons only; `PauseController` = pause state + timeScale + scene exit; `SettingsController` = settings UI + apply |

**Cursor policy `[TEAM DECISION]`:** the pause flow unlocks/relocks the cursor because `GameManager` (`Assets/Scripts/GameManager.cs`) only locks it on `Awake`. Cursor ownership should eventually live in ONE place (probably GameManager, Core dev) — flagged, not implemented.

---

## 5. Scene flow

```
 MainMenu.unity ──Play──► TestingScene.unity (game)
      ▲                         │
      │                  Esc / Start ──► Pause
      │              (timeScale 0, panel shown)
      │                 Resume ──► timeScale 1, back to game
      │                 Settings ──► Settings panel (still paused)
      │                 Main Menu ──► timeScale 1, load MainMenu
      └────────────────────────┘
 Settings (from Main Menu or Pause) → applied immediately, session-only
```

- **Main Menu → game:** `Play` loads `TestingScene` (temporary target, §2 build note).
- **Game → pause:** `Esc` (keyboard) or `Start` (gamepad) → `Time.timeScale = 0`, cursor unlocked, panel shown.
- **Pause → Resume:** `Time.timeScale = 1`, cursor relocked, panel hidden.
- **Pause → Settings:** settings panel shown above pause (still `timeScale 0`). `Esc` while settings open closes settings only (stays paused).
- **Pause → Main Menu:** `Time.timeScale = 1`, cursor unlocked, `LoadScene("MainMenu")`.
- **Settings:** volume/fullscreen/resolution apply immediately; nothing is written to disk.

**Time scaling contract:** `Time.timeScale` is set to `0` on pause and restored to `1` on **resume AND on leaving to main menu** (both paths), so a paused exit never leaves the game frozen.

---

## 6. Main Menu (to implement)

| Item | Detail |
|---|---|
| Scene | `Assets/Scenes/MainMenu.unity` — created. Contents: EventSystem (InputSystemUIInputModule), Screen Space Overlay Canvas (CanvasScaler 1920×1080, matching TestingScene), full-screen background, title text, Play/Settings/Quit buttons, Settings panel (hidden) |
| Camera | none — Screen Space Overlay UI needs no camera; background is a full-screen Image `[TEAM DECISION]: 3D backdrop = FUTURE` |
| Script | `Assets/Scripts/UI/MainMenuController.cs` — wires Play → `LoadScene("TestingScene")`, Settings → show panel, Quit → `Application.Quit()` (builds only; editor no-op) |
| Quit | `Application.Quit()` — document that it only works in built players |

## 7. Pause Menu (to implement)

| Item | Detail |
|---|---|
| Scene | `TestingScene.unity` — **appended** a new `PauseMenu` Canvas (Screen Space Overlay, `m_SortingOrder: 1` above the debug Canvas, root added to `SceneRoots`). Existing Canvas/EventSystem/GameObjects untouched |
| Input | `PauseController` on the always-active pause Canvas; polls `Keyboard.current.escapeKey` + `Gamepad.current.startButton` (`wasPressedThisFrame`) |
| Behaviour | Pause: `timeScale 0`, cursor unlocked, panel shown. Resume: `timeScale 1`, cursor locked, settings+panel hidden. Main Menu: `timeScale 1`, cursor unlocked, load scene. Esc while settings open → close settings, stay paused |
| Script | `Assets/Scripts/UI/PauseController.cs` |
| Panels | `PausePanel` (Resume / Settings / Main Menu) + `SettingsPanel` — both start inactive; `PauseController` and `SettingsController` live on the always-active Canvas so their `Start`/`Update` run despite the panels being inactive |

## 8. Settings foundation (to implement)

| Setting | Control | Apply mechanism | Supported? |
|---|---|---|---|
| Master volume | Slider (0..1) | `AudioListener.volume = value` (no mixer exists) | yes — audio module present (`com.unity.modules.audio`) |
| Fullscreen | Toggle | `Screen.fullScreen` + `Screen.SetResolution(w, h, FullScreenWindow/Windered)` | yes |
| Resolution | Cycle button + label (1920×1080 / 1600×900 / 1280×720 / 1024×768) | `Screen.SetResolution(...)` | yes — fixed preset list, deterministic; `Screen.resolutions` varies per monitor so it is NOT used `[PROPOSED]` |
| Input rebinding / keyboard layout | — | not supported by the project | `[NOT SUPPORTED]` — no rebinding code exists |
| Quality presets | — | `QualitySettings` exists but has no authored presets | `[FUTURE]` |

**Persistence — PROPOSAL ONLY (not implemented):**

- Settings are **session-only**: changes apply immediately, live in the scene's instance fields, and reset on app restart. Nothing is written to `PlayerPrefs` or disk.
- `[PROPOSED]` cross-scene/session persistence options for the team to decide:
  1. **DontDestroyOnLoad holder** — a small `SettingsSession` component created at boot that survives scene loads; each scene's `SettingsController` reads/writes it via Inspector wiring. No statics.
  2. **PlayerPrefs/JSON** — save master volume/fullscreen/resolution under a settings key (matches `ROGUELIKE_SYSTEM.md §4.8` `SaveSystem` pattern `[PROPOSED]`).
  3. **Fold into the future meta save system** — when the Roguelike save lands (`[MISSING]`, sprint 10+), display settings ride along.
- `[TEAM DECISION]` — which option, and whether settings persist across scene loads *within* a session.

---

## 9. Input handling

- **Existing:** `InputController` (`Assets/Scripts/InputController.cs`) exposes `OnMoveInput` / `OnSprintInput` / `OnJumpStart` static events from the generated `InputSystem` wrapper. Not modified.
- **New (this task):** `PauseController` reads `Keyboard.current.escapeKey` and `Gamepad.current.startButton` via `wasPressedThisFrame`. Rationale:
  - reuses the existing (only) input system package,
  - works while `timeScale == 0`,
  - touches neither `InputSystem.inputactions` nor the generated `InputSystem.cs` (no regen / no hand-editing generated code),
  - no second input wrapper.
- **`[PROPOSED] [TEAM DECISION]`:** add a `UI` action map (`Pause`, `Submit`/`Cancel` reuse) to `Assets/InputSystem.inputactions` for unified rebinding later. Not done now — it would require regenerating `InputSystem.cs` in the Unity Editor.

---

## 10. Files

### Created

| File | Kind |
|---|---|
| `docs/UI_MENU_SYSTEM_PLAN.md` | this plan |
| `Assets/Scripts/UI/MainMenuController.cs` (+ `.meta`) | script |
| `Assets/Scripts/UI/PauseController.cs` (+ `.meta`) | script |
| `Assets/Scripts/UI/SettingsController.cs` (+ `.meta`) | script |
| `Assets/Scripts/UI/` folder `.meta` | folder meta (Unity would create it otherwise) |
| `Assets/Scenes/MainMenu.unity` (+ `.meta`) | scene |

### Modified

| File | Change |
|---|---|
| `Assets/Scenes/TestingScene.unity` | **appended** a new `PauseMenu` Canvas (pause panel + settings panel + `PauseController`/`SettingsController`) and registered its root in `SceneRoots`. No existing block edited |

### Not modified (explicitly)

`InputSystem.inputactions`, `InputSystem.cs`, `InputController.cs`, `GameManager.cs`, all Player/Enemy/Combat/Roguelike scripts and prefabs, `ProjectSettings/*`, `Packages/*`.

---

## 11. Tests (static verification only)

No Unity Editor / runtime access on this machine → results are **STATIC VERIFIED** (YAML + wiring + code review). A future **RUNTIME VERIFIED** pass in the Editor is required.

| # | Test | Method | Expected |
|---|---|---|---|
| 1 | MainMenu scene has EventSystem + Canvas + 3 buttons | YAML grep | PASS (structure present) |
| 2 | Buttons wired to `MainMenuController.Play/OpenSettings/Quit` | code review (`AddListener`) | PASS |
| 3 | `Play` loads `TestingScene` | code review | PASS (documented temporary target) |
| 4 | Pause canvas present in TestingScene, root in `SceneRoots`, no existing fileID collisions | YAML grep | PASS |
| 5 | Esc/Start toggles pause; `timeScale` 0 on pause, 1 on resume & main-menu exit | code review | PASS |
| 6 | Settings: volume→`AudioListener.volume`, fullscreen→`Screen.fullScreen`, resolution→`Screen.SetResolution` | code review | PASS |
| 7 | Settings panel starts inactive; controllers on always-active canvas | YAML grep | PASS |
| 8 | No Player/Enemy/Combat/Roguelike file modified | `git status` | PASS |
| 9 | No `ProjectSettings/*` or `Packages/*` modified | `git status` | PASS |
| 10 | Script guids in scene YAML match `.meta` files | guid compare | PASS |

---

## 12. Risks & open questions

| Risk / question | Type | Mitigation |
|---|---|---|
| Hand-authored scene YAML could desync from Editor output | low | Modeled on `TestingScene` blocks; no persistent-call YAML (all wiring in `Start`), minimising fragile sections; Unity re-serialises cleanly on first open |
| Pause input is hardcoded to Esc/Start | `[TEAM DECISION]` | Add `UI/Pause` action to `InputSystem.inputactions` later (§9) |
| Cursor ownership split between GameManager and PauseController | `[TEAM DECISION]` | Core dev owns cursor policy; pause restores lock on resume today |
| Settings don't persist across scenes/sessions | `[TEAM DECISION]` | §8 options; none implemented (no save system) |
| Build scene list is stale (`SampleScene.unity` no longer exists) | `[TEAM DECISION]` | Register `MainMenu` + `TestingScene` in Build Settings; until then `Play` works in Editor only |
| Resolution presets fixed list vs `Screen.resolutions` | `[TEAM DECISION]` | fixed list chosen for determinism; dynamic list `[PROPOSED]` |
| `Quit` does nothing in the Editor | fact | documented; works in builds |

---

## 13. Definition of Done

- [x] Plan written (`docs/UI_MENU_SYSTEM_PLAN.md`).
- [x] `MainMenu.unity` scene + `MainMenuController` (Play/Settings/Quit).
- [x] Pause menu appended to `TestingScene` + `PauseController` (Resume/Settings/Main Menu; `timeScale` set on pause, restored on resume and on exit).
- [x] Settings foundation (`SettingsController`: master volume / fullscreen / resolution), session-only.
- [x] Save system = plan-only (`§8`), no implementation.
- [x] No Player/Enemy/Combat/Roguelike/GameManager/InputController code touched.
- [x] No `ProjectSettings/*`, `Packages/*`, or prefabs touched.
- [x] Static tests from §11 pass; runtime verification deferred to the Editor.
- [x] No commit/push/merge — working tree only.
