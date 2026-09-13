using System;
using UnityEngine;

/// <summary>
/// Core progression logic: handles XP awards from enemy kills and room finishes,
/// manages level-up calculations, and grants pending upgrade tokens.
/// </summary>
public class ProgressionSystem
{
    public ProgressionData Data { get; }

    public event Action<int, int, int> OnXpChanged;         // currentXp, xpRequired, level
    public event Action<int, int> OnLevelUp;               // newLevel, pendingUpgrades
    public event Action<int> OnPendingUpgradesChanged;      // pendingUpgrades

    public int CurrentLevel => Data.level;
    public int CurrentXp => Data.currentXp;
    public int XpRequired => Data.xpRequired;
    public int PendingUpgrades => Data.pendingUpgrades;
    public bool HasPendingUpgrades => Data.pendingUpgrades > 0;
    public float XpRatio => Data.XpRatio;

    public ProgressionSystem(ProgressionData data = null)
    {
        Data = data ?? new ProgressionData();
    }

    /// <summary>
    /// Add XP to the player's progression. Triggers level-ups and allocates upgrade tokens.
    /// </summary>
    public void AddXp(int amount, string source = "")
    {
        if (amount <= 0) return;

        Data.currentXp += amount;
        Data.totalXpEarned += amount;

        bool leveledUp = false;
        while (Data.currentXp >= Data.xpRequired)
        {
            Data.currentXp -= Data.xpRequired;
            Data.level++;
            Data.xpRequired = Data.CalculateXpRequiredForLevel(Data.level);
            Data.pendingUpgrades++;
            leveledUp = true;
            Debug.Log($"[ProgressionSystem] Level Up! Now Level {Data.level}. Pending Upgrades: {Data.pendingUpgrades}");
            OnLevelUp?.Invoke(Data.level, Data.pendingUpgrades);
        }

        OnXpChanged?.Invoke(Data.currentXp, Data.xpRequired, Data.level);

        if (leveledUp)
        {
            OnPendingUpgradesChanged?.Invoke(Data.pendingUpgrades);
        }
    }

    /// <summary>
    /// Invoked when an enemy is killed.
    /// </summary>
    public void AwardEnemyKill(GameObject enemy = null, int xpOverride = -1)
    {
        Data.enemiesKilled++;
        int xp = xpOverride > 0 ? xpOverride : Data.xpPerEnemy;
        AddXp(xp, "Enemy Killed");
    }

    /// <summary>
    /// Invoked when a floor / room is completed.
    /// </summary>
    public void AwardRoomCleared(int xpOverride = -1, int bonusUpgrades = 0)
    {
        Data.roomsCleared++;
        int xp = xpOverride > 0 ? xpOverride : Data.xpPerRoom;
        AddXp(xp, "Room Cleared");

        if (bonusUpgrades > 0)
        {
            AddPendingUpgrades(bonusUpgrades);
        }
    }

    public void AddPendingUpgrades(int count)
    {
        if (count <= 0) return;
        Data.pendingUpgrades += count;
        OnPendingUpgradesChanged?.Invoke(Data.pendingUpgrades);
    }

    public bool ConsumePendingUpgrade()
    {
        if (Data.pendingUpgrades <= 0) return false;
        Data.pendingUpgrades--;
        Data.upgradesChosen++;
        OnPendingUpgradesChanged?.Invoke(Data.pendingUpgrades);
        return true;
    }

    public void Reset()
    {
        Data.Reset();
        OnXpChanged?.Invoke(Data.currentXp, Data.xpRequired, Data.level);
        OnPendingUpgradesChanged?.Invoke(Data.pendingUpgrades);
    }
}
