# ROGUELIKE SPRINT PLAN

> Planning-only document. No code, assets, or project files are modified by this plan.
> Evidence labels: `[EXISTS]` in project / `[PARTIAL]` / `[MISSING]` / `[PROPOSED]` / `[TEAM DECISION]` / `[BLOCKED]` / `[FUTURE]`

---

## 1. EXECUTIVE SUMMARY

We are adding a **roguelike run system** to an existing 3D hack-and-slash prototype. The project today is a single testing scene with a player, some enemies, and basic combat — but the combat loop is incomplete (enemies cannot actually damage the player; dying enemies crash). The roguelike system gives the game its structure: a run that can fail, reward that carries through a run, and a reason to restart.

**What V0.1 delivers (the ONLY scope of this plan's first implementation):** the foundational **data layer** — a `WeaponType` enum, a `WeaponData` ScriptableObject, and three base weapon asset files (Katana / Spear / Whip) with normalized values.

**What V0.1 explicitly does NOT include:** XP, levels, upgrades, gold, shop, meta-progression, procedural floors, save files, or random choice events. Those are later sprints. V0.1 is the single lowest-risk step that every later sprint depends on.

**The core idea of the whole system** (for design rationale, see `ROGUELIKE_SYSTEM.md`):
1. Enemies are spawned **per-floor** using a **cost budget** — hard enemies cost more, so a floor's difficulty curve is predictable and tunable.
2. Dying enemies drop **souls (currency)** that buy **upgrades between floors**.
3. Upgrades modify **normalized stats (0..1)** instead of raw numbers, so every stat scales safely forever.
4. The run has a clear **lifecycle**: floor → upgrade → floor → ... → death or victory.

**The single most important rule for this plan:** no sprint starts before its dependencies are done. The first implementation is blocked on nothing; every later sprint is gated by the sprint before it (see the dependency graph, §7).

---

## 2. RESPONSIBILITY MATRIX

| System | Owner | Status | V0.1 Included? |
|---|---|---|---|
| Player (movement, stats, combat) | Player dev | `[EXISTS]` — see §3 | No — reuse only |
| Enemy (AI, stats, death) | Enemy dev | `[EXISTS]` + `[BLOCKED]` | No — reuse only |
| Weapon / Combat | **Unverified** | `[MISSING]` — no weapon code | **Yes (data only)** |
| Roguelike (run manager, spawns, economy) | **Me** | `[MISSING]` | Yes (data only) |
| UI / HUD | Unassigned | `[MISSING]` | No |

**Contract rules:**
- I do not touch Player or Enemy code. I consume them through existing public surface only (`PlayerController.Entity`, `EnemyEntity.OnDied`, `PatrolRoute`-style ScriptableObjects).
- Weapon/Combat ownership is **unverified** — must be confirmed by the team before Sprint 4. If ownership is unclear, whoever implements combat uses the `WeaponData` assets this plan creates.
- `[TEAM DECISION]` — see the What-I-Should-Do list: **confirm Weapon/Combat owner** and **approve the normalized base values**.

---

## 3. CURRENT PROJECT STATE (VERIFIED)

### What exists today

| Area | Evidence | Notes |
|---|---|---|
| Player entity | `Assets\Scripts\Player\PlayerEntity.cs:7-16,30-60` | `health/maxHealth/baseDamage/baseDefense` + `TakeDamage/Heal/SetMaxHealth` |
| Player stats in scene | `Assets\Scenes\TestingScene.unity:8433-8436` | serialized `100 / 100 / 10 / 5` |
| Player accessor | `Assets\Scripts\Player\PlayerController.cs:13,27` | `Entity` property exposes `PlayerEntity` |
| Enemy entity | `Assets\Scripts\Enemy\EnemyEntity.cs:6-14,25-29` | `health/damage/defense/poise`, `Initialize()`, **`OnDied` event at :22** |
| Enemy FSM | `Assets\Scripts\Enemy\EnemyStateMachine.cs` | generic FSM over `IEstate` — pattern to reuse |
| Enemy cost-based spawn intent | `Assets\Scripts\Enemy\EnemyController.cs:36-43` | design comment already says spawns should be cost-based — we implement it |
| ScriptableObject precedent | `Assets\Scripts\Enemy\PatrolRoute.cs` + `Assets\prefabs\routes\level 1\routeA.asset` | the exact data-asset pattern to copy for `WeaponData` |
| GameManager | `Assets\Scripts\GameManager.cs:7-11` | cursor-lock only — the future bootstrap point for a run |
| Input | `Assets\Scripts\InputController.cs` | static events — how UI/interaction will hook in |
| Weapon model | `Assets\Katana\Sheathe.fbx`, scene object `KatanaWeapon` at `TestingScene.unity:7991` | the only weapon asset present |

### What is missing / broken

| Gap | Evidence | Impact |
|---|---|---|
| **No weapon/combat code** | grep `weapon\|sword\|katana\|spear\|whip` in `Assets\Scripts` → 0 hits | combat system must be built from scratch (Sprint 4) |
| **No XP/level code** | grep `xp\|experience\|level` → 2 hits, both in the design comment at `EnemyController.cs:40-41` | entire economy is greenfield |
| **Enemies can't hurt player** | `DealDamage.cs:14-18` — the damage call is **commented out** | player takes no damage; run can never end → roguelike "loss" is impossible until fixed |
| **Enemy death crashes** | `DieState.cs:11-18` — `agent`/`animator` unassigned → NullReference | enemies can't die cleanly; run progression stalls |
| **No UI scripts** | grep hits none | no HUD/canvas logic at all |

**Critical dependencies (not mine, but they gate my system):**
- `[BLOCKED]` `DealDamage.cs:14-18` — player must be able to take damage for a run to end. Owner: Enemy/Combat dev.
- `[BLOCKED]` `DieState.cs:11-18` — clean death required for drops and floor completion. Owner: Enemy dev.
- The player/enemy `[TEAM DECISION]` on whether to swap raw stats for normalized values is *deferred* — V0.1 keeps my normalized data **internal to the Roguelike layer** and converts at the boundary.

---

## 4. NORMALIZED BASE DATA (THE CORE DESIGN DECISION)

All roguelike stats are stored as **normalized floats in [0, 1]** so upgrades, stacking, and scaling never overflow and stay mathematically simple.

`[PROPOSED — TEAM DECISION]` — initial normalized baselines:

| Stat | Normalized value | Maps to raw (at V0.1 defaults) |
|---|---|---|
| Weapon damage | 0.3 | raw 30 (weapon) |
| Attack speed | 0.5 | raw 0.5 hits/sec |
| Attack range | 0.2 | short melee |
| Crit chance | 0.1 | 10% |
| Crit multiplier | 0.5 | ×1.5 damage |

Player raw stats stay untouched (`[EXISTS]`: 100 HP / 10 dmg / 5 def). The Roguelike layer converts normalized→raw only when handing values to existing code.

**Why normalized instead of raw integers?** Raw-integer stacking (e.g., +5 damage) has to be tuned against every future source of scaling forever. A `[0,1]` scale is bounded by definition — a single clamp guarantees no runaway numbers. This is the foundation the whole economy builds on, so getting it approved early matters.

---

## 5. V0.1 SCOPE (FIRST IMPLEMENTATION)

V0.1 is **deliberately boring**: three files that hold numbers, and nothing else. It is the guaranteed-safe first merge that unblocks every later sprint.

| Deliverable | Type | Contents |
|---|---|---|
| `WeaponType` enum | C# `Assets\Scripts\Roguelike\WeaponType.cs` | `Katana, Spear, Whip` |
| `WeaponData` | C# ScriptableObject `Assets\Scripts\Roguelike\WeaponData.cs` | `type`, `displayName`, `damage/attackSpeed/range/critChance/critMultiplier` (normalized), `cooldown`, `CreateMenu` attribute |
| 3 asset files | `.asset` in `Assets\Roguelike\Data\Weapons\` | `Katana.asset`, `Spear.asset`, `Whip.asset` with values from §4 |

**Explicitly out of V0.1:** combat behaviour, spawning, run manager, drops, economy, UI, save, procedural generation.

**Success criteria for V0.1:**
1. `Create → Roguelike → Weapon Data` menu exists.
2. Three `.asset` files exist, inspector shows normalized values.
3. No compile errors, scene untouched, nothing else changed.

---

## 6. DATA ARCHITECTURE

Follow the exact `PatrolRoute.cs` precedent (`[EXISTS]`).

```
Assets\Scripts\Roguelike\
  WeaponType.cs          enum
  WeaponData.cs          ScriptableObject (CreateAssetMenu)
Assets\Roguelike\Data\Weapons\    *.asset instances
```

`WeaponData` fields (all normalized `[0,1]` unless noted):

```csharp
[CreateAssetMenu(fileName = "WeaponData", menuName = "Roguelike/Weapon Data")]
public class WeaponData : ScriptableObject {
    public WeaponType type;
    public string displayName;
    [Range(0f, 1f)] public float damage;
    [Range(0f, 1f)] public float attackSpeed;
    [Range(0f, 1f)] public float attackRange;
    [Range(0f, 1f)] public float critChance;
    [Range(0f, 1f)] public float critMultiplier;
    public float cooldown;
}
```

- All future roguelike data (upgrades, floors, enemy budgets, drops) follows this same pattern: **ScriptableObject + `.asset` instances + normalized values**.
- No static singletons, no scene dependencies — data assets are loadable from anywhere a future `RunManager` needs them.

---

## 7. SPRINT STRUCTURE (SPRINT 0–11)

Each sprint is one small, reviewable step. Duration suggestion: 2–3 days per sprint for a small team, 1 week for a solo dev.

| Sprint | Deliverable | Depends on | Owner |
|---|---|---|---|
| **0** | Team decisions locked: §4 values approved, Weapon/Combat owner named | — | Team |
| **1** | `WeaponType` enum + `WeaponData` ScriptableObject (compile clean) | 0 | Me |
| **2** | 3 `.asset` weapon files with approved values | 1 | Me |
| **3** | Unblock `DealDamage` + `DieState` so combat loop closes | 0 | Player/Enemy dev |
| **4** | Basic weapon behaviour: equip weapon, deal damage using `WeaponData` | 2, 3 | Weapon/Combat owner |
| **5** | Weapon switching (cycle Katana/Spear/Whip at runtime) | 4 | Weapon/Combat owner |
| **6** | `RunManager`: start/end run, track floor index, hook death detection | 3, 4 | Me |
| **7** | Cost-based spawner implementing `EnemyController.cs:36-43` intent | 6 | Me |
| **8** | Souls/currency drop on `OnDied` (`EnemyEntity.cs:22`) | 6, 7 | Me |
| **9** | Upgrade shop between floors; upgrades mutate normalized stats | 8 | Me |
| **10** | Run rewards + basic meta-progression (unlocks persist across runs) | 9 | Me |
| **11** | HUD + floor transitions + UI polish | 9 | UI dev |

**Why this order:** data → combat → run → spawn → reward. Each sprint ends in a *shippable-but-boring* state: after Sprint 2 the game is unchanged but has the data every other sprint reads; after Sprint 4 you can swing a Katana with real damage numbers.

---

## 8. SPRINT DEPENDENCY GRAPH

```
      0 (decisions)
     / |      \
    1  3       4.owner-name
    2  |        |
    3--+        |
    4 ----------
    5
    6
    7  -> 8 -> 9 -> 10 -> 11
```

Read it as: **Sprint 6 (RunManager) cannot start until both Sprint 3 (working damage/death) and Sprint 4 (working combat) land.** There is no skipping. If 3 slips, everything after 6 slips with it — the graph is a serial chain after Sprint 5.

Parallel lanes possible: `[1→2]` (data) runs in parallel with `[3]` (combat fixes) and `[4→5]` (weapon behaviour), merging at Sprint 6.

---

## 9. TEAM PARALLELIZATION

| Lane | Sprints | Can run while |
|---|---|---|
| **Me (Roguelike)** | 1, 2 → 6, 7, 8, 9, 10 | others build combat |
| **Player/Enemy dev** | 3 (fix DealDamage, DieState) | I build data |
| **Weapon/Combat owner** | 4, 5 | I build data + RunManager |
| **UI dev** | 11 (also preps canvas prefabs in parallel) | any |

- Sprints 1–2 and 3 can start immediately and in parallel — they touch disjoint folders.
- Sprint 4 needs both 2 and 3, so the first true merge point is where parallelism re-converges.
- Rule: **disjoint folder = parallel-safe**. My files live under `Assets\Scripts\Roguelike\` and `Assets\Roguelike\`, no other owner touches them.

---

## 10. INTER-SYSTEM CONTRACTS

These are the stable seams; both sides code against the signature, not each other.

1. **Roguelike → Player:** `PlayerController.Entity` (property, `[EXISTS] PlayerController.cs:27`). Roguelike reads/writes stats through this. **Player dev keeps this property stable.**
2. **Enemy → Roguelike (drops):** `EnemyEntity.OnDied` (event, `[EXISTS] EnemyEntity.cs:22`). Roguelike subscribes to award souls. **Enemy dev keeps this event name/signature stable.**
3. **Roguelike → Spawns:** `EnemyController`'s spawn API (`[EXISTS] EnemyController.cs:36-43` comment defines the intent). Roguelike supplies a budget, enemy controller places units. **Contract: `SpawnWithBudget(float cost)`.**
4. **Weapon → WeaponData:** combat code reads normalized fields from `WeaponData` (`[PROPOSED] §6`). **Roguelike keeps field names stable.**

Any change to a contract requires a team heads-up **before** merge, not after.

---

## 11. FIRST IMPLEMENTATION (SPRINTS 0–2, DETAILED)

### Sprint 0 — Decisions (blocking, quick)
1. Approve §4 normalized values (or amend them now — changing later is expensive).
2. Name the Weapon/Combat owner.
3. Agree contract signatures from §10.

### Sprint 1 — Code files
- Create `Assets\Scripts\Roguelike\WeaponType.cs` (enum: `Katana, Spear, Whip`).
- Create `Assets\Scripts\Roguelike\WeaponData.cs` (ScriptableObject per §6).
- Verify: menu `Create → Roguelike → Weapon Data` appears; project compiles with **zero errors**; no existing file modified.

### Sprint 2 — Data assets
- Create `Assets\Roguelike\Data\Weapons\Katana.asset`, `Spear.asset`, `Whip.asset`.
- Fill values from §4 (amend as per Sprint 0 decision).
- Verify: 3 assets exist in `Assets\Roguelike\Data\Weapons\`; each opens in inspector with the normalized fields; scene untouched.

**Suggested commit each sprint ends with:** one small commit, message matching the repo style (check `git log` for precedent).

---

## 12. TESTING STRATEGY

Because V0.1 is data-only, testing is lightweight now and ramps up later.

| Sprint | Test method |
|---|---|
| 1–2 | Compile check + manual inspector inspection + one placeholder use in a throwaway scene (never committed) |
| 3 | Play test: enemy hits player → HP drops; player kills enemy → no crash, enemy despawns cleanly |
| 4–5 | Play test: equip each weapon, confirm damage/range/speed visibly differ |
| 6 | Run test: die → run ends → prompt; survive floor → next floor starts |
| 7 | Play test: floor spawns a *fixed total cost* of enemies every time |
| 8 | Kill enemy → souls counter increments |
| 9 | Spend souls → upgrade persists for the rest of the run |
| 10–11 | Full run loop test: start → 3+ floors → death → meta-unlock visible |

- No test framework exists in the project; don't introduce one for V0.1. Manual play tests are the standard until volume demands automation.
- Regression rule: **every sprint ends by replaying the previous sprint's test list** — the chain never backslides silently.

---

## 13. GIT / TEAM WORKFLOW

- Repo root has a `.git` (fresh — nothing committed yet). `docs\`, `Assets\`, `Packages\`, `ProjectSettings\` are the relevant top-level folders.
- Recommend: `main` protected; each sprint = one branch (`roguelike/v0.1`, `weapon-combat`, etc.); merge via PR with the sprint's verify checklist as the PR body.
- Data assets (`.asset`) are YAML text in Unity — they diff cleanly; **do not** add them to `.gitignore`.
- Never commit `Library/` (Unity cache) or `.vs/`, `.vscode/` unless the team already decided otherwise.
- Check the existing repo history (`git log --oneline`) before writing commit messages to match style.

---

## 14. RISK REGISTER

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Combat/weapon owner never confirmed | High | Sprint 4+ stalls | Resolve in Sprint 0; if no owner, one person must take it |
| `DealDamage`/`DieState` fixes slip | Medium | Sprints 6+ stall | Surface early; graph is serial after 5 |
| Team dislikes normalized stats | Medium | Rework of §4 values | Decide in Sprint 0, before any code uses them |
| Contract drift (property/event renames) | Low | Runtimes break silently | §10 headsup rule; PR review checks signatures |
| Unity YAML merge conflicts on `.asset` | Low | Annoying | Keep weapons on one owner's branch at a time |

---

## 15. DEFINITION OF DONE (PER SPRINT)

A sprint is DONE when **all** of these hold:
1. Its deliverables exist in the repo (not just locally).
2. Project compiles with zero errors.
3. The previous sprint's tests still pass (no regression).
4. No files outside its assigned folder scope were modified.
5. A short "what I did / what's next" note is posted for the team.

---

## 16. FUTURE EXTENSION (BEYOND V0.1 / BEYOND SPRINT 11)

- **Procedural floors** — new rooms via cost-budget templates; needs a room tile set (`[MISSING]`).
- **Rogue-lite meta** — persistent unlock tree; needs save system (`[MISSING]`).
- **Relic/affix system** — modify normalized stats at runtime via multipliers; trivial once stats are normalized.
- **Advanced weapon families** — more types beyond Katana/Spear/Whip; just new `.asset` files + enum values.
- **Enemy variety** — new enemies are new data entries + prefabs; no system change.
- Full design rationale for all of these lives in `ROGUELIKE_SYSTEM.md`.

---

## 17. BEGINNER GLOSSARY

- **ScriptableObject**: a Unity data container that lives in the Project window as a `.asset`; safe way to store game data without scenes.
- **Normalized value**: a number from 0 to 1 (e.g., 0.5 = "half").
- **Budget**: a total "cost" a floor can spend spawning enemies; expensive enemies consume more budget.
- **Roguelike vs roguelite**: roguelike = full reset on death; roguelite = small permanent unlocks persist (what this plan's end state is).
- **Meta-progression**: permanent unlocks that survive death, earned across runs.
- **Merge point**: the sprint where two parallel lanes recombine; everything before it is independently mergeable.

---

## 18. ONE-PAGE ROADMAP

```
V0.1  [S0-S2]  Decisions → WeaponData code → 3 weapon assets      ← FIRST IMPLEMENTATION (data only)
V0.2  [S3-S5]  Combat works (damage+death) → weapons equip & swap
V0.3  [S6-S8]  RunManager → cost-based spawns → souls drops
V0.4  [S9-S11] Upgrade shop → meta-progression → HUD & polish
```

**V0.1 today = three small files holding numbers, nothing else.**
**V0.4 = a playable roguelite loop: floor → upgrade → floor → death → unlock.**

---

# WHAT I SHOULD DO TOMORROW

1. **Get Sprint 0 approved in the team chat:** paste the §4 normalized-value table and ask the team to approve or amend it before any code is written.
2. **Confirm the Weapon/Combat owner** with the team (responsibility matrix, §2) — nothing beyond data can move without this.
3. **Create the V0.1 code files** (`WeaponType.cs` + `WeaponData.cs` under `Assets\Scripts\Roguelike\`), verify the `Create → Roguelike → Weapon Data` menu works and the project compiles with zero errors.
4. **Create the three `.asset` weapon files** under `Assets\Roguelike\Data\Weapons\` using the approved values, and confirm the scene is untouched.
5. **Post the dependency warning:** `DealDamage.cs:14-18` and `DieState.cs:11-18` must be fixed by their owners before Sprint 6 — flag it now so it's not a surprise at the merge point.
