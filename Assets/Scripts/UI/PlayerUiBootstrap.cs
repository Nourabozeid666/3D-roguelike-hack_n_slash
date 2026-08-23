using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the whole Player UI (HUD, upgrade screen, game over screen) at runtime under its own
/// Screen-Space-Overlay canvas and wires it to TEMPORARY mock sources behind the real interfaces
/// (IPlayerHudSource / IUpgradeSource / IGameOverSource). The real player-stat/run systems swap in
/// behind those same interfaces later — this bootstrap only exists to make the UI playable in
/// TestingScene now, and it exposes the mocks/presenters so test drivers can push data and observe
/// state. The whole tree is runtime-built, so the scene only holds one GameObject with this script.
/// </summary>
public class PlayerUiBootstrap : MonoBehaviour
{
    public event Action Ready;

    /// <summary>Raised after RetryRun() reset the screens/HUD mocks — the run owner subscribes to
    /// restart the actual run (delete save + scene reload). UI owns none of that.</summary>
    public event Action RetryRequested;

    /// <summary>Raised when the game-over screen's main-menu button is clicked — the run owner
    /// subscribes to load the menu scene.</summary>
    public event Action MainMenuRequested;

    public MockPlayerHudSource HudSource { get; private set; }
    public MockUpgradeSource UpgradeSource { get; private set; }
    public MockGameOverSource GameOverSource { get; private set; }

    public PlayerHudPresenter HudPresenter { get; private set; }
    public UpgradeSelectPresenter UpgradePresenter { get; private set; }
    public GameOverPresenter GameOverPresenter { get; private set; }

    public PlayerHudController HudController { get; private set; }
    public UpgradeSelectController UpgradeSelectController { get; private set; }
    public GameOverScreenController GameOverScreenController { get; private set; }

    [SerializeField] private bool enableDemoDriver = true;

    void Awake()
    {
        Build();
    }

    void Start()
    {
        if (enableDemoDriver)
            gameObject.AddComponent<PlayerUiDemoDriver>();
    }

    void Build()
    {
        GameObject canvasGo = new GameObject("PlayerUI");
        canvasGo.layer = 5;
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        canvasGo.AddComponent<GraphicRaycaster>();
        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();

        GameObject hudGo = new GameObject("Hud", typeof(RectTransform));
        hudGo.transform.SetParent(canvasRect, false);
        HudController = hudGo.AddComponent<PlayerHudController>();
        HudController.Initialize();

        GameObject selectGo = new GameObject("UpgradeSelect", typeof(RectTransform));
        selectGo.transform.SetParent(canvasRect, false);
        UpgradeSelectController = selectGo.AddComponent<UpgradeSelectController>();
        UpgradeSelectController.Initialize();

        GameObject overGo = new GameObject("GameOver", typeof(RectTransform));
        overGo.transform.SetParent(canvasRect, false);
        GameOverScreenController = overGo.AddComponent<GameOverScreenController>();
        GameOverScreenController.Initialize();

        HudSource = new MockPlayerHudSource();
        UpgradeSource = new MockUpgradeSource();
        GameOverSource = new MockGameOverSource();

        HudPresenter = new PlayerHudPresenter(HudController);
        UpgradePresenter = new UpgradeSelectPresenter(UpgradeSelectController);
        GameOverPresenter = new GameOverPresenter(GameOverScreenController);

        UpgradeSelectController.CardClicked += index => UpgradePresenter.Select(index);
        GameOverScreenController.RetryClicked += RetryRun;
        GameOverScreenController.MainMenuClicked += () => MainMenuRequested?.Invoke();

        HudPresenter.Bind(HudSource);
        UpgradePresenter.Bind(UpgradeSource);
        GameOverPresenter.Bind(GameOverSource);

        Ready?.Invoke();
    }

    /// <summary>Close the end screens and reset the mock HUD to defaults (a fresh run), then raise
    /// RetryRequested so the run owner can restart the real run.</summary>
    public void RetryRun()
    {
        GameOverScreenController.Hide();
        UpgradeSelectController.Hide();
        HudSource.SetPlayerHud(PlayerHudData.Default());
        RetryRequested?.Invoke();
    }
}
