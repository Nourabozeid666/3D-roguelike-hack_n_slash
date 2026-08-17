# Enemy ↔ SpawnSystem Integration (Sprint 4 follow-up)

> **Status:** IMPLEMENTED — branch `fix/enemy-spawn-integration` (off `origin/main` `236d306`).
> Current, authoritative contract for how the Enemy system and the Roguelike SpawnSystem talk to each
> other. Supersedes the `[WAITING FOR ENEMY SYSTEM]` rows in `docs/ROGUELIKE_SPAWNING_SPRINT_4.md`
> and the "NOT implemented / BLOCKED" verdict in `docs/ROGUELIKE_RUN_SYSTEM_SPRINT_4.md`.
> Verification: external dotnet harness (stubs), `415/415` checks passing (`tools/spawn-integration-test`).

---

## 1. What this integration does

Connects the Roguelike spawner to enemy death and per-floor stat scaling so that:

- a spawned enemy tells SpawnSystem when it dies → `AliveCount()` decrements, and the last death raises
  `FloorCleared` (idempotently);
- SpawnSystem applies floor-scaled stats to each spawned enemy before it initializes.

## 2. The two seams

| Seam | File | Responsibility |
|---|---|---|
| **`IEnemySpawned`** | `Assets/Scripts/Roguelike/Spawning/IEnemySpawned.cs` | **Death-only** contract: `event Action OnDied`. |
| **`ISpawnStatConfig`** | `Assets/Scripts/Roguelike/Spawning/ISpawnStatConfig.cs` | **Spawn-time scaling** seam: `BaseMaxHealth` / `BaseDamage` / `ConfigureForSpawn(maxHealth, baseDamage)`. |

`IEnemySpawned` was narrowed from the original `{ event Action Died; void ApplyFloorScaling(...); }`
to a death-only contract. Floor scaling is deliberately NOT on the death contract: SpawnSystem owns
floors/growth/budget and pushes **absolute, already-scaled** values through `ISpawnStatConfig`. The
Enemy side never learns about floor progression, multipliers, or growth rates.

## 3. Implementations

| Type | Implements | Detail |
|---|---|---|
| `EnemyController` | `IEnemySpawned` + `ISpawnStatConfig` | Forwards `EnemyEntity.OnDied → IEnemySpawned.OnDied`; health via `SetMaxHealth`; per-instance `RuntimeDamage` written by `ConfigureForSpawn`, read by `DealDamage` and `SacrificeAttack`. |
| `TestEnemy` (test double) | `IEnemySpawned` + `ISpawnStatConfig` | Stores scaled absolute values as working `Health`/`Damage`; `Die()` fires `OnDied` then destroys itself. |
| `DoubleNotifyEnemy` (harness-only) | `IEnemySpawned` | Fires `OnDied` twice to prove SpawnSystem death handling is idempotent. |

## 4. SpawnSystem integration points

`SpawnSystem.cs`:

1. `InstantiateEnemy` — after instantiation, subscribes `spawned.OnDied += () => OnEnemyDied(enemy)`
   for any `IEnemySpawned` component; enemies without it are not tracked.
2. `ApplyFloorScaling(GameObject, EnemyArchetype, int floor)` — new private static method: reads
   `ISpawnStatConfig` base stats, computes `healthScale = (1 + HealthGrowthPerFloor)^(floor-1)` and
   `damageScale = (1 + DamageGrowthPerFloor)^(floor-1)`, then calls `ConfigureForSpawn(scaled, scaled)`.
3. `OnEnemyDied` — **idempotent**: `alive.Remove(enemy)` returning `false` (already removed) stops the
   handler, so a duplicate `OnDied` can never double-decrement `AliveCount` or double-raise
   `FloorCleared`.

## 5. Death path (authoritative, single source)

`EnemyEntity` is the authoritative death source:

