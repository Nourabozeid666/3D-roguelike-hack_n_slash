using System;

/// <summary>
/// Event-driven source of the Game Over run summary (floor reached / enemies defeated / run time).
/// The real run-end/death system plugs in later behind this same interface; the UI listens to
/// Changed, never polls. The dotnet harness drives MockGameOverSource to prove the screen flow.
/// </summary>
public interface IGameOverSource
{
    /// <summary>Raised when a run ends and the summary is ready; the screen renders and shows on this.</summary>
    event Action<GameOverData> Changed;

    /// <summary>Current run summary (GameOverData.Default() until the first update).</summary>
    GameOverData GetGameOver();
}

/// <summary>
/// TEMPORARY mock run summary for the Game Over screen so it is fully wired before the real
/// death/run-end system exists. Replaced behind IGameOverSource later. No Unity APIs.
/// </summary>
public sealed class MockGameOverSource : IGameOverSource
{
    public event Action<GameOverData> Changed;

    GameOverData data;

    public MockGameOverSource(GameOverData initial)
    {
        data = initial;
    }

    public MockGameOverSource()
        : this(GameOverData.Default())
    {
    }

    public GameOverData GetGameOver() => data;

    /// <summary>Publish a new run summary; raises Changed (only when it actually differs) so the
    /// screen renders and shows.</summary>
    public void SetGameOver(in GameOverData next)
    {
        if (next.Equals(data)) return;
        data = next;
        Changed?.Invoke(data);
    }
}
