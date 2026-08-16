# Roguelike System — Implementation Plan (v0.1)

**Owner:** Roguelike System developer
**Scope of this document:** practical, incremental plan for building the Roguelike System in a team project.
**Base document:** `docs/ROGUELIKE_SYSTEM.md` (high-level design — NOT all of it is approved or planned for now).

> **Status note (cleanup branch `fix/roguelike-spawn-cleanup`):** the `WeaponData` ScriptableObject / `WeaponType` enum described in this plan were implemented then **removed as obsolete** — the merged Combat system ships the real weapon abstraction (`Assets/Scripts/Combat/Objects/WeaponObject.cs` + `AttackData.cs`, `KatanaWeapon.prefab`, `BasicTestingSword.asset`), and `WeaponData`/`WeaponType` had zero references. The weapon-stats parts of this plan should be read as history, not as the current design.

> **How to read this document**
> Every claim about existing code is labeled:
> - **[EXISTS]** — present in the repo today, with a file reference.
> - **[PARTIAL]** — present but incomplete/broken.
> - **[MISSING]** — absent from the repo.
> - **[PROPOSED]** — suggested by this plan, not yet approved.
> - **[TEAM DECISION]** — must be answered by the team before implementation.
> - **[FUTURE]** — explicitly deferred to a later version.

---

## 1. Responsibility Boundary

The Roguelike System is **not** the Player system and **not** the Enemy system. Its job is the *run structure and progression data* — not how the character moves, not how enemies think.

| System | Owner | What Roguelike needs from it | What Roguelike must NOT own |
|---|---|---|---|
| **Player System** | Player dev | Runtime player state (`PlayerEntity`), the equipped-weapon reference, combat/attack behavior | PlayerController, PlayerContext (movement), PlayerEntity, jump/dash/wallrun states. Do NOT edit movement code. |
| **Enemy System** | Enemy dev | `EnemyEntity` stats, `OnDied` event (for future drops), an enemy prefab to spawn | EnemyController, enemy states, poise/stagger, attack states. Do NOT edit AI code. |
| **UI System** | UI dev | Screens: HUD, upgrade-choice screen, game-over summary. A trigger event from Roguelike when a choice is shown | Canvas layout, styling, animations, input for menus. Roguelike provides the *data* + the *event*; UI dev builds the panels. |
| **Core / GameManager** | Core dev | Bootstrap (`GameManager`), scene lifecycle, cursor lock | Scene loading, build settings, GameManager itself |
| **Weapon / Combat (runtime)** | **[TEAM DECISION]** — likely Player dev | Equips `WeaponData`, applies its damage/range/speed at attack time | The *swing/attack logic*. Roguelike only owns the `WeaponData` *data container*. |
| **Roguelike System** | **You** | Runs, upgrades, spawn budgets, meta-progression, save data | — |

**Key boundary rule:** Roguelike owns **data containers and progression flow** (`WeaponData`, `ItemDefinition`, `RunData`, upgrade selection). It does **not** own the code that makes the player swing a sword or the enemy chase.

---

## 2. Current Project State

All claims verified against the repo at `E:\course\03_Programming_Data\game_dev\projects\3D-roguelike-hack_n_slash`.

### 2.1 What already exists

| Item | Status | Evidence |
|---|---|---|
| `PlayerEntity` — health, maxHealth, baseDamage, baseDefense + `TakeDamage/Heal/SetMaxHealth` | **[EXISTS]** | `Assets\Scripts\Player\PlayerEntity.cs:7-16,30-60` |
| Player stats serialized in the scene | **[EXISTS]** | `TestingScene.unity:8433-8436` (`health:100, maxHealth:100, baseDamage:10, baseDefense:5`) |
| `PlayerController` holds a `PlayerEntity` and exposes it as `Entity` | **[EXISTS]** | `PlayerController.cs:13,27` |
| `EnemyEntity` — health, damage, defense, poise + `Initialize()` | **[EXISTS]** | `Assets\Scripts\Enemy\EnemyEntity.cs:6-14,25-29` |
| Enemy stats serialized in the scene | **[EXISTS]** | `TestingScene.unity:12344-12345` (`maxHealth:100, baseDamage:20`); inactive enemy zeroed at `:14699-14700` |
| `EnemyEntity.OnDied` event (future drops hook) | **[EXISTS]** | `EnemyEntity.cs:22` |
| Patrol routes (ScriptableObject pattern to copy) | **[EXISTS]** | `Assets\prefabs\routes\level 1\routeA.asset` + `PatrolRoute.cs` |
| NavMesh baked for the current arena | **[EXISTS]** | `Assets\Scenes\TestingScene\NavMesh-navMesh Settings.asset` |
| Katana 3D model + materials (visual only) | **[EXISTS]** | `Assets\Katana\Sheathe.fbx`, `KatanaWeapon` GameObject at `TestingScene.unity:7991` |