- `EnemyEntity.TakeDamage` now has a dead-guard (`currentHealth <= 0` returns early) so repeated
  lethal hits or a post-death `Kill()` cannot re-fire `OnDied`.
- `EnemyEntity.Kill()` already guards `currentHealth <= 0` (explode path → `SacrificeAttack.cs:104-105`).
- `EnemyController` forwards both through the one `IEnemySpawned.OnDied` event.

Order of operations (no double-notify possible):

```
TakeDamage/Kill → EnemyEntity.OnDied (fires once)
   ├→ EnemyController.HandleDied → SetState<DieState/ExplodeState leave-alone>
   └→ EnemyController.OnDied (IEnemySpawned) → SpawnSystem.OnEnemyDied (idempotent)
```

## 6. Explode path (verified)

`SacrificeAttack.ExplodingInAction()` calls `enemyController.EnemyEntity.Kill()`
(`SacrificeAttack.cs:104-105`), so explode deaths already flow through the same authoritative
`OnDied`. `EnemyController.HandleDied()` leaves enemies already in `ExplodeState` alone (their death
is handled by the explode state), so there is no double state transition. No duplication added.

## 7. Per-instance stat storage (health vs damage)

- **Health:** `EnemyEntity.SetMaxHealth` (existing authoritative API) gates the stored `maxHealth`;
  `Initialize()` then starts `currentHealth` at the scaled max. Health scaling is fully applied.
- **Damage (real enemy): fully applied.** `EnemyController.ConfigureForSpawn` stores the floor-scaled
  damage in a per-instance `runtimeDamage` field (exposed as `RuntimeDamage`). Attacks read this
  value instead of the shared ScriptableObject base:
  - `DealDamage.cs` calls `playerController.Entity.TakeDamage(enemyController.RuntimeDamage)` (was
    commented out; now active with the per-instance value).
  - `SacrificeAttack.cs` scales `config.ExplosionDamage` proportionally:
    `config.ExplosionDamage * (RuntimeDamage / config.BaseDamage)` — so explosion damage tracks
    the same floor scaling as melee damage. Shared ScriptableObjects are never mutated.
  - `EnemyEntity.baseDamage` remains unused by combat (intentionally not written).

## 8. TestEnemy contract

`TestEnemy` (`Assets/Scripts/Roguelike/Spawning/Testing/TestEnemy.cs`):

- `BaseMaxHealth` / `BaseDamage` return the raw serialized `baseHealth` / `baseDamage`.
- `ConfigureForSpawn(maxHealth, baseDamage)` stores them as `Health` / `Damage`.
- `Die()` is `dead`-guarded (no-op after death) → `OnDied` → `Destroy(gameObject)`.
- `Health`/`Damage` are also (re)initialized in `Awake` from the raw base; for spawned instances
  `ConfigureForSpawn` runs after `Instantiate` (post-Awake) so the scaled values win.

## 9. EnemyController changes

`Assets/Scripts/Enemy/EnemyController.cs`:

- `public class EnemyController : MonoBehaviour, IEnemySpawned, ISpawnStatConfig`
- `public event Action OnDied;` — forwarded from `enemyEntity.OnDied` in `Start`.
- `Start()` null-safe: `if (enemyEntity == null) enemyEntity = GetComponent<EnemyEntity>();` before
  `Initialize()`.
- `BaseMaxHealth` reads `enemyEntity.MaxHealth`; `BaseDamage` reads the **authoritative runtime
  damage config** `enemyAttackConfig.BaseDamage` (NOT the unused `EnemyEntity.baseDamage`).
- `ConfigureForSpawn` resolves `enemyEntity` via `GetComponent` if needed, then `SetMaxHealth` (health
  is fully applied) and stores the floor-scaled damage in `runtimeDamage` (exposed as `RuntimeDamage`).
  Runs before `Start/Initialize`, so the scaled health is the initial health and the scaled damage
  is available to attacks.

## 10. EnemyEntity changes

