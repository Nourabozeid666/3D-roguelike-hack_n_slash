# Roguelike System — Design

Design document for turning the 3D-roguelike hack'n'slash prototype into a run-based roguelike:
procedural floors, cost-based enemy spawning, run-scoped upgrades, economy, and meta-progression.

Everything below follows the conventions already in the codebase (verified in `PROJECT_ANALYSIS.md` and `ARCHITECTURE.md`):

- **No asmdefs** — everything compiles into `Assembly-CSharp`.
- **No namespaces** — scripts are flat, exactly like the existing files.
- **Hubs + data bags** — a MonoBehaviour hub (like `PlayerController` / `EnemyController`) owns wiring; a `[Serializable]` bag holds data.
- **Generic state machine over an interface** — mirrors `EnemyStateMachine<T>` / `IEstate` (`EnemyStateMachine.cs`).
- **ScriptableObjects for data** — mirrors `PatrolRoute`.
- **Events for cross-cutting communication** — mirrors `InputController` static events and `EnemyEntity` events.
- **`file:line` citations** where the design touches existing code.

---

## 1. Goals

1. **Run loop** — a single attempt = one run: start → floors → upgrade choices → death or (eventual) win.
2. **Procedural floors** — each floor is a fresh arena layout with a spawn budget.
3. **Cost-based enemy spawning** — this is already the stated design intent in `EnemyController.cs:36-43` (archetype cost, budget per floor, cost scaling with depth). This design implements that comment.
4. **Run-scoped upgrades** — after clearing a floor, pick 1 of 3 random upgrades that stack across the run.
5. **Economy** — enemies drop currency; currency buys meta upgrades between runs.
6. **Meta progression** — persistent unlocks/saves survive death.
7. **Reuses existing systems** — the player/enemy FSMs, poise, dissolve effect, NavMesh, and Cinemachine stay untouched.

---

## 2. Run Loop

```
┌─────────────── Meta Lobby (between runs) ───────────────┐
│  spend currency on permanent upgrades → press Start      │
└──────────────────────────┬───────────────────────────────┘
                           ▼
              RunStateMachine (RunManager)
┌──────────────────────────────────────────────────────────┐
│  LobbyState ──Start──► FloorStartState                   │
│                            │  generate floor N arena     │
│                            │  run SpawnSystem(budget)    │
│                            ▼                            │
│                      FloorActiveState ◄────┐             │
│                            │  combat       │             │
│                     (player died)  (room cleared)        │
│                            │               │             │
│                     RunEndState    FloorClearedState     │
│                     (summary)         │  show 3 upgrades │
│                            │          │  pick one        │
│                            │          └─► FloorStartState│ (N+1)
└────────────────────────────┴─────────────────────────────┘
                           ▼
                     return to Meta Lobby
```

---

## 3. Proposed Folder Structure

```
Assets/Scripts/Roguelike/                 ← ALL new code (default Assembly-CSharp)
├── Run/
│   ├── RunManager.cs                     (hub: owns FSM + run data)
│   ├── RunData.cs                        ([Serializable] run-scoped bag)
│   ├── RunStateMachine.cs                (generic FSM over IRunState — clone of EnemyStateMachine)
│   ├── RunState.cs                       (abstract state base)
│   └── States/
│       ├── LobbyState.cs
│       ├── FloorStartState.cs
│       ├── FloorActiveState.cs
│       ├── FloorClearedState.cs
│       └── RunEndState.cs
├── Dungeon/
│   ├── FloorLayout.cs                    ([Serializable] grid model of an arena)
│   ├── FloorGenerator.cs                 (turns FloorLayout into scene objects)
│   ├── RoomDefinition.cs                 (ScriptableObject: arena variant)
│   └── SpawnPoint.cs                     (MonoBehaviour marker for enemy spawns)
├── Spawning/
│   ├── SpawnSystem.cs                    (cost-based spawning, per design comment)
│   ├── EnemyArchetype.cs                 (ScriptableObject: cost + scaling per floor)
│   └── SpawnTable.cs                     (ScriptableObject: archetype list + budget curve)
├── Items/
│   ├── ItemDefinition.cs                 (ScriptableObject: an upgrade/stat package)
│   ├── StatModifier.cs                   ([Serializable] one stat change)
│   ├── Inventory.cs                      (run-scoped list of acquired items)
│   └── UpgradePool.cs                    (ScriptableObject: pool of items for a run)
├── Economy/
│   ├── CurrencyManager.cs                (run currency + meta currency)
│   ├── DropTable.cs                      (ScriptableObject: loot probabilities)
│   └── Pickup.cs                         (MonoBehaviour: coin/gem pickup)
├── Meta/
│   ├── MetaProgressData.cs               ([Serializable] persistent save model)
│   ├── MetaUpgradeDefinition.cs          (ScriptableObject: permanent upgrade)
│   └── SaveSystem.cs                     (PlayerPrefs/JSON persistence)
└── UI/
    ├── HUDController.cs                  (health/currency/floor text)
    ├── UpgradeScreenUI.cs                (3-choice upgrade screen)
    └── GameOverUI.cs                     (run summary + return-to-lobby)
```

