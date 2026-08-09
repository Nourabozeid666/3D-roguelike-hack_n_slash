using System;

/// <summary>
/// Player HUD snapshot rendered by the in-run UI. PLAIN DATA CONTRACT — no Unity APIs, so the dotnet
/// harness can construct, compare and round-trip it. Owned by the Roguelike/UI system. Produced by a
/// data source (real player stats + run floor later, MockPlayerHudSource today) and consumed by
/// PlayerHudPresenter -> IPlayerHudView. Display values only: health/XP themselves are owned by the
/// real player systems elsewhere.
/// </summary>
[Serializable]
public struct PlayerHudData : IEquatable<PlayerHudData>
{
    public int currentHealth;
    public int maxHealth;
    public int xp;
    public int xpRequired;
    public int level;
    public int floor;

    public PlayerHudData(int currentHealth, int maxHealth, int xp, int xpRequired, int level, int floor)
    {
        this.currentHealth = currentHealth;
        this.maxHealth = maxHealth;
        this.xp = xp;
        this.xpRequired = xpRequired;
        this.level = level;
        this.floor = floor;
    }

    /// <summary>Full-health, level 1, floor 1 defaults used until a real source reports in.</summary>
    public static PlayerHudData Default()
        => new PlayerHudData(100, 100, 0, 100, 1, 1);

    /// <summary>0..1 for the health fill bar; 0 when there is no max.</summary>
    public float HealthRatio
        => maxHealth > 0 ? Math.Max(0f, Math.Min(1f, (float)currentHealth / maxHealth)) : 0f;

    /// <summary>0..1 for the XP fill bar; 0 when there is no required XP.</summary>
    public float XpRatio
        => xpRequired > 0 ? Math.Max(0f, Math.Min(1f, (float)xp / xpRequired)) : 0f;

    public bool Equals(PlayerHudData other)
        => currentHealth == other.currentHealth
        && maxHealth == other.maxHealth
        && xp == other.xp
        && xpRequired == other.xpRequired
        && level == other.level
        && floor == other.floor;

    public override bool Equals(object obj) => obj is PlayerHudData other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + currentHealth;
            hash = hash * 31 + maxHealth;
            hash = hash * 31 + xp;
            hash = hash * 31 + xpRequired;
            hash = hash * 31 + level;
            hash = hash * 31 + floor;
            return hash;
        }
    }

    public static bool operator ==(PlayerHudData a, PlayerHudData b) => a.Equals(b);
    public static bool operator !=(PlayerHudData a, PlayerHudData b) => !a.Equals(b);

    public override string ToString()
        => $"HP {currentHealth}/{maxHealth} | XP {xp}/{xpRequired} | Lv {level} | Floor {floor}";
}
