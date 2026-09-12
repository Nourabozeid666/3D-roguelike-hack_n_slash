using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// ScriptableObject database holding the pool of available IStatModifier upgrades.
/// Handles weighted random rolling based on modifier Chance, and formats upgrades
/// into presentation-ready UpgradeCardData for the UI.
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Roguelike/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [SerializeField] private List<ScriptableObject> upgrades = new();

    public IReadOnlyList<ScriptableObject> Upgrades => upgrades;

    public void SetUpgrades(IEnumerable<ScriptableObject> list)
    {
        upgrades.Clear();
        if (list != null)
        {
            foreach (var item in list)
            {
                if (item is IStatModifier)
                    upgrades.Add(item);
            }
        }
    }

    /// <summary>
    /// Pick N unique upgrades from the pool, weighted by their Chance property.
    /// </summary>
    public List<ScriptableObject> RollUpgrades(int count = 3, ISet<ScriptableObject> excluded = null)
    {
        List<ScriptableObject> candidates = new List<ScriptableObject>();
        for (int i = 0; i < upgrades.Count; i++)
        {
            var u = upgrades[i];
            if (u != null && (excluded == null || !excluded.Contains(u)))
            {
                candidates.Add(u);
            }
        }

        if (candidates.Count <= count)
            return new List<ScriptableObject>(candidates);

        List<ScriptableObject> chosen = new List<ScriptableObject>();
        for (int step = 0; step < count && candidates.Count > 0; step++)
        {
            float totalWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                float weight = (candidates[i] is IStatModifier sm) ? Mathf.Max(0.01f, sm.Chance) : 1f;
                totalWeight += weight;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            int selectedIndex = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                float weight = (candidates[i] is IStatModifier sm) ? Mathf.Max(0.01f, sm.Chance) : 1f;
                cumulative += weight;
                if (roll <= cumulative)
                {
                    selectedIndex = i;
                    break;
                }
            }

            chosen.Add(candidates[selectedIndex]);
            candidates.RemoveAt(selectedIndex);
        }

        return chosen;
    }

    /// <summary>
    /// Convert an IStatModifier ScriptableObject into an UpgradeCardData for UI presentation.
    /// </summary>
    public static UpgradeCardData ConvertToCardData(ScriptableObject asset)
    {
        if (asset == null)
            return new UpgradeCardData("empty", "Unknown", "No effect", "", "sword");

        string rawName = asset.name;
        string id = rawName;
        string title = CleanTitle(rawName);
        string valueText = "";
        string description = "";
        string iconKey = "sword";

        if (asset is IStatModifier mod)
        {
            iconKey = GetIconKey(mod.TargetStat);
            valueText = ExtractValueText(rawName, mod);
            description = FormatDescription(mod, asset);
        }
        else
        {
            description = rawName;
        }

        return new UpgradeCardData(id, title, description, valueText, iconKey);
    }

    static string CleanTitle(string rawName)
    {
        // Strip [Passive], [Conditional], [Cursed]
        string clean = rawName;
        clean = Regex.Replace(clean, @"^\[.*?\]\s*", "");

        // If parenthesized details exist at the end, extract the title part
        Match m = Regex.Match(clean, @"^(.*?)\s*\(.*?\)$");
        if (m.Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
        {
            return m.Groups[1].Value.Trim();
        }

        // Clean up names like "+ 1 Length"
        if (clean.StartsWith("+") || clean.StartsWith("-"))
        {
            if (clean.Contains("Length")) return "Blade Reach";
            if (clean.Contains("Size")) return "Blade Mass";
            if (clean.Contains("Damage")) return "Damage Boost";
        }

        return clean.Trim();
    }

    static string ExtractValueText(string rawName, IStatModifier mod)
    {
        // Check if rawName already has (e.g. (+4 Damage) or (HP < 30%, +40% Damage))
        Match m = Regex.Match(rawName, @"\((.*?)\)");
        if (m.Success)
        {
            return m.Groups[1].Value.Trim();
        }

        // For legacy names like "[HP -50%] + 10% Damage"
        if (rawName.Contains("+ 10% Damage")) return "+10% Damage (HP < 50%)";
        if (rawName.Contains("+ 1 Length")) return "+1.0 Blade Length";
        if (rawName.Contains("+ 1 Size")) return "+1.0 Blade Size";
        if (rawName.Contains("+5 Damage")) return "+5 Damage";

        return mod.TargetStat.ToString();
    }

    static string FormatDescription(IStatModifier mod, ScriptableObject asset)
    {
        string baseDesc = mod.TargetStat switch
        {
            StatType.MaxHealth => mod.ModifierPolarity == StatModifierPolarity.Positive
                ? "Increases your maximum health."
                : "Reduces maximum health in exchange for power.",
            StatType.Defense => mod.ModifierPolarity == StatModifierPolarity.Positive
                ? "Reinforces armor to reduce incoming damage."
                : "Weakens defense, increasing incoming damage.",
            StatType.AttackDamage => mod.ModifierPolarity == StatModifierPolarity.Positive
                ? "Sharpens your blade, increasing attack damage."
                : "Reduces base attack damage.",
            StatType.AttackSpeed => mod.ModifierPolarity == StatModifierPolarity.Positive
                ? "Increases sword swing and recovery speed."
                : "Weighs you down, slowing your attack swings.",
            StatType.CritChance => "Increases the likelihood of landing critical strikes.",
            StatType.CritMultiplier => "Deals heavier devastating damage on critical strikes.",
            StatType.WeaponLength => "Extends blade reach to hit enemies from further away.",
            StatType.WeaponSize => "Enlarges the blade mass for heavier, sweeping blows.",
            _ => "Modifies player attributes."
        };

        if (asset is ConditionalEffect ce)
        {
            string condText = ce.Condition switch
            {
                Condition.LessThan => $"Active while {ce.ConditionStat} is below {ce.ConditionPercentageValue:0}%",
                Condition.GreaterThan => $"Active while {ce.ConditionStat} exceeds {ce.ConditionPercentageValue:0}",
                _ => $"Active while {ce.ConditionStat} equals {ce.ConditionPercentageValue:0}"
            };
            return $"{baseDesc}\n[{condText}]";
        }

        return baseDesc;
    }

    static string GetIconKey(StatType stat)
    {
        return stat switch
        {
            StatType.MaxHealth or StatType.Health or StatType.Defense => "heart",
            StatType.AttackSpeed => "boots",
            _ => "sword"
        };
    }

#if UNITY_EDITOR
    [ContextMenu("Populate From Project")]
    public void PopulateFromProject()
    {
        upgrades.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Scripts/Roguelike/StatModifiers" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var so = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so is IStatModifier)
            {
                upgrades.Add(so);
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[UpgradeDatabase] Populated {upgrades.Count} upgrades from project.");
    }
#endif

    public static UpgradeDatabase GetDefaultDatabase()
    {
        UpgradeDatabase db = Resources.Load<UpgradeDatabase>("UpgradeDatabase");
        if (db != null && db.Upgrades.Count > 0) return db;

#if UNITY_EDITOR
        if (db == null)
        {
            db = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeDatabase>("Assets/Resources/UpgradeDatabase.asset");
            if (db != null && db.Upgrades.Count > 0) return db;
        }

        var runtimeDb = ScriptableObject.CreateInstance<UpgradeDatabase>();
        runtimeDb.PopulateFromProject();
        return runtimeDb;
#else
        return db;
#endif
    }
}
