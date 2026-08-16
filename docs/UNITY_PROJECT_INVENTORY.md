# Unity Project Inventory — TestingScene (Static Evidence)

- **Date:** 2026-08-08
- **Branch:** `feature/roguelike-system` (HEAD `cc11a51` "feat: add roguelike run controller")
- **Method:** YAML/serialized-file analysis only (`.unity`, `.prefab`, `.asset`, `.cs.meta`). **No runtime verification.** Everything below is *static evidence*.
- **Evidence base:** `Assets/Scenes/TestingScene.unity` (15,848 lines, 542 serialized blocks), prefabs under `Assets/prefabs/`, `ProjectSettings/TagManager.asset`, script GUID→file mapping via `.meta` files.
- **Status of this doc:** untracked planning material under `docs/`. Nothing modified, committed, or pushed.

> **Status note (cleanup branch `fix/roguelike-spawn-cleanup`):** this inventory is a historical snapshot of `feature/roguelike-system`. `WeaponType.cs` / `WeaponData.cs` (see §1/§9 below) were **removed as obsolete** on the cleanup branch — the merged Combat system provides the real weapon abstraction (`Assets/Scripts/Combat/Objects/WeaponObject.cs` + `AttackData.cs`). Prefab paths cited here (`Assets/prefabs/…`) were since renamed to `Assets/Prefabs/…` on `main`.

---

## 1. SCENES

| Scene | Notes |
|---|---|
| `Assets/Scenes/TestingScene.unity` | **The test/main scene.** Contains GameManager, Player, 2 enemies, arena (Walls/Ground), NavMesh surfaces, 2 camera setups, Canvas debug UI. |
| `Assets/_Recovery/0.unity` | Recovery/dev artifact — not production evidence. |
| `Assets/Player_v.3/Showcase.unity` | Player showcase/dev scene — not production evidence. |
| `Assets/ShaderGraph_Dissolve/URP/URP Samples.unity` | URP sample scene — not production evidence. |

---

## 2. SCENE HIERARCHY (root objects, 14 roots)

```
Global Volume
├─ (Volume component; no children)
Player [tag "Player"(invalid), layer 7 "Wall"]
├─ PlayerObj [Animator, CapsuleCollider, mesh]
│  ├─ Orientation [mesh]
│  └─ KatanaWeapon [Transform ONLY — no script/mesh]
Ground [BoxCollider, NavMeshSurface]
navMesh Settings [NavMeshSurface]
Walls
├─ Wall, Wall (1) .. Wall (8)   [each: MeshFilter, MeshRenderer, BoxCollider]
Directional Light [Light, UniversalAdditionalLightData]
FreeLook Camera                    (primary, Unity-6-style Cinemachine FreeLook, no child rigs)
Enemy                              ⚠ DISABLED (m_IsActive: 0) — TreeEnt model, tag "Enemy"
├─ Hurt Box [SphereCollider, DealDamage]
├─ root ── TreeEnt skeleton (~130 bones; foot_l has BoxCollider + DealDamage)
├─ TreeEntAsh [SkinnedMeshRenderer]
└─ Rig 1 [Animation Rigging Rig]
Exploding Enemy                    (ACTIVE — Bruiser/werewolf model, tag Untagged)
├─ mixamorig:* skeleton (~75 bones)
├─ Werewolf_Mouth / WerewolfBody / WerewolfEye / WerewolfEye001 / WerewolfHead [SkinnedMeshRenderer]
└─ Rig 1 [Animation Rigging Rig]
GameManager [GameManager, DebugService, InputController]
Main Camera [Camera, AudioListener, PlayerCamera, CinemachineBrain, UniversalAdditionalCameraData]
EventSystem [EventSystem, UI input module]
FreeLook Camera BACKUP - not fully upgradable by CM    (legacy CinemachineFreeLook)
├─ TopRig [CinemachineVirtualCamera] └─ cm [Composer/OrbitalTransposer/Pipeline]
├─ MiddleRig [CinemachineVirtualCamera] └─ cm
└─ BottomRig [CinemachineVirtualCamera] └─ cm
Canvas [Canvas, CanvasScaler (1920×1080), GraphicRaycaster]   (root via RectTransform)
├─ PlayerCurrentState  [legacy UI Text]  text: "Current State : None / Previous State : None"
└─ EnemyCurrentState (1) [legacy UI Text] text: "Current State : None / Previous State : None"
```

