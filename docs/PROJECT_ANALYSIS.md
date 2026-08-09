# Project Analysis — 3D-Roguelike Hack'n'Slash

Analysis of the Unity project at `E:\course\03_Programming_Data\game_dev\projects\3D-roguelike-hack_n_slash`.
All claims cite a file path or a line number inside a file. Where something is inferred rather than observed, it is explicitly marked **[assumption]**.

---

## 1. Project Overview

| Item | Value | Source |
|---|---|---|
| Engine | Unity `6000.3.20f1` | `ProjectSettings\ProjectVersion.txt` |
| Render pipeline | URP (`com.unity.render-pipelines.universal 17.3.0`) | `Packages\manifest.json`, `Assets\Settings\PC_RPAsset.asset` |
| Input | New Input System (active input handler = 1), generated code in `Assets\InputSystem.cs` | `ProjectSettings\PlayerSettings.asset`, `Assets\InputSystem.inputactions` |
| Genre (as implemented) | 3D third-person movement + enemy AI prototype | `Assets\Scenes\TestingScene.unity` |
| Git | Not a repository (no `.git` folder) | `git rev-parse` failed |
| Documentation | No GDD, no design docs; `README.md` is a stub | glob for `**/*.md` |

Key packages (`Packages\manifest.json`): `com.unity.ai.navigation 2.0.13`, `com.unity.animation.rigging 1.4.1`, `com.unity.cinemachine 3.1.7`, `com.unity.inputsystem 1.19.0`, `com.unity.ugui 2.0.0`, `com.unity.visualscripting 1.9.11`, `com.unity.timeline 1.8.12`.

Tags/layers: tag `Enemy`; custom layers `Ground = 6`, `Wall = 7` (`ProjectSettings\TagManager.asset`).

**What exists:** a playable-ish third-person player controller (move / sprint-dash / jump / wall-run code), a state-machine enemy AI with poise/stagger, patrol routes, NavMesh baking, a spawn dissolve effect, and debug HUD text.

**What does not exist yet:** any damage application, player combat, ranged/sacrifice/strong/combo attacks, UI screens/menus, audio, save/upgrade systems, level/room generation, and a spawn system (only described in a comment).

---

## 2. Scene Analysis

4 scenes:

| Scene | Role |
|---|---|
| `Assets\Scenes\TestingScene.unity` | Main test scene (465 KB, the real work lives here) |
| `Assets\Player_v.3\Showcase.unity` | Model showcase for the player character |
| `Assets\ShaderGraph_Dissolve\URP\URP Samples.unity` | Vendor sample for the dissolve shader graph |
| `Assets\_Recovery\0.unity` | Earlier backup of the test scene (contains Player, Walls, GameManager, "CurrentState") |

**Build Settings** (`ProjectSettings\EditorBuildSettings.asset`) reference only `Assets/Scenes/SampleScene.unity` (guid `99c9720ab356a0642a771bea13969a05`) — **this file does not exist** in the project. `TestingScene` is not in the build list.

**TestingScene root objects** (verified from serialized transforms with `m_Father: {fileID: 0}` and object reads):
- `Player` (+ child `PlayerObj`, + `KatanaWeapon`)
- `Main Camera`, `FreeLook Camera` (active), `FreeLook Camera BACKUP - not fully upgradable by CM` (inactive)
- `Canvas` (debug Texts `PlayerCurrentState` ~line 7872, `EnemyCurrentState (1)` ~line 134)
- `EventSystem`, `Global Volume`, `Directional Light`
- `Ground`, `Walls` (9 wall objects), `navMesh Settings` (NavMeshSurface + baked NavMeshData, line 491, asset `Assets\Scenes\TestingScene\NavMesh-navMesh Settings.asset`)
- `GameManager` (cursor lock only)
- `Enemy` (INACTIVE, line 12933) — BruiserMonster model tagged `Enemy`
- `Exploding Enemy` (ACTIVE) — BruiserMonster model
- `TreeEntAsh` — leaf child of the `Enemy` object