`Assets/Scripts/Enemy/EnemyEntity.cs` (tracked at `Assets/Scripts/enemy/EnemyEntity.cs`):

- `TakeDamage` dead-guard: `if (currentHealth <= 0f) return;` after the `damage <= 0` guard — one
  authoritative death transition (supports point 7 of the mandate: a single OnDied flow, no
  double-notify). This is the only behavioral change to EnemyEntity.

## 11. Debug/display updates

`SpawnTestDebugDisplay.cs` subscribes `enemy.OnDied += ...` (was `enemy.Died`). No behavior change.

## 12. Harness updates

`tools/spawn-integration-test/`:

- `Program.cs` — `Died` → `OnDied`; existing scenarios `SpawnStatConfigScenario` (base-stats read +
  floor-2 scaling seam) and `DeathIdempotencyScenario` (double-notify → decrement/clear exactly once,
  `Die()` twice is a no-op); new scenarios `RuntimeDamageConfigScenario` (per-instance runtimeDamage
  written by ConfigureForSpawn, scaled across floors), `OnDiedLethalDamageScenario` (TakeDamage →
  OnDied fires once at zero, post-death TakeDamage is no-op), `ExplosionDeathFlowScenario`
  (Kill() → OnDied → SpawnSystem, double Kill is no-op, explosion proportional scaling); new stubs
  `RuntimeDamageEnemy`, `LethalEnemy`, `ExplodingEnemy`.
- `spawn_integration_test.csproj` — compiles `ISpawnStatConfig.cs` (real source) with the rest.
- Result: **436/436 checks pass** (baseline 415 → +21, no coverage removed).

## 13. Play Mode driver

`SpawnSystemTestDriver.cs` needs no contract change (it drives `RunController` + `SpawnSystem` via
`TestEnemy.Die()` and `FloorCleared`); it compiles as-is against the new seam. Its floor-scaling
assertions still hold because `TestEnemy.Health` is set through `ISpawnStatConfig.ConfigureForSpawn`.

## 14. Prefab blocker (production enemy, NOT fixed here)

`Assets/Prefabs/enemies/TreeEntAsh.prefab` is unfinished: null `enemyEntity` reference,
`targetTransform: {fileID: 0}`, `_debugText: 0`, missing Attack1/Explode animations, and the
Enemy-side case-collision caveat below. SpawnSystem-side integration is complete **without** a
production prefab; wiring the real enemy is a single archetype `prefab` swap once the prefab is
fixed. Per instruction this prefab is reported, not redesigned.

## 15. Repository case collision (reported, not fixed)

`Assets/Scripts/Enemy/` and `Assets/Scripts/enemy/` **both exist in the git tree** on `origin/main`
(case-sensitive on the index, `core.ignorecase=true` on Windows). The working tree is one physical
`Assets/Scripts\Enemy` directory containing a union of both sides' files (e.g. `EnemyEntity.cs` lives
under the `enemy/` tree; `EnemyController.cs` under `Enemy/`). Reported per instruction; not fixed
here.

## 16. Boundaries preserved

- SpawnSystem never references `EnemyController` / `EnemyEntity` types (component interfaces only).
- Enemy code never references floors, multipliers, growth rates, or SpawnSystem.
- `RunController` unchanged; `RunSession` unchanged.
- No singleton, no static state, no EventBus additions.

## 17. Verification commands

```
dotnet run --project tools/spawn-integration-test/spawn_integration_test.csproj -c Release
    → [SpawnIntegration] ALL 436 CHECKS PASSED
dotnet build tools/spawn-integration-test/spawn_integration_test.csproj -c Release
    → Build succeeded, 0 Warnings, 0 Errors
```

Unity Play Mode remains unverified on this machine (no Unity Editor); the play-mode
`SpawnSystemTestDriver` self-check should be re-run in-editor.

## 18. Superseded doc rows

