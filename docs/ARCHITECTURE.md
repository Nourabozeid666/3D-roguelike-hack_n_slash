# Architecture — 3D-Roguelike Hack'n'Slash

Architecture documentation for the Unity project at `E:\course\03_Programming_Data\game_dev\projects\3D-roguelike-hack_n_slash`.
All claims cite a file path or a line number inside a file. Where something is inferred rather than observed, it is explicitly marked **[assumption]**.

---

## 1. Overview

A single-scene 3D third-person prototype built around **two parallel state machines**:

- **Player** — generic `StateMachine<T>` + `State<T>` (`Assets\Scripts\FiniteStateMachine.cs`) driving rigidbody movement.
- **Enemy** — `EnemyStateMachine<T>` over `IEstate` (`Assets\Scripts\Enemy\EnemyStateMachine.cs`) driving NavMeshAgent AI, with a **nested combat-action machine** inside `AttackState`.

Each side has one **hub MonoBehaviour** that owns all wiring:

| Side | Hub | Role |
|---|---|---|
| Player | `PlayerController` (`Assets\Scripts\Player\PlayerController.cs`) | Runs the movement loop, exposes context properties to states, translates camera-relative input to forces |
| Enemy | `EnemyController` (`Assets\Scripts\Enemy\EnemyController.cs`) | Owns entity + FSM, translates "what happened" (entity events) into "which state" |

The design intent is captured in the header comment at `EnemyController.cs:7-43` (grunt/sacrifice/ranged/defend archetypes, poise/stagger design, cost-based spawn system). None of the archetype or spawn work is implemented.

---

## 2. Folder Architecture

```
Assets
├── Scripts                              ← all game code (default Assembly-CSharp)
│   ├── FiniteStateMachine.cs            (player StateMachine<T> + State<T>)
│   ├── InputController.cs               (static input-event bridge)
│   ├── GameManager.cs                   (cursor lock only)
│   ├── DebugService.cs                  (object-pooled debug spheres — DEAD, see §8)
│   ├── Player\
│   │   ├── PlayerController.cs          (player hub)
│   │   ├── PlayerContext.cs             (movement data bag, [Serializable])
│   │   ├── PlayerEntity.cs              (player stats, IEntity)
│   │   ├── PlayerCamera.cs              (DISABLED in scene; Cinemachine used instead)
│   │   └── PlayerMovementStates\        (Idle/Move/Sprint/Dash/Jump/Land/Slide/WallRun)
│   ├── Enemy\
│   │   ├── EnemyController.cs           (enemy hub)
│   │   ├── EnemyEntity.cs               (health + poise, IEnemyEntity)
│   │   ├── EnemyStateMachine.cs         (generic FSM + IEstate, nested-machine capable)
│   │   ├── EnemyState.cs                (abstract state base)
│   │   ├── PatrolRoute.cs               (ScriptableObject waypoint list)
│   │   ├── enemy states\                (Spown/Patrol/Chase/Attack/Stagger/Die)
│   │   └── Attack states\               (Melee/Exploding/Ranged/Sacrifice/Strong/Combo/DealDamage/ChooseAttack)
│   ├── Combat\CombatContext.cs          (EMPTY class — dead)
│   ├── Data\Constants.cs                (const ALPHA = 0.5f)
│   └── Interfaces\IEntity.cs, IEnemyEntity.cs
├── Materials\
│   └── shaders\EnemyDissolve.cs         (608-line C# effect — code misfiled under Materials)
├── Settings                             (URP pipeline assets, volume profiles)
├── prefabs\
│   ├── TreeEnt{Ash,Birch,Oak,Spruce}.prefab  (Ash = full enemy wrapper; others model-only)
│   └── routes\level 1\first route\waypoint.prefab (+1/2/3) + routeA.asset
├── Scenes\TestingScene.unity            (MAIN scene) + baked NavMeshData
├── Plugins\UniTask\                     (vendored async library, 3 asmdefs)
├── PackOfTreeEnts\  Player_v.3\  Expoded enemy\  Katana\  ShaderGraph_Dissolve\   (vendor art)
├── Textures\                            (loose textures)
└── _Recovery\0.unity                    (old scene backup)
```

Vendor folders (`PackOfTreeEnts`, `Player_v.3`, `Expoded enemy`, `Katana`, `ShaderGraph_Dissolve`, `Plugins\UniTask`) are third-party assets; `Scripts\Combat` and `DebugService` are dead code.

