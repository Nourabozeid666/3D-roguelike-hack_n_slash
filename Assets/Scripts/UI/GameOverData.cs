using System;

/// <summary>
/// Run summary shown on the Game Over screen. PLAIN DATA CONTRACT — no Unity APIs, harness-testable.
/// runTimeSeconds is the run duration in seconds; the view formats it via RunTimeText() as M:SS (or
/// H:MM:SS past an hour). Produced by a data source (real run stats later, MockGameOverSource today)
/// and consumed by GameOverPresenter.
/// </summary>
[Serializable]
public struct GameOverData : IEquatable<GameOverData>
{
    public int floorReached;
    public int enemiesDefeated;
    public float runTimeSeconds;

    public GameOverData(int floorReached, int enemiesDefeated, float runTimeSeconds)
    {
        this.floorReached = floorReached;
        this.enemiesDefeated = enemiesDefeated;
        this.runTimeSeconds = runTimeSeconds;
    }

    public static GameOverData Default()
        => new GameOverData(0, 0, 0f);

    /// <summary>"M:SS" (or "H:MM:SS" past an hour) for the run-time stat line.</summary>
    public string RunTimeText()
    {
        int total = Math.Max(0, (int)Math.Floor(runTimeSeconds));
        int hours = total / 3600;
        int minutes = (total % 3600) / 60;
        int seconds = total % 60;
        return hours > 0
            ? $"{hours}:{minutes:00}:{seconds:00}"
            : $"{minutes}:{seconds:00}";
    }

    public bool Equals(GameOverData other)
        => floorReached == other.floorReached
        && enemiesDefeated == other.enemiesDefeated
        && runTimeSeconds == other.runTimeSeconds;

    public override bool Equals(object obj) => obj is GameOverData other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + floorReached;
            hash = hash * 31 + enemiesDefeated;
            hash = hash * 31 + runTimeSeconds.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(GameOverData a, GameOverData b) => a.Equals(b);
    public static bool operator !=(GameOverData a, GameOverData b) => !a.Equals(b);
}