### 2.2 What is partially implemented

| Item | Status | Evidence |
|---|---|---|
| Damage to the player | **[PARTIAL — commented out]** | `DealDamage.cs:14-18` — the `if()` + `TakeDamage` call are commented out. **The player cannot currently take damage.** |
| Enemy death | **[PARTIAL — crashes]** | `DieState.cs:11-18` — `agent`/`animator` never assigned → NullReferenceException on death. |
| Player combat/weapon behavior | **[PARTIAL — absent]** | No weapon/attack script exists in `Assets\Scripts` (grep for weapon/sword/katana/spear/whip: **0 hits**). |

### 2.3 What is missing

| Item | Status | Evidence |
|---|---|---|
| Any weapon data (Damage/Range/AttackSpeed) | **[MISSING]** | grep `weapon|sword|katana|spear|whip` in `Assets\Scripts` → no matches |
| Weapon `ScriptableObject` or weapon prefab wiring | **[MISSING]** | Only 2 gameplay `.asset` files exist: `routeA.asset`, `NavMesh-navMesh Settings.asset` |
| XP / level / level-up flow | **[MISSING]** | grep `xp|experience|level` → only 2 hits, both in an enemy-design comment (`EnemyController.cs:40-41`) |
| Upgrade / item / meta / save systems | **[MISSING]** | No such code or assets |
| A GDD file in the repo | **[MISSING]** | glob `**/*.md` → only `README.md` + `docs/*.md`; weapon families (Katana/Spear/Whip) are team knowledge, not in the repo |

### 2.4 What my system can safely depend on

- `PlayerEntity` as the player's **runtime** stat holder **[EXISTS]** — but it is owned by the Player dev; I must **ask before changing it**.
- The ScriptableObject pattern shown by `PatrolRoute.cs` — the cleanest precedent for `WeaponData`.
- `PlayerController.Entity` accessor (`PlayerController.cs:27`) as the eventual way to reach player stats.
- The design doc `docs/ROGUELIKE_SYSTEM.md` (approved direction, not yet implemented).

### 2.5 What is broken and blocks my system

| Blocker | Evidence | Impact on Roguelike |
|---|---|---|
| No player damage possible | `DealDamage.cs:14-18` | A roguelike needs the player to be able to lose. Blocks any "run ends on death" logic. |
| Enemy death crashes | `DieState.cs:11-18` | Blocked rooms can never be cleared. Blocks "floor cleared" logic. |
| No combat runtime / no weapon behavior | no weapon code | `WeaponData` can exist as **pure data** without it, but it cannot be *verified end-to-end* until combat exists. |

### 2.6 What is NOT ready yet

- Procedural floors, economy, meta progression, save system, multipliers, random upgrade choices — all **[FUTURE]** (see §15). The design doc proposes them; the team has not approved them and nothing in the repo requires them yet.

---

## 3. MVP Definition — Version 0.1

**V0.1 = Base Stats / Base Weapon Data only.**

V0.1 is intentionally tiny: it produces the **data foundation** that later versions build on. No gameplay behavior changes.

