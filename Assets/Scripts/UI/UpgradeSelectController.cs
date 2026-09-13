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

    [Header("Input Tuning")]
    [SerializeField] private float selectionCooldown = 0.5f;

    readonly List<UpgradeCardController> cards = new();
    RectTransform cardRow;
    float canSelectTime;
    bool areCardsClickable;

    public event Action<int> CardClicked;

    public float SelectionCooldown
    {
        get => selectionCooldown;
        set => selectionCooldown = Mathf.Max(0f, value);
    }

    public bool CanSelect => Time.unscaledTime >= canSelectTime;

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
        canSelectTime = Time.unscaledTime + selectionCooldown;
        areCardsClickable = false;

        for (int i = 0; i < offered.Count; i++)
        {
            GameObject cardGo = new GameObject("Card" + i, typeof(RectTransform));
            cardGo.transform.SetParent(cardRow, false);
            UpgradeCardController card = cardGo.AddComponent<UpgradeCardController>();
            card.Initialize();
            card.Present(offered[i], i);
            card.SetClickable(false);
            card.Clicked += idx => OnCardClickedInternal(idx);
            cards.Add(card);
        }
        LayoutCards();
        gameObject.SetActive(true);
    }

    void OnCardClickedInternal(int index)
    {
        if (!CanSelect) return;
        CardClicked?.Invoke(index);
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
        areCardsClickable = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy || cards.Count == 0) return;

        if (!areCardsClickable && CanSelect)
        {
            areCardsClickable = true;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    cards[i].SetClickable(true);
                }
            }
        }

        if (!CanSelect) return;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            {
                if (cards.Count > 0) OnCardClickedInternal(0);
            }
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            {
                if (cards.Count > 1) OnCardClickedInternal(1);
            }
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
            {
                if (cards.Count > 2) OnCardClickedInternal(2);
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                if (cards.Count > 0) OnCardClickedInternal(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                if (cards.Count > 1) OnCardClickedInternal(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                if (cards.Count > 2) OnCardClickedInternal(2);
            }
        }
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
        const float cardWidth = 300f;
        float totalWidth = cards.Count * cardWidth + Mathf.Max(0, cards.Count - 1) * gap;
        float startX = -totalWidth * 0.5f + cardWidth * 0.5f;
        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform rect = (RectTransform)cards[i].transform;
            float x = startX + i * (cardWidth + gap);
            PlayerUiKit.Pin(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0), rect.sizeDelta);
        }
    }
}
