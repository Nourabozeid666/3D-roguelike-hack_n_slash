using System;

/// <summary>
/// View contract for the Game Over screen. Implemented by GameOverScreenController (Unity); the
/// harness fake records the summary. Present renders the stats AND shows the screen — it is only
/// ever reached via a Changed event from the source (a run ending), never on bind.
/// </summary>
public interface IGameOverView
{
    /// <summary>Render the run summary and show the screen.</summary>
    void Present(in GameOverData data);
}

/// <summary>
/// Subscribes to an IGameOverSource and forwards the run summary to the Game Over screen. Event-
/// driven only: the screen renders/shows on each Changed event and never from Start() alone. Plain C#.
/// </summary>
public sealed class GameOverPresenter
{
    readonly IGameOverView view;

    GameOverData data;

    public GameOverPresenter(IGameOverView view)
    {
        this.view = view;
    }

    public GameOverData Current => data;

    /// <summary>Subscribe to a source. The screen stays hidden until the source raises Changed.</summary>
    public void Bind(IGameOverSource source)
    {
        source.Changed += OnSourceChanged;
    }

    public void Unbind(IGameOverSource source)
    {
        source.Changed -= OnSourceChanged;
    }

    void OnSourceChanged(GameOverData next)
    {
        data = next;
        view.Present(data);
    }
}