| In scope for V0.1 | NOT in scope for V0.1 |
|---|---|
| `WeaponData` ScriptableObject class **[PROPOSED]** | Multipliers |
| `WeaponData` assets: Katana, Spear, Whip (values `TBD — Team Decision`) | Procedural generation |
| Base player stats — documented and mapped to existing `PlayerEntity` | Economy |
| A `WeaponType` enum (Katana/Spear/Whip) | Meta progression |
| A team-agreed stats table (§4) | Save system |
| A runtime *read-only* check that the data loads | Complex randomization |
| | Advanced upgrade stacking |
| | Full run state machine (`RunManager`, `RunData`, states) |

**Why this is the right MVP:** the project already has `PlayerEntity` with base stats and a ScriptableObject precedent (`PatrolRoute`). V0.1 reuses both patterns and produces one self-contained, reviewable deliverable that unblocks the team's weapon/combat decisions — without touching anyone else's code.

---

## 4. Base Stats Design

Every stat below is justified by existing code or by an explicit team need. **No numbers are invented here** — values are `TBD — Team Decision` where the repo/GDD doesn't define them.

### 4.1 Player Stats

| Stat | Why it exists | Owner | Where it exists today | V0.1 required? | Postponed? |
|---|---|---|---|---|---|
| Health (current) | Player must be able to take damage and die | Player dev | `PlayerEntity.cs:7` `health`, serialized `TestingScene.unity:8433` | Yes (reuse as-is) | — |
| Max Health | Damage/heal clamping, upgrades | Player dev | `PlayerEntity.cs:8` `maxHealth` | Yes (reuse as-is) | — |
| Base Damage | Base offensive value that upgrades will build on | **[TEAM DECISION]** — Player or Roguelike | `PlayerEntity.cs:9` `baseDamage`, `TestingScene.unity:8435` | Yes (reuse) | — |
| Base Defense | Damage-reduction input (`damage - baseDefense * ALPHA`) | Player dev | `PlayerEntity.cs:10` `baseDefense`, `Constants.cs` `ALPHA` | Yes (reuse) | — |
| Move Speed | Movement tuning | Player dev | `PlayerContext.cs:22-28` `walkSpeed/sprintSpeed` | No | Postpone — owned by Player dev |

### 4.2 Weapon Stats (all in the new `WeaponData`)

| Stat | Why it exists | Owner | Where it exists today | V0.1 required? | Postponed? |
|---|---|---|---|---|---|
| Damage | Core weapon value; upgrade target | **[TEAM DECISION]** | **[MISSING]** | Yes (as data, value TBD) | — |
| Range | Distinguishes Katana (short) vs Spear (long) vs Whip (mid) | **[TEAM DECISION]** | **[MISSING]** | Yes (as data, value TBD) | — |
| Attack Speed | Distinguishes weapon feel; upgrade target | **[TEAM DECISION]** | **[MISSING]** | Yes (as data, value TBD) | — |

**Important:** these are **data fields on a ScriptableObject** in V0.1. Nothing consumes them until combat exists (owned by Player dev). That is fine — data first, behavior later.

### 4.3 Enemy Stats

| Stat | Why it exists | Owner | Where it exists today | V0.1 required? | Postponed? |
|---|---|---|---|---|---|
| Max Health | Enemy durability | Enemy dev | `EnemyEntity.cs:7`, `TestingScene.unity:12344` | No | Postpone — Enemy dev's territory |
| Base Damage | Enemy threat | Enemy dev | `EnemyEntity.cs:8`, `TestingScene.unity:12345` | No | Postpone |
| Poise | Stagger system | Enemy dev | `EnemyEntity.cs:11-13` | No | Postpone |
| OnDied event | Future drops/scoring | Enemy dev | `EnemyEntity.cs:22` | No (note the hook) | Postpone |

Roguelike **reads** these later (scaling, drops) but never **owns** them.

### 4.4 Roguelike-specific Stats

| Stat | Why it exists | Owner | Where it exists today | V0.1 required? | Postponed? |
|---|---|---|---|---|---|
| XP / XP Reward | Level-up flow | **[TEAM DECISION]** | **[MISSING]** (no XP code) | No | Postpone — nothing requires it yet; ask team about intended flow (§11) |
| Floor / Run progress | Which floor you're on | You (Roguelike) | **[MISSING]** | No | Postpone to V0.3+ (`RunData`) |
| Upgrade count / stacks | Upgrade progression | You (Roguelike) | **[MISSING]** | No | Postpone to V0.2+ |

**XP is explicitly not invented here** — the project has zero XP code and the team has not defined a level-up flow. It stays **[TEAM DECISION]/[MISSING]**.

---

## 5. Weapon Data

The GDD (team knowledge) defines three weapon families. **No numerical values exist in the repo and none are invented here.**

| Weapon | Damage | Range | Attack Speed | Source / Decision Status |
|---|---|---|---|---|
| Katana (Sword) | `TBD — Team Decision` | `TBD — Team Decision` | `TBD — Team Decision` | Family defined by GDD; numbers undefined. Model exists (`Assets\Katana\Sheathe.fbx`). |
| Spear | `TBD — Team Decision` | `TBD — Team Decision` | `TBD — Team Decision` | Family defined by GDD; numbers undefined. No model in repo. |
| Whip | `TBD — Team Decision` | `TBD — Team Decision` | `TBD — Team Decision` | Family defined by GDD; numbers undefined. No model in repo. |

**Values the team must decide BEFORE implementation:**
1. Katana: damage, range, attack speed.
2. Whether Spear/Whip should even have V0.1 assets yet (models are missing — data can still be created).
3. Whether **Range** and **Attack Speed** are weapon stats (recommended) or player stats.
4. Whether **Damage** lives on the weapon or on the player (currently `PlayerEntity.baseDamage` exists — decide if that stays as the player base and the weapon adds on top, or if the weapon fully replaces it).

---

## 6. Data Ownership

| Data | Recommended owner | Reason |
|---|---|---|
| `PlayerEntity` | Player dev (runtime) | Already exists, already serialized in the scene, already owned |
| `Weapon` (equipped weapon *instance*/reference) | Player dev (runtime) | It's what the player holds; only the player dev knows how it attaches |
| `WeaponData` (ScriptableObject) | **You (Roguelike)** | It is *configuration data*; upgrades (Roguelike) must be able to reference and modify it. Creating the SO in Roguelike keeps one owner of progression data. **[PROPOSED]** |
| `ItemDefinition` / `UpgradeData` | **You (Roguelike)** | The upgrade system is your system |
| `RunData` | **You (Roguelike)** | Current-run temporary state |

**The split that matters:** `WeaponData` = *static base configuration* (a `.asset` file). `PlayerEntity` = *runtime player state* (a component field). An equipped weapon connects the two; the connection lives where the Player dev puts it, but it **reads** `WeaponData` that you own.

---

## 7. V0.1 Data Model

```
WeaponType (enum: Katana, Spear, Whip)          [PROPOSED]
    |
    └── used by
WeaponData (ScriptableObject)                   [PROPOSED]
    ├── weaponType  : WeaponType
    ├── displayName : string
    ├── damage      : float   (TBD)
    ├── range       : float   (TBD)
    └── attackSpeed : float   (TBD)

Player (runtime, owned by Player dev)           [EXISTS]
    └── Equipped Weapon  ──►  WeaponData        [PROPOSED connection]
            └── (reads damage/range/attackSpeed later, when combat exists)

PlayerEntity (runtime stats)                    [EXISTS — untouched in V0.1]
    └── health / maxHealth / baseDamage / baseDefense
```

That is the entire V0.1 model: **one enum, one ScriptableObject, three assets.**

---

## 8. Upgrade System — Future Compatibility

V0.1 must be shaped so V0.2/V0.3 can grow into:

```
Final Value = (Base Value + Flat Bonuses) × Multipliers
```

**What V0.1 does:**
- Store each stat as a **single plain `float`** on `WeaponData` (e.g. `damage`).
- Give each field a **clear, stable name** (Damage, Range, AttackSpeed) so a future modifier system has clean keys to target.

