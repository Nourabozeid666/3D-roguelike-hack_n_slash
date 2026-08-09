using System;
using System.Collections.Generic;

/// <summary>
/// Event-driven source of upgrade offers shown on the upgrade screen (a list of cards). The real
/// loot/upgrade system plugs in later behind this same interface; the UI listens to Changed, never
/// polls. The dotnet harness drives MockUpgradeSource to prove the card flow.
/// </summary>
public interface IUpgradeSource
{
    /// <summary>Raised whenever a new set of offers is available; the screen re-renders on this.</summary>
    event Action<IReadOnlyList<UpgradeCardData>> Changed;

    /// <summary>Currently offered cards (empty until the first offer).</summary>
    IReadOnlyList<UpgradeCardData> GetUpgrades();
}

/// <summary>
/// TEMPORARY mock upgrade offers (3 placeholder cards) so the upgrade screen is fully wired before
/// the real loot/upgrade system exists. Replaced behind IUpgradeSource later. No Unity APIs.
/// </summary>
public sealed class MockUpgradeSource : IUpgradeSource
{
    public event Action<IReadOnlyList<UpgradeCardData>> Changed;

    IReadOnlyList<UpgradeCardData> cards;

    public MockUpgradeSource(IReadOnlyList<UpgradeCardData> initial)
    {
        cards = initial ?? Array.Empty<UpgradeCardData>();
    }

    public MockUpgradeSource()
        : this(CreateDefaultCards())
    {
    }

    public IReadOnlyList<UpgradeCardData> GetUpgrades() => cards;

    /// <summary>Offer a new set of cards; raises Changed so the screen re-renders.</summary>
    public void SetUpgrades(IReadOnlyList<UpgradeCardData> next)
    {
        cards = next ?? Array.Empty<UpgradeCardData>();
        Changed?.Invoke(cards);
    }

    /// <summary>Three placeholder offers (id/title/description/value/iconKey). The numbers are mock
    /// values until the real upgrade system defines them — never gameplay-true.</summary>
    public static IReadOnlyList<UpgradeCardData> CreateDefaultCards()
        => new[]
        {
            new UpgradeCardData("upg_damage", "Sharpened Edge",
                "Your attacks hit harder.", "+25% damage", "sword"),
            new UpgradeCardData("upg_vitality", "Vitality",
                "Your maximum health grows.", "+50 max HP", "heart"),
            new UpgradeCardData("upg_haste", "Swift Boots",
                "You move faster between attacks.", "+15% speed", "boots"),
        };
}