**Data assets** (created in the Editor):
```
Assets/Data/
├── Archetypes/   grunt_ash.asset, bruiser_explode.asset, ...
├── Items/        swift_boots.asset, iron_skin.asset, ...
├── Meta/         meta_upgrade_speed.asset, ...
├── DropTables/   grunt_drops.asset, bruiser_drops.asset
├── UpgradePools/ floor_1_pool.asset, floor_2_pool.asset
└── Rooms/        arena_1.asset, arena_2.asset
```

---

## 4. Core Systems — Code

### 4.1 Run state machine (mirrors `EnemyStateMachine.cs`)

`RunStateMachine.cs`

```csharp
using System.Collections.Generic;

public interface IRunState
{
    void Enter();
    void Exit();
    void Tick();
}

public class RunStateMachine<T> where T : class, IRunState
{
    private T currentState = null;
    private T previousState;
    private readonly Dictionary<System.Type, T> states = new();

    public T CurrentState => currentState;
    public T PreviousState => previousState;

    public void AddState(T state) => states[state.GetType()] = state;

    public void Tick() => currentState?.Tick();

    public void SetState<TState>() where TState : class, T
    {
        if (currentState is TState) return;
        T prev = currentState;
        currentState?.Exit();
        if (states.TryGetValue(typeof(TState), out var next))
        {
            currentState = next;
            currentState.Enter();
            previousState = prev;
        }
    }
}
```

`RunState.cs`

```csharp
public abstract class RunState : IRunState
{
    protected RunManager run;
    protected RunState(RunManager run) => this.run = run;
    public abstract void Enter();
    public virtual void Exit() { }
    public virtual void Tick() { }
}
```

### 4.2 Run data bag (mirrors `PlayerContext.cs`)

`RunData.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunData
{
    [Header("Progress")]
    public int floor = 1;
    public int clearedRooms = 0;

    [Header("Run currency (spent this run, lost on death)")]
    public int coins = 0;

    [Header("Meta currency (banked after run ends)")]
    public int essence = 0;

    [Header("Scaling")]
    public float enemyBudget = 10f;         // spawn budget for current floor
    public float enemyBudgetGrowth = 1.4f;  // multiplier per floor
    public float enemyStatGrowth = 1.12f;   // health/damage multiplier per floor

    public void StartNewRun()
    {
        floor = 1;
        clearedRooms = 0;
        coins = 0;
        essence = 0;
        enemyBudget = 10f;
    }

    public void AdvanceFloor()
    {
        floor++;
        clearedRooms++;
        enemyBudget *= enemyBudgetGrowth;
    }
}
```

### 4.3 Run manager hub (mirrors `EnemyController`)

`RunManager.cs`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    [SerializeField] internal RunData runData = new RunData();
    [SerializeField] internal SpawnSystem spawnSystem;
    [SerializeField] internal FloorGenerator floorGenerator;
    [SerializeField] internal UpgradeScreenUI upgradeUI;
    [SerializeField] internal GameOverUI gameOverUI;
    [SerializeField] internal PlayerEntity playerEntity;

    private RunStateMachine<RunState> stateMachine = new();

    public static RunManager Instance { get; private set; }

    public RunStateMachine<RunState> StateMachine => stateMachine;

    void Awake()
    {
        Instance = this;

        stateMachine.AddState(new LobbyState(this));
        stateMachine.AddState(new FloorStartState(this));
        stateMachine.AddState(new FloorActiveState(this));
        stateMachine.AddState(new FloorClearedState(this));
        stateMachine.AddState(new RunEndState(this));

        stateMachine.SetState<LobbyState>();
    }

    void Update() => stateMachine.Tick();

    public void StartRun()
    {
        runData.StartNewRun();
        stateMachine.SetState<FloorStartState>();
    }

    public void EndRun(bool victory)
    {
        BankMetaCurrency();
        gameOverUI.Show(runData, victory);
        stateMachine.SetState<RunEndState>();
    }

    public void ReturnToLobby() => stateMachine.SetState<LobbyState>();

    public void FloorCleared() => stateMachine.SetState<FloorClearedState>();

    private void BankMetaCurrency()
    {
        var meta = SaveSystem.Load();
        meta.essence += runData.essence;
        if (runData.floor > meta.bestFloor) meta.bestFloor = runData.floor;
        meta.totalRuns++;
        SaveSystem.Save(meta);
    }
}
```

### 4.4 States

`States/FloorStartState.cs` — generate the arena, spawn enemies by budget, then go live.

```csharp
using UnityEngine;