**What V0.1 must NOT do (postpone):**
- No multiplier fields (`damageMultiplier`, `rangeMultiplier`).
- No modifier **arrays** / `StatModifier` lists on `WeaponData`.
- No "base + bonus" split fields inside `WeaponData`.
- No stat-modifier system like the one sketched in `ROGUELIKE_SYSTEM.md` §4.6.

**Why postpone:** flat plain fields are the simplest thing compatible with the project. Adding arrays/modifiers now would create unused complexity, and there is no combat code to consume them anyway. When V0.2 needs flat upgrades, the upgrade system adds `(flatBonus)` and sums into the base — the `WeaponData` fields don't change. When V0.4 needs multipliers, a separate layer multiplies the summed result. **The data stays stable; the layers around it grow.**

The existing `PlayerEntity` already hints at this future: it has `addedDamage[]`, `damageMultipliers[]`, etc. (`PlayerEntity.cs:12-15`) — but they are unused. **Do not copy that pattern into V0.1.** Leave `PlayerEntity` alone (Player dev's file).

---

## 9. Implementation Phases

Each phase is small, reviewable, and does not require the next.

### Phase 0 — Team Decisions
- **Goal:** resolve the `[TEAM DECISION]` items so V0.1 can start.
- **Files:** none.
- **Dependencies:** none.
- **Result:** a signed-off stats table (§4, §5).
- **Definition of Done:** §11 checklist answered; weapon table has values or explicit "TBD, approve these three defaults to start".
- **NOT yet:** any code.

### Phase 1 — Base Stats
- **Goal:** agree and record the base stats tables (§4).
- **Files to create:** `docs/STATS.md` (team-facing table) **[PROPOSED]**.
- **Existing files to touch:** none.
- **Dependencies:** Phase 0.
- **Result:** a single source of truth for stats.
- **Definition of Done:** every stat has owner + V0.1/Postponed label.
- **NOT yet:** weapon assets, upgrades.

### Phase 2 — Weapon Data (the real V0.1 build)
- **Goal:** create `WeaponData` ScriptableObject + assets.
- **Files to create:** `Assets\Scripts\Roguelike\Data\WeaponType.cs`, `WeaponData.cs`; `Assets\Data\Weapons\Katana.asset`, `Spear.asset`, `Whip.asset` **[PROPOSED]**.
- **Existing files to touch:** none (Player/Enemy files stay untouched).
- **Dependencies:** Phase 1 values.
- **Result:** three weapon assets in the Project window with correct fields.
- **Definition of Done:** assets exist, fields populated, and they show up in the Inspector (verified by §14 tests).
- **NOT yet:** connecting the weapon to the player, combat, upgrades.

### Phase 3 — First Upgrade Data
- **Goal:** prove the data structure can support flat upgrades.
- **Files to create:** `Assets\Scripts\Roguelike\Data\UpgradeData.cs` (flat fields only: e.g. `flatDamageBonus`) + first `.asset` **[PROPOSED]**.
- **Existing files to touch:** none.
- **Dependencies:** Phase 2.
- **Result:** an upgrade asset that references a weapon stat.
- **Definition of Done:** upgrade asset exists and its value is visible.
- **NOT yet:** applying it to anything at runtime.

### Phase 4 — Connect Upgrade to Runtime Stats
- **Goal:** apply flat bonuses at runtime (first real behavior).
- **Files to create:** a small runtime applier in Roguelike **[PROPOSED]**.
- **Existing files to touch:** likely `PlayerEntity` or wherever the Player dev exposes stats — **requires Player dev coordination**.
- **Dependencies:** Phase 3 + Player dev agreement.
- **Result:** picking up an upgrade changes a real stat (visible in HUD/debug).
- **Definition of Done:** stat change verifiable in Play Mode.
- **NOT yet:** upgrades being random, run-scoped.

### Phase 5 — Run Integration
- **Goal:** a minimal run frame (`RunData`, simple `RunManager`).
- **Files to create:** `RunData.cs`, `RunManager.cs` **[PROPOSED]**.
- **Existing files to touch:** `GameManager.cs` (bootstrap) — **coordinate with Core dev**.
- **Dependencies:** Phase 4.
- **Result:** a run start/end/restart flow exists.
- **Definition of Done:** start a run, upgrade, die/end, restart — all working.
- **NOT yet:** floors, procedural generation, economy.

### Phase 6 — Random Upgrade Choices
- **Goal:** roll 3 upgrades after a floor clear.
- **Files to create:** upgrade UI trigger + pool **[PROPOSED]**.
- **Existing files to touch:** none (UI dev builds panels).
- **Dependencies:** Phase 5, UI dev.
- **Definition of Done:** clearing triggers a 3-choice screen; picking one applies it.

### Phase 7 — Multipliers
- **Goal:** add the `× multipliers` layer.
- **Files to create:** modifier layer in Roguelike **[PROPOSED]**.
- **Definition of Done:** `(base + flat) × mult` verifiable. **(Do NOT build this in V0.1.)**

### Phase 8 — Economy
### Phase 9 — Meta Progression
### Phase 10 — Procedural Floors
- **Definition of Done (each):** as defined in `docs/ROGUELIKE_SYSTEM.md`; **all [FUTURE]** — do not schedule before Phases 0–6 are done.

---

## 10. FIRST TASK ONLY

Do exactly this tomorrow. One sitting. Nothing below changes gameplay code.

### 1. What to discuss with the team (5 min)
- Tell them: "I'm creating `WeaponData` ScriptableObjects as the base for upgrades. I need weapon stat values and the ownership of weapon damage."

### 2. What decisions must be made
- Who owns `WeaponData` (recommend: Roguelike).
- Whether Damage/Range/AttackSpeed are weapon stats (recommend: yes).
- Initial Katana values (start with team-approved placeholders).

### 3. What document/table to prepare
- A draft of the §5 weapon table with `TBD — Team Decision` filled in for Katana. Bring it to the meeting.

### 4. What files to inspect (read-only)
- `Assets\Scripts\Player\PlayerEntity.cs` — the stat holder you must not break.
- `Assets\Scripts\Enemy\PatrolRoute.cs` — the ScriptableObject pattern to copy.
- `Assets\prefabs\routes\level 1\routeA.asset` — what a data asset looks like.
- `docs\ROGUELIKE_SYSTEM.md` + `docs\ARCHITECTURE.md` — your design + architecture references.

### 5. What code NOT to touch yet
- `Assets\Scripts\Player\**` (Player dev)
- `Assets\Scripts\Enemy\**` (Enemy dev)
- `GameManager.cs` (Core dev)
- `DealDamage.cs`, `DieState.cs` (Enemy/Combat fixes — note them as blockers, don't fix them)

### 6. Definition of Done (all must be true)
- [ ] Team agreed on who owns `WeaponData` and the Katana placeholder values.
- [ ] `Assets\Scripts\Roguelike\Data\WeaponData.cs` created (enum + ScriptableObject) **[PROPOSED — requires team approval of the plan]**.
- [ ] Three `.asset` files created (Katana/Spear/Whip) with agreed fields.
- [ ] All three assets display correct values in the Inspector.
- [ ] No existing gameplay file was modified.
- [ ] Commit message reflects "V0.1 weapon data foundation".

---

## 11. Team Questions

Checklist for the next team meeting. Only relevant questions — each maps to a decision the plan needs.

| # | Question | Why it matters | Related to |
|---|---|---|---|
| 1 | Who owns weapon stats (Player dev or Roguelike)? | Determines the §1 boundary | §1, §6 |
| 2 | Who owns player stats (`PlayerEntity`)? | I must not touch it without consent | §1, §4.1 |
| 3 | Are weapon stats fixed per weapon, or upgradeable? | If upgradeable, `WeaponData` stays read-only and upgrades live elsewhere | §6, §8 |
| 4 | What does an upgrade modify? (damage only? range? speed?) | Defines `UpgradeData` fields | §8 |
| 5 | Are upgrades run-scoped (reset each run)? | Determines whether `RunData` needs an upgrade list | §9 Phase 5 |
| 6 | When does the player receive an upgrade? (floor clear? XP? both?) | Defines the trigger event | §9 Phase 6 |
| 7 | Are Katana/Spear/Whip balanced differently, or same family? | Validates Range/AttackSpeed as distinguishing stats | §5 |
| 8 | What are the initial base values for Katana (and Spear/Whip)? | Unblocks V0.1 assets | §5 |
| 9 | Should Range be a weapon stat? | Confirms the §4.2 table | §4.2 |
| 10 | Should Attack Speed be a weapon stat? | Confirms the §4.2 table | §4.2 |
| 11 | What is the intended XP/level-up flow? | XP is **[MISSING]**; decides whether I build it | §4.4 |
| 12 | Who owns XP? | Avoids duplicating Enemy-death hooks | §4.4, §1 |
| 13 | Who triggers the upgrade screen? (Roguelike event vs UI listens?) | Defines the Roguelike→UI event | §1, §9 Phase 6 |
| 14 | Are Phases 7–10 (`ROGUELIKE_SYSTEM.md`) approved to schedule later, or on hold? | Sets expectations for the roadmap | §15 |

---

## 12. Dependency Map

```
WeaponData (ScriptableObject) ──────────────── [NEW] — you
    ↓
WeaponType enum ────────────────────────────── [NEW] — you
    ↓
Katana / Spear / Whip .asset ───────────────── [NEW] — you  (values [TEAM DECISION])
    ↓
Equipped weapon reference ──────────────────── [NEEDS CHANGE] — Player dev
    ↓
PlayerEntity (runtime stats) ───────────────── [EXISTS] — Player dev (UNTOUCHED in V0.1)
    ↓
Player / Combat attack behavior ────────────── [BLOCKED] — no combat code exists yet
    ↓
UpgradeData ────────────────────────────────── [NEW] — you, Phase 3
    ↓
Runtime stat application ───────────────────── [NEEDS CHANGE] — requires Player dev
    ↓
RunManager / RunData ───────────────────────── [NEW] — you, Phase 5+
```

Legend: `[EXISTS]` = in repo; `[NEEDS CHANGE]` = exists, requires modification by its owner; `[NEW]` = to be created; `[TEAM DECISION]` = blocked on the team; `[BLOCKED]` = cannot proceed until a dependency exists.

**The one hard dependency:** V0.1 (`WeaponData` data) does NOT depend on anything that is `[BLOCKED]`. You can build and verify it entirely in the Inspector.

---

## 13. Beginner Implementation Guide

Everything below is **for Phase 2** (V0.1 build). It explains the Unity concepts behind each step.

**Concept first — ScriptableObject vs Prefab vs plain field:**
- A **ScriptableObject** is a *data file* stored in the Project, independent of any scene. Perfect for weapon stats: one Katana asset can be reused everywhere, and editing the asset updates every user. (The project already does this for patrol routes — see `routeA.asset`.)
- A **prefab** is a *reusable GameObject* (models, colliders, components). The Katana model is a prefab/FBX, not data about *damage*.
- A **serialized field** (`[SerializeField] float damage;`) is a value that shows in the Inspector and is saved with its owner.

### Steps (in Unity)

1. **Create the folder.** In the Project window, right-click → Create → Folder: `Assets\Scripts\Roguelike\Data`. *Why:* keeps your code separate from Player/Enemy code so you never collide with teammates' folders.
2. **Create the enum + ScriptableObject class.** Create `WeaponType.cs` and `WeaponData.cs` as described in §7. *Why:* `[CreateAssetMenu]` on the class is what lets you create assets in step 3. The class is just a blueprint — no behavior yet.
3. **Create the asset through the Editor.** Right-click `Assets\Data` → Create → Roguelike → WeaponData. *Why:* this generates a `.asset` file — an instance of your data class. (This mirrors how `routeA.asset` is an instance of `PatrolRoute`.)
4. **Create a Katana asset.** Name it `Katana`. *Why:* each weapon family gets its own asset so they can hold different numbers.
5. **Assign values.** With `Katana.asset` selected, set Damage / Range / AttackSpeed in the Inspector. *Why:* serialized fields appear here; the values are saved to the `.asset`, not to any scene.
6. **Connect the asset to the weapon.** *Later phase, after team decision:* the Player dev adds a `WeaponData` reference to the player's weapon object. *Why:* data only becomes a *weapon* when something holds it. In V0.1 you don't need this — the asset is verifiable on its own.
7. **Enter Play Mode.** *Why:* you confirm the asset still loads at runtime (ScriptableObjects are loaded with the scene/project; a null reference here would mean a bad asset).
8. **Verify runtime values.** Use a debug log or the existing debug Text pattern to print `weaponData.damage`. *Why:* proves the data path works end-to-end before any real gameplay uses it.

**The difference that matters:** step 3–5 create **data** (a file). Step 6 is the first **connection** (a reference). They are separate actions — doing them in this order keeps V0.1 reviewable.

---

## 14. Test Plan — V0.1

| Test | What to check | How | Pass criteria |
|---|---|---|---|
| Inspector visibility | The asset shows all fields | Select `Katana.asset` | Damage/Range/AttackSpeed/WeaponType visible and editable |
| Asset independence | Spear/Whip hold different values | Select each asset | Each has its own stored values; editing one doesn't change another |
| Play Mode load | Assets survive entering Play Mode | Open scene → Play | No missing-reference warnings in Console |
| Runtime read | Data is readable from a script | Temporary `Debug.Log(weaponData.damage)` (remove after test) | Prints the Inspector value |
| Reference correctness | No `Missing (Script)` on the assets | Select asset in Inspector | No missing-script header |
| Switching weapons | The *same* class serves different values | Create Katana + Spear asset, log both | Different values for different assets |
| Data asset change propagation | Editing the asset updates the *same* weapon everywhere | Change `Katana.asset` damage → reload/re-enter Play | All consumers (once connected) see the new value |

**What these tests prove:** that the data layer is sound (assets, serialization, runtime load) — which is the entire goal of V0.1. It does **not** test combat, because combat doesn't exist yet.

---

## 15. Future Roadmap

Adjusted from the generic V0.x ladder to match the **actual** project state (blockers at V0.2, no combat code):

```
V0.1  Base weapon data + base stats        ← YOU ARE HERE
        [WeaponData SO + 3 assets; no gameplay changes]
    ↓
V0.2  Flat upgrades                        [blocked by: combat/equip code]
        [UpgradeData: base + flatBonus]
    ↓
V0.3  Run-scoped upgrade selection         [needs RunData, upgrade trigger]
    ↓
V0.4  Multipliers                          [(base + flat) × mult]
    ↓
V0.5  Run Manager (start/end/restart)      [needs GameManager coordination]
    ↓
V0.6  Enemy scaling / spawn budget         [reads EnemyEntity — Enemy dev territory]
    ↓
V0.7  Economy (drops/currency)             [needs EnemyEntity.OnDied — exists]
    ↓
V0.8  Meta progression + save system
    ↓
V0.9  Procedural floors                    [highest risk: runtime NavMesh rebuild]
```

**Two pre-existing blockers must be resolved by their owners before V0.2–V0.3:**
- Player damage is commented out — `DealDamage.cs:14-18` (Enemy/Combat).
- Enemy death crashes — `DieState.cs:11-18` (Enemy dev).
They don't block V0.1 (data only), but they block anything with actual combat/death.

---

# TL;DR — What I Do First

1. **Read** `PlayerEntity.cs`, `PatrolRoute.cs`, and `docs/ROGUELIKE_SYSTEM.md` — understand the stat holder and the ScriptableObject pattern.
2. **Ask the team** the §11 questions (weapon/player stat ownership, Katana values, what upgrades modify).
3. **Create** `Assets\Scripts\Roguelike\Data\WeaponData.cs` (+ `WeaponType`) as a ScriptableObject with Damage / Range / AttackSpeed — `TBD` values until the team answers.
4. **Create three assets** (Katana, Spear, Whip) in `Assets\Data\Weapons\` and fill in the agreed values in the Inspector.
5. **Verify** via §14 tests that the assets load and display correctly in Play Mode, and **commit** without touching any Player/Enemy/GameManager code.