Root-count note: 13 `Transform` roots + `Canvas` (RectTransform root) = 14 root GameObjects. `FreeLook Camera BACKUP` is the only camera with explicit rig children (TopRig/MiddleRig/BottomRig); the primary `FreeLook Camera` has no serialized child rigs.

---

## 3. GAMEOBJECT → COMPONENTS (detailed)

### Player (GO 1863943522)
- Transform, MeshFilter, Rigidbody (mass 1), PlayerController
- LocalPosition `{2.95, 1.548, 4.72}`, layer 7, **tag "Player"** (invalid — see §14)
- PlayerObj (1899011098): Transform, MeshFilter, MeshRenderer, CapsuleCollider, Animator (controller guid `da65a6d2` = player animator controller)
  - Orientation: Transform, MeshFilter, MeshRenderer
  - KatanaWeapon: Transform only

### Enemy (GO 5289385902289650030) — ⚠ m_IsActive: 0
- Transform, EnemyController, NavMeshAgent (radius .5, speed 3.5, accel 200, stop dist 0, height 2), Animator (controller `cefaf9ad` = TreeEntAshAnimatorController), EnemyDissolve, Rigidbody (useGravity 0, isKinematic 1, constraints 80), Animation-Rigging RigBuilder, BoneRenderer
- Tag "Enemy", layer 0. Stats serialized in EnemyController.enemyEntity = **0/0/0/0/0/100** (see §14.5)

### Exploding Enemy (GO 2282024516536603690) — ACTIVE
- Transform, EnemyController, Animator (controller `4d6bc721` = Exploded monster controller, avatar BruiserMonster FBX), RigBuilder, BoneRenderer, NavMeshAgent (same defaults), EnemyDissolve, Rigidbody (same)
- Tag Untagged, layer 0. LocalPosition `{6.72, 0.55, 23.25}`, scale `{2,2,2}`.
- Stats serialized in EnemyController.enemyEntity = **100/100/20/20/100/100**

### GameManager (GO 103219077)
- Transform, GameManager, DebugService, InputController
- DebugService: `debugSpherePrefab` → `Assets/Materials/DebugSphere.prefab`, `poolSize 5`, `visualizationEnabled 0`

### Main Camera (GO 963194225)
- Transform, Camera, AudioListener, PlayerCamera (`player` → Player transform, `sensitivity 0.1`), CinemachineBrain, UniversalAdditionalCameraData

### EventSystem
- EventSystem (UnityEngine.EventSystems) + InputSystem UI input module

### Ground / navMesh Settings / Walls
- Ground: Transform, MeshFilter, MeshRenderer, BoxCollider, NavMeshSurface (package `com.unity.ai.navigation`)
- navMesh Settings: Transform + NavMeshSurface
- Walls (8): each Transform, MeshFilter, MeshRenderer, BoxCollider; layer 7 "Wall"