public class FloorStartState : RunState
{
    public FloorStartState(RunManager run) : base(run) { }

    public override void Enter()
    {
        // 1. clear previous floor
        run.floorGenerator.ClearFloor();

        // 2. build a fresh arena for this floor
        run.floorGenerator.Generate(run.runData.floor);

        // 3. spend the floor budget on enemies
        run.spawnSystem.Populate(
            budget: run.runData.enemyBudget,
            floor:  run.runData.floor
        );

        // 4. go live
        run.StateMachine.SetState<FloorActiveState>();
    }
}
```

`States/FloorActiveState.cs` — combat runs; watches for all-clear or player death.

```csharp
using UnityEngine;

public class FloorActiveState : RunState
{
    private int remainingAtEnter;

    public FloorActiveState(RunManager run) : base(run) { }

    public override void Enter()
    {
        remainingAtEnter = run.spawnSystem.AliveCount();
    }

    public override void Tick()
    {
        // player death ends the run
        if (run.playerEntity.Health <= 0f)
        {
            run.EndRun(victory: false);
            return;
        }

        // no enemies left → floor cleared
        if (run.spawnSystem.AliveCount() == 0)
        {
            run.FloorCleared();
        }
    }
}
```

`States/FloorClearedState.cs` — roll 3 upgrades, wait for a pick, then next floor.

```csharp
using UnityEngine;

public class FloorClearedState : RunState
{
    private bool picked = false;

    public FloorClearedState(RunManager run) : base(run) { }

    public override void Enter()
    {
        picked = false;
        run.upgradeUI.Show(
            count: 3,
            onPicked: (item) =>
            {
                picked = true;
                run.upgradeUI.Hide();
                run.runData.AdvanceFloor();
                run.StateMachine.SetState<FloorStartState>();
            }
        );
    }

    public override void Tick()
    {
        // safety: if the UI is closed without a pick, skip the floor
        if (!picked && !run.upgradeUI.IsOpen)
        {
            run.runData.AdvanceFloor();
            run.StateMachine.SetState<FloorStartState>();
        }
    }
}
```

`States/LobbyState.cs` / `States/RunEndState.cs` — placeholder phases.

```csharp
using UnityEngine;

public class LobbyState : RunState
{
    public LobbyState(RunManager run) : base(run) { }

    public override void Enter()
    {
        Time.timeScale = 1f;
        // show meta lobby / main menu
    }
}

public class RunEndState : RunState
{
    public RunEndState(RunManager run) : base(run) { }

    public override void Enter()
    {
        // game over / victory screen is already shown by RunManager.EndRun
    }
}
```

> **IMPLEMENTED (test-only), differs from the design above:** the actual Run System is a slim, plain-C#
> `RunController` (enum-based `RunStateMachine`, `RunState` enum, `RunData`) with no `RunManager`
> MonoBehaviour and no state classes. The floor-clear path is wired via `SpawnSystem.FloorCleared`
> (report-only event, `SpawnSystem.cs`) → `RunController.CompleteFloor()` (`FloorActive→FloorCleared`)
> → `RunController.StartNextFloor()` (`FloorCleared→FloorStart` + `RunData.AdvanceFloor()`) →
> `SpawnSystem.Populate(budget, floor)` → `RunController.BeginFloor()`. The test-only
> `SpawnSystemTestDriver` is the integration owner (see `docs/ROGUELIKE_SPAWNING_SPRINT_4.md` §4/§4a).
> `FloorGenerator`, `UpgradeScreenUI`, `GameOverUI`, `SaveSystem` remain future work.

---

### 4.5 Cost-based enemy spawning (implements `EnemyController.cs:36-43`)

`EnemyArchetype.cs` (ScriptableObject)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "Archetype", menuName = "Roguelike/Enemy Archetype")]
public class EnemyArchetype : ScriptableObject
{
    public string displayName;
    public EnemyController prefab;         // the full enemy wrapper (see TreeEntAsh.prefab)

    [Header("Spawn cost (design comment: higher cost = stronger, later floors)")]
    public int cost = 3;

    [Header("Per-floor scaling multipliers")]
    public float healthGrowthPerFloor = 0.12f;   // +12% per floor
    public float damageGrowthPerFloor = 0.08f;

    [Header("Runtime instance data")]
    public EnemyEntityStats baseStats;     // defaults baked in the prefab override at spawn time
}

[System.Serializable]
public class EnemyEntityStats
{
    public float maxHealth = 100f;
    public float baseDamage = 20f;
    public float baseDefense = 20f;
    public float maxPoise = 100f;
}
```

