using System;
using System.Collections.Generic;

/// <summary>
/// View contract for one upgrade card. Implemented by UpgradeCardController (Unity); the fake in the
/// harness records state transitions so enabled/disabled/selected are all testable.
/// </summary>
public interface IUpgradeCardView
{
    /// <summary>Fill the card with the offer data at the given index.</summary>
    void Present(UpgradeCardData card, int index);

    /// <summary>Highlight the card as the chosen one.</summary>
    void SetSelected(bool selected);

    /// <summary>Enable/disable interaction (disabled cards grey out and cannot be selected).</summary>
    void SetInteractable(bool interactable);
}

/// <summary>
/// View contract for the upgrade selection screen (the 3-card container). Implemented by
/// UpgradeSelectController (Unity); the harness fake records present/state calls.
/// </summary>
public interface IUpgradeSelectView
{
    /// <summary>Show the given offers as cards (list-driven — one card per entry, never hardcoded).</summary>
    void ShowSelection(IReadOnlyList<UpgradeCardData> cards);

    /// <summary>Set a card's interaction + highlight state (after one is chosen the rest lock out).</summary>
    void SetCardState(int index, bool enabled, bool selected);
}

/// <summary>
/// Owns the upgrade-selection rules (pick one card, then the rest lock): subscribes to an
/// IUpgradeSource, shows each offer set, resolves the pick, locks the others and raises CardSelected.
/// Plain C# — the Unity screen is only a view; the harness tests the selection rules directly.
/// </summary>
public sealed class UpgradeSelectPresenter
{
    /// <summary>Raised when the player resolves a pick (the locked-in card).</summary>
    public event Action<UpgradeCardData> CardSelected;

    readonly IUpgradeSelectView view;

    IReadOnlyList<UpgradeCardData> cards = Array.Empty<UpgradeCardData>();
    int selectedIndex = -1;
    bool dismissed;

    public UpgradeSelectPresenter(IUpgradeSelectView view)
    {
        this.view = view;
    }

    public int SelectedIndex => selectedIndex;

    /// <summary>The locked-in card, or null while selection is still open.</summary>
    public UpgradeCardData? SelectedCard
        => selectedIndex >= 0 && selectedIndex < cards.Count ? cards[selectedIndex] : (UpgradeCardData?)null;

    public bool SelectionResolved => selectedIndex >= 0;

    public bool Dismissed => dismissed;

    public IReadOnlyList<UpgradeCardData> Offers => cards;

    /// <summary>Subscribe to a source. The screen stays hidden until the source raises Changed with a
    /// real offer — it is never shown from Start() alone.</summary>
    public void Bind(IUpgradeSource source)
    {
        source.Changed += OnSourceChanged;
    }

    public void Unbind(IUpgradeSource source)
    {
        source.Changed -= OnSourceChanged;
    }

    void OnSourceChanged(IReadOnlyList<UpgradeCardData> next)
    {
        cards = next ?? Array.Empty<UpgradeCardData>();
        selectedIndex = -1;
        dismissed = false;
        view.ShowSelection(cards);
    }

    /// <summary>Pick a card. Only the first pick is honored; once resolved the rest are locked.</summary>
    public void Select(int index)
    {
        if (SelectionResolved || dismissed) return;
        if (index < 0 || index >= cards.Count) return;
        selectedIndex = index;
        for (int i = 0; i < cards.Count; i++)
            view.SetCardState(i, i == index, i == index);
        CardSelected?.Invoke(cards[index]);
    }

    /// <summary>Close the screen without picking (e.g. the run ended mid-offer).</summary>
    public void Dismiss()
    {
        dismissed = true;
    }
}
