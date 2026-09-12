using System;

// Harness-only doubles for the scene scripts the game-over flow wires together (PlayerController,
// PauseController, PlayerUiBootstrap). The REAL versions live under Assets/Scripts but are not part
// of the harness compile list (see spawn_integration_test.csproj); these stubs exist purely so
// RunBootstrap/GameOverFlow compile headlessly and their logic can be driven by tests. They carry
// state only — they implement no behavior.

/// <summary>Minimal stand-in for the combat-owned player entity; GameOverFlow reads Health and
/// subscribes OnDied (the real PlayerEntity raises it exactly once per life). OnDied is a FIELD,
/// not an event — same idiom as RetryRequested below — so tests can raise it externally.</summary>
public class PlayerEntity
{
    public float Health = 100f;
    public Action OnDied;
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    public PlayerEntity Entity;
}

public class PauseController : UnityEngine.MonoBehaviour
{
    public bool enabled = true;
}

/// <summary>Stand-in exposing exactly the surface RunBootstrap/GameOverFlow consume. Note that the
/// Retry/MainMenu delegates are FIELDS (the real type declares events) so tests can raise them.</summary>
public class PlayerUiBootstrap : UnityEngine.MonoBehaviour
{
    public Action RetryRequested;
    public Action MainMenuRequested;
    public MockGameOverSource GameOverSource;
}

public class RoguelikeProgressionBootstrap : UnityEngine.MonoBehaviour
{
    public bool IsSelectingUpgrade => false;
    public void CaptureProgression(SaveData save) { }
}

public class FloorTransitionManager : UnityEngine.MonoBehaviour
{
    public object ExitPortal => null;
}