`SpawnPoint.cs` (MonoBehaviour marker)

```csharp
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private bool isElite = false;
    public bool IsElite => isElite;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isElite ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
```

`SpawnSystem.cs` — picks affordable archetypes and fills spawn points until the budget runs out.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{
    [SerializeField] private SpawnTable table;        // archetype pool + budget curve
    private readonly List<EnemyController> alive = new();

    public int AliveCount() => alive.Count;

    public void Populate(float budget, int floor)
    {
        ClearAlive();

        var points = GetComponentsInChildren<SpawnPoint>(includeInactive: true);
        if (points.Length == 0 || table == null) return;

        float remaining = budget;

        // keep spending while we can afford at least the cheapest archetype
        int cheapest = int.MaxValue;
        foreach (var a in table.Archetypes) cheapest = Mathf.Min(cheapest, a.cost);

        while (remaining >= cheapest && points.Length > 0)
        {
            EnemyArchetype archetype = PickAffordable(table.Archetypes, remaining);
            SpawnPoint point = PickRandomPoint(points);

            EnemyController enemy = InstantiateEnemy(archetype, point, floor);
            alive.Add(enemy);

            remaining -= archetype.cost;
        }
    }

    private EnemyArchetype PickAffordable(List<EnemyArchetype> archetypes, float budget)
    {
        List<EnemyArchetype> affordable = archetypes.FindAll(a => a.cost <= budget);
        return affordable[Random.Range(0, affordable.Count)];
    }

    private SpawnPoint PickRandomPoint(SpawnPoint[] points)
        => points[Random.Range(0, points.Length)];

    private EnemyController InstantiateEnemy(EnemyArchetype archetype, SpawnPoint point, int floor)
    {
        EnemyController enemy = Instantiate(archetype.prefab, point.transform.position, point.transform.rotation);
        ApplyFloorScaling(enemy, archetype, floor);
        return enemy;
    }

    private void ApplyFloorScaling(EnemyController enemy, EnemyArchetype archetype, int floor)
    {
        EnemyEntity entity = enemy.EnemyEntity;
        float scale = Mathf.Pow(archetype.healthGrowthPerFloor + 1f, floor - 1);

        entity.SetMaxHealth(archetype.baseStats.maxHealth * scale);
        entity.SetBaseDamage(archetype.baseStats.baseDamage * scale);
        entity.SetBaseDefense(archetype.baseStats.baseDefense);
        entity.SetMaxPoise(archetype.baseStats.maxPoise);
        entity.Initialize();
    }

    private void ClearAlive()
    {
        foreach (var e in alive)
            if (e != null) Destroy(e.gameObject);
        alive.Clear();
    }
}
```

`SpawnTable.cs` (ScriptableObject)

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnTable", menuName = "Roguelike/Spawn Table")]
public class SpawnTable : ScriptableObject
{
    public List<EnemyArchetype> Archetypes;

    [Tooltip("Base budget for floor 1; RunData grows it per floor")]
    public float baseBudget = 10f;
}
```

