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
        if (!RunSession.EnterFromMenu)
        {
            // Direct scene open (e.g. Editor Play Mode): the test-only SpawnSystemTestDriver owns the
            // run and runs its automated checks; this bootstrap stays out of the way.
            return;
        }
        if (spawnSystem == null)
        {
            Debug.LogWarning("[RunBootstrap] spawnSystem reference is null; run not started");
            return;
        }

        spawnSystem.FloorCleared += OnFloorCleared;

        WireGameOverFlow();

        SaveData save;
        if (saves.TryLoad(out save) && Run.TryRestore(save))
        {
            Debug.Log($"[RunBootstrap] Resumed run: floor {Run.Data.floor}");
        }
        else
        {
            Run.StartRun();
            saves.Save(Run.Capture()); // initial checkpoint: floor 1, before it is played
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
    /// Report-only bridge (same contract as the test driver): SpawnSystem REPORTS a real all-clear;
    /// this asks the RunController to handle it. The state machine's guarded transitions prevent
    /// restart loops (FloorCleared is only reachable from FloorActive via a real all-clear).
    /// </summary>
    void OnFloorCleared()
    {
        if (!spawnSystem.IsFloorCleared) return;
        if (!Run.CompleteFloor()) return;
        StartCoroutine(AdvanceToNextFloor());
    }

    IEnumerator AdvanceToNextFloor()
    {
        yield return new WaitForSeconds(floorClearPauseSeconds);
        Run.StartNextFloor();               // FloorCleared -> FloorStart + RunData.AdvanceFloor()
        saves.Save(Run.Capture());          // checkpoint: next floor's start, before it is played
        PopulateAndBeginFloor();
    }
}
