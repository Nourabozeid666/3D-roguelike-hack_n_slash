using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity view for the upgrade selection screen: a full-screen dim overlay + centered panel holding a
/// horizontal row of upgrade cards. Implements IUpgradeSelectView; UpgradeSelectPresenter drives it.
/// ShowSelection is list-driven — one card per offered UpgradeCardData, rebuilt on every offer, never
/// hardcoded. Card clicks are forwarded through CardClicked; the host routes them to the presenter's
/// Select (pick + lock). Unity-only: the pick/lock rules live in the harness-tested presenter.
/// </summary>
public class UpgradeSelectController : MonoBehaviour, IUpgradeSelectView
{
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color PanelColor = new Color(0.09f, 0.1f, 0.14f, 0.98f);

    readonly List<UpgradeCardController> cards = new();
    RectTransform cardRow;

    public event Action<int> CardClicked;

    /// <summary>Build the overlay + panel. The screen starts hidden until the first real offer.</summary>
    public void Initialize()
    {
        RectTransform root = (RectTransform)transform;
        PlayerUiKit.Stretch(root);

        Image dim = PlayerUiKit.Image("Dim", transform, DimColor);
        dim.raycastTarget = true;
        PlayerUiKit.Stretch(dim.rectTransform);

        RectTransform panel = PlayerUiKit.Rect("Panel", transform);
        PlayerUiKit.Pin(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 560));
        Image panelBack = panel.gameObject.AddComponent<Image>();
        panelBack.color = PanelColor;
        panelBack.raycastTarget = true;

        Text title = PlayerUiKit.Text("Title", panel, 40, TextAnchor.MiddleCenter, Color.white);
        title.fontStyle = FontStyle.Bold;
        title.text = "CHOOSE AN UPGRADE";
        PlayerUiKit.Pin(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(1100, 64));

        cardRow = PlayerUiKit.Rect("Cards", panel);
        PlayerUiKit.Pin(cardRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1040, 380));

        gameObject.SetActive(false);
    }

    public void ShowSelection(IReadOnlyList<UpgradeCardData> offered)
    {
        ClearCards();
        for (int i = 0; i < offered.Count; i++)
        {
            GameObject cardGo = new GameObject("Card" + i, typeof(RectTransform));
            cardGo.transform.SetParent(cardRow, false);
            UpgradeCardController card = cardGo.AddComponent<UpgradeCardController>();
            card.Initialize();
            card.Present(offered[i], i);
            cards.Add(card);
        }
        LayoutCards();
        gameObject.SetActive(true);
    }

    public void SetCardState(int index, bool enabled, bool selected)
    {
        if (index < 0 || index >= cards.Count) return;
        cards[index].SetInteractable(enabled);
        cards[index].SetSelected(selected);
    }

    /// <summary>Hide the screen (e.g. the run ended mid-offer, or a retry).</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void ClearCards()
    {
        for (int i = cards.Count - 1; i >= 0; i--)
            Destroy(cards[i].gameObject);
        cards.Clear();
    }

    void LayoutCards()
    {
        const float gap = 30f;
        float x = 0f;
        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform rect = (RectTransform)cards[i].transform;
            PlayerUiKit.Pin(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0), rect.sizeDelta);
            x += rect.sizeDelta.x + gap;
        }
    }
}