**Note on the existing enemy code** — `EnemyEntity` currently exposes no setters for damage/defense/poise. The spawn system above assumes three small additions to `EnemyEntity.cs` (see §6, integration change #4).

---

### 4.5a Floor-based enemy availability & composition selection

**Goal:** new enemy types unlock over the run by floor (no hardcoded enemy types or floor numbers), and a floor's spawn is a *composition* — a chosen mix of unlocked archetypes — picked from a precomputed/cached ranked set instead of a per-spawn search.

**Unlock rule (`SpawnTable.cs`).** `unlockInterval` (int, default 3). The archetype at list index `i` becomes available starting at floor `1 + i * unlockInterval`. So interval 3 → index 0 floors 1–3, index 1 floors 4–6, index 2 floors 7–9, …. `SpawnTable.AvailableForFloor(floor)` returns that prefix of the pool (the whole pool once everything is unlocked). Unlocking only *expands* the pool — a newly unlocked enemy is never guaranteed to spawn.

**Target enemy count.** Derived deterministically: `target = floor(budget / cheapest available cost)`. This is always achievable (`target × cheapest ≤ budget`), so a valid composition always exists for any run pool. This single derivation point is where an explicit target-count design would slot in later.

**Composition ranking (`EnemyCompositionSelector.cs`).** The budget is a **maximum**, not exact. For a given (pool, target, budget) the selector enumerates every combination-with-repetition of exactly `target` enemies whose total cost ≤ budget, then ranks:

1. satisfy the target count and stay ≤ budget (every candidate does);
2. best budget use — highest total cost without exceeding the budget;
3. variety **only among equally-ranked** compositions — most distinct archetype types;
4. controlled randomness only between the final equally-ranked candidates.

**Cache.** Results are cached keyed on `(floor, target, budget)` (the pool is a pure function of the floor). A `Populate` does one cache lookup and picks a candidate; no recursive search runs per spawn and no brute-force retry loop exists.

**Fallback.** If no valid composition exists (defensive; not reachable with the derived target), `SpawnSystem.SpawnFallback` spawns the largest affordable count of the cheapest archetype, never exceeding the budget, and logs a warning — never a silently invalid composition.

**Current flow (`SpawnSystem.Populate(budget, floor)`).** `AvailableForFloor` → target count → cached composition lookup → rank/select → spawn onto SpawnPoints (without replacement). `LastCompositionInfo` exposes a read-only summary (floor / available types / target / composition / cost / budget) for the test HUD.

---

### 4.6 Items & run-scoped upgrades

`StatModifier.cs`

```csharp
using System;

public enum StatType
{
    MaxHealth,
    BaseDamage,
    BaseDefense,
    MoveSpeed,
    DashSpeed,
    JumpForce,
    CritChance,
    CooldownRate
}

[Serializable]
public class StatModifier
{
    public StatType stat;
    public float amount;        // additive (e.g. +25 MaxHealth)
    public float multiplier = 1f; // multiplicative (e.g. ×1.5 damage); default 1 = no change
}
```

`ItemDefinition.cs` (ScriptableObject)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Roguelike/Item")]
public class ItemDefinition : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public StatModifier[] modifiers;
}
```

`Inventory.cs` — holds items for the run; re-applies everything to the player.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private PlayerEntity player;
    private readonly List<ItemDefinition> items = new();

    public IReadOnlyList<ItemDefinition> Items => items;

    public void Add(ItemDefinition item)
    {
        items.Add(item);
        Rebuild();
    }

    public void Clear()
    {
        items.Clear();
        Rebuild();
    }

    private void Rebuild()
    {
        // reset player to base, then re-apply every item (keeps stacking correct)
        player.ResetModifiers();
        foreach (var item in items)
            player.ApplyModifier(item.modifiers);
    }
}
```

**Note:** `PlayerEntity` currently has flat `health/defense/damage` fields. The modifier system needs a small extension to `PlayerEntity.cs` (see §6, integration change #2). No change to `IEntity` is required.

`UpgradePool.cs` (ScriptableObject)

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradePool", menuName = "Roguelike/Upgrade Pool")]
public class UpgradePool : ScriptableObject
{
    [Tooltip("Which items can be offered after clearing a floor")]
    public List<ItemDefinition> pool;
}
```

`UpgradeScreenUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using System;

public class UpgradeScreenUI : MonoBehaviour
{
    [SerializeField] private UpgradePool pool;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] choiceButtons;   // 3 buttons
    [SerializeField] private Text[] choiceLabels;

    public bool IsOpen => panel.activeSelf;

    private Action<ItemDefinition> onPicked;

    public void Show(int count, Action<ItemDefinition> picked)
    {
        onPicked = picked;
        var rolled = Roll(count);

        for (int i = 0; i < choiceButtons.Length && i < rolled.Count; i++)
        {
            int index = i; // capture for closure
            ItemDefinition item = rolled[i];
            choiceLabels[i].text = item.displayName + "\n" + item.description;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() =>
            {
                onPicked?.Invoke(item);
            });
        }

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    private List<ItemDefinition> Roll(int count)
    {
        List<ItemDefinition> rolled = new();
        List<ItemDefinition> remaining = new(pool.pool);

        while (rolled.Count < count && remaining.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, remaining.Count);
            rolled.Add(remaining[idx]);
            remaining.RemoveAt(idx);
        }

        return rolled;
    }
}
```

---

### 4.7 Economy & drops

`CurrencyManager.cs`

```csharp
using System;

public class CurrencyManager : MonoBehaviour
{
    public event Action<int> OnCoinsChanged;
    public event Action<int> OnEssenceChanged;

    public int Coins { get; private set; }
    public int Essence { get; private set; }

    public void AddCoins(int amount)
    {
        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void AddEssence(int amount)
    {
        Essence += amount;
        OnEssenceChanged?.Invoke(Essence);
    }

    public void ResetRunCurrency()
    {
        Coins = 0;
        OnCoinsChanged?.Invoke(Coins);
    }
}
```

`DropTable.cs` (ScriptableObject) + `Pickup.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "DropTable", menuName = "Roguelike/Drop Table")]
public class DropTable : ScriptableObject
{
    [Header("Run currency")]
    public int coinMin = 1;
    public int coinMax = 3;

    [Header("Essence drops (only on floor clear / elite)")]
    public int essenceMin = 0;
    public int essenceMax = 0;

    [Header("Item chance on kill")]
    [Range(0f, 1f)] public float itemChance = 0f;
    public ItemDefinition item;

