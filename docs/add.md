12. Folder Architecture
12.1 Complete map (grouped by responsibility)
Assets
├── Scripts                                ← ALL game code (production)
│   ├── Player\
│   │   ├── PlayerController.cs            (movement FSM hub)
│   │   ├── PlayerContext.cs               (movement data bag)
│   │   ├── PlayerEntity.cs                (player stats)
│   │   ├── PlayerCamera.cs                (DISABLED in scene)
│   │   └── PlayerMovementStates\          (Idle/Move/Sprint/Dash/Jump/Land/Slide/WallRun)
│   ├── Enemy\
│   │   ├── EnemyController.cs             (enemy FSM hub)
│   │   ├── EnemyEntity.cs                 (health/poise)
│   │   ├── EnemyStateMachine.cs           (generic FSM + IEstate)
│   │   ├── EnemyState.cs                  (abstract state)
│   │   ├── PatrolRoute.cs                 (ScriptableObject)
│   │   ├── enemy states\                  (Spown/Patrol/Chase/Attack/Stagger/Die)
│   │   └── Attack states\                 (Melee/Exploding/Ranged/Sacrifice/Strong/Combo/DealDamage/ChooseAttack)
│   ├── Combat\CombatContext.cs            (EMPTY — dead)
│   ├── Data\Constants.cs                  (one const)
│   ├── Interfaces\IEntity.cs, IEnemyEntity.cs
│   ├── InputController.cs                 (static input events)
│   ├── GameManager.cs                     (cursor lock only)
│   ├── FiniteStateMachine.cs              (player StateMachine<T> + State<T>)
│   └── DebugService.cs                    (object-pooled spheres — DEAD)
├── Materials                              ← materials + physics materials + DebugSphere.prefab
│   └── shaders\EnemyDissolve.cs           (CODE inside a Materials folder — misfiled)
├── Settings                               ← URP pipeline assets + volume profiles (production)
├── prefabs\
│   ├── TreeEntAsh.prefab                  (the only full enemy prefab)
│   ├── TreeEntBirch/Oak/Spruce.prefab     (model-only — no gameplay)
│   └── routes\level 1\first route\waypoint.prefab (+1/2/3)
│       └── routes\level 1\routeA.asset    (PatrolRoute SO → 4 waypoints)
├── Scenes\
│   ├── TestingScene.unity                 (MAIN/production scene)
│   ├── TestingScene\NavMesh-navMesh Settings.asset  (baked navmesh)
│   ├── Player_v.3\Showcase.unity          (vendor demo scene)
│   └── ShaderGraph_Dissolve\URP\URP Samples.unity (vendor demo scene)
├── Plugins\UniTask\                       (vendored third-party async lib)
├── PackOfTreeEnts\                        (VENDOR art pack — TreeEntAsh/Birch/Oak/Spruce)
├── Player_v.3\                            (VENDOR art pack — SciFiTrooper, UNUSED by test scene)
├── Expoded enemy\                         (VENDOR art pack — BruiserMonster + "Exploded monster.controller")
├── Katana\                                (VENDOR art — katana + Blender sources)
├── ShaderGraph_Dissolve\                  (VENDOR shader package + sample scene)
├── Textures\                              (loose textures)
└── _Recovery\0.unity                      (BACKUP of an older TestingScene)
12.2 Per-folder detail
Folder	Purpose	Important files	Depends on
Scripts	All gameplay code. There is no Utilities or Audio or UI code folder [Not Found]	See §13	UniTask (DashState), UnityEngine packages
Scripts\Player	Player movement FSM	PlayerController.cs, PlayerContext.cs, PlayerEntity.cs	StateMachine, InputController, UniTask
Scripts\Enemy	Enemy AI + combat actions	EnemyController.cs, EnemyEntity.cs, EnemyStateMachine.cs	AI Navigation, Animator
Scripts\Combat	Empty stub	CombatContext.cs (empty class)	—
Scripts\Data	Constants	Constants.cs (ALPHA=0.5f)	—
Scripts\Interfaces	Entity contracts	IEntity.cs, IEnemyEntity.cs	—
Materials	Materials, physics materials, DebugSphere	PlayerMaterial/WallMaterial/EnemyNotSliding.physicMaterial, EnemyMateral.mat, Basic-Material.mat, Wireframe.mat, TestMaterial*.mat, DebugSphere.prefab	—
Materials\shaders	Dissolve effect	EnemyDissolve.cs (608-line C#), EnemyDissolveMaterial.mat	ShaderGraph_Dissolve
Settings	URP config	PC_RPAsset.asset (used), Mobile_RPAsset.asset, renderers, DefaultVolumeProfile.asset	—
prefabs	Enemy + route assets	TreeEntAsh.prefab, routeA.asset, waypoint prefabs	Scripts\Enemy
Scenes	Game scene + navmesh data	TestingScene.unity	everything
Plugins\UniTask	Async/await tasks library	Runtime/Editor/External asmdefs	—
PackOfTreeEnts	4 tree-entity character packs	TreeEntAsh\Meshes\TreeEntAsh.FBX, TreeEntAshAnimatiorController.controller, huge PNGs (≤32 MB)	—
Player_v.3	Player character pack	SK_SciFiTrooperManV3.fbx, SK_SciFiTrooperManV3 Variant.prefab, Showcase.unity	—
Expoded enemy	Bruiser/exploding monster	BruiserMonster\...\Exploded monster.controller, 7 .anim clips	—
Katana	Sword + source files	Sheathe.fbx (in scene), .blend/.blend1 sources	—
ShaderGraph_Dissolve	Dissolve shader package	SubGraphs, URP Samples.unity, EnemyDissolveMaterial.mat	—
Textures	Loose textures	texture_01.png, texture_05.png	—
_Recovery	Old scene backup	0.unity	—
12.3 Special folders
- Dead folders: Scripts\Combat (empty class), Scripts\Data is fine. Materials\shaders\EnemyDissolve.cs is live code in the wrong place.
- Backup folders: _Recovery (an older snapshot of TestingScene — same object IDs: Player, PlayerObj, KatanaWeapon, Main Camera, FreeLook Camera + BACKUP, Walls, Ground, GameManager, Canvas CurrentState; it lacks the Enemy/Exploding-Enemy work) Fact. Assets\Katana\**\*.blend1 (Blender autosave backups).
- Vendor assets: PackOfTreeEnts, Player_v.3, Expoded enemy, Katana, ShaderGraph_Dissolve, Plugins\UniTask.
- Duplicate/redundant: two enemies in the scene share the BruiserMonster model (Enemy + Exploding Enemy); two FreeLook cameras (active + "BACKUP - not fully upgradable by CM"); TreeEntAsh.prefab vs the scene's raw FBX usage; three cm particle systems in the scene (leftovers). Fact (all verified above).
- Temporary / experimental: Wireframe.mat, TestMaterial*.mat, cm particles, Textures root folder, Orientation child of PlayerObj (visible marker on layer 7).
- Not Found: any Audio, UI (folders/assets), Animations central folder, Models central folder, Utilities code folder.
13. Dependency Graph
13.1 PlayerController
PlayerController (MonoBehaviour)
│  [Fact: serialized context + playerEntity; reads input via static events]
├── PlayerContext (Serializable data bag)
│     ├── Rigidbody          (scene: Player root RB, useGravity=false, continuous)
│     ├── Transform playerCamera (scene: Main Camera)
│     ├── Transform playerModel (scene: PlayerObj)
│     ├── Animator           (scene: {fileID:0} — NULL ⚠)
│     ├── Text debugText     (scene: "PlayerCurrentState")
│     └── LayerMask groundLayer=6 / wallLayer=7
├── PlayerEntity (IEntity)
│     ├── Constants.ALPHA    (damage reduction formula)
│     └── event OnDamageTaken / OnHealed
├── StateMachine<PlayerController> (FiniteStateMachine.cs)
│     └── State<PlayerController>*   (7 player states)
│           └── each state holds ref back to owner + animator
├── InputController (static events: OnMoveInput, OnSprintInput, OnJumpStart)
├── PlayerCamera (disabled in scene; camera look actually handled by Cinemachine)
└── UniTask (via PlayerDashState)
Responsibilities: PlayerController owns the movement loop (FixedUpdate: state tick + gravity + drag + move; Update: rotate + max-velocity + queued jump), exposes properties (Velocity, CanMove, UseCustomGravity…) that states mutate, and translates camera-relative input into world forces.
13.2 EnemyController
EnemyController (MonoBehaviour)
│  [Fact: Start() grabs NavMeshAgent+Animator, builds FSM, wires entity events]
├── NavMeshAgent (scene: on root of Exploding Enemy / Enemy)
├── Animator     (scene: Exploded monster.controller / TreeEntAsh controller)
├── EnemyEntity (IEnemyEntity)          → events OnDamageTaken/OnStaggered/OnDied
├── EnemyStateMachine<EnemyState>       (EnemyStateMachine.cs)
│     └── EnemyState* (6)                → each holds ref back to EnemyController
│           └── AttackState
│                 └── EnemyStateMachine<CombatActionState>  (nested machine)
│                       └── CombatActionState* (6 attacks)
├── PatrolRoute (ScriptableObject)      → routeA.asset → 4 waypoint prefabs
├── Transform targetTransform           (scene: Player)
├── Text _debugText                     (scene: "EnemyCurrentState (1)")
└── UnityEngine.InputSystem.Keyboard    (debug H/J keys)
Responsibilities: EnemyController is the only object holding both the entity and the FSM; it translates "what happened" (entity events) into "which state" (per its own header comment at EnemyController.cs:46-50 Fact), drives detection (SeeThePlayer), and writes the debug HUD.
13.3 Other systems
GameManager  ──► (none)         cursor lock only (GameManager.cs)
Combat       ──► [Not Found]    CombatContext.cs is an empty class; no combat system
Weapon       ──► [Not Found]    KatanaWeapon is a bare PrefabInstance of Sheathe.fbx, no components
Enemy AI     ──► EnemyController FSM + NavMeshAgent + PatrolRoute (see 13.2)
UI           ──► uGUI Canvas: 2 Texts only (PlayerCurrentState, EnemyCurrentState (1)); EventSystem
Camera       ──► Cinemachine FreeLook Camera → Main Camera (CinemachineBrain)
                 PlayerCamera.cs exists but m_Enabled=0 (disabled)
Input        ──► InputController → static events → PlayerController
                 InputSystem.inputactions (Move/Jump/Camera/Sprint/LightAttack/HeavyAttack)
                 PlayerCamera also creates an InputSystem instance (unused while disabled)
Navigation   ──► NavMeshSurface + NavMeshData asset (Assets\Scenes\TestingScene\NavMesh-navMesh Settings.asset)
Animation    ──► Animators + 2 controllers (TreeEntAsh, Exploded monster) + Animation Rigging (Rig/RigBuilder/BoneRenderer, all with EMPTY effectors)
13.4 Cross-cutting concerns
- Circular dependencies: the two FSM patterns are intentionally cyclic — PlayerController → StateMachine → State → PlayerController, and EnemyController → EnemyStateMachine → EnemyState → EnemyController Fact, benign state-pattern cycle.
- Tight coupling: every player/enemy state holds a direct reference to its owner; EnemyController wires all entity→state translation; InputController is a global static-event hub Fact.
- Singletons: DebugService.Instance (never instantiated — not in any scene) Fact.
- Static classes / static state: InputController static events; Constants static const; PlayerContext/PlayerEntity are instance data Fact.
- Event-based communication: EnemyEntity.OnDamageTaken/OnStaggered/OnDied, InputController.OnMoveInput/OnSprintInput/OnJumpStart, IEntity.OnDamageTaken/OnHealed Fact.
- ScriptableObjects: PatrolRoute (used by both scene enemies), ChooseAttack (empty, unused) Fact.
- Third-party libraries: UniTask (vendored at Assets\Plugins\UniTask), Cinemachine 3, Animation Rigging, AI Navigation, uGUI, Visual Scripting (installed, 0 graphs used), Timeline Fact.
13.5 High-level system graph
                     ┌──────────────────────────────┐
                     │      InputSystem.inputactions│
                     └──────────────┬───────────────┘
                                    │ (generated InputSystem class)
                        ┌───────────▼───────────┐
                        │    InputController     │  static events
                        └───────────┬───────────┘
                                    │
                        ┌───────────▼───────────┐        ┌─────────────┐
                        │    PlayerController    │◄──────│ PlayerContext│
                        └───┬───────┬───────┬────┘        └─────────────┘
                    ┌───────┘       │       └────────────┐
                    ▼               ▼                    ▼
            Player StateMachine  PlayerEntity       Cinemachine FreeLook
                  │                                    (Main Camera)
                  └──(unused wallrun/dash)──┐
                                            │
                        ┌───────────────────▼───────────────────┐
                        │              EnemyController           │
                        └───┬───────┬───────┬───────┬───────┬────┘
                            │       │       │       │       │
              ┌─────────────┘       │       │       │       └─────────┐
              ▼                     ▼       ▼       ▼                 ▼
        EnemyEntity           NavMeshAgent  Animator  PatrolRoute   AttackState
         (poise)                             (Exploded/       (nested combat machine)
                                              TreeEnt)              │
                                            ▲                       ▼
                                            │            MeleeAttack (only one ever entered)
                                     EnemyDissolve (spawn fx)
14. New Developer Onboarding Guide
14.1 Project Goal
Current goal (as built): a single-scene 3D third-person prototype proving out player movement (state machine) and enemy AI (patrol → chase → attack → stagger → die with poise), on a NavMesh arena, using URP + Cinemachine. This is prototype code, not a shippable game. The header comment in EnemyController.cs:7-43 is the de-facto design note (grunt/sacrifice/ranged/defend archetypes + a cost-based spawn system) — none of that is implemented.
14.2 Current Development State
Finished / working Fact, code-verified:
- Player movement: idle, move, sprint, jump (+queue), land; camera-relative movement; custom gravity/drag; max velocity clamp.
- Enemy AI skeleton: spawn → patrol (waypoints) → chase (destination only) → attack (melee anim) → stagger (via debug keys) → die (crashes, see pitfalls).
- Poise/stagger entity math, entity events.
- Patrol routes via ScriptableObject (routeA + 4 waypoints).
- Dissolve spawn effect (EnemyDissolve).
- NavMesh baked for the arena.
- Cinemachine FreeLook camera working.
Partially implemented:
- Dash (works, but triggered on every sprint press instead of a real dash action).
- Attack variety — only MeleeAttack runs; 5 other attack classes are NotImplementedException.
- Player animation — states exist but animation calls are commented out; player Animator has a missing controller.
- Wall-run — complete code, disabled (PlayerController.cs:39).
- Enemy chase — animation + destination set, but no real per-frame pursuit in ChaseState.
Completely missing Not Found:
- Damage application (DealDamage commented out), player health effects, win/lose, death of player.
- Player combat (LightAttack/HeavyAttack input actions exist but nothing subscribes).
- UI menus/HUD beyond debug text; audio; save; upgrades; loot; spawn system; levels/rooms.
- Any GDD/design doc.
14.3 Where To Start
Read in this order (small files first):
1. Scripts\Player\PlayerController.cs — the hub; everything the player does flows through it.
2. Scripts\FiniteStateMachine.cs — the player FSM core (very short).
3. Scripts\Player\PlayerContext.cs — the data bag that makes serialization work.
4. Scripts\InputController.cs — input → static events.
5. Scripts\Enemy\EnemyController.cs — the enemy hub + the design comment at the top.
6. Scripts\Enemy\EnemyEntity.cs — poise loop (short).
7. Scripts\Enemy\enemy states\*.cs — the 6 enemy states (each is tiny).
8. Scripts\Enemy\EnemyStateMachine.cs — nested-machine trick used by AttackState.
9. Scripts\Materials\shaders\EnemyDissolve.cs — the most polished code in the project; a good reference for renderer/material handling.
Why this order: every later file either consumes or is consumed by the hubs (PlayerController/EnemyController). The state machines and data bags are the architectural backbone; the states are just small leaves that mutate the context.
14.4 Reading Order (learning path)
 1. Input → InputSystem.inputactions + InputController.cs + generated Assets\InputSystem.cs (what the player can press; note LightAttack/HeavyAttack exist but are wired to nothing).
 2. Player data → PlayerContext.cs, PlayerEntity.cs, Interfaces\IEntity.cs.
 3. Player FSM → FiniteStateMachine.cs, then PlayerMovementStates\PlayerIdleState.cs → PlayerMoveState.cs → PlayerJumpState.cs → PlayerSprintState.cs → PlayerDashState.cs → PlayerSlideState.cs → PlayerWallRunState.cs.
 4. Camera → PlayerCamera.cs (disabled) then note Cinemachine FreeLook in the scene.
 5. Enemy data → EnemyEntity.cs, Interfaces\IEnemyEntity.cs, PatrolRoute.cs.
 6. Enemy FSM → EnemyStateMachine.cs, EnemyState.cs, then enemy states\SpownState → PatrolState → ChaseState → AttackState → StaggerState → DieState.
 7. Combat actions → Attack states\AttackState.cs (nested machine), MeleeAttack.cs, ExplodingAttack.cs, DealDamage.cs, then the 4 NotImplementedException stubs.
 8. Effect → EnemyDissolve.cs.
 9. Scenes → open TestingScene and inspect Player / Enemy / Exploding Enemy / navMesh Settings / FreeLook Camera.
10. Project config → Packages\manifest.json, ProjectSettings\TagManager.asset (layers 6=Ground, 7=Wall), ProjectSettings\EditorBuildSettings.asset.
Reason: breadth-first from the two hubs outward; leaves (states) are trivial once the hub is understood; the dissolve effect is deliberately last because it is self-contained and independent.
14.5 Important Scenes
Scene	Type	Use
Scenes\TestingScene.unity	Production/prototype	The only scene with gameplay. Everything you test happens here.
Scenes\TestingScene\NavMesh-navMesh Settings.asset	Data	Baked navmesh for the arena (goes with the scene folder).
Player_v.3\Showcase.unity	Vendor example	Shows the SciFiTrooper model; not gameplay.
ShaderGraph_Dissolve\URP\URP Samples.unity	Vendor example	Shows the dissolve shader package.
_Recovery\0.unity	Backup	Older snapshot of TestingScene; ignore (do not delete without confirmation).
Assets\Scenes\SampleScene.unity	[Not Found]	Referenced by build settings but does not exist.
14.6 Important Prefabs
- prefabs\TreeEntAsh.prefab — the only fully-wrapped enemy prefab (EnemyController + NavMeshAgent + Animator(TreeEnt controller) + Rigidbody + Rig + EnemyDissolve + DealDamage + 2 missing-script components). Note: it is not placed in the scene — the scene uses the raw FBX instead.
- prefabs\TreeEntBirch/Oak/Spruce.prefab — model-only (no controllers).
- prefabs\routes\level 1\first route\waypoint.prefab (+ 1/2/3) → referenced by prefabs\routes\level 1\routeA.asset (the PatrolRoute used by both scene enemies).
- Player_v.3\Prefabs\SK_SciFiTrooperManV3 Variant.prefab — the player model pack, not used in TestingScene (the scene player is a placeholder cube).
How they connect: routeA.asset → waypoint prefabs; EnemyController (scene/prefab) → routeA.asset → waypoints. The TreeEntAsh prefab is the intended "one-stop" enemy but is bypassed in the scene.
14.7 Important Systems
Summarized in §13 (Player FSM, Enemy FSM + nested attack machine, Poise, Input events, Patrol routes, Dissolve effect, NavMesh, Cinemachine, Debug HUD).
14.8 Things A New Developer Should NOT Touch
- Vendor assets: PackOfTreeEnts, Player_v.3, Expoded enemy, Katana, ShaderGraph_Dissolve, Plugins\UniTask.
- Unfinished/experimental: PlayerWallRunState (disabled on purpose), PlayerSlideState (empty), Scripts\Combat\CombatContext.cs, DebugService.cs (would crash — Resources.Load("DebugSphere") with no Resources folder), Attack states\* stubs (will NotImplementedException if ever selected).
- Broken: DieState.cs (crashes on entry); the inactive Enemy object in the scene; the FreeLook Camera BACKUP.
- Backup: _Recovery\0.unity.
14.9 Common Pitfalls
 1. Missing player animator controller — guid da65a6d29ce66da459988c23539ddd08 referenced by PlayerObj Animator, exists nowhere in the project. Player animation silently does nothing.
 2. PlayerController.context.animator is null in the scene (TestingScene.unity:8413 animator: {fileID: 0}).
 3. DieState NullReferenceException — agent/animator never assigned (DieState.cs:11-18).
 4. Animation name mismatches — enemy code plays "Idle"/"GetHit"/"GetStun"/"Death", but the TreeEnt controller has Idle1/GetHit1-3/Stun and no Death; only the Exploded monster.controller matches the code names.
 5. Mismatched controller on a model — the inactive Enemy object is a BruiserMonster rig using the TreeEnt controller.
 6. Build settings point at a missing scene — EditorBuildSettings lists Assets/Scenes/SampleScene.unity (doesn't exist); TestingScene isn't listed.
 7. Missing script references — 2 guids (b2d8418b…, fff0960e…) on both TreeEntAsh.prefab and the Exploding Enemy resolve to no script in Assets. Assumption: deleted scripts or package-sourced components.
 8. Missing volume profile — the scene Global Volume references guid a6560a91… which matches no asset.
 9. Commented-out logic — DealDamage.cs:14-18 (no player damage), player animation calls, wall-run registration, the wall-run trigger block in PlayerController.Update.
10. Scene values differ from script defaults — e.g. gravity -20, sprintSpeed 12, walkSpeed 8, jumpForce 12 in the scene vs -25/145/100/5 in code.
11. Static event leak — PlayerController.Start subscribes lambdas to InputController static events with no OnDestroy un-subscribe; reloading the scene duplicates handlers (double jump on reload).
12. No damage anywhere — you cannot currently lose health in the game.
15. Execution Flow
15.1 Bootstrap (Play in Editor)
Intended flow (from code):
Unity Play (TestingScene open)
   │
   ├── Awake (all objects) — order NOT guaranteed by Unity
   │     ├── GameManager.Awake        → Cursor.visible=false; lock (GameManager.cs:7-11)
   │     ├── InputController.Awake    → new InputSystem(); wire Move/Jump/Sprint (InputController.cs:21-29)
   │     ├── PlayerController.Awake   → build StateMachine; add 7 states; SetState<PlayerIdleState> (PlayerController.cs:29-48)
   │     └── EnemyDissolve.Awake      → clone renderer materials, compute range, hide (EnemyDissolve.cs:56-124)
   ├── OnEnable: InputController → enable PlayerMovement map
   ├── Start (all objects)
   │     ├── PlayerController.Start   → subscribe InputController events (PlayerController.cs:50-71)
   │     └── EnemyController.Start    → GetComponent agent/animator; build FSM; wire entity events;
   │                                   SetState<SpownState>; stoppingDistance (EnemyController.cs:94-120)
   │     └── EnemyDissolve.Start      → PlaySpawnEffect (dissolve in)
   └── Loop:
         ├── EnemyController.Update   → FSM.Tick → SeeThePlayer → debug keys/text (EnemyController.cs:146-179)
         ├── PlayerController.FixedUpdate → FSM.Update → gravity → drag → Move (PlayerController.cs:123-129)
         ├── PlayerController.Update  → Rotate → MaxVelocity → queued jump (PlayerController.cs:131-151)
         └── StateMachine.Update      → current player state Update + debug text (FiniteStateMachine.cs:28-35)
Current actual flow — two differences:
A) Build: EditorBuildSettings lists only a missing scene
   → a Player build would contain NO valid scene; Play-in-editor runs whatever scene is open (TestingScene) [Fact]

B) The player's Animator never animates:
   Animator.controller = missing guid (da65a6d29…) AND PlayerController.context.animator = null
   → player states run, movement works, but no player animation [Fact]