### Hurt Box (child of Enemy)
- Transform, SphereCollider, DealDamage (`damage 20`, `enemyController` → Enemy's EnemyController)
- Also `foot_l` bone: BoxCollider + DealDamage (`damage 20`, same enemyController)

---

## 4. PREFABS (under `Assets/prefabs/`)

| Prefab | Contents |
|---|---|
| `TreeEntAsh.prefab` | PrefabInstance of `TreeEntAsh.FBX` (guid `cbbfbfed`). Root adds: **Animator** (TreeEnt controller, avatar 0, ApplyRootMotion 1), **EnemyController** (targetTransform `{fileID:0}` ⚠, `_debugText` `{fileID:0}` ⚠, patrolRoute→routeA, attackRange 6, patrol 3 / chase 8), **EnemyDissolve**, **Rigidbody** (kinematic, no gravity), **NavMeshAgent** (r .5 / speed 3.5 / h 2), **CapsuleCollider** (r .72 / h 2.32 / not trigger / EnemyNotSliding mat), **RigBuilder**, **BoneRenderer**. Plus two child colliders: BoxCollider(trigger)+DealDamage(20) and BoxCollider(trigger)+DealDamage(20). |
| `TreeEntBirch.prefab`, `TreeEntOak.prefab`, `TreeEntSpruce.prefab` | Same pattern (FBX variant + gameplay components). |
| `routes/level 1/routeA.asset` | **PatrolRoute** ScriptableObject (`Assembly-CSharp::PatrolRoute`); `wayPoints` → 4 waypoint prefabs. |
| `routes/level 1/first route/waypoint.prefab` (+ `(1)` `(2)` `(3)`) | Single GameObject with Transform only (e.g., waypoint at `{0.6, 0.5, 25.18}`). |
| `Materials/DebugSphere.prefab` | Referenced by GameManager's DebugService (debug visualization). |

Scene does **not** use TreeEntAsh.prefab as a whole — the two enemy roots are hand-placed instances; the TreeEnt body/skeleton is a raw FBX instance (stripped PrefabInstance of `TreeEntAsh.FBX`), the Bruiser/werewolf is the "Exploded enemy" FBX.

---

## 5. SCRIPT CONNECTIONS (GUID → source)

Scene MonoBehaviour GUIDs that resolve inside `Assets/`:

| GUID | Script | Used on |
|---|---|---|
| `3e5f855d…` | `Scripts/GameManager.cs` | GameManager |
| `e9e1f8a8…` | `Scripts/DebugService.cs` | GameManager |
| `55ea699a…` | `Scripts/InputController.cs` | GameManager |
| `61e60d08…` | `Scripts/Player/PlayerController.cs` | Player |
| `8f865b08…` | `Scripts/Player/PlayerCamera.cs` | Main Camera |
| `0875c81e…` | `Scripts/Enemy/EnemyController.cs` | Enemy, Exploding Enemy |
| `ac10486b…` | `Scripts/Enemy/Attack states/DealDamage.cs` | Hurt Box, foot_l, prefab triggers |
| `06ea807e…` | `Materials/shaders/EnemyDissolve.cs` (misfiled under Materials) | both enemies |
| `fb13b98d…` | `Scripts/Enemy/PatrolRoute.cs` | routeA.asset |

Script GUIDs that resolve to **packages** (NOT missing, identified via `m_EditorClassIdentifier`):
- `Unity.Cinemachine.*` — FreeLook Camera, Main Camera, BACKUP camera (`CinemachineCamera`, `CinemachineOrbitalFollow`, `CinemachineRotationComposer`, `CinemachineInputAxisController`, `CinemachineFreeLookModifier`, `CinemachineCollisionImpulseSource`, `CinemachineComposer`, `CinemachineOrbitalTransposer`, `CinemachinePipeline`, `CinemachineBrain`, `CinemachineDoNotUpgrade`)
- `Unity.AI.Navigation.NavMeshSurface` — Ground, navMesh Settings
- `Unity.Animation.Rigging.RigBuilder` / `BoneRenderer` / `Rig` — both enemies + `Rig 1` children (GUIDs `fff0960e…`, `b2d8418b…`, `70b342d8…` were previously suspected "missing" — **they are not missing**)
- URP `Volume` (Global Volume), `UniversalAdditionalLightData` (Directional Light), `UniversalAdditionalCameraData` (Main Camera)
- uGUI `UnityEngine.UI.Text`, `CanvasScaler`, `GraphicRaycaster`, `EventSystem`, input UI module

No script GUID in the scene is unresolvable → **no missing scripts** in TestingScene.

---

## 6. SERIALIZED REFERENCES (cross-object wiring)

| Field | Value (static) |
|---|---|
| `PlayerController.context.rb` | Player Rigidbody (1863943527) |
| `PlayerController.context.playerCamera` | Main Camera GO (963194228) |
| `PlayerController.context.playerModel` | PlayerObj Transform (1899011099) |
| `PlayerController.context.debugText` | PlayerCurrentState Text (1508081998) |
| `PlayerController.context.animator` | **{fileID: 0} — UNASSIGNED** |
| `PlayerController.context.groundLayer` | bit 6 (Ground); `wallLayer` bit 7 (Wall) |
| `EnemyController.targetTransform` (both) | Player Transform (1863943526) |
| `EnemyController.patrolRoute` (both) | routeA.asset |
| `EnemyController._debugText` (both) | EnemyCurrentState (1) Text (92381217) |
| `DealDamage.enemyController` (both scene) | Enemy's EnemyController |
| `DebugService.debugSpherePrefab` | Materials/DebugSphere.prefab |
| `PlayerCamera.player` | Player Transform |

---

## 7. RUN SYSTEM STATUS

- `Assets/Scripts/Roguelike/` (all committed on `feature/roguelike-system`): `RunController.cs` (`cc11a51`), `RunData.cs` (`2e1d63e`), `RunState.cs` + `RunStateMachine.cs` (`a473850`), `WeaponType.cs` + `WeaponData.cs` (`0f91335`).
- **Static finding:** none of the Roguelike scripts are attached to any object in TestingScene (no `RunController`/`RunData`/`RunStateMachine` GUID in the scene). The run controller is code-only; it is **not wired into the scene** and there is **no floor/spawn bootstrap object**.
- `GameManager.cs` has no serialized fields (minimal; per prior docs: cursor-lock only).

## 8. SPAWN SYSTEM STATUS

- **None present.** No spawner MonoBehaviour, no spawn-point GameObjects, no enemy archetype/table assets in the scene. The two enemies are hand-placed scene instances, not spawn products.
- `RunController.BeginFloor()` (per prior docs) depends on `RunData.enemyBudget` / `floor` — a spawn system consuming those does not exist yet in static evidence.

## 9. ENEMY SYSTEM STATUS

- `EnemyController` on both enemies; `enemyEntity` serialized stats differ per instance (§3). Both wired to Player transform + routeA + debug text.
- NavMeshAgent defaults r .5 / speed 3.5; EnemyController patrol 3.6 / chase 9 (scene override) vs prefab 3 / 8.
- Animation Rigging present (RigBuilder/BoneRenderer/Rig) on both.
- Code-level facts from prior docs (cross-reference, not runtime-verified here): `EnemyEntity.OnDied` (:22) / `OnDamageTaken` (:61) exist; `EnemyController` subscribes `OnDied += HandleDied` (:103-104); `EnemyEntity` stat properties read-only — no setters; `DealDamage.cs:14-18` damage call commented out; `DieState.cs:11-18` has NRE risk on unassigned agent/animator.

## 10. PLAYER SYSTEM STATUS

- Player root has PlayerController (context fully wired except `animator` unassigned) + Rigidbody; PlayerObj child has CapsuleCollider + Animator (player controller, guid `da65a6d2`).
- Player camera: PlayerCamera on Main Camera + CinemachineBrain; FreeLook Camera (primary) + legacy BACKUP.
- Code-level facts from prior docs: `PlayerController` exposes `IEntity Entity` (:27) = `playerEntity`; `CombatContext` commented out (:14).

## 11. COMBAT / WEAPON STATUS

- `KatanaWeapon` child exists on PlayerObj as a **Transform only** (no mesh, no script, no WeaponData reference).
- Weapon foundation committed (`WeaponData` / `WeaponType`) but **no weapon component is attached anywhere** in the scene.
- DealDamage triggers exist on enemy Hurt Box / foot_l / prefab foot colliders (`damage 20`), wired to EnemyController.

## 12. UI STATUS

- Canvas (1920×1080 CanvasScaler, ScreenMatchMode 0.5 match) with **two legacy `UI.Text` debug labels**: `PlayerCurrentState` and `EnemyCurrentState (1)` (static text "Current State : None / Previous State : None"; font size 211 at 0.17 scale).
- No HUD, health bar, damage numbers, or run-state UI. `RunState`/`RunData` are not surfaced to UI.

## 13. BROKEN / MISSING (static evidence)

1. **Player tag invalid:** scene Player GO has `m_TagString: Player`, but `TagManager.asset` defines only `Enemy` — "Player" tag does not exist (object will show an undefined-tag warning in-editor).
2. **Player layer mislabel:** Player root `m_Layer: 7` = "Wall".
3. **PlayerController.context.animator = 0** (unassigned).
4. **"Enemy" (TreeEnt) is disabled** (`m_IsActive: 0`); only "Exploding Enemy" is active.
5. **Disabled Enemy has zeroed stats** (`enemyEntity` 0/0/0/0/0/100) — re-enabling it gives a broken enemy.
6. **Active "Exploding Enemy" is tag Untagged** (not "Enemy") on layer 0.
7. **TreeEntAsh.prefab's EnemyController** has `targetTransform`/`_debugText` unassigned (prefab not self-contained; scene overrides them).
8. **Roguelike run system not in scene** (no RunController object, no spawn bootstrap).
9. **No spawn points / spawn system / archetype assets** for the run loop.
10. **Duplicate camera** ("FreeLook Camera BACKUP" — legacy CM FreeLook kept around; only primary is likely used).
11. Two `DealDamage` instances serialized with `m_EditorClassIdentifier: '::'` (editor-added; cosmetic, script resolves fine).

## 14. ARCHITECTURE GRAPH

```
                     ┌──────────────────────────────────────────────────────────────┐
                     │                    TestingScene (arena)                       │
   Player ── PlayerController (context) ── playerCamera ── Main Camera (PlayerCamera)│
     │                                   └─ playerModel ─ PlayerObj (Animator,       │
     │                                      CapsuleCollider) └─ KatanaWeapon [empty]  │
     └─ Rigidbody ───────────────────────────────────────────┐                       │
                                                             │                       │
   Enemy (TreeEnt) ─ DISABLED ┐                              │                       │
     ├─ EnemyController ◄─────┴─ targetTransform             │                       │
     │   ├─ enemyEntity (0/0/0/0/0/100)                      │                       │
     │   ├─ patrolRoute ──► routeA ──► 4 waypoint prefabs    │                       │
     │   └─ _debugText ──► EnemyCurrentState (1) [UI.Text]   │                       │
     ├─ Hurt Box [DealDamage 20] ──► EnemyController         │                       │
     ├─ NavMeshAgent / Animator / EnemyDissolve / RigBuilder │                       │
     └─ foot_l [DealDamage 20] ──► EnemyController           │                       │
   Exploding Enemy ─ ACTIVE                                  │                       │
     ├─ EnemyController ── enemyEntity (100/100/20/20/100/100)│                      │
     ├─ NavMeshAgent / Animator / EnemyDissolve / RigBuilder │                       │
     └─ (werewolf skeleton, 5 SkinnedMeshRenderers)          │                       │
                                                             │                       │
   GameManager [GameManager, DebugService, InputController]  │                       │
   Main Camera [PlayerCamera, CinemachineBrain] ─── FreeLook Camera (primary)        │
   Canvas [PlayerCurrentState, EnemyCurrentState (1)]        │                       │
   Ground / navMesh Settings [NavMeshSurface]  ◄── NavMeshAgent ─────────────────────┘
   Walls (8) [BoxColliders]  ──────────────────►  playerContext.wallLayer (layer 7)
                                                                                     │
   ── NOT WIRED ──────────────────────────────────────────────────────────────────  │
   Assets/Scripts/Roguelike/{RunController,RunData,RunState,RunStateMachine}         │
   Assets/Scripts/Roguelike/{WeaponData,WeaponType}  ──► KatanaWeapon (nothing)      │
   (no spawner, no spawn points, no RunController component in scene)                │
```

## 15. RUN SYSTEM READINESS → "Can we start Sprint 4 right now?"

**Verdict: BLOCKED — UNITY EDITOR WORK + TEAM DECISION REQUIRED** (consistent with `docs/ROGUELIKE_RUN_SYSTEM_SPRINT_4.md`).

What static evidence supports, concretely:
- **Ready (code):** Run side committed (`RunController`, `RunData.enemyBudget`/`floor`, `BeginFloor()`), enemy `OnDied`/`OnDamageTaken` events exist, PatrolRoute asset exists (routeA + waypoints).
- **Blocked — editor work (scene):** no spawn points; no enemy archetype/table assets; `RunController` not attached to any object; active enemy lacks the `Enemy` tag; Player tag/layer are wrong; TreeEnt "Enemy" disabled with zeroed stats.
- **Blocked — team dependency (code, per prior docs, not runtime-verified here):** `EnemyEntity` has no stat setters (spawner can't configure enemies), `DealDamage` damage call commented out, `DieState` has unassigned-agent/animator NRE risk, and an explicit spawn-system owner decision is outstanding.

A Sprint-4 start requires, at minimum: (a) a scene bootstrap object carrying `RunController`, (b) a spawner + spawn points, (c) enemy archetype wiring so `enemyBudget`/`floor` can drive `EnemyController.enemyEntity` configuration, and (d) the Team Decision on spawn-system ownership. None of (a)-(d) exist in the scene today.

## 16. IMPORTANT LIMITATION

- All findings are **static/serialized-file evidence**. Package scripts are identified from `m_EditorClassIdentifier` + package GUID resolution; no Unity runtime or editor session was used.
- Code-level claims (EnemyEntity setters, DealDamage commented call, DieState NRE, PlayerController `IEntity.Entity`, run-state tests 26/26) are cross-referenced from prior planning docs under `docs/` and were **not** re-verified in this pass.
- "BROKEN/MISSING" items are static anomalies (unassigned refs, disabled GO, invalid tags) — whether they manifest as runtime failures must be confirmed in-editor/play mode.
- `docs/` remains untracked; no files were modified or committed as part of this inventory.