    public void Roll(Vector3 at, CurrencyManager currency, Transform dropParent)
    {
        int coins = Random.Range(coinMin, coinMax + 1);
        if (coins > 0)
            SpawnPickup(at, dropParent).Setup(currency, coins, isEssence: false);

        int essence = Random.Range(essenceMin, essenceMax + 1);
        if (essence > 0)
            SpawnPickup(at, dropParent).Setup(currency, essence, isEssence: true);
    }

    private Pickup SpawnPickup(Vector3 at, Transform parent)
    {
        // Pooling recommended (mirror EnemyDissolve); placeholder spawns a GO
        return ObjectPool.Get<Pickup>(at, parent);
    }
}
```

```csharp
using UnityEngine;

public class Pickup : MonoBehaviour
{
    private CurrencyManager currency;
    private int amount;
    private bool isEssence;

    public void Setup(CurrencyManager currency, int amount, bool isEssence)
    {
        this.currency = currency;
        this.amount = amount;
        this.isEssence = isEssence;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (isEssence) currency.AddEssence(amount);
        else currency.AddCoins(amount);

        ObjectPool.Release(this);
    }
}
```

**Wiring drops to kills** — hook into the existing `EnemyEntity.OnDied` event in `EnemyController` (which already fires on death, `EnemyEntity.cs:48`). See §6 integration change #3.

---

### 4.8 Meta progression & save

> **IMPLEMENTED (run-level saves only):** the CURRENT-run checkpoint save (`SaveData` / `RunSaveService` /
> `RunBootstrap`, Main Menu Continue/New Run) landed in **Sprint 6 — see
> [`ROGUELIKE_SAVE_SPRINT_6.md`](ROGUELIKE_SAVE_SPRINT_6.md)**. The META progression (essence/best-floor/
> unlocks persisting ACROSS runs) below remains `[PROPOSED] — FUTURE WORK`, not implemented.

`MetaProgressData.cs`

```csharp
using System;
using System.Collections.Generic;

[Serializable]
public class MetaProgressData
{
    public int essence = 0;              // banked between runs
    public int bestFloor = 0;
    public int totalRuns = 0;
    public List<string> purchasedUpgrades = new();  // ids of MetaUpgradeDefinition
}
```

`SaveSystem.cs` — JSON in PlayerPrefs (simple, no file I/O).

```csharp
using UnityEngine;

public static class SaveSystem
{
    private const string Key = "roguelike_meta_v1";

    public static MetaProgressData Load()
    {
        string raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(raw)) return new MetaProgressData();
        return JsonUtility.FromJson<MetaProgressData>(raw);
    }

    public static void Save(MetaProgressData data)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
```

`MetaUpgradeDefinition.cs` (ScriptableObject)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "MetaUpgrade", menuName = "Roguelike/Meta Upgrade")]
public class MetaUpgradeDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public int cost;                     // essence to unlock
    public StatModifier modifier;        // permanent modifier applied on every run start
    public int maxLevel = 1;
}
```

Applying meta upgrades at run start is done by `RunManager.StartRun()` calling the same `PlayerEntity.ApplyModifier` path the `Inventory` uses (see §4.6).

---

### 4.9 Floor / arena generation

`FloorLayout.cs` — a small serializable model of one arena.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FloorLayout
{
    public Vector2Int size;                    // grid size of the arena
    public List<Vector2Int> floorTiles = new();
    public List<Vector2Int> wallTiles = new();
    public List<SpawnPointData> spawns = new();
    public Vector3 playerStart;
}

[Serializable]
public class SpawnPointData
{
    public Vector3 position;
    public bool isElite;
}
```

`FloorGenerator.cs`

```csharp
using UnityEngine;

public class FloorGenerator : MonoBehaviour
{
    [SerializeField] private RoomDefinition[] rooms;   // arena variants for floors
    [SerializeField] private GameObject floorTilePrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private SpawnPoint spawnPointPrefab;
    [SerializeField] private GameObject player;

    private Transform floorRoot;

    public void Generate(int floor)
    {
        RoomDefinition room = rooms[Mathf.Min(floor - 1, rooms.Length - 1)];
        room = floor > rooms.Length ? CreateScaledVariant(room, floor) : room;

        floorRoot = new GameObject($"Floor {floor}").transform;
        floorRoot.SetParent(transform);

        foreach (var tile in room.Layout.floorTiles)
            Instantiate(floorTilePrefab, new Vector3(tile.x, 0f, tile.y), Quaternion.identity, floorRoot);

        foreach (var tile in room.Layout.wallTiles)
            Instantiate(wallPrefab, new Vector3(tile.x, 0f, tile.y), Quaternion.identity, floorRoot);

        foreach (var s in room.Layout.spawns)
        {
            SpawnPoint sp = Instantiate(spawnPointPrefab, s.position, Quaternion.identity, floorRoot);
            sp.SetElite(s.isElite);
        }

        // Move the player to the start; (re)bake NavMesh after layout is placed
        player.transform.position = room.Layout.playerStart;
        RebuildNavMesh();
    }

