using System;

// Harness-only doubles for the scene scripts the game-over flow wires together (PlayerController,
// PauseController, PlayerUiBootstrap). The REAL versions live under Assets/Scripts but are not part
// of the harness compile list (see spawn_integration_test.csproj); these stubs exist purely so
// RunBootstrap/GameOverFlow compile headlessly and their logic can be driven by tests. They carry
// state only — they implement no behavior.

/// <summary>Minimal stand-in for the combat-owned player entity; GameOverFlow only reads Health.</summary>
public class StubPlayerEntity
{
    public float Health = 100f;
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    public StubPlayerEntity Entity;
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
