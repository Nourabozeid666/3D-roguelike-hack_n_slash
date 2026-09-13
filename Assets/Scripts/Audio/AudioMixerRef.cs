using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Read-only asset that tells the Audio Core how to reach the AudioMixer and its groups.
///
/// This is the ONLY place gameplay-visible code may be told about mixer groups: the concrete
/// AudioCore reads it, gameplay/UI depend only on IAudioService and never see mixer groups.
///
/// In this phase the asset is optional. If Mixer (or a needed group/parameter) is null/missing,
/// AudioCore falls back to per-AudioSource volume driven by the same AudioMixerSettings decibel
/// model, so the core works immediately and upgrades to the real mixer without code changes.
///
/// The .mixer asset itself is authorable only in the Unity Editor (it is a binary asset, not
/// hand-editable YAML), so this asset is normally created/populated in-editor. Until then the
/// runtime fallback keeps everything functional.
///
/// ExposedParameterNames must match the names of exposed "Xxx Volume" float parameters on the
/// mixer groups. They are read at runtime; missing params trigger the fallback for that bus only.
/// </summary>
[CreateAssetMenu(fileName = "AudioMixerRef", menuName = "Audio/Audio Mixer Ref")]
public sealed class AudioMixerRef : ScriptableObject
{
    [Header("Mixer")]
    [Tooltip("The AudioMixer asset to drive. Leave null to use the per-source fallback.")]
    [SerializeField] private AudioMixer mixer;

    [Header("Groups")]
    [Tooltip("Master group is optional here; typically the global exposed Master parameter lives on the master group.")]
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup ambientGroup;

    [Header("Exposed parameters (must match mixer exposed names)")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SfxVolume";
    [SerializeField] private string uiVolumeParam = "UiVolume";
    [SerializeField] private string ambientVolumeParam = "AmbientVolume";

    public AudioMixer Mixer => mixer;
    public AudioMixerGroup MasterGroup => masterGroup;
    public AudioMixerGroup MusicGroup => musicGroup;
    public AudioMixerGroup SfxGroup => sfxGroup;
    public AudioMixerGroup UiGroup => uiGroup;
    public AudioMixerGroup AmbientGroup => ambientGroup;

    public string MasterVolumeParam => masterVolumeParam;
    public string MusicVolumeParam => musicVolumeParam;
    public string SfxVolumeParam => sfxVolumeParam;
    public string UiVolumeParam => uiVolumeParam;
    public string AmbientVolumeParam => ambientVolumeParam;

    /// <summary>Returns the serialized group for a bus (may be null).</summary>
    public AudioMixerGroup GetGroup(AudioBus bus)
    {
        switch (bus)
        {
            case AudioBus.Master: return MasterGroup;
            case AudioBus.Music: return MusicGroup;
            case AudioBus.Sfx: return SfxGroup;
            case AudioBus.Ui: return UiGroup;
            case AudioBus.Ambient: return AmbientGroup;
            default: return null;
        }
    }

    /// <summary>Returns the exposed parameter name for a bus (may be null/empty).</summary>
    public string GetVolumeParam(AudioBus bus)
    {
        switch (bus)
        {
            case AudioBus.Master: return MasterVolumeParam;
            case AudioBus.Music: return MusicVolumeParam;
            case AudioBus.Sfx: return SfxVolumeParam;
            case AudioBus.Ui: return UiVolumeParam;
            case AudioBus.Ambient: return AmbientVolumeParam;
            default: return null;
        }
    }
}