---

## 3. Core Patterns

### 3.1 Player state machine

`StateMachine<T>` (`FiniteStateMachine.cs`) is a plain class storing states in `Dictionary<Type, State<T>>`:

- `AddState` → calls `state.SetState(this, owner)` and registers it (`:37-41`).
- `SetState<T>` → **exits the current state first**, then enters the new one (`:43-54`). Note: no same-state guard — re-entering a state re-runs its `Enter`.
- `Update` → ticks the current state and writes debug text (`:28-35`).
- `CheckState<T>` → `currentState.GetType() == typeof(T)` (`:23-26`).

`State<T>` is abstract: `Enter() / Update() / Exit()`, plus `SetState` injecting `_stateMachine` + `_owner`.

The player states are constructed in `PlayerController.Awake` (`PlayerController.cs:29-48`). Each receives `context.animator`, but **every animation call in the states is commented out** (`PlayerMoveState.cs:17`, `PlayerSprintState.cs:11`, `PlayerIdleState.cs:11`). Wall-run registration is commented out (`PlayerController.cs:39`).

### 3.2 Enemy state machine (nested-capable)

`EnemyStateMachine<T>` where `T : class, IEstate` (`EnemyStateMachine.cs`):

- `SetState<TState>` where `TState : class, T` — **skips if already in that state** (`:42-43`), otherwise exits current and enters new (`:40-55`).
- Supports nested machines: `AttackState` holds an `EnemyStateMachine<CombatActionState>` (`AttackState.cs:21`).

`EnemyState` base (`EnemyState.cs`): holds `enemyController`, virtual `CanBeInterrupted => true` (overridden to `false` by Spawn/Stagger/Die), abstract `Enter/Exit/Tick`.

### 3.3 Entity → event → state pipeline

`EnemyEntity.TakeDamage(damage, poiseDamage)` (`EnemyEntity.cs:38-62`) is a clean decision loop:

1. `damage <= 0` → ignore.
2. health `<= 0` → `OnDied` (dead things don't also stagger).
3. poise `<= 0` → reset poise to max, fire `OnStaggered`.
4. else → fire `OnDamageTaken(damage)`.

`EnemyController.Start` subscribes these to state transitions (`EnemyController.cs:94-120`):

| Event | Transition |
|---|---|
| `OnDied` | `SetState<DieState>()` |
| `OnDamageTaken` | `SetState<StaggerState>()` + reaction Hit |
| `OnStaggered` | `SetState<StaggerState>()` + reaction Stun |

Stagger/Die return through coroutines: `StaggerState` waits its duration then `SetState<PatrolState>()` (`StaggerState.cs:54-59`); `DieState` destroys the object after 3s (`DieState.cs:31-35`).

### 3.4 Data bags

- `PlayerContext` (`PlayerContext.cs`) — `[Serializable]` class holding references (rb, playerCamera, playerModel, animator, debugText, groundLayer/wallLayer) and tuning floats (gravity, speeds, drag, MaxVelocity, jump). Serialized on the MonoBehaviour; scene values differ from script defaults (e.g. scene `gravity:-20` vs script `-25`, `sprintSpeed:12` vs `145`, `walkSpeed:8` vs `100`).
- `PlayerEntity` / `EnemyEntity` — plain serializable classes owned by the controllers. Damage math in `PlayerEntity.cs:34-37`: `damage - baseDefense * Constants.ALPHA`.

---

## 4. Input Flow

`InputController` (`InputController.cs`) is a static-event bridge:

- `Awake` creates `new InputSystem()` (generated from `Assets\InputSystem.inputactions`).
- `OnEnable/OnDisable` enable/disable the `PlayerMovement` action map.
- Static events: `OnMoveInput (Vector2)`, `OnSprintInput (bool)`, `OnJumpStart`.

`PlayerController.Start` subscribes lambdas to these events (`PlayerController.cs:53-69`):

- `OnJumpStart` → jump if grounded, else `queueJump = true`.
- `OnMoveInput` → writes `context.moveDirection`.
- `OnSprintInput` → sets `isSprinting` and **forces `PlayerDashState` on every sprint press** (`:66-69`).

**Pitfall [Fact]:** these lambdas are never unsubscribed (`OnDestroy` has no unsubscribe). Reloading the scene duplicates handlers → double jump/dash triggers.

The input actions `LightAttack` and `HeavyAttack` (left/right mouse) exist in the actions file but **nobody subscribes** to them — dead path.

---

## 5. Execution Flow

### 5.1 Player

```
Unity Play (TestingScene)
  ├─ Awake: PlayerController builds StateMachine + 7 states, SetState<Idle>
  ├─ Start: subscribes InputController static events
  └─ Loop:
       FixedUpdate → _stateMachine.Update() → ApplyCustomGravity → ApplyCustomDrag → Move (PlayerController.cs:123-129)
       Update      → Rotate → MaxVelocityUpdate → queued jump (PlayerController.cs:131-151)
```

State transitions (verified in code):
- `Idle ⇄ Move` on `MoveDirection.magnitude ≥ 0.1` (`PlayerIdleState.cs:19-22,27-30`, `PlayerMoveState.cs:18-20,34-37`).
- `Move ⇄ Sprint` on `IsSprinting` (`PlayerMoveState.cs:22-25`).
- Sprint press → `PlayerDashState` **directly** (`PlayerController.cs:66-69`); dash ends → Sprint/Move/Idle (`PlayerDashState.cs:36-47`).
- Jump press → `PlayerJumpState` + impulse (`PlayerController.cs:217-231`); airborne press queues jump, executed on landing (`:135-139`).
- `PlayerJumpState.Update` starts a coroutine **every frame** (`PlayerJumpState.cs:21`) that lands after 0.2s → `PlayerLandState` → immediately `PlayerIdleState` (`PlayerLandState.cs:30`).
- `PlayerSlideState` registered but never entered. `PlayerWallRunState` fully written but registration commented out.

**Dead paths [Fact]:** animation (all `_animator.Play` calls commented out + missing controller guid), combat (LightAttack/HeavyAttack unsubscribed), death (nothing calls `PlayerEntity.TakeDamage`).

### 5.2 Enemy

```
SpownState ─3s─► PatrolState ─(player seen)─► ChaseState ─(in range)─► AttackState(Melee)
     ▲                  │                          │                        │
     │             (lost target)           (per-frame SetState)      (on hurt events)
     └──────────────────┴──────────────────────────┴────────────────► StaggerState
                                                                     DieState (crashes, §7)
```

- Detection runs in `EnemyController.Update → SeeThePlayer` (`EnemyController.cs:146-234`), **not** in `ChaseState` (its `Tick` is empty, `ChaseState.cs:26-28`).
- `SeeThePlayer` calls `SetState<ChaseState>()` **every frame** while the player is in range (`:228`).
- `AttackState.Enter` hardcodes `MeleeAttack` (`AttackState.cs:39`); 6 combat actions are registered but never selected. `AttackState.Tick` only rotates toward the player (`:48-53`).
- `DealDamage.OnTriggerEnter` finds the player but the damage call is **commented out** (`DealDamage.cs:14-18`) → the player can never be hurt.
- `MeleeAttack` plays `"Attack1"` (`MeleeAttack.cs:16`).

---

## 6. Dependency Graph

### 6.1 PlayerController

```
PlayerController (MonoBehaviour)
├── PlayerContext (Serializable) ── rb, playerCamera, playerModel, animator (NULL in scene),
│                                   debugText, groundLayer=6, wallLayer=7, tuning floats
├── PlayerEntity (IEntity) ── Constants.ALPHA, events OnDamageTaken/OnHealed
├── StateMachine<PlayerController> ── 7 × State<PlayerController> (each holds owner + animator)
├── InputController (static events) ── OnMoveInput / OnSprintInput / OnJumpStart
└── (via PlayerDashState) Cysharp.Threading.Tasks (UniTask, async void Enter)
```

### 6.2 EnemyController

```
EnemyController (MonoBehaviour)
├── NavMeshAgent (scene: root of Exploding Enemy / Enemy)
├── Animator (scene: Exploded monster.controller / TreeEntAsh controller)
├── EnemyEntity (IEnemyEntity) ── events OnDamageTaken/OnStaggered/OnDied
├── EnemyStateMachine<EnemyState> ── 6 × EnemyState (each holds owner ref)
│     └── AttackState
│           └── EnemyStateMachine<CombatActionState> ── 6 × CombatActionState
├── PatrolRoute (ScriptableObject) ── routeA.asset ── 4 waypoint prefabs
├── Transform targetTransform (scene: Player)
├── Text _debugText (scene: EnemyCurrentState (1))
└── UnityEngine.InputSystem.Keyboard (debug H/J keys)
```

### 6.3 Cross-cutting

- **Intentional circular dependency (state pattern):** PlayerController → StateMachine → State → PlayerController; EnemyController → EnemyStateMachine → EnemyState → EnemyController.
- **Event-based communication:** `EnemyEntity` damage/stagger/die events; `InputController` static input events; `IEntity` damage/heal events.
- **ScriptableObjects:** `PatrolRoute` (used by both scene enemies); `ChooseAttack` (empty, unused).
- **Third-party:** UniTask (vendored), Cinemachine 3 (FreeLook cameras), Animation Rigging, AI Navigation (NavMeshSurface + baked data), uGUI, Visual Scripting (installed, 0 graphs), Timeline.

---

## 7. Known Issues (architecture-impacting)

1. **`DieState` NullReference** — `agent`/`animator` are never assigned in its constructor (`DieState.cs:11-18`); all other states assign them. `Enter()` dereferences both (`:17-18`) → crash on death. The player cannot kill enemies.
2. **No damage anywhere** — `DealDamage.cs:14-18` commented out; player never loses health.
3. **Missing player animator controller** — guid `da65a6d29ce66da459988c23539ddd08` referenced by the player Animator, exists nowhere in the project.
4. **`context.animator` null** — `TestingScene.unity:8413` `animator: {fileID: 0}`.
5. **Animation state-name mismatch** — code plays `Idle/GetHit/GetStun/Death`; TreeEnt controller exposes `Idle1/GetHit1-3/Stun` (no `Death`). Only `Exploded monster.controller` matches the code names.
6. **Scene enemies** — only `Exploding Enemy` is active; `Enemy` is inactive with zeroed stats (`health 0/0`). Both write to the same debug Text.
7. **Missing script references** — two guids (`b2d8418b…`, `fff0960e…`) on `Exploding Enemy` and `TreeEntAsh.prefab` resolve to no script in `Assets` **[assumption: deleted scripts or package components]**.
8. **Static event leak** — no unsubscription of `InputController` events on `PlayerController` destruction.
9. **`ChaseState.Tick` empty + per-frame `SetState`** — state churn; pursuit logic lives in the controller.
10. **Debug/log spam** — `Debug.Log` per patrol waypoint (`PatrolState.cs:36-37`), coroutine per `PlayerJumpState.Update`, debug text rebuilt every frame via `GetType().ToString()` (`EnemyController.cs:161-178`, `FiniteStateMachine.cs:33`).
11. **`DebugService` would crash** — `Resources.Load<GameObject>("DebugSphere")` with no `Assets\Resources` folder.

---

## 8. Dead Code Inventory

| Item | Why dead |
|---|---|
| `Scripts\Combat\CombatContext.cs` | Empty class; combat system commented out everywhere |
| `DebugService.cs` | No `Resources` folder; not in any scene |
| `PlayerCamera.cs` | `m_Enabled: 0` in scene; Cinemachine handles cameras |
| `PlayerSlideState.cs` | Registered, never entered |
| `PlayerWallRunState.cs` | Complete but registration commented out (`PlayerController.cs:39`) |
| `RangedShootAttack/SacrificeAttack/StrongAttack/ComboAttack` | Throw `NotImplementedException`; never selected |
| `ChooseAttack.cs` | Empty ScriptableObject |
| `LightAttack` / `HeavyAttack` input actions | No subscribers |
| `DealDamage` damage call | Commented out (`DealDamage.cs:14-18`) |
| Inactive `Enemy` in scene | `m_IsActive: 0`, zeroed stats |
| `FreeLook Camera BACKUP` | Inactive duplicate |
| `_Recovery\0.unity` | Old scene snapshot |

---

## 9. Suggested Reading Order

1. `Scripts\Player\PlayerController.cs` — the player hub.
2. `Scripts\FiniteStateMachine.cs` — player FSM core (very short).
3. `Scripts\Player\PlayerContext.cs` — the data bag.
4. `Scripts\InputController.cs` — input → static events.
5. `Scripts\Enemy\EnemyController.cs` — enemy hub + design comment at top.
6. `Scripts\Enemy\EnemyEntity.cs` — poise loop (short).
7. `Scripts\Enemy\enemy states\*.cs` — the 6 tiny enemy states.
8. `Scripts\Enemy\EnemyStateMachine.cs` — nested-machine trick used by `AttackState`.
9. `Scripts\Materials\shaders\EnemyDissolve.cs` — the most polished code; reference for renderer/material handling.
