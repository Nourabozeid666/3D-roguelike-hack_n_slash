using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity view for the Game Over screen: title, run summary (floor reached / enemies defeated / run
/// time) and RETRY + MAIN MENU buttons. Implements IGameOverView; GameOverPresenter drives it. Present
/// fills the summary AND shows the screen — it is only ever reached via a Changed event (a run
/// ending), never on bind. Buttons raise events for the host to handle (no scene loads are hardcoded
/// here). Unity-only: the summary formatting lives in the harness-tested GameOverData contract.
/// </summary>
public class GameOverScreenController : MonoBehaviour, IGameOverView
{
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color PanelColor = new Color(0.09f, 0.1f, 0.14f, 0.98f);
    static readonly Color TitleColor = new Color(0.85f, 0.22f, 0.22f, 1f);
    static readonly Color ButtonColor = new Color(0.2f, 0.34f, 0.6f, 1f);
    static readonly Color SecondaryButtonColor = new Color(0.24f, 0.26f, 0.32f, 1f);

    Text floorText;
    Text enemiesText;
    Text timeText;

    public event Action RetryClicked;
    public event Action MainMenuClicked;

    /// <summary>Build the screen. The controller's own RectTransform IS the full-screen root.</summary>
    public void Initialize()
    {
        RectTransform root = (RectTransform)transform;
        PlayerUiKit.Stretch(root);

        Image dim = PlayerUiKit.Image("Dim", transform, DimColor);
        dim.raycastTarget = true;
        PlayerUiKit.Stretch(dim.rectTransform);

        RectTransform panel = PlayerUiKit.Rect("Panel", transform);
        PlayerUiKit.Pin(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 660));
        Image panelBack = panel.gameObject.AddComponent<Image>();
        panelBack.color = PanelColor;
        panelBack.raycastTarget = true;

        Text title = PlayerUiKit.Text("Title", panel, 64, TextAnchor.MiddleCenter, TitleColor);
        title.fontStyle = FontStyle.Bold;
        title.text = "GAME OVER";
        PlayerUiKit.Pin(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(780, 90));

        floorText = PlayerUiKit.Text("FloorText", panel, 32, TextAnchor.MiddleCenter, Color.white);
        PlayerUiKit.Pin(floorText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(780, 44));

        enemiesText = PlayerUiKit.Text("EnemiesText", panel, 32, TextAnchor.MiddleCenter, Color.white);
        PlayerUiKit.Pin(enemiesText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -222), new Vector2(780, 44));

        timeText = PlayerUiKit.Text("TimeText", panel, 32, TextAnchor.MiddleCenter, Color.white);
        PlayerUiKit.Pin(timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -274), new Vector2(780, 44));

        Button retry = PlayerUiKit.Button("RetryButton", panel, ButtonColor);
        PlayerUiKit.Pin(retry.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150, -210), new Vector2(280, 60));
        Text retryLabel = PlayerUiKit.Text("Label", retry.transform, 24, TextAnchor.MiddleCenter, Color.white);
        PlayerUiKit.Stretch(retryLabel.rectTransform);
        retryLabel.text = "RETRY / NEW RUN";
        retry.onClick.AddListener(() => RetryClicked?.Invoke());

        Button mainMenu = PlayerUiKit.Button("MainMenuButton", panel, SecondaryButtonColor);
        PlayerUiKit.Pin(mainMenu.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150, -210), new Vector2(280, 60));
        Text menuLabel = PlayerUiKit.Text("Label", mainMenu.transform, 24, TextAnchor.MiddleCenter, Color.white);
        PlayerUiKit.Stretch(menuLabel.rectTransform);
        menuLabel.text = "MAIN MENU";
        mainMenu.onClick.AddListener(() => MainMenuClicked?.Invoke());

        gameObject.SetActive(false);
    }

    public void Present(in GameOverData data)
    {
        floorText.text = $"FLOOR REACHED: {data.floorReached}";
        enemiesText.text = $"ENEMIES DEFEATED: {data.enemiesDefeated}";
        timeText.text = $"RUN TIME: {data.RunTimeText()}";
        gameObject.SetActive(true);
    }

    /// <summary>Hide the screen (e.g. a retry starts a new run).</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
