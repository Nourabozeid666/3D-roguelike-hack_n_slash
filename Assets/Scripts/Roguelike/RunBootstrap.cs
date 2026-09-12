using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Production run owner for the game scene (TestingScene.unity). When the scene is entered from the
/// Main Menu (RunSession.EnterFromMenu), it either resumes the saved run (Continue) or starts a fresh
/// floor-1 run (New Run), populates the floor through SpawnSystem and flips the run to FloorActive.
///
/// Persistence boundary: RunBootstrap owns WHEN saves happen, RunSaveService owns HOW, RunController
/// owns run state. Save points are the START of every floor (including the first) — a checkpoint is
/// written before the floor is played, so quitting at any time resumes that floor from its start and
/// a run is never silently lost. Mid-floor state (alive enemies) is NOT persisted: the real Enemy
/// System is not integrated yet, so a resumed floor is always repopulated fresh.
///
/// No singleton, no EventBus: this MonoBehaviour is the single scene instance that bridges
/// SpawnSystem's report-only FloorCleared event to the RunController's guarded transitions.
/// </summary>
public class RunBootstrap : MonoBehaviour
{
    [SerializeField] SpawnSystem spawnSystem;
    [SerializeField] PlayerUiBootstrap playerUi;
    [SerializeField] PauseController pauseController;

    public const string GameSceneName = "TestingScene";
    public const string MenuSceneName = "MainMenu";

    /// <summary>How long the run stays in FloorCleared before the next floor populates, so the
    /// cleared state is visible (matches the test driver's default). Tuning value.</summary>
    [SerializeField] float floorClearPauseSeconds = 1f;

    RunSaveService saves;

    void Awake()
    {
        saves = new();
    }

    /// <summary>The real RunController this bootstrap drives, exposed so the debug HUD can read live
    /// run state/floor (SpawnTestDebugDisplay).</summary>
    public RunController Run { get; } = new();

    void OnDestroy()
    {
        if (spawnSystem != null) spawnSystem.FloorCleared -= OnFloorCleared;
        if (playerUi != null)
        {
            playerUi.RetryRequested -= OnRetryRequested;
            playerUi.MainMenuRequested -= OnMainMenuRequested;
        }
    }

    void Start()
    {
        SpawnSystemTestDriver testDriver = Object.FindFirstObjectByType<SpawnSystemTestDriver>();
        if (!RunSession.EnterFromMenu && testDriver != null && testDriver.enabled)
        {
            // Direct scene open with an active test harness: let test driver run
            return;
        }

        if (spawnSystem == null)
        {
            spawnSystem = Object.FindFirstObjectByType<SpawnSystem>();
        }

        if (spawnSystem == null)
        {
            Debug.LogWarning("[RunBootstrap] spawnSystem reference is null; run not started");
            return;
        }

        spawnSystem.FloorCleared += OnFloorCleared;

        WireGameOverFlow();
        WireProgression();
        WireTransitionManager();

        SaveData save;
        if (saves.TryLoad(out save) && Run.TryRestore(save))
        {
            Debug.Log($"[RunBootstrap] Resumed run: region {Run.Data.currentRegionIndex + 1}, floor {Run.Data.floor}");
        }
        else
        {
            Run.StartRun();
            SaveData initialSave = Run.Capture();
            RoguelikeProgressionBootstrap prog = Object.FindFirstObjectByType<RoguelikeProgressionBootstrap>();
            prog?.CaptureProgression(initialSave);
            saves.Save(initialSave);
            Debug.Log("[RunBootstrap] Started a new run: floor 1");
        }

        PopulateAndBeginFloor();
    }

    /// <summary>Populate the current floor's budget and flip FloorStart -> FloorActive.</summary>
    void PopulateAndBeginFloor()
    {
        spawnSystem.Populate(Run.Data.enemyBudget, Run.Data.floor);
        Run.BeginFloor();
    }

    /// <summary>
    /// Game-over wiring (production mode only): create the runtime GameOverFlow bridge and route
    /// the end screen's Retry / Main Menu intents back here. Serialized references win; the
    /// FindFirstObjectByType fallbacks keep this working when only some references are assigned.
    /// Every collaborator is optional — a scene without UI/pause/player degrades to a flow that
    /// still ends the run, just without publishing a summary.
    /// </summary>
    void WireGameOverFlow()
    {
        if (playerUi == null) playerUi = Object.FindFirstObjectByType<PlayerUiBootstrap>();
        if (pauseController == null) pauseController = Object.FindFirstObjectByType<PauseController>();

        GameObject flowGo = new GameObject("GameOverFlow");
        GameOverFlow flow = flowGo.AddComponent<GameOverFlow>();
        flow.Configure(
            Object.FindFirstObjectByType<PlayerController>(),
            spawnSystem,
            Run,
            playerUi,
            pauseController);

        if (playerUi == null)
        {
            Debug.LogWarning("[RunBootstrap] PlayerUiBootstrap not found; retry/menu intents unwired");
            return;
        }
        playerUi.RetryRequested += OnRetryRequested;
        playerUi.MainMenuRequested += OnMainMenuRequested;
    }

    void WireProgression()
    {
        if (Object.FindFirstObjectByType<RoguelikeProgressionBootstrap>() != null) return;
        GameObject progGo = new GameObject("ProgressionBootstrap");
        progGo.AddComponent<RoguelikeProgressionBootstrap>();
    }

    void WireTransitionManager()
    {
        if (Object.FindFirstObjectByType<FloorTransitionManager>() != null) return;
        GameObject transitionGo = new GameObject("FloorTransitionManager");
        transitionGo.AddComponent<FloorTransitionManager>();
    }

    /// <summary>Game Over > Retry: clean new run with exactly Main Menu > New Run semantics — delete
    /// the save, unfreeze time, reload the game scene so this bootstrap starts a fresh floor 1.</summary>
    void OnRetryRequested()
    {
        Time.timeScale = 1f;
        saves.Delete();
        RunSession.EnterFromMenu = true;
        SceneManager.LoadScene(GameSceneName);
    }

    /// <summary>Game Over > Main Menu: unfreeze time and load the menu. The save is kept on purpose:
    /// the checkpoint written at the current floor's start stays available via Continue.</summary>
    void OnMainMenuRequested()
    {
        Time.timeScale = 1f;
        RunSession.EnterFromMenu = true;
        SceneManager.LoadScene(MenuSceneName);
    }

    /// <summary>
    /// Report-only bridge: SpawnSystem REPORTS a real all-clear;
    /// if FloorTransitionManager is active, it coordinates the Exit Portal.
    /// Otherwise, falls back to the default timer advance.
    /// </summary>
    void OnFloorCleared()
    {
        if (!spawnSystem.IsFloorCleared) return;
        if (!Run.CompleteFloor()) return;

        FloorTransitionManager transitionMgr = Object.FindFirstObjectByType<FloorTransitionManager>();
        if (transitionMgr == null || !transitionMgr.enabled || transitionMgr.ExitPortal == null)
        {
            StartCoroutine(AdvanceToNextFloor());
        }
    }

    IEnumerator AdvanceToNextFloor()
    {
        yield return new WaitForSeconds(floorClearPauseSeconds);
        Run.StartNextFloor();               // FloorCleared -> FloorStart + RunData.AdvanceFloor()
        saves.Save(Run.Capture());          // checkpoint: next floor's start, before it is played
        PopulateAndBeginFloor();
    }
}
