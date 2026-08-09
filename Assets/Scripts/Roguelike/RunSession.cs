/// <summary>
/// Scene-entry handoff between the Main Menu and the game scene. The menu sets EnterFromMenu=true
/// right before SceneManager.LoadScene(gameSceneName): the game scene's RunBootstrap then owns the
/// run (Continue or New Run) and the test-only SpawnSystemTestDriver defers its automated checks.
/// When the scene is opened directly (e.g. Editor Play Mode), the flag is false and the automated
/// spawn test drives the scene exactly as before.
///
/// This is a plain boolean flag — not a singleton, not a service, not an EventBus. It carries no
/// state beyond "which entry path started this scene load"; the run's continue-vs-new decision is
/// inferred from the save file, not from this flag.
/// </summary>
public static class RunSession
{
    public static bool EnterFromMenu;
}