15.2 Player flow
Intended:  Spawn → Input → Movement → Animation → Combat → Death
                                      │            │        │
Current:   Spawn → Input → Movement ──┴─(none)─────┴──(none)─┘
   - Animation: state calls commented out; controller missing → no effect
   - Combat: LightAttack/HeavyAttack actions exist but unsubscribed → never fires
   - Death: PlayerEntity.TakeDamage exists but nothing calls it; no player health UI
[Fact]
State transitions (verified in code):
- Idle → Move when MoveDirection.magnitude ≥ 0.1 (PlayerIdleState.cs:19-22,27-30)
- Move → Idle when input < 0.1; Move → Sprint if sprinting (PlayerMoveState.cs)
- Sprint press (InputController.OnSprintInput=true) → PlayerDashState directly (PlayerController.cs:66-69); dash ends → Sprint/Move/Idle (PlayerDashState.cs:36-47)
- Jump press: if grounded → SetState<PlayerJumpState> + impulse (PlayerController.cs:53-64,217-231); if airborne → queueJump, executed on landing in Update (PlayerController.cs:135-139)
- JumpState runs a coroutine every Update (PlayerJumpState.cs:21) → lands → PlayerLandState → immediately PlayerIdleState (PlayerLandState.cs:30)
- Slide state: registered but never entered; WallRun: registered state exists but registration is commented out
15.3 Enemy flow
Intended (per code):
 SpawnState ──3s──► PatrolState ──(player seen)──► ChaseState ──(in range)──► AttackState(Melee)
      ▲                 │                              │                            │
      │                 │                              │                            │
      │            (lost target)                (reached/lost)               (on hurt:)
      │                 │                              │                            │
      └─────────────────┴──────────────────────────────┴──◄── StaggerState ──(duration over)
                                                              DieState (never reached — crashes)