- `docs/ROGUELIKE_SPAWNING_SPRINT_4.md` — `IEnemySpawned.ApplyFloorScaling` / `IEnemySpawned.Died`
  rows, `[WAITING FOR ENEMY SYSTEM]` risks, TestEnemy description → updated + banner.
- `docs/ROGUELIKE_RUN_SYSTEM_SPRINT_4.md` — "NOT implemented / BLOCKED" verdict, `needs EnemyEntity
  setters`, death-hook `future` row → updated + banner.
- `docs/ARCHITECTURE.md` §1 — "None of the archetype or spawn work is implemented" → corrected.
- `docs/ROGUELIKE_SYSTEM.md` §4.5 sketch — implementation note added (seam differs from design sketch).

## 19. Files changed

| File | Change |
|---|---|
| `Assets/Scripts/Roguelike/Spawning/IEnemySpawned.cs` | Death-only contract (+meta intact). |
| `Assets/Scripts/Roguelike/Spawning/ISpawnStatConfig.cs` (+`.meta`) | New scaling seam; doc updated for applied damage. |
| `Assets/Scripts/Roguelike/Spawning/SpawnSystem.cs` | OnDied subscription, new `ApplyFloorScaling(GameObject,…)`, idempotent `OnEnemyDied`, doc. |
| `Assets/Scripts/Enemy/EnemyController.cs` | Implements both interfaces, null-safe Start, forwards OnDied; health via `SetMaxHealth`, per-instance `runtimeDamage` written by `ConfigureForSpawn`, exposed as `RuntimeDamage`. |
| `Assets/Scripts/Enemy/Attack states/DealDamage.cs` | Uncommented damage call, now uses `enemyController.RuntimeDamage` with alive guard. |
| `Assets/Scripts/Enemy/Attack states/SacrificeAttack.cs` | Explosion damage scaled proportionally from `RuntimeDamage` instead of raw SO. |
| `Assets/Scripts/Enemy/EnemyEntity.cs` | `TakeDamage` dead-guard only (no new setter). |
| `Assets/Scripts/Roguelike/Spawning/Testing/TestEnemy.cs` | `OnDied`, `ISpawnStatConfig`, guarded `Die()`. |
| `Assets/Scripts/Roguelike/Spawning/Testing/SpawnTestDebugDisplay.cs` | `enemy.OnDied +=`. |
| `tools/spawn-integration-test/Program.cs`, `spawn_integration_test.csproj` | Harness: `OnDied`, 2 new scenarios, `ISpawnStatConfig.cs` include. |
| `docs/ROGUELIKE_SPAWNING_SPRINT_4.md`, `docs/ROGUELIKE_RUN_SYSTEM_SPRINT_4.md`, `docs/ARCHITECTURE.md`, `docs/ROGUELIKE_SYSTEM.md` | Supersede/note updates. |
| `docs/ENEMY_SPAWN_INTEGRATION.md` | This report. |

## 20. Out of scope / not changed

- `RunBootstrap`, `RunController`, `RunData`, `RunSession`, `SpawnSystemTestDriver` — untouched.
- `TreeEntAsh.prefab` production wiring — blocked (see §14).
- `EnemyEntity.baseDamage` remains unused by combat (intentionally not written by ConfigureForSpawn;
  `DealDamage` and `SacrificeAttack` read `RuntimeDamage` from `EnemyController` instead).
- `docs/PROJECT_AUDIT_2026-08-09.md`, `Assets/_Recovery/0 (1).unity`, `ShaderGraphSettings.asset` —
  preserved/pre-existing, not part of this branch's commit.

## 21. Follow-up (next)

- Fix `TreeEntAsh.prefab` (assign `enemyEntity`, `targetTransform`, `_debugText`, attack/explode
  animations), then swap the archetype `prefab` from `TestEnemy` to the real enemy.
- Resolve the `Enemy/` vs `enemy/` directory collision (index case cleanup) — requires a case-sensitive
  checkout.
