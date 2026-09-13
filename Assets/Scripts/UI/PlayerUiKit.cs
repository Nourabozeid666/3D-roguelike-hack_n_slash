using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared runtime construction helpers for the Roguelike player UI (HUD, upgrade screen, game over
/// screen). Legacy uGUI is built entirely at runtime so the UI stays data-driven and needs no scene
/// authoring — mirrors the approach SpawnTestDebugDisplay already uses. Unity-only: NOT part of the
/// dotnet harness (the harness tests the plain-C# data/presenter layer instead).
/// </summary>
public static class PlayerUiKit
{
    static Font cachedFont;
    static Font cachedHeaderFont;

    /// <summary>
    /// Body font reused from the MainMenu/PauseMenu visual language (Orbitron-VariableFont_wght, the
    /// font the menus already reference). Falls back to the legacy built-in font if the resource is
    /// missing, so the UI still constructs in authoring scenes that lack the font folder.
    /// </summary>
    public static Font DefaultFont
    {
        get
        {
            if (cachedFont == null)
                cachedFont = Resources.Load<Font>("Orbitron-VariableFont_wght") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
        }
    }

    /// <summary>
    /// Emphasis/header font (Orbitron-SemiBold, the same weight MainMenu/PauseMenu headers use).
    /// Falls back to the body font when unavailable.
    /// </summary>
    public static Font HeaderFont
    {
        get
        {
            if (cachedHeaderFont == null)
                cachedHeaderFont = Resources.Load<Font>("Orbitron-SemiBold") ?? DefaultFont;
            return cachedHeaderFont;
        }
    }

    /// <summary>Create an empty RectTransform GameObject parented to the given transform.</summary>
    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    /// <summary>Create a solid-color Image (null sprite renders a white quad).</summary>
    public static Image Image(string name, Transform parent, Color color)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    /// <summary>Create a legacy UI Text (non-raycast by default so it never blocks buttons).</summary>
    public static Text Text(string name, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = Rect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>Create a UI Text using the emphasis font (menu headers: screen titles, LVL/FLOOR accents).</summary>
    public static Text HeaderText(string name, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        Text text = Text(name, parent, fontSize, alignment, color);
        text.font = HeaderFont;
        return text;
    }

    /// <summary>Create a legacy UI Button (Image graphic + Button on the same GameObject).</summary>
    public static Button Button(string name, Transform parent, Color color)
    {
        Image image = Image(name, parent, color);
        image.raycastTarget = true;
        return image.gameObject.AddComponent<Button>();
    }

    /// <summary>
    /// Apply the menu button feedback (hover/pressed/disabled tints) used by the styled screens.
    /// Color-tint multiplication only: the target graphic keeps its color and each state just
    /// lightens or darkens it, matching how the MainMenu/PauseMenu buttons read.
    /// </summary>
    public static void StyleButton(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.2f, 1f);
        colors.pressedColor = new Color(0.82f, 0.84f, 0.92f, 1f);
        colors.selectedColor = new Color(1.08f, 1.08f, 1.12f, 1f);
        colors.disabledColor = new Color(0.65f, 0.65f, 0.68f, 0.65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;
    }

    /// <summary>Add a readable outline to a text element.</summary>
    public static void Outline(Text text)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    /// <summary>Stretch a rect to fill its parent.</summary>
    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>Pin a rect to an anchor point (pivot at that anchor, position in its units).</summary>
    public static void Pin(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
