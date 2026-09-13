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

    /// <summary>The real production HUD source bound in ConnectRealHudSource (null outside the
    /// menu-entry path). Exposed so RoguelikeProgressionBootstrap can attach the live XP/level feed
    /// regardless of Start-order between the two bootsraps.</summary>
    public RunPlayerHudSource RealHudSource { get; private set; }

    [SerializeField] private bool enableDemoDriver = true;

    IPlayerHudSource boundHud;

    void Awake()
    {
        Build();
    }

    void Start()
    {
        // Production entry (Main Menu -> game scene): bind the REAL health/floor source and skip the
        // test demo driver. Direct scene open (Editor Play Mode) keeps the mock + demo driver as-is.
        if (RunSession.EnterFromMenu)
        {
            ConnectRealHudSource();
            return;
        }
        if (enableDemoDriver)
            gameObject.AddComponent<PlayerUiDemoDriver>();
    }

    /// <summary>Production-mode HUD source: locate the live player + run bootstrap and swap the real
    /// health/floor feed in behind the same IPlayerHudSource seam. No-ops gracefully when either the
    /// player or the run owner is absent (authoring scene).</summary>
    void ConnectRealHudSource()
    {
        RunBootstrap runBootstrap = UnityEngine.Object.FindFirstObjectByType<RunBootstrap>();
        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (runBootstrap == null || player == null || player.Entity == null) return;

        // Find the progression host if it was already created (Awake/AfterSceneLoad).
        // If not yet present, RoguelikeProgressionBootstrap will attach via SetProgression in its
        // own Start, covering whichever Start ordering occurs.
        RoguelikeProgressionBootstrap progressionHost =
            UnityEngine.Object.FindFirstObjectByType<RoguelikeProgressionBootstrap>();
        ProgressionSystem progression = progressionHost != null ? progressionHost.Progression : null;

        RunPlayerHudSource source = new RunPlayerHudSource(
            player.Entity,
            () => runBootstrap.Run.CurrentFloor,
            progression);
        RealHudSource = source;
        source.Enable();
        BindHudSource(source);
    }

    /// <summary>Swap the currently-bound HUD source for another (unchanged mock-&gt;real binding seam).
    /// The presenter unbinds the old and binds the new; identical to the initial mock bind, so
    /// presenter logic is unchanged.</summary>
    public void BindHudSource(IPlayerHudSource source)
    {
        if (source == null || source == boundHud) return;
        if (boundHud != null)
        {
            if (boundHud is RunPlayerHudSource real) real.Disable();
            if (HudPresenter != null) HudPresenter.Unbind(boundHud);
        }
        boundHud = source;
        if (HudPresenter != null) HudPresenter.Bind(boundHud);
    }

    void Build()
    {
        var existingEs = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingEs == null)
        {
            GameObject esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            var inputModule = esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
        else
        {
            var uiModule = existingEs.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule != null && uiModule.actionsAsset == null)
            {
                uiModule.AssignDefaultActions();
            }
        }

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

        boundHud = HudSource; // initial (mock) bind; production swaps via BindHudSource
        HudPresenter.Bind(boundHud);
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
