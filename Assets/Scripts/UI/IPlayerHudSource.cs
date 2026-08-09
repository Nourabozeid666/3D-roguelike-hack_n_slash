using System;

/// <summary>
/// Event-driven data source for the in-run Player HUD. The real source (player stats / run floor)
/// plugs in later behind this same interface; the UI only listens to Changed, it never polls. The
/// dotnet harness drives MockPlayerHudSource to prove the whole chain updates on the event.
/// </summary>
public interface IPlayerHudSource
{
    /// <summary>Raised whenever the HUD data changes; the presenter re-renders on this.</summary>
    event Action<PlayerHudData> Changed;

    /// <summary>Current HUD snapshot (PlayerHudData.Default() until the first update).</summary>
    PlayerHudData GetPlayerHud();
}

/// <summary>
/// TEMPORARY mock data source so the HUD is fully wired and playable before the real player stat
/// system exists. Clearly separated from real sources; replaced behind IPlayerHudSource when real
/// stats/floor data arrive. Dedupes identical snapshots so identical data costs one render. No Unity
/// APIs — harness-testable.
/// </summary>
public sealed class MockPlayerHudSource : IPlayerHudSource
{
    public event Action<PlayerHudData> Changed;

    PlayerHudData data;

    public MockPlayerHudSource(PlayerHudData initial)
    {
        data = initial;
    }

    public MockPlayerHudSource()
        : this(PlayerHudData.Default())
    {
    }

    public PlayerHudData GetPlayerHud() => data;

    /// <summary>Push a new HUD snapshot; raises Changed (only when it actually differs) so
    /// subscribers re-render.</summary>
    public void SetPlayerHud(in PlayerHudData next)
    {
        if (next.Equals(data)) return;
        data = next;
        Changed?.Invoke(data);
    }
}