Current actual:
 SpawnState ──3s──► PatrolState ──► ChaseState ──► AttackState(Melee)
   · Patrol advances waypoints (Debug.Log spam per waypoint — PatrolState.cs:36-37)
   · SeeThePlayer runs in EnemyController.Update (NOT in ChaseState — ChaseState.Tick is empty)
   · AttackState.Tick only faces the player; MeleeAttack just plays Attack1; no damage (DealDamage commented)
   · H/J keys → enemyEntity.TakeDamage(10,5)/(10,999) → Stagger (plays GetHit/GetStun if controller has them)
   · DieState.Enter → NullReferenceException (agent/animator unassigned) → death path BROKEN
[Fact]
Important nuances:
- The only active enemy is Exploding Enemy; Enemy is m_IsActive:0 and its entity is zeroed → it never runs.
- Exploding Enemy uses Exploded monster.controller whose states match the code names (Idle/Walk/Run/GetHit/GetStun/Death/Attack1) Fact, so patrol/chase/stagger anims would play on it — but the DieState crash means Death never completes.
- Both EnemyControllers write to the same debug Text (EnemyCurrentState (1)).
15.4 Event subscriptions & state transitions summary
Event	Subscriber	Where	Notes
InputController.OnMoveInput	PlayerController.Start (lambda)	PlayerController.cs:65	never un-subscribed (leak across reloads)
InputController.OnSprintInput	PlayerController.Start (lambda)	PlayerController.cs:66-69	forces Dash on every sprint press
InputController.OnJumpStart	PlayerController.Start (lambda)	PlayerController.cs:53-64	never un-subscribed
InputController.OnJumpStart	PlayerWallRunState.Enter/Exit	PlayerWallRunState.cs:92,142	state disabled
EnemyEntity.OnDamageTaken	EnemyController.Start	EnemyController.cs:104	→ StaggerState(Hit) if interruptible
EnemyEntity.OnStaggered	EnemyController.Start	EnemyController.cs:103	→ StaggerState(Stun)
EnemyEntity.OnDied	EnemyController.Start	EnemyController.cs:102	→ DieState (crashes)
Input actions Move/Jump/Sprint	InputController.Awake	InputController.cs:24-28	static-event bridge
Input actions LightAttack/HeavyAttack	nobody	—	dead path
15.5 Issues found
- Bottlenecks: Debug.Log per patrol waypoint (PatrolState.cs:36-37); coroutine spam in PlayerJumpState.Update; per-frame GetType().ToString() + StringToHash-style debug text building (EnemyController.cs:161-178, FiniteStateMachine.cs:33).
- Initialization issues: DieState null fields; PlayerController.context.animator null; zeroed Enemy entity; missing player controller; missing volume profile; InputController.Awake runs before scene wiring of static events is safe (it isn't — events fire after OnEnable, so this is benign Assumption).
- Race conditions: static InputController events accumulate on scene reload (duplicate subscriptions → double-trigger of jump/dash). Fact: no un-subscribe anywhere
- Missing references: player animator controller, SampleScene in build settings, input-actions settings object in build config (052faaac…), 2 script guids, volume profile, PlayerController.animator field.
- Dead execution paths: WallRun (disabled), Slide (never entered), Land (instant), PlayerCamera (disabled), DebugService (not in scene), LightAttack/HeavyAttack actions, Ranged/Sacrifice/Strong/Combo/Exploding attacks (never selected), ChooseAttack SO, CombatContext, inactive Enemy, FreeLook Camera BACKUP, cm particle systems, _Recovery scene.
- Systems that never execute: player combat, enemy death (crash), player damage, any real attack selection.
- Systems that execute but have no visible effect: player Animator (missing controller), enemy stagger on TreeEnt controller (missing GetHit/GetStun state names), H/J debug keys on an enemy whose DieState/StaggerState anim won't play.
End of sections 12–15. If you want, I can also fold these four sections into the existing PROJECT_ANALYSIS.md (requires a file write — say the word and I'll append).
▣  Build · Big Pickle · 6m 0s
Objective
- Analyze the Unity project E:\course\03_Programming_Data\game_dev\projects\3D-roguelike-hack_n_slash to produce developer documentation: (a) an 11-section gap analysis vs a GDD (delivered earlier), and (b) documentation sections 12–15 (Folder Architecture, Dependency Graph, New Developer Onboarding, Execution Flow). Work is evidence-only; no files modified, no code generated.
Important Details
- No GDD exists anywhere — glob for **/*.md found only a 2-line README.md; GDD Coverage section states "Not Found". Fact
- Project facts verified:
- Unity 6000.3.20f1; URP com.unity.render-pipelines.universal 17.3.0; new Input System (active handler=1), generated Assets\InputSystem.cs.
- Packages: com.unity.ai.navigation 2.0.13, com.unity.animation.rigging 1.4.1, com.unity.cinemachine 3.1.7, com.unity.inputsystem 1.19.0, com.unity.ugui 2.0.0, com.unity.visualscripting 1.9.11 (0 graphs used), com.unity.timeline 1.8.12 (Packages\manifest.json).
- Layers: Ground=6, Wall=7; tag Enemy (ProjectSettings\TagManager.asset).
- 4 scenes: Assets\Scenes\TestingScene.unity (production/prototype; only scene with gameplay), Assets\Player_v.3\Showcase.unity (vendor), Assets\ShaderGraph_Dissolve\URP\URP Samples.unity (vendor), Assets\_Recovery\0.unity (BACKUP — older snapshot of TestingScene containing the same object IDs: Player/PlayerObj/KatanaWeapon/Main Camera/FreeLook Camera+BACKUP/Walls/Ground/GameManager/Canvas CurrentState; lacks Enemy/Exploding-Enemy work).
- Build settings broken: ProjectSettings\EditorBuildSettings.asset references non-existent Assets/Scenes/SampleScene.unity (guid 99c9720ab356a0642a771bea13969a05); TestingScene not in build; input-actions settings object 052faaac586de48259a63d0c4782560b also missing.
- TestingScene objects (all file/line-verified): GameManager (line 216, only cursor lock); Main Camera (component list at line 1790-1795; PlayerCamera script guid 8f865b080b974fd43837bfb5a66070d8 is m_Enabled: 0 at line 1884 — disabled; CinemachineBrain 72ece51f2901e7445ab60da3685d6b5f); FreeLook Camera (active, line 1985) + "FreeLook Camera BACKUP - not fully upgradable by CM" (inactive, line 2577); Canvas with debug Texts PlayerCurrentState (Text 1508081998 on GO 1508081996) and EnemyCurrentState (1) (Text 92381217 on GO 92381216) — both enemies write to the same Enemy text; navMesh Settings (NavMeshSurface line ~461-491, baked NavMeshData guid 3e39b5825e940244c98c6edd17da2370 = Assets\Scenes\TestingScene\NavMesh-navMesh Settings.asset); Walls (layer 7, WallMaterial.physicMaterial guid b84a46f96da214742bb0e523bf5c85ac); 3 cm particle-system leftovers (lines 1185, 7566, 8845); Animation Rigging components on Player (Rig 1 GO 1915120067 with Rig comp 1915120068, TopRig/BottomRig/MiddleRig) and on both enemies (RigBuilder/BoneRenderer/Rig, all m_Effectors: []).
- Player wiring: PlayerController.cs ML at lines 8398-8440: animator: {fileID: 0} (line 8413 — NULL), playerCamera=Main Camera (963194228), playerModel=PlayerObj (1899011099), debugText=1508081998, groundLayer=6/wallLayer=7, gravity -20, sprintSpeed 12, walkSpeed 8, dashSpeed 50, dashDuration 0.2, jumpForce 12, airMoveSpeedMultiplier 0.9, MaxVelocity 12, customDrag 1 (differs from script defaults -25/145/100/5/0.05). PlayerObj = capsule collider (PlayerMaterial guid 19d0345770297c342ae4deda4fa09f4f) + cube mesh (10208) + MeshRenderer + Animator m_Controller: {fileID: 9100000, guid: da65a6d29ce66da459988c23539ddd08} (line 8568) — controller guid exists nowhere → no player animation Fact; context.animator also null. Player root: Rigidbody (interpolate, useGravity=0, constraints=80) at line 8363-8389, local pos 2.95,1.548,4.72.
- Enemy wiring: the only ACTIVE enemy is Exploding Enemy (root GO 2282024516536603690, line 10440, 8 components: Rigidbody, EnemyController, RigBuilder, missing-script comps 4755974160996898028=guid fff0960ef4ea6e04eac66b4a7fd2189d and 4755974160996898027=guid b2d8418b0b9634b1892b0268dd9c2743 — neither guid exists in any .meta Fact → Missing Script components, EnemyDissolve, plus 2 more comps; NavMeshAgent on root; Animator controller guid 4d6bc7216dac3744394674b5f0a6f7a9 = Assets\Expoded enemy\kalaban-bruiser-monster-walking\source\BruiserMonster\Exploded monster.controller — its state names Idle/Walk/Run/GetHit/GetStun/Death/Attack1 match the names code uses); Entity stats at lines 12342-12348: health 100/100, damage 20, defense 20, poise 100/100, target=Player, routeA, detection 12, lose 16, range 3, patrol 3.6, chase 9, waypointStop 1, viewHalf 290.
- Enemy Enemy (root GO 5289385902289650030, inactive line 12933, tag Enemy) — Bruiser rig with TreeEntAnimator controller cefaf9ad7daa5724aa50d81370b08dae (mismatch) and zeroed entity (lines 14698-14703: health 0/0 etc.) — dead on arrival.
- Key code facts:
- Assets\Scripts\Enemy\Attack states\DealDamage.cs:14-18 — damage completely commented out (if () + TakeDamage), player can never be hurt.
- Assets\Scripts\Enemy\enemy states\DieState.cs:11-18 — agent/animator never assigned in constructor → NullReferenceException on death (player cannot kill enemies either).
- Assets\Scripts\Enemy\enemy states\ChaseState.cs:26-28 — Tick() empty (no pursuit logic; chasing happens via EnemyController.SeeThePlayer which calls SetState<ChaseState>() per-frame while player in range).
- Assets\Scripts\Enemy\enemy states\AttackState.cs:39 — hardcoded combatActions.SetState<MeleeAttack>(); 6 actions registered (:28-33); Tick only rotates toward player.
- RangedShootAttack/SacrificeAttack/StrongAttack/ComboAttack all throw NotImplementedException; ChooseAttack.cs is an empty ScriptableObject; ExplodingAttack plays Attack1.
- Assets\Scripts\Enemy\enemy states\StaggerState.cs — plays "GetHit"/"GetStun" (lines 35, 40) which do not exist on TreeEnt controller (GetHit1-3, Stun); works only on Exploded monster controller.
- Assets\Scripts\Enemy\EnemyStateMachine.cs — nested-machine capable generic FSM over IEstate, SetState skips same-state.
- Assets\Scripts\Enemy\PatrolRoute.cs — ScriptableObject with List<Transform> wayPoints; routeA.asset references 4 waypoint prefabs (guids cb3599299ebed074db275a9e1ec51115, 149596c79b14c584dbb35011a2d93fa5, 3568a9447f8f4094886db828c9887769, c0fa5d5cbb1850b4aa2916e11aa266a5) at Assets\prefabs\routes\level 1\first route\waypoint*.prefab.
- PatrolState.cs:36-37 — Debug.Log per waypoint (spam).
- PlayerJumpState.cs:21 — StartCoroutine every Update frame (spam).
- PlayerLandState.cs:30 — immediately SetState<PlayerIdleState>().
- PlayerDashState.cs — async void Enter() + UniTask delay; dash reached directly from sprint press (PlayerController.cs:66-69).
- PlayerWallRunState.cs — complete (WallRunDirection enum, jump-off-wall, InputController subscriptions in enter/exit at lines 92/142) but registration commented out at PlayerController.cs:39; PlayerSlideState.cs — empty, registered but never entered.
- PlayerController.Start subscribes lambdas to static InputController events with no OnDestroy unsubscribe → duplicate handlers on scene reload (double jump/dash) Fact: no un-subscribe anywhere.
- DebugService.cs:32 — Resources.Load<GameObject>("DebugSphere"); no Assets\Resources folder exists; DebugSphere.prefab lives in Assets\Materials\; not in scene. Dead code.
- GameManager.cs — cursor lock only; CombatContext.cs empty class; Constants.cs — ALPHA=0.5f.
- PlayerEntity.cs:34-37 — CalculateDamageReduction = damage - baseDefense * ALPHA; EnemyEntity.cs — poise loop (damage → dead? → stagger? → damage-taken) at :38-62, events OnDamageTaken/OnStaggered/OnDied; EnemyController wires them → Stagger/Die states.
- SpownState.cs — plays "Idle" (exists on Exploded, not on TreeEnt which has Idle1), waits 3s → PatrolState; PatrolState plays "Walk", ChaseState plays "Run".
- Assets\Scripts\Player\PlayerCamera.cs — disabled in scene; Assets\Scripts\FiniteStateMachine.cs — debug text via GetType().ToString() every frame + SetState exits current first.
- Assets\Materials\shaders\EnemyDissolve.cs — 608-line dissolve/edge-particles, static Shader.PropertyToID, caches renderers/materials.
- InputSystem.inputactions — PlayerMovement map: Move/WASD+arrows+gamepad, Jump/space/gamepadSouth, Camera/mouse+rightStick, Sprint/leftShift+leftShoulder; also LightAttack (left mouse) and HeavyAttack (right mouse) — subscribed by nobody (dead path).
- Prefabs: Assets\prefabs\TreeEntAsh.prefab — full enemy wiring (EnemyController guid 0875c81e6cfa4664abb3d790241cbca4, NavMeshAgent on root GO 5289385901807528947, Animator TreeEnt, Rigidbody, Rig, EnemyDissolve 06ea807e7bbfae642b6120c12b30acaa, 2× DealDamage and 2 more script-guids incl. the same two missing guids fff0960e…/b2d8418b…, source FBX guid cbbfbfed98a59d041a4645301e1ebdb9); TreeEntBirch/Oak/Spruce.prefab — model-only (0 EnemyController/NavMeshAgent/GameObjects serialized). Scene uses raw FBX instead of the full prefab.
- Vendor/duplicate/leftover classification: vendor = PackOfTreeEnts, Player_v.3 (SciFiTrooper — its FBX/prefab/scene are not used by TestingScene), Expoded enemy, Katana (Sheathe.fbx used; .blend1 autosaves), ShaderGraph_Dissolve, Plugins\UniTask; dead = Scripts\Combat, DebugService, PlayerCamera (disabled), PlayerSlideState, PlayerWallRunState (disabled), 4 attack stubs, LightAttack/HeavyAttack, inactive Enemy, FreeLook Camera BACKUP, cm particles, _Recovery, Textures\; test/experimental materials = Wireframe.mat, TestMaterial.mat, TestMaterial 1.mat, Basic-Material.mat; missing volume profile guid a6560a915ef98420e9faacc1c7438823 referenced by Global Volume (line 2354) but matches no asset; Settings\ = PC_RPAsset 4b83569d67af61e458304325a23e5dfd (used), Mobile_RPAsset, DefaultVolumeProfile ab09877e2e707104187f6f83e2f62510, nothing matches the scene's volume guid.
- Execution flow conclusions: Player chain Spawn→Input→Movement works; Animation/Combat/Death are dead paths (null/missing animator, commented calls, missing controller). Enemy chain Spawn→Patrol→Chase→Attack→Stagger works logically for Exploding Enemy (Exploded controller matches all state names except attack-damage), but DieState crashes; the intended die path never completes. Two diagrams (intended/current) produced in section 15.
- Classification labels used throughout the answer: every claim labeled [Fact], [Assumption], or [Not Found].
Work State
Completed
- Original 11-section report written in full as PROJECT_ANALYSIS.md (at project root) covering: Project Overview, Scene Analysis, Systems Found, GDD Coverage=Not Found, Script Analysis, Architecture, Assets, Git Readiness (no .git), Risks (15 items), Learning Guide, 10 Questions for the team.
- Sections 12–15 (Folder Architecture, Dependency Graph, New Developer Onboarding, Execution Flow) fully authored as a Markdown response — NOT written to a file (per user instruction "Do NOT modify any files"); offered to append them to PROJECT_ANALYSIS.md if requested.
- Verified all folder/component wiring for this answer: root-GO list of TestingScene and _Recovery\0.unity; Animator controllers (3) + GUIDs; NavMeshAgent owners; Rigidbody owners; component-block GUIDs for both enemies; input-actions structure (incl. LightAttack/HeavyAttack bindings); text-object mappings; Global Volume missing profile; physics materials; waypoint GUID chain; TreeEntAsh prefab script GUID set.
Active
- None — the documentation request is complete.
Blocked
- (none) — no blockers; GDD simply does not exist (documented as Not Found).
Next Move
1. (none) — documentation delivered; user must prompt if they want sections 12–15 appended to PROJECT_ANALYSIS.md (a file write).
2. If continuing analysis: verify whether the two missing script guids (b2d8418b0b9634b1892b0268dd9c2743, fff0960ef4ea6e04eac66b4a7fd2189d) resolve inside Library\ScriptAssemblies (Library folder did not exist at scan time), and optionally reconcile PlayerController serialized-vs-script default values.
Relevant Files
- PROJECT_ANALYSIS.md (project root): 11-section report written this session — the deliverable from the earlier request.
- Assets\Scenes\TestingScene.unity: primary scene; all line references above (Player wiring ~8398-8440, enemy wiring ~12329-12348 and 14684-14703, Exploding Enemy root 10423-10445, GameManager 216, Global Volume missing profile 2354).
- Assets\Scenes\TestingScene\NavMesh-navMesh Settings.asset + .meta: baked navmesh (guid 3e39b5825e940244c98c6edd17da2370).
- Assets\_Recovery\0.unity: backup scene (ignore; contains same old object IDs as TestingScene without the enemies).
- Assets\prefabs\TreeEntAsh.prefab: the only complete enemy prefab (bypassed in the scene; has the 2 missing script guids too).
- Assets\prefabs\routes\level 1\routeA.asset + Assets\prefabs\routes\level 1\first route\waypoint*.prefab: PatrolRoute SO chain used by both scene enemies.
- Assets\Expoded enemy\kalaban-bruiser-monster-walking\source\BruiserMonster\Exploded monster.controller (guid 4d6bc7216dac3744394674b5f0a6f7a9): the only controller whose state names match enemy code.
- Assets\PackOfTreeEnts\TreeEntAsh\TreeEntAshAnimatiorController.controller (guid cefaf9ad7daa5724aa50d81370b08dae): states Idle1/GetHit1-3/Stun/Attack1-7/Run/Walk — mismatched against code names.
- Assets\Scripts\Enemy\enemy states\DieState.cs: null-ref on death (unassigned agent/animator).
- Assets\Scripts\Enemy\Attack states\DealDamage.cs: commented-out damage.
- Assets\Scripts\Enemy\Attack states\AttackState.cs: hardcoded MeleeAttack.
- Assets\Scripts\Player\PlayerController.cs: hub; sprint→dash; static-event subscriptions never unsubscribed; wallrun registration commented at line 39.
- Assets\InputSystem.inputactions: input maps incl. unused LightAttack/HeavyAttack.
- Assets\Materials\DebugSphere.prefab: debug sphere in wrong folder (Assets\Resources missing → DebugService would crash).
- ProjectSettings\EditorBuildSettings.asset: references missing SampleScene.unity + missing input settings guid 052faaac586de48259a63d0c4782560b.