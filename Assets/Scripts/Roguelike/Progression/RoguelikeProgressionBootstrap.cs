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

    readonly HashSet<int> processedEnemyIds = new();

    void Awake()
    {
        InitializeSystems();
    }

    void Start()
    {
        BindCollaborators();
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
            UpgradeSystem = new UpgradeSelectionSystem(
                database,
                playerController,
                Progression,
                playerUi.UpgradeSelectController,
                playerUi.UpgradePresenter
            );
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

        UpgradeSystem?.Unbind();
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
    }

    void HandleDamageTaken(float dmg, AttackEffectData effect) => UpdateHud();
    void HandleHealed(float amt) => UpdateHud();
    void HandleMaxHealthChanged(float max) => UpdateHud();
    void HandleXpChanged(int cur, int req, int lvl) => UpdateHud();

    void HandlePlayerDied()
    {
        UpgradeSystem?.Dismiss();
    }

    void HandleRetryRequested()
    {
        processedEnemyIds.Clear();
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
