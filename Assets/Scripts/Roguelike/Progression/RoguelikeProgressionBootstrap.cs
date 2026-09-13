using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-level integration bridge for the Roguelike Progression and Upgrade system.
/// Hooks enemy deaths and room completions to XP gain, synchronizes the player HUD
/// in real-time, and feeds upgrade rolls into the Player UI selection screen.
/// Works automatically in both production runs (from MainMenu) and direct scene play.
/// </summary>
public class RoguelikeProgressionBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SpawnSystem spawnSystem;
    [SerializeField] private RunBootstrap runBootstrap;
    [SerializeField] private PlayerUiBootstrap playerUi;
    [SerializeField] private UpgradeDatabase database;

    [Header("Tuning")]
    [SerializeField] private int xpPerEnemy = 25;
    [SerializeField] private int xpPerRoom = 100;
    [SerializeField] private int baseXpRequired = 100;
    [SerializeField] private float xpGrowthPerLevel = 1.25f;
    [SerializeField] private int bonusUpgradesPerRoom = 0;

    public ProgressionSystem Progression { get; private set; }
    public UpgradeSelectionSystem UpgradeSystem { get; private set; }
    public bool IsSelectingUpgrade => UpgradeSystem != null && UpgradeSystem.IsSelecting;

    readonly HashSet<int> processedEnemyIds = new();
    readonly List<string> appliedUpgradeIds = new();

    void Awake()
    {
        InitializeSystems();
    }

    void Start()
    {
        BindCollaborators();

        RunSaveService saves = new RunSaveService();
        if (saves.TryLoad(out SaveData save))
        {
            RestoreProgression(save);
        }

        UpdateHud();
    }

    void OnDestroy()
    {
        UnbindCollaborators();
    }

    void InitializeSystems()
    {
        // 1. Database
        if (database == null)
        {
            database = UpgradeDatabase.GetDefaultDatabase();
        }

        // 2. Progression System
        ProgressionData data = new ProgressionData
        {
            xpPerEnemy = xpPerEnemy,
            xpPerRoom = xpPerRoom,
            baseXpRequired = baseXpRequired,
            xpGrowthPerLevel = xpGrowthPerLevel
        };
        Progression = new ProgressionSystem(data);
    }

    void BindCollaborators()
    {
        // Resolve references if not serialized
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (spawnSystem == null) spawnSystem = FindFirstObjectByType<SpawnSystem>();
        if (runBootstrap == null) runBootstrap = FindFirstObjectByType<RunBootstrap>();
        if (playerUi == null) playerUi = FindFirstObjectByType<PlayerUiBootstrap>();

        // If PlayerUiBootstrap is missing, create it dynamically
        if (playerUi == null)
        {
            GameObject uiGo = new GameObject("PlayerUiBootstrap");
            playerUi = uiGo.AddComponent<PlayerUiBootstrap>();
        }

        // Disable demo driver so keystrokes don't conflict with live progression
        PlayerUiDemoDriver demo = playerUi.GetComponent<PlayerUiDemoDriver>();
        if (demo != null) demo.enabled = false;

        // Create UpgradeSelectionSystem
        if (playerUi != null && Progression != null)
        {
            if (playerUi.UpgradePresenter != null && playerUi.UpgradeSource != null)
            {
                playerUi.UpgradePresenter.Unbind(playerUi.UpgradeSource);
            }
            UpgradeSystem = new UpgradeSelectionSystem(
                database,
                playerController,
                Progression,
                playerUi.UpgradeSelectController,
                playerUi.UpgradePresenter
            );
            UpgradeSystem.OnUpgradeApplied += HandleUpgradeApplied;
        }

        // Wire Enemy Deaths & Room Clears
        if (spawnSystem != null)
        {
            spawnSystem.EnemyDefeated += HandleEnemyDefeated;
            spawnSystem.FloorCleared += HandleFloorCleared;
        }

        EnemyController.GlobalEnemyDied += HandleGlobalEnemyDied;
        TestEnemy.GlobalEnemyDied += HandleTestEnemyDied;

        // Wire Player Health & Death
        if (playerController != null && playerController.Entity is PlayerEntity pe)
        {
            pe.OnDamageTaken += HandleDamageTaken;
            pe.OnHealed += HandleHealed;
            pe.OnMaxHealthChanged += HandleMaxHealthChanged;
            pe.OnDied += HandlePlayerDied;
        }

        // Wire HUD Updates
        Progression.OnXpChanged += HandleXpChanged;

        // Wire Retry
        if (playerUi != null)
        {
            playerUi.RetryRequested += HandleRetryRequested;
        }
    }

    void UnbindCollaborators()
    {
        if (spawnSystem != null)
        {
            spawnSystem.EnemyDefeated -= HandleEnemyDefeated;
            spawnSystem.FloorCleared -= HandleFloorCleared;
        }

        EnemyController.GlobalEnemyDied -= HandleGlobalEnemyDied;
        TestEnemy.GlobalEnemyDied -= HandleTestEnemyDied;

        if (playerController != null && playerController.Entity is PlayerEntity pe)
        {
            pe.OnDamageTaken -= HandleDamageTaken;
            pe.OnHealed -= HandleHealed;
            pe.OnMaxHealthChanged -= HandleMaxHealthChanged;
            pe.OnDied -= HandlePlayerDied;
        }

        if (Progression != null)
        {
            Progression.OnXpChanged -= HandleXpChanged;
        }

        if (playerUi != null)
        {
            playerUi.RetryRequested -= HandleRetryRequested;
        }

        if (UpgradeSystem != null)
        {
            UpgradeSystem.OnUpgradeApplied -= HandleUpgradeApplied;
            UpgradeSystem.Unbind();
        }
    }

    void HandleEnemyDefeated(GameObject enemy)
    {
        if (enemy == null) return;
        int id = enemy.GetInstanceID();
        if (processedEnemyIds.Add(id))
        {
            Progression.AwardEnemyKill(enemy);
            Debug.Log($"[Progression] Enemy killed! +{xpPerEnemy} XP (Total: {Progression.CurrentXp}/{Progression.XpRequired})");
        }
    }

    void HandleGlobalEnemyDied(EnemyController enemy)
    {
        if (enemy == null) return;
        int id = enemy.gameObject.GetInstanceID();
        if (processedEnemyIds.Add(id))
        {
            Progression.AwardEnemyKill(enemy.gameObject);
            Debug.Log($"[Progression] Enemy killed! +{xpPerEnemy} XP (Total: {Progression.CurrentXp}/{Progression.XpRequired})");
        }
    }

    void HandleTestEnemyDied(TestEnemy enemy)
    {
        if (enemy == null) return;
        int id = enemy.gameObject.GetInstanceID();
        if (processedEnemyIds.Add(id))
        {
            Progression.AwardEnemyKill(enemy.gameObject);
            Debug.Log($"[Progression] Enemy killed! +{xpPerEnemy} XP (Total: {Progression.CurrentXp}/{Progression.XpRequired})");
        }
    }

    void HandleFloorCleared()
    {
        processedEnemyIds.Clear();
        Progression.AwardRoomCleared(xpOverride: -1, bonusUpgrades: bonusUpgradesPerRoom);
        Debug.Log($"[Progression] Room cleared! +{xpPerRoom} XP (Total: {Progression.CurrentXp}/{Progression.XpRequired})");
        UpdateHud();

        CheckAndPresentFloorEndUpgrades();
    }

    public void CheckAndPresentFloorEndUpgrades()
    {
        if (UpgradeSystem != null && Progression != null && Progression.HasPendingUpgrades)
        {
            UpgradeSystem.PresentNextUpgrade();
        }
    }

    void HandleDamageTaken(float dmg, AttackEffectData effect) => UpdateHud();
    void HandleHealed(float amt) => UpdateHud();
    void HandleMaxHealthChanged(float max) => UpdateHud();
    void HandleXpChanged(int cur, int req, int lvl) => UpdateHud();

    void HandlePlayerDied()
    {
        UpgradeSystem?.Dismiss();
    }

    void HandleUpgradeApplied(ScriptableObject asset)
    {
        if (asset != null && !appliedUpgradeIds.Contains(asset.name))
        {
            appliedUpgradeIds.Add(asset.name);
        }
    }

    public void CaptureProgression(SaveData save)
    {
        if (save == null) return;
        save.playerLevel = Progression != null ? Progression.CurrentLevel : 1;
        save.currentXp = Progression != null ? Progression.CurrentXp : 0;
        save.pendingUpgrades = Progression != null ? Progression.PendingUpgrades : 0;
        save.appliedUpgradeIds = new List<string>(appliedUpgradeIds);
        if (playerController != null && playerController.Entity != null)
        {
            save.currentHealth = playerController.Entity.Health;
        }
    }

    public void RestoreProgression(SaveData save)
    {
        if (save == null) return;
        if (Progression != null)
        {
            Progression.Data.level = Mathf.Max(1, save.playerLevel);
            Progression.Data.currentXp = Mathf.Max(0, save.currentXp);
            Progression.Data.pendingUpgrades = Mathf.Max(0, save.pendingUpgrades);
            Progression.Data.xpRequired = Progression.Data.CalculateXpRequiredForLevel(Progression.Data.level);
        }

        appliedUpgradeIds.Clear();
        if (save.appliedUpgradeIds != null)
        {
            appliedUpgradeIds.AddRange(save.appliedUpgradeIds);
        }

        if (playerController != null && playerController.Entity is PlayerEntity pe && database != null)
        {
            foreach (string upId in appliedUpgradeIds)
            {
                ScriptableObject modAsset = null;
                for (int i = 0; i < database.Upgrades.Count; i++)
                {
                    if (database.Upgrades[i] != null && database.Upgrades[i].name == upId)
                    {
                        modAsset = database.Upgrades[i];
                        break;
                    }
                }
                if (modAsset is IStatModifier mod)
                {
                    pe.AddModifier(mod);
                    Debug.Log($"[RoguelikeProgressionBootstrap] Restored modifier '{upId}' onto player.");
                }
            }

            if (save.currentHealth > 0f && !pe.IsDead)
            {
                float diff = save.currentHealth - pe.Health;
                if (diff > 0f) pe.Heal(diff);
                else if (diff < 0f) pe.TakeDamage(-diff);
            }
        }

        UpdateHud();
    }

    void HandleRetryRequested()
    {
        processedEnemyIds.Clear();
        appliedUpgradeIds.Clear();
        Progression?.Reset();
        UpdateHud();
    }

    public void UpdateHud()
    {
        if (playerUi == null || playerUi.HudSource == null) return;

        int curHp = 100;
        int maxHp = 100;
        if (playerController != null && playerController.Entity != null)
        {
            curHp = Mathf.Max(0, Mathf.RoundToInt(playerController.Entity.Health));
            maxHp = Mathf.Max(1, Mathf.RoundToInt(playerController.Entity.MaxHealth));
        }

        int floor = 1;
        if (runBootstrap != null && runBootstrap.Run != null)
        {
            floor = runBootstrap.Run.CurrentFloor;
        }

        PlayerHudData data = new PlayerHudData(
            curHp,
            maxHp,
            Progression != null ? Progression.CurrentXp : 0,
            Progression != null ? Progression.XpRequired : 100,
            Progression != null ? Progression.CurrentLevel : 1,
            floor
        );

        playerUi.HudSource.SetPlayerHud(data);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (FindFirstObjectByType<RoguelikeProgressionBootstrap>() == null)
        {
            if (FindFirstObjectByType<PlayerController>() != null || FindFirstObjectByType<SpawnSystem>() != null)
            {
                GameObject go = new GameObject("RoguelikeProgressionBootstrap");
                go.AddComponent<RoguelikeProgressionBootstrap>();
            }
        }
    }
}
