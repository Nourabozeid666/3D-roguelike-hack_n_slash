using System;

/// <summary>
/// Pure, serializable decibel-based audio volume model. No Unity API - harness-testable.
///
/// The final system drives an AudioMixer's exposed *Volume* parameters using decibels
/// rather than directly setting AudioListener.volume. This class owns the mapping between
/// a normalized slider value (0..1) and the runner value actually applied (decibels, below 0dB),
/// so the rest of the system never has to reason in decibels.
///
/// Defaults are deliberately sane but authorable: each bus keeps a CurrentVolume (0..1) and the
/// mixer exposes a matching dB value derived through MapToDecibels().
///
/// NOTE: this file intentionally contains no reference to UnityEngine.AudioMixer. AudioCore
/// reads these values and is responsible for writing them to the mixer (or the per-source
/// fallback). Keeping the math here lets it run under the dotnet harness.
/// </summary>
[Serializable]
public class AudioMixerSettings
{
    /// <summary>Master output volume (0..1).</summary>
    public float MasterVolume = 1f;

    /// <summary>Music bus volume (0..1).</summary>
    public float MusicVolume = 0.8f;

    /// <summary>SFX bus volume (0..1).</summary>
    public float SfxVolume = 1f;

    /// <summary>UI bus volume (0..1).</summary>
    public float UiVolume = 0.9f;

    /// <summary>Reserved for a future Ambient pass.</summary>
    public float AmbientVolume = 0.8f;

    // Decibel window. 0dB (slider 1) is full; the floor (-80dB, effectively silent) is the
    // conventional Unity near-silence bound so a 0 slider truly silences the bus.
    public const float MaxDecibels = 0f;
    public const float MinDecibels = -80f;

    /// <summary>
    /// Maps a normalized volume (0..1) to the decibels the mixer should receive.
    /// Uses a simple power curve so the middle of the slider is audible but not clipping.
    /// </summary>
    public float MapToDecibels(float normalized)
    {
        float clamped = Clamp01(normalized);
        if (clamped <= 0f)
        {
            return MinDecibels;
        }

        // Re-map through an exponent so low slider values taper instead of snapping off:
        // 1.0 -> 0dB, 0.5 -> about -30dB, near 0 -> -80dB. Simple, monotonic, no clipping.
        double t = Math.Pow(clamped, 3.0);                          // 0..1
        double interp = MaxDecibels + (1.0 - t) * (MinDecibels - MaxDecibels);
        return (float)interp;
    }

    /// <summary>
    /// Maps a normalized volume (0..1) to a LINEAR amplitude (0..1) suitable for AudioSource.volume.
    /// Used only by the per-source fallback path; the real mixer path uses MapToDecibels + SetFloat.
    /// </summary>
    public float MapToLinear(float normalized)
    {
        if (Clamp01(normalized) <= 0f)
        {
            return 0f;
        }

        float dB = MapToDecibels(normalized);
        return (float)Math.Pow(10.0, dB / 20.0);
    }

    /// <summary>Normalized volume for a bus, defaulting sensibly if never assigned.</summary>
    public float GetVolume(AudioBus bus)
    {
        switch (bus)
        {
            case AudioBus.Master: return MasterVolume;
            case AudioBus.Music: return MusicVolume;
            case AudioBus.Sfx: return SfxVolume;
            case AudioBus.Ui: return UiVolume;
            case AudioBus.Ambient: return AmbientVolume;
            default: return 1f;
        }
    }

    /// <summary>Sets the normalized volume for a bus (0..1).</summary>
    public void SetVolume(AudioBus bus, float normalized)
    {
        float v = Clamp01(normalized);
        switch (bus)
        {
            case AudioBus.Master: MasterVolume = v; break;
            case AudioBus.Music: MusicVolume = v; break;
            case AudioBus.Sfx: SfxVolume = v; break;
            case AudioBus.Ui: UiVolume = v; break;
            case AudioBus.Ambient: AmbientVolume = v; break;
        }
    }

    private static float Clamp01(float value)
    {
        if (value < 0f) return 0f;
        if (value > 1f) return 1f;
        return value;
    }
}
