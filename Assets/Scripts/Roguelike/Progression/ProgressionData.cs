using System;
using UnityEngine;

/// <summary>
/// Serializable data contract tracking the player's run progression, experience, and upgrade tokens.
/// Plain data model suitable for persistence and UI data-binding.
/// </summary>
[Serializable]
public class ProgressionData
{
    [Header("Level & XP")]
    public int level = 1;
    public int currentXp = 0;
    public int xpRequired = 100;
    public int pendingUpgrades = 0;

    [Header("Lifetime Stats")]
    public int totalXpEarned = 0;
    public int enemiesKilled = 0;
    public int roomsCleared = 0;
    public int upgradesChosen = 0;

    [Header("Progression Tuning")]
    public int baseXpRequired = 100;
    public float xpGrowthPerLevel = 1.25f;
    public int xpPerEnemy = 25;
    public int xpPerRoom = 100;

    public float XpRatio => xpRequired > 0 ? Mathf.Clamp01((float)currentXp / xpRequired) : 0f;

    public void Reset()
    {
        level = 1;
        currentXp = 0;
        xpRequired = baseXpRequired;
        pendingUpgrades = 0;
        totalXpEarned = 0;
        enemiesKilled = 0;
        roomsCleared = 0;
        upgradesChosen = 0;
    }

    /// <summary>
    /// Smooth exponential curve for level thresholds.
    /// </summary>
    public int CalculateXpRequiredForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return baseXpRequired;
        return Mathf.RoundToInt(baseXpRequired * Mathf.Pow(xpGrowthPerLevel, targetLevel - 1));
    }
}
