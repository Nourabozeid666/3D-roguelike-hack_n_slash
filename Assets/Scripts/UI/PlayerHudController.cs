using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity view for the in-run Player HUD: health bar, XP bar, level and floor text. Implements
/// IPlayerHudView so PlayerHudPresenter renders into it. Built at runtime (legacy uGUI) pinned to the
/// bottom-left of the PlayerUI canvas — Present simply paints whatever data it is given, so the same
/// component works with the mock source today and the real stat source later. Unity-only: the HUD
/// rules (ratios, clamping, dedupe) live in the harness-tested data contract and presenter.
/// </summary>
public class PlayerHudController : MonoBehaviour, IPlayerHudView
{
    static readonly Color BarBackColor = new Color(0.07f, 0.07f, 0.1f, 0.92f);
    static readonly Color HealthFillColor = new Color(0.72f, 0.16f, 0.16f, 1f);
    static readonly Color XpFillColor = new Color(0.24f, 0.44f, 0.85f, 1f);

    Image healthFill;
    Image xpFill;
    Text healthText;
    Text xpText;
    Text levelText;
    Text floorText;

    /// <summary>Build the HUD panel + bars. The controller's own RectTransform IS the panel.</summary>
    public void Initialize()
    {
        RectTransform panel = (RectTransform)transform;
        PlayerUiKit.Pin(panel, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(640, 120));

        levelText = PlayerUiKit.Text("LevelText", transform, 22, TextAnchor.LowerLeft, Color.white);
        PlayerUiKit.Outline(levelText);
        PlayerUiKit.Pin(levelText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 86), new Vector2(220, 28));

        floorText = PlayerUiKit.Text("FloorText", transform, 22, TextAnchor.LowerRight, Color.white);
        PlayerUiKit.Outline(floorText);
        PlayerUiKit.Pin(floorText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(420, 86), new Vector2(220, 28));

        healthFill = BuildBar("HealthBar", 44, 32, HealthFillColor);
        healthText = BuildBarText("HealthText", healthFill.rectTransform, 20);
        xpFill = BuildBar("XpBar", 0, 24, XpFillColor);
        xpText = BuildBarText("XpText", xpFill.rectTransform, 18);
    }

    public void Present(in PlayerHudData data)
    {
        if (healthFill == null) return;
        healthFill.fillAmount = data.HealthRatio;
        xpFill.fillAmount = data.XpRatio;
        healthText.text = $"{data.currentHealth} / {data.maxHealth}";
        xpText.text = $"{data.xp} / {data.xpRequired}";
        levelText.text = $"LVL {data.level}";
        floorText.text = $"FLOOR {data.floor}";
    }

    Image BuildBar(string name, float y, float height, Color fillColor)
    {
        Image back = PlayerUiKit.Image(name, transform, BarBackColor);
        back.raycastTarget = false;
        PlayerUiKit.Pin(back.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, y), new Vector2(640, height));

        Image fill = PlayerUiKit.Image("Fill", back.transform, fillColor);
        fill.raycastTarget = false;
        PlayerUiKit.Stretch(fill.rectTransform);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        return fill;
    }

    Text BuildBarText(string name, RectTransform bar, int fontSize)
    {
        Text text = PlayerUiKit.Text(name, bar, fontSize, TextAnchor.MiddleCenter, Color.white);
        PlayerUiKit.Outline(text);
        PlayerUiKit.Stretch(text.rectTransform);
        return text;
    }
}