    public void ClearFloor()
    {
        if (floorRoot != null) Destroy(floorRoot.gameObject);
    }

    private RoomDefinition CreateScaledVariant(RoomDefinition template, int floor)
    {
        // placeholder for procedural growth: scale grid / add spawns
        return template;
    }

    private void RebuildNavMesh()
    {
        // Call NavMeshSurface.BuildNavMesh() on the scene's NavMeshSurface.
        // (Baked data in Assets\Scenes\TestingScene\NavMesh-navMesh Settings.asset
        //  will no longer match dynamic floors — must rebuild at runtime.)
    }
}
```

`RoomDefinition.cs` (ScriptableObject)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "Room", menuName = "Roguelike/Room Definition")]
public class RoomDefinition : ScriptableObject
{
    public string displayName;
    public FloorLayout Layout;
}
```

**Caveat [Fact]:** the current scene uses a **baked** NavMesh (`Assets\Scenes\TestingScene\NavMesh-navMesh Settings.asset`). Procedural floors need a runtime `NavMeshSurface.BuildNavMesh()` call after layout generation (the `com.unity.ai.navigation 2.0.13` package is already installed, `Packages\manifest.json:3`). This is the biggest risk in the whole design — see §8.

---

## 5. UI

| Screen | Script | Content |
|---|---|---|
| HUD | `HUDController.cs` | player health, run coins, floor number (Text; reuse the debug-Text pattern) |
| Upgrade choice | `UpgradeScreenUI.cs` | 3 buttons, shown by `FloorClearedState` |
| Game over / victory | `GameOverUI.cs` | floor reached, coins, essence banked, best floor; "Return to Lobby" |

All are plain uGUI (package `com.unity.ugui 2.0.0` already present). The existing Canvas in `TestingScene` can host them.

---

## 6. Integration with Existing Code

The design reuses existing systems and only touches a few files. Every change below is a **minimal edit**, not a rewrite.

### #1 — `GameManager.cs` — bootstrapping
`GameManager` is currently cursor-lock only (`GameManager.cs:7-11`). Make it the bootstrap that instantiates `RunManager` and the HUD, and keeps the cursor lock while a run is active.

```csharp
public class GameManager : MonoBehaviour
{
    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        // instantiate RunManager prefab / find it, and start the run FSM
    }
}
```

### #2 — `PlayerEntity.cs` — support modifiers
`PlayerEntity` (`PlayerEntity.cs`) has flat fields and a damage formula (`:34-37`). Add a modifier list and a base reset so `Inventory`/meta upgrades can stack cleanly.

```csharp
// additions to PlayerEntity.cs
[SerializeField] private float baseHealth = 100f;   // separate from current health
[SerializeField] private float baseDamage = 10f;
[SerializeField] private float baseDefense = 5f;

private float bonusHealth, bonusDamage, bonusDefense;
private float dmgMult = 1f, defMult = 1f;

public void ApplyModifier(StatModifier[] modifiers)
{
    foreach (var m in modifiers)
    {
        if (m.stat == StatType.MaxHealth) bonusHealth += m.amount;
        else if (m.stat == StatType.BaseDamage) bonusDamage += m.amount;
        else if (m.stat == StatType.BaseDefense) bonusDefense += m.amount;
        if (m.multiplier != 1f)
        {
            if (m.stat == StatType.BaseDamage) dmgMult *= m.multiplier;
            if (m.stat == StatType.BaseDefense) defMult *= m.multiplier;
        }
    }
    SetMaxHealth((baseHealth + bonusHealth) * /*hpMult*/ 1f);
}

public void ResetModifiers()
{
    bonusHealth = bonusDamage = bonusDefense = 0f;
    dmgMult = defMult = 1f;
    SetMaxHealth(baseHealth);
}
```

### #3 — `EnemyController.cs` / `DealDamage.cs` — enable damage + drops
- **Damage:** the `DealDamage.cs:14-18` call is commented out. Uncomment it so enemies actually damage the player, and use the archetype-scaled `EnemyEntity.BaseDamage`.
- **Drops:** in `EnemyController.Start` (`:94-120`), subscribe a new handler to `enemyEntity.OnDied` that calls `dropTable.Roll(transform.position, currency, transform.parent)`. The event already exists (`EnemyEntity.cs:48`).

### #4 — `EnemyEntity.cs` — setters for scaling
The spawn system needs to override stats. Add setters (and keep existing `SetMaxHealth`, `:31-36`):

