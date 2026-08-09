using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity view for ONE upgrade card (icon, title, value, description + click). Implements
/// IUpgradeCardView so UpgradeSelectPresenter drives it. The card is generic: it only paints the
/// UpgradeCardData it is given, so the row stays list-driven (one card per offered entry, never
/// hardcoded). Clicking raises Clicked(index); the screen controller forwards it to the presenter's
/// Select. Unity-only: the pick/lock rules live in the harness-tested presenter.
/// </summary>
public class UpgradeCardController : MonoBehaviour, IUpgradeCardView
{
    static readonly Color CardBackColor = new Color(0.12f, 0.13f, 0.18f, 0.96f);
    static readonly Color SelectedBorderColor = new Color(0.42f, 0.62f, 0.95f, 1f);
    static readonly Color IconTintDefault = new Color(0.55f, 0.58f, 0.65f, 1f);
    static readonly Color TitleColor = new Color(1f, 1f, 1f, 1f);
    static readonly Color ValueColor = new Color(0.92f, 0.76f, 0.3f, 1f);
    static readonly Color DescriptionColor = new Color(0.78f, 0.8f, 0.86f, 1f);

    public event Action<int> Clicked;

    int index = -1;
    bool locked;
    Image back;
    Image border;
    Image iconBack;
    Text iconText;
    Text titleText;
    Text valueText;
    Text descriptionText;
    CanvasGroup group;

    /// <summary>Build the card visuals. The controller's own RectTransform IS the card (300x380).</summary>
    public void Initialize()
    {
        RectTransform self = (RectTransform)transform;
        self.sizeDelta = new Vector2(300, 380);

        group = gameObject.AddComponent<CanvasGroup>();

        back = PlayerUiKit.Image("Back", transform, CardBackColor);
        back.raycastTarget = true;
        PlayerUiKit.Stretch(back.rectTransform);

        border = PlayerUiKit.Image("Border", transform, new Color(0f, 0f, 0f, 0f));
        border.raycastTarget = false;
        PlayerUiKit.Stretch(border.rectTransform);

        Button button = back.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            if (!locked) Clicked?.Invoke(index);
        });

        RectTransform iconRect = PlayerUiKit.Rect("Icon", transform);
        PlayerUiKit.Pin(iconRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(110, 110));
        iconBack = iconRect.gameObject.AddComponent<Image>();
        iconBack.raycastTarget = false;
        iconBack.color = IconTintDefault;
        iconText = PlayerUiKit.Text("IconGlyph", iconRect, 48, TextAnchor.MiddleCenter, Color.white);
        PlayerUiKit.Stretch(iconText.rectTransform);

        titleText = PlayerUiKit.Text("Title", transform, 26, TextAnchor.MiddleCenter, TitleColor);
        titleText.fontStyle = FontStyle.Bold;
        PlayerUiKit.Pin(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -142), new Vector2(280, 34));

        valueText = PlayerUiKit.Text("Value", transform, 20, TextAnchor.MiddleCenter, ValueColor);
        PlayerUiKit.Pin(valueText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -184), new Vector2(280, 26));

        descriptionText = PlayerUiKit.Text("Description", transform, 18, TextAnchor.MiddleCenter, DescriptionColor);
        PlayerUiKit.Pin(descriptionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -216), new Vector2(272, 132));
    }

    public void Present(UpgradeCardData card, int index)
    {
        this.index = index;
        titleText.text = card.title;
        valueText.text = card.valueText;
        descriptionText.text = card.description;

        Color tint;
        switch (card.iconKey)
        {
            case "heart": tint = new Color(0.8f, 0.25f, 0.25f, 1f); break;
            case "sword": tint = new Color(0.55f, 0.6f, 0.7f, 1f); break;
            case "boots": tint = new Color(0.68f, 0.5f, 0.32f, 1f); break;
            default: tint = IconTintDefault; break;
        }
        iconBack.color = tint;
        iconText.text = string.IsNullOrEmpty(card.iconKey)
            ? "?"
            : char.ToUpperInvariant(card.iconKey[0]).ToString();
    }

    public void SetSelected(bool selected)
    {
        border.color = selected ? SelectedBorderColor : new Color(0f, 0f, 0f, 0f);
    }

    public void SetInteractable(bool interactable)
    {
        locked = !interactable;
        group.alpha = interactable ? 1f : 0.45f;
        group.blocksRaycasts = interactable;
    }
}
