using UnityEngine;

/// <summary>
/// Runtime bridge from a live run to the Game Over screen. Created by RunBootstrap (production mode
/// only): it polls the player's health and, on death, stops gameplay exactly once — wave release
/// off first (no further spawns or clears), then run -> RunEnd, time frozen, cursor freed and the
/// PauseController disabled so pause input cannot stack on top of the end screen — and publishes
/// the real summary through the existing IGameOverSource seam (the UI keeps listening to Changed).
///
/// Polling instead of a death event is deliberate: through PlayerController the entity is only
/// typed as IEntity (no death member on that interface), and Update runs even at
/// Time.timeScale == 0, so a lethal hit landing on the freeze frame is still detected.
/// PlayerEntity additionally exposes a concrete OnDied event for direct listeners (HUD/audio).
/// The elapsed run time uses scaled Time.time captured at Configure (run start),
/// which excludes paused seconds by construction. Scene navigation stays in RunBootstrap: retry /
/// main-menu intents arrive only as PlayerUiBootstrap events the bootstrap subscribes to.
/// </summary>
public class GameOverFlow : MonoBehaviour
{
    PlayerController player;
    SpawnSystem spawnSystem;
    RunController run;
    PlayerUiBootstrap ui;
    PauseController pause;

    float startedAt;
    bool triggered;

    /// <summary>True once the death flow ran; Trigger() and Update() are no-ops afterwards.</summary>
    public bool Triggered => triggered;

    /// <summary>Wire the flow to its collaborators. Every reference is optional by design: missing
    /// pieces degrade to no-ops (e.g. test-driver scenes without a player/UI), never exceptions.</summary>
    public void Configure(PlayerController player, SpawnSystem spawnSystem, RunController run,
        PlayerUiBootstrap ui, PauseController pause)
    {
        this.player = player;
        this.spawnSystem = spawnSystem;
        this.run = run;
        this.ui = ui;
        this.pause = pause;
        startedAt = Time.time;
        triggered = false;
    }

    void Update()
    {
        if (triggered || player == null || player.Entity == null) return;
        if (player.Entity.Health > 0f) return;
        Trigger();
    }

    /// <summary>Stop gameplay and publish the summary exactly once: waves off, run ended, time
    /// frozen, cursor unlocked, pause disabled, real GameOverData pushed through IGameOverSource.</summary>
    public void Trigger()
    {
        if (triggered) return;
        triggered = true;

        if (spawnSystem != null)
            spawnSystem.DisableWaveRelease();

        if (run != null)
            run.EndRun(); // guarded transition; false here only means "already ended"

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (pause != null)
            pause.enabled = false;

        if (ui != null && ui.GameOverSource != null)
        {
            int floor = run != null ? Mathf.Max(1, run.CurrentFloor) : 1;
            int defeated = spawnSystem != null ? spawnSystem.TotalDefeated : 0;
            float seconds = Mathf.Max(0f, Time.time - startedAt);
            ui.GameOverSource.SetGameOver(new GameOverData(floor, defeated, seconds));
        }
    }
}