```csharp
public void SetBaseDamage(float value) => baseDamage = value;
public void SetBaseDefense(float value) => baseDefence = value;
public void SetMaxPoise(float value) { maxPoise = value; currentPoise = value; }
```

### #5 — `DieState.cs` — fix the death crash
`DieState.cs:11-18` never assigns `agent`/`animator` → NullReference on death. Roguelike runs **require** enemy death to work. Fix: assign them in the constructor exactly like the other states (`SpownState.cs:11-15`). Without this, `FloorActiveState` can never see a cleared room.

### #6 — `InputController.cs` / actions file
Add two actions to `Assets\InputSystem.inputactions` and static events in `InputController` (mirroring `:24-28`): `Pause` and `Confirm` (used by the upgrade screen / game-over UI). Keep the existing `PlayerMovement` map untouched.

### #7 — Scene setup (TestingScene)
- Add a `RunManager` GameObject (with `SpawnSystem`, `FloorGenerator`, `Inventory`, `CurrencyManager`, HUD, upgrade UI, game-over UI).
- Mark existing ground/walls as tile sources OR let `FloorGenerator` build fresh arenas; simplest first step: **keep the current arena as "floor 1"** and generate later floors.
- Add `SpawnPoint` markers where enemies currently sit.
- Add a `Player` tag is already `Enemy`; ensure the player GameObject has the `Player` tag for `Pickup.OnTriggerEnter` (`other.CompareTag("Player")`).

---

## 7. Implementation Order (Phases)

**Phase 1 — Run shell (playable loop, no procedural floors)**
1. `RunData`, `RunStateMachine`, `RunState`, `RunManager`, states.
2. `GameManager` bootstrap.
3. HUD (health/coins/floor).
4. Death → `RunEndState` → summary → lobby. *(Requires #5 DieState fix + DealDamage uncomment so death is possible.)*

**Phase 2 — Spawning (cost-based, per design comment)**
5. `EnemyArchetype`, `SpawnTable`, `SpawnPoint`, `SpawnSystem`.
6. `EnemyEntity` setters (#4).
7. Drops → `DropTable`, `Pickup`, `CurrencyManager` (#3).

**Phase 3 — Upgrades**
8. `StatModifier`, `ItemDefinition`, `Inventory`, `UpgradePool`, `UpgradeScreenUI`.
9. `PlayerEntity` modifier support (#2).
10. Floor clear → upgrade choice → next floor.

**Phase 4 — Meta progression**
11. `MetaProgressData`, `SaveSystem`, `MetaUpgradeDefinition`.
12. Bank essence at run end; meta upgrade application on run start.

**Phase 5 — Procedural floors**
13. `FloorLayout`, `RoomDefinition`, `FloorGenerator`.
14. Runtime NavMesh rebuild (highest risk, §8).
15. `SpawnPointData` generation inside room layouts.

**Phase 6 — Polish**
16. Object pooling for pickups/enemies (mirror `EnemyDissolve.cs` pool).
17. Input `Pause`/`Confirm`, audio, screenshake, etc.

---

## 8. Risks & Open Questions

1. **Runtime NavMesh (highest risk) [Fact].** The scene's NavMesh is baked (`TestingScene\NavMesh-navMesh Settings.asset`). Procedural floors require `NavMeshSurface.BuildNavMesh()` at runtime; the `com.unity.ai.navigation` package supports it, but baking cost and failure handling must be validated early (Phase 5, spike first).
2. **Player death is currently impossible [Fact].** `DealDamage.cs:14-18` is commented out, so `FloorActiveState`'s death branch can't trigger until damage is enabled.
3. **Enemy death currently crashes [Fact].** `DieState` null-ref (`DieState.cs:11-18`) must be fixed before any roguelike loop can progress.
4. **`PlayerEntity` has no modifier layer.** The existing flat stats need the small extension in #2; `Inventory` and meta upgrades both depend on it.
5. **Entity stats are serialized on the scene/prefab, not on archetypes.** `EnemyEntity` fields are set in the scene (`TestingScene.unity:12342-12348`). The archetype `baseStats` override in `SpawnSystem.ApplyFloorScaling` must run after `Initialize()` to be authoritative.
6. **No room variety content exists.** `RoomDefinition` assets and `EnemyArchetype` assets must be authored in the Editor (Phase 2/5 content work).
7. **Open questions for the team:**
   - Should coins be spendable mid-run (in-floor shops), or only feed meta progression?
   - Is the intended flow arena-per-floor (one big room) or multi-room dungeons with corridors? (This design assumes **arena-per-floor**, extending the current single-arena scene.)
   - Does "winning" exist (e.g. beat floor N), or is the loop endless with score/floor tracking?
   - Where does essence drop — only elites/floor-clear, or per-kill?