### Player wiring (facts)
- `PlayerController` MonoBehaviour serialized at `TestingScene.unity:8398-8440`: `animator: {fileID: 0}` (line 8413, **animator reference unset**), `groundLayer=6, wallLayer=7`, `gravity:-20`, `sprintSpeed:12`, `walkSpeed:8`, `jumpForce:12`, `airMoveSpeedMultiplier:0.9`, `customDrag:1`, `MaxVelocity:12` (differs from script defaults `-25/145/100/5/0.05`).
- `PlayerObj` Animator references controller guid `da65a6d29ce66da459988c23539ddd08` (line 8568) — **this guid exists nowhere in the project** (verified by meta search). Player animations cannot play.
- `PlayerObj` = cube mesh + MeshRenderer + CapsuleCollider (physics material) + Animator. The visible 3D model is a child (`Rig 1` GO at line ~8591).

### Enemy wiring (facts)
- Two `EnemyController` components in the scene, both `targetTransform` = Player transform (`1863943526`), both `patrolRoute` = `routeA.asset`:
  - **On "Exploding Enemy"** (root GO `2282024516536603690`, ACTIVE): `TestingScene.unity:12329-12348` — `detectionDistance:12, loseTargetDistance:16, attackRange:3, patrolSpeed:3.6, chaseSpeed:9, viewHalfAngle:290`, entity `health 100/100, damage 20, defense 20, poise 100/100`. Also has `EnemyDissolve` and **two script references that could not be resolved** (guids `b2d8418b0b9634b1892b0268dd9c2743`, `fff0960ef4ea6e04eac66b4a7fd2189d` — no `.meta` in `Assets`; **[assumption]** deleted scripts or package components, since `Library/` is absent).
  - **On "Enemy" root** (GO `5289385902289650030`, INACTIVE): `TestingScene.unity:14684-14703` — same detection values but entity **all zeros** (`health 0/0, damage 0, defense 0, poise 100/0`) → instantly dead / invalid. This object's Animator (line 9744) uses the **TreeEntAsh** controller (`cefaf9ad7daa5724aa50d81370b08dae`) on a **BruiserMonster** rig (mixamorig/ring bones) — **[assumption]** mismatch that would break animation playback.

**Net effect:** the only active enemy at playtime is "Exploding Enemy"; the "Enemy" object (with the sensible Animator story) is inactive and its stats are zeroed.

---

## 3. Systems Found

| System | Where | Status |
|---|---|---|
| Player movement FSM (Idle/Move/Sprint/Dash/Jump/Land/Slide) | `Assets\Scripts\Player\PlayerMovementStates\*.cs` | Implemented (WallRun disabled at `PlayerController.cs:39`) |
| Custom gravity / drag / max velocity | `PlayerController.cs:74-121` | Implemented |
| Enemy FSM (Spawn/Patrol/Chase/Attack/Stagger/Die) + nested combat-action machine | `Assets\Scripts\Enemy\enemy states\*`, `AttackState.cs` | Mostly implemented; chase/attack logic hollow (see §5) |
| Poise/stagger + death events | `EnemyEntity.cs` | Implemented |
| Patrol routes (ScriptableObject) | `PatrolRoute.cs`, `Assets\prefabs\routes\level 1\routeA.asset` | Implemented |
| Debug HUD state text | `FiniteStateMachine.cs:32-33`, `EnemyController.cs:161-178` | Implemented |
| Dissolve spawn effect (material + edge particles) | `EnemyDissolve.cs` (608 lines), `Assets\Materials\shaders\EnemyDissolveMaterial.mat` | Implemented |
| Debug sphere pool | `DebugService.cs` | Implemented but **unusable**: `Resources.Load<GameObject>("DebugSphere")` and no `Assets\Resources` folder exists; not present in scene |
| Entity damage scaffolding | `PlayerEntity.cs`, `EnemyEntity.cs`, `IEntity.cs`, `IEnemyEntity.cs` | Stubbed |
| **Actual damage dealing** | `DealDamage.cs` | **Disabled** (logic commented out, §5) |
| Attack selection (ChooseAttack) | `ChooseAttack.cs` | Empty ScriptableObject |
| Ranged / Sacrifice / Strong / Combo attacks | `Attack states\*.cs` | Throw `NotImplementedException` |
| Spawn system (cost-based archetype spawning) | `EnemyController.cs:36-43` header comment only | Not implemented |
| GameManager | `GameManager.cs` | Cursor lock only |
| Audio / UI menus / save / upgrades / loot | — | Absent |

