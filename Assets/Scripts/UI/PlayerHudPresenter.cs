using System;

/// <summary>
/// View contract for the in-run HUD. Implemented by the Unity HUD (PlayerHudController) but defined
/// here so the presenter logic is plain C# and covered by the dotnet harness via a recording fake.
/// </summary>
public interface IPlayerHudView
{
    /// <summary>Render the given HUD snapshot (initial bind AND every data change).</summary>
    void Present(in PlayerHudData data);
}

/// <summary>
/// Subscribes to an IPlayerHudSource and forwards every data change to the view. Event-driven only:
/// nothing polls and the view is never refreshed from Start() alone — it re-renders each time the
/// source raises Changed. Identical snapshots are deduped so one value costs one render. Plain C#.
/// </summary>
public sealed class PlayerHudPresenter
{
    readonly IPlayerHudView view;

    PlayerHudData data;

    public PlayerHudPresenter(IPlayerHudView view)
    {
        this.view = view;
    }

    public PlayerHudData Current => data;

    /// <summary>Subscribe to a source and render its current snapshot immediately.</summary>
    public void Bind(IPlayerHudSource source)
    {
        source.Changed += OnSourceChanged;
        OnSourceChanged(source.GetPlayerHud());
    }

    public void Unbind(IPlayerHudSource source)
    {
        source.Changed -= OnSourceChanged;
    }

    void OnSourceChanged(PlayerHudData next)
    {
        if (next.Equals(data)) return;
        data = next;
        view.Present(data);
    }
}
