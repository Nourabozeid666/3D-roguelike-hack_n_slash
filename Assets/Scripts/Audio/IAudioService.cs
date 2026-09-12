using UnityEngine;

/// <summary>
/// Caller-facing seam for the Audio Core. Gameplay and UI code depend on this interface, not on
/// AudioCore, AudioMixerGroups, or raw AudioSources - so callers never learn about mixer internals
/// and the implementation can change without editing consumers.
///
/// IMPORTANT (Phase 1 scope): the SFX/Music methods exist so integration points can be proven, but
/// the core does NOT yet contain gameplay-specific sound wiring. Specific sounds arrive in later
/// content/integration phases and simply call PlaySfx/PlayMusic with their own clips.
///
/// Every method is safe to call before/after full initialization; a properly implemented service
/// degrades gracefully (no-op) when no mixer or clip is available, so callers do not need guards.
/// </summary>
public interface IAudioService
{
    /// <summary>True after the core has been initialized and is ready to route audio.</summary>
    bool IsAvailable { get; }

    /// <summary>Currently configured settings (read-only view for menus/debug).</summary>
    AudioMixerSettings Settings { get; }

    /// <summary>Sets the normalized (0..1) volume for a bus and applies it to output.</summary>
    void SetVolume(AudioBus bus, float normalized);

    /// <summary>Returns the current normalized (0..1) volume for a bus.</summary>
    float GetVolume(AudioBus bus);

    /// <summary>Plays a one-shot SFX on the SFX bus at an optional per-play gain.</summary>
    void PlaySfx(AudioClip clip, float volume = 1f);

    /// <summary>Plays a clip on the music bus (optionally looping). Re-uses the single music source.</summary>
    void PlayMusic(AudioClip clip, bool loop = true);

    /// <summary>Stops music playback, if any.</summary>
    void StopMusic();
}