---

## 4. GDD Coverage

**Not Found.** No GDD (`.md`/`.txt`/`.docx`/`.pdf` other than a shader readme) exists anywhere in the repo, and none was supplied. The requirement table cannot be completed until a GDD is provided. The closest thing to a design statement is the design comment in `EnemyController.cs:7-43` (enemy archetypes, poise design, cost-based spawn system).

---

## 5. Script Analysis (highlights with citations)

- `PlayerController.cs` — hub. Movement in `FixedUpdate` (`:123-129`), rotation + speed clamp in `Update` (`:131-151`). Sprint input forces `PlayerDashState` on every sprint press (`:66-69`). `Move` applies camera-relative velocity forces (`:188-205`). Jump queues when airborne (`:53-64`).
- `PlayerJumpState.cs:21` — `StartCoroutine(UpdateCoroutine())` is called **every Update frame** while the state is active; the coroutine ends by re-entering `PlayerLandState`. Coroutine spam.
- `PlayerDashState.cs:17` — `async void Enter()` with UniTask delay; state machine is not designed for async entry.
- `PlayerWallRunState.cs` — fully written wall-run (enter/exit, jump-off-wall, cancellation) but registration is commented out in `PlayerController.cs:39`. Dead-but-complete.
- `EnemyController.cs` — owns both entity + FSM ("what happened" → "which state"): wires stagger/die/damage events (`:94-120`), debug keys `H`/`J` to test damage (`:152-156`), `SeeThePlayer` drives target→attack/chase/patrol transitions (`:186-234`).
- `EnemyStateMachine.cs` — generic machine over `IEstate`, supports nested machines; `SetState` skips if already in state (`:42-43`).
- `AttackState.cs:39` — `combatActions.SetState<MeleeAttack>()` is **hardcoded**; there is no attack selection even though 6 combat actions are registered (`:28-33`).
- `DieState.cs:11-18` — constructor does not assign `agent`/`animator` (all other states do). `Enter()` dereferences them (`:17-18`) → **NullReferenceException when an enemy dies**.
- `ChaseState.cs:26-28` — `Tick()` is empty; the actual chasing is done by `EnemyController.SeeThePlayer`, which calls `SetState<ChaseState>()` on **every frame** the player is in range (`EnemyController.cs:228`).
- `StaggerState.cs` — plays `"GetHit"`/`"GetStun"` (`:35`,`:40`), but the TreeEntAsh controller exposes `GetHit1/2/3` and `Stun` (verified state list) → **no matching state; Unity logs a warning and plays nothing** for the TreeEntAsh enemy. (`DieState`'s `"Death"` and `SpownState`'s `"Idle"` similarly don't exist on the TreeEnt controller — it has `Idle1`.)
- `DealDamage.cs:14-18` — the `if ()` guard and the `TakeDamage` call are commented out → **no damage is ever applied to the player**.
- `EnemyEntity.cs` — clean poise loop: damage → dead? → poise? → staggered → else damage-taken (`:38-62`).
- `PlayerEntity.cs:34-37` — `CalculateDamageReduction` = `damage - baseDefense * Constants.ALPHA` (ALPHA = 0.5, `Constants.cs`).
- `DebugService.cs:32` — `Resources.Load<GameObject>("DebugSphere")`; `DebugSphere.prefab` sits in `Assets\Materials\` (not `Resources`) and there is no `Resources` folder → pool would `Instantiate(null)`. Not referenced in the scene today.
- `GameManager.cs` — cursor lock only; no game state.

---

## 6. Architecture

- **Two parallel state machines:**
  - Player: generic `StateMachine<T>` + abstract `State<T>` (`FiniteStateMachine.cs`), states in `Assets\Scripts\Player\`.
  - Enemy: `EnemyStateMachine<T>` over `IEstate` (`EnemyStateMachine.cs`), states in `Assets\Scripts\Enemy\enemy states\`, plus a nested machine inside `AttackState` over `CombatActionState` for combat actions.
- **Data classes:** `PlayerContext` (`[Serializable]`, holds refs + tuning — serialized correctly in the scene), `PlayerEntity`, `EnemyEntity` (plain serializable classes owned by MonoBehaviours). No ScriptableObject-driven tuning except patrol routes.
- **Events:** `EnemyEntity` raises `OnDamageTaken/OnStaggered/OnDied`; `EnemyController` subscribes and maps them to states. Input flows through static C# events in `InputController` (no DI, no event bus beyond that).
- **One hub per side:** `PlayerController` / `EnemyController` translate state into physics/agent/animation commands.
- **Dependencies:** vendored UniTask at `Assets\Plugins\UniTask` (3 asmdefs). All game code compiles into default `Assembly-CSharp` (no project asmdefs). Cinemachine 3 drives the FreeLook cameras; Animation Rigging rigs present in the scene; Visual Scripting package is installed but unused (0 graphs).

---

## 7. Assets

| Asset group | Location | Notes |
|---|---|---|
| TreeEnts (4 variants) | `Assets\PackOfTreeEnts\` | Models, animations, controllers; very large textures (up to ~32 MB PNGs) |
| Player character | `Assets\Player_v.3\` | SciFiTrooper + `Showcase.unity` + `SK_SciFiTrooperManV3 Variant.prefab` |
| Katana | `Assets\Katana\` | `Sheathe.fbx` used in scene; includes Blender sources (`.blend`, `.blend1` backups) |
| Bruiser Monster (exploding enemy) | `Assets\Expoded enemy\` | FBX + 7 `.anim` clips + `Exploded monster.controller` |
| Dissolve shader | `Assets\ShaderGraph_Dissolve\` | Sub-graphs, sample URP scene, `EnemyDissolveMaterial.mat` |
| Textures | `Assets\Textures\` | — |
| NavMesh | `Assets\Scenes\TestingScene\NavMesh-navMesh Settings.asset` | Baked navmesh for the test scene |

Animator controllers: `TreeEntAshAnimatiorController.controller` (guid `cefaf9ad...`, states incl. `Attack1-7`, `GetHit1-3`, `Stun`, `Run`, `Walk`, `Idle1`) and `Exploded monster.controller` (states `Idle, Walk, Run, GetHit, GetStun, Death, Attack1` — **matches the names the code uses**).

Prefabs (`Assets\prefabs\`): `TreeEntAsh.prefab` (full enemy wrapper: EnemyController, NavMeshAgent, Animator→TreeEnt controller, Rig, EnemyDissolve, 2× DealDamage); `TreeEntBirch/Oak/Spruce.prefab` are **model-only** (no EnemyController/NavMeshAgent — verified).

No audio assets exist anywhere (no `.wav/.mp3/.ogg/.aiff`, no clips). No UI sprites/screens.

---

## 8. Git Readiness

- **Not a Git repository.** `git rev-parse` fails; there is no `.git` directory.
- `.gitignore` exists and is a standard Unity ignore file; `ignore.conf` additionally ignores `.vscode` (folder present with `vstuc` attach config — should not be committed).
- A `.slnx` solution file exists at root.
- Items to review before the first commit:
  - `Assets\_Recovery\0.unity` — backup scene, likely disposable.
  - `Assets\Katana\**\*.blend1` — Blender autosave backups.
  - Large PNG textures in `PackOfTreeEnts` / `Player_v.3` (repo size).
  - `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/` — already gitignored.
- Recommended next step: `git init`, confirm `.gitignore` coverage, commit `Assets/` + `ProjectSettings/` + `Packages/`.

---

## 9. Risks

1. **Missing player animator controller** (`da65a6d29...`) — referenced by the player's Animator but absent from the project; player animation is broken until restored.
2. **Missing scene in build settings** — `SampleScene.unity` doesn't exist; `TestingScene` isn't in the build.
3. **No damage anywhere** — `DealDamage.cs` logic is commented out; enemies can never hurt the player.
4. **`DieState` NullReference** — unassigned `agent`/`animator` (`DieState.cs:11-18`) → crash on enemy death.
5. **Animation state-name mismatch** — enemy code plays `Idle/GetHit/GetStun/Death` but the TreeEnt controller has `Idle1/GetHit1-3/Stun` and no `Death`; the scene's `Enemy` also has the TreeEnt controller on a Bruiser rig.
6. **Inactive/zeroed enemy** — the "Enemy" object is inactive with zeroed stats; only "Exploding Enemy" runs.
7. **Two unresolvable script references** on "Exploding Enemy" (guids `b2d8418b...`, `fff0960e...`) — missing-script components unless they're package components.
8. **`ChaseState.Tick` empty + per-frame `SetState`** — state churn and no real pursuit logic.
9. **Unset `animator` on `PlayerController`** (`TestingScene.unity:8413`) — no player animation control even after the controller is restored.
10. **Coroutine spam** in `PlayerJumpState.Update`.
11. **`AttackState` hardcodes Melee** — no archetype variety despite 6 registered actions.
12. **`DebugService` would crash** if added to the scene (no `Resources` folder).
13. **No GDD / no design doc** — coverage and priorities are undefined.
14. **Repo hygiene** — `.blend1` files, `_Recovery`, multi-hundred-MB textures.
15. **Scene serialized values differ from script defaults** — tuning lives only in the scene; easy to lose.

---

## 10. Learning Guide (reading order)

1. `Assets\Scripts\FiniteStateMachine.cs` + `PlayerController.cs` — the movement state machine, how `Update`/`FixedUpdate` split work, queueJump.
2. `PlayerMovementStates\*` — how each movement state mutates the context.
3. `PlayerWallRunState.cs` — the most complete single feature (currently disabled).
4. `EnemyStateMachine.cs` + `EnemyController.cs` — the entity→event→state pipeline and `SeeThePlayer`.
5. `enemy states\*` — Spawn → Patrol → Chase → Attack → Stagger → Die flow.
6. `EnemyEntity.cs` + `PlayerEntity.cs` — poise/damage math.
7. `EnemyDissolve.cs` — Unity-object-pool + runtime material/renderer management; good C# reference.
8. Scene wiring — open `TestingScene`, inspect `Player`, `Enemy`, `Exploding Enemy`, `navMesh Settings`, FreeLook cameras.

---

## 11. Questions for the Team

1. Where is the **GDD**? (A written design doc is required for any coverage/QA.)
2. What happened to the **player animator controller** (guid `da65a6d29...`)? Can it be restored or rebuilt?
3. Is `Assets/Scenes/SampleScene.unity` (in build settings) expected, or should build settings point at `TestingScene`?
4. Should `Assets\_Recovery\0.unity` be deleted (or kept as backup and gitignored)?
5. What are the two missing scripts on **Exploding Enemy** (guids `b2d8418b...`, `fff0960e...`)? Deleted `DealDamage`/`Rig`? Recreate or remove.
6. Is the **TreeEntAsh** enemy the intended "grunt" archetype, and is the **BruiserMonster** the "exploding" archetype? The scene mixes their animators.
7. Who owns **attack selection** (`ChooseAttack` is empty, `AttackState` hardcodes Melee)?
8. Should **player damage** (currently commented out in `DealDamage`) be enabled, and is there a player combat system planned?
9. Is the **poise damage** (H/J debug keys) a temporary debug harness or a shipped mechanic?
10. Is wall-running meant to be enabled? (`PlayerController.cs:39` is commented out.)
