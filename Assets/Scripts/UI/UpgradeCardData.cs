using System;

/// <summary>
/// One selectable upgrade offered on the upgrade screen. PLAIN DATA CONTRACT — no Unity APIs, so the
/// dotnet harness can construct and compare offers. Icons are referenced by key (iconKey); the Unity
/// view maps the key to a Sprite, so this contract stays serializable and testable. Owned by the
/// Roguelike/UI system; produced by a data source (real loot/upgrade system later, MockUpgradeSource
/// today) and consumed by UpgradeSelectPresenter.
/// </summary>
[Serializable]
public struct UpgradeCardData : IEquatable<UpgradeCardData>
{
    public string id;
    public string title;
    public string description;
    public string valueText;
    public string iconKey;

    public UpgradeCardData(string id, string title, string description, string valueText, string iconKey)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.valueText = valueText;
        this.iconKey = iconKey;
    }

    public bool IsValid => !string.IsNullOrEmpty(id);

    public bool Equals(UpgradeCardData other)
        => string.Equals(id, other.id, StringComparison.Ordinal)
        && string.Equals(title, other.title, StringComparison.Ordinal)
        && string.Equals(description, other.description, StringComparison.Ordinal)
        && string.Equals(valueText, other.valueText, StringComparison.Ordinal)
        && string.Equals(iconKey, other.iconKey, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is UpgradeCardData other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (id != null ? id.GetHashCode() : 0);
            hash = hash * 31 + (title != null ? title.GetHashCode() : 0);
            hash = hash * 31 + (description != null ? description.GetHashCode() : 0);
            hash = hash * 31 + (valueText != null ? valueText.GetHashCode() : 0);
            hash = hash * 31 + (iconKey != null ? iconKey.GetHashCode() : 0);
            return hash;
        }
    }

    public static bool operator ==(UpgradeCardData a, UpgradeCardData b) => a.Equals(b);
    public static bool operator !=(UpgradeCardData a, UpgradeCardData b) => !a.Equals(b);
}
