using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Production Audio Core: the single, persistent owner of audio output. Implements IAudioService.
///
/// RESPONSIBILITIES
///   - Persistent across scene changes (DontDestroyOnLoad); owned/created once programmatically.
///   - Owns the AudioSources (a small SFX pool + one music source) - callers never touch raw sources.
///   - Drives an AudioMixer through decibels when an AudioMixerRef is assigned, else falls back to
///     per-source volume using the same AudioMixerSettings decibel model (functional without a mixer).
///   - Guards against duplicate instances and degrades gracefully when uninitialized.
///
/// NOT in scope (later phases): gameplay/Music/UI sound content, clip libraries, advanced pooling,
/// persistence. This class contains no hard-coded gameplay sounds.
///
/// See the OWNERSHIP / LIFETIME block below for the ownership model, and
/// docs/personal/AUDIO_IMPLEMENTATION_PHASE1B.md for the Editor-required mixer handoff.
/// </summary>
public sealed class AudioCore : MonoBehaviour, IAudioService
{
    [Header("Configuration")]
    [SerializeField] private AudioMixerRef audioMixerRef;

    [Tooltip("Number of pooled SFX sources. More = more concurrent one-shots.")]
    [SerializeField, Min(1)] private int sfxSourceCount = 8;

    [Tooltip("Optional starting volumes; serialized so a scene can override defaults.")]
    [SerializeField] private AudioMixerSettings settings = new AudioMixerSettings();

    // ---- runtime state ----
    private AudioSource[] sfxSources;
    private AudioSource musicSource;
    private int sfxRoundRobin;
    private bool initialized;

    // Registered owner token: the ONE justified static in the audio domain, so a scene/UI can
    // discover the persistent owner. Mirrors the repo's single documented static-bool exception;
    // not a god-object - all behavior is reached through IAudioService on this instance.
    private static AudioCore instance;
    public static AudioCore Instance => instance;

    /// <summary>Exposes the core as the caller seam (never null at runtime in a live scene).</summary>
    public static IAudioService Service => instance;

    public bool IsAvailable => initialized && instance == this;

    public AudioMixerSettings Settings => settings;

    // ---------------------------------------------------------------------------------------------
    // OWNERSHIP / LIFETIME
    //
    // Ownership model: a SINGLE persistent AudioCore is created programmatically before the first
    // scene loads and survives every scene load/unload (DontDestroyOnLoad). This guarantees exactly
    // one core at runtime and keeps it alive across MainMenu <-> TestingScene transitions, where the
    // project uses single-scene SceneManager.LoadScene (no scene is DontDestroyOnLoad today).
    //
    // This is deliberately NOT placed as a scene object in MainMenu/TestingScene: adding it to every
    // scene would need naive scene-YAML edits and risk accidental duplicates. Instead the audio root
    // is self-owned here, so:
    //   - no scene hand-editing is required to establish the core,
    //   - the duplicate guard in Awake() additionally protects against any future scene-embedded core.
    //
    // The Editor-authored AudioMixerRef asset (Assets/Audio/Mixer/Resources/AudioMixerRef.asset)
    // and its MainMixer.mixer are created in the Unity Editor. At runtime the core self-loads the
    // ref via Resources.Load in Start() and connects through Configure(). If that asset is absent
    // or the mixer is unavailable, the core runs on its per-source volume fallback. See
    // docs/personal/AUDIO_IMPLEMENTATION_PHASE1B.md.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Guarantees a single persistent AudioCore exists before the first scene loads. Registry of the
    /// registered-owner token; a no-op once one is already present. Requires the UnityEngine
    /// RuntimeInitialize attribute; inert in the dotnet stub build.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInitialized()
    {
        if (instance != null)
        {
            return;
        }

        GameObject owner = new GameObject("AudioCore");
        owner.AddComponent<AudioCore>();
    }

    /// <summary>
    /// Assigns (or replaces) the AudioMixerRef that points the core at the real AudioMixer, then
    /// re-applies all bus volumes and re-routes owned sources to the mixer groups. Safe to call
    /// before or after initialization; passing null reverts to the per-source fallback.
    /// </summary>
    public void Configure(AudioMixerRef mixerRef)
    {
        audioMixerRef = mixerRef;
        ApplyAllVolumes();

        if (!initialized)
        {
            return;
        }

        if (sfxSources != null)
        {
            AudioMixerGroup sfxGroup = ResolveGroup(AudioBus.Sfx);
            for (int i = 0; i < sfxSources.Length; i++)
            {
                if (sfxSources[i] != null) sfxSources[i].outputAudioMixerGroup = sfxGroup;
            }
        }
        if (musicSource != null)
        {
            musicSource.outputAudioMixerGroup = ResolveGroup(AudioBus.Music);
        }
    }

    // ---------------------------------------------------------------------------------------------

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Duplicate AudioCore: keep the existing registered owner, destroy this one.
            Debug.LogWarning($"AudioCore: duplicate instance '{name}' destroyed; keeping existing owner.", this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSources();
        ApplyAllVolumes();

        initialized = true;
    }

    private void Start()
    {
        // Self-connect the optional AudioMixerRef asset so this core drives the real AudioMixer
        // without any scene embedding. Asset lives under a Resources/ folder (see the phase doc).
        // Guarded: when absent/failed it degrades to the per-source fallback (no-op).
        AudioMixerRef refAsset;
        try
        {
            refAsset = Resources.Load<AudioMixerRef>("AudioMixerRef");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"AudioCore: Resources.Load<AudioMixerRef> failed ({e.Message}); using per-source fallback.");
            refAsset = null;
        }

        if (refAsset != null)
        {
            Configure(refAsset);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        DisposeSources();
    }

    private void InitializeSources()
    {
        DisposeSources();

        if (settings == null)
        {
            settings = new AudioMixerSettings();
        }

        // SFX pool
        int count = Mathf.Max(1, sfxSourceCount);
        sfxSources = new AudioSource[count];
        for (int i = 0; i < count; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;                      // UI/overlay by default; gameplay can override per call later
            s.outputAudioMixerGroup = ResolveGroup(AudioBus.Sfx);
            s.volume = settings.MapToLinear(settings.GetVolume(AudioBus.Sfx));
            sfxSources[i] = s;
        }

        // Music source (single, looping-capable)
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.outputAudioMixerGroup = ResolveGroup(AudioBus.Music);
        musicSource.volume = settings.MapToLinear(settings.GetVolume(AudioBus.Music));
    }

    private void DisposeSources()
    {
        if (sfxSources != null)
        {
            for (int i = 0; i < sfxSources.Length; i++)
            {
                if (sfxSources[i] != null)
                {
                    Destroy(sfxSources[i]);
                }
            }
            sfxSources = null;
        }

        if (musicSource != null)
        {
            Destroy(musicSource);
            musicSource = null;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // IAudioService
    // ---------------------------------------------------------------------------------------------

    public void SetVolume(AudioBus bus, float normalized)
    {
        settings.SetVolume(bus, normalized);
        ApplyBusVolume(bus);
    }

    public float GetVolume(AudioBus bus)
    {
        if (settings == null)
        {
            return 1f;
        }
        return settings.GetVolume(bus);
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!initialized || clip == null || sfxSources == null || sfxSources.Length == 0)
        {
            return;
        }

        AudioSource source = sfxSources[sfxRoundRobin];
        sfxRoundRobin = (sfxRoundRobin + 1) % sfxSources.Length;
        source.PlayOneShot(clip, Mathf.Max(0f, volume));
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!initialized || musicSource == null || clip == null)
        {
            return;
        }

        musicSource.loop = loop;
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return; // already playing this track
        }

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Internals: mixer OR per-source volume fallback
    // ---------------------------------------------------------------------------------------------

    private void ApplyAllVolumes()
    {
        foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
        {
            ApplyBusVolume(bus);
        }
    }

    private void ApplyBusVolume(AudioBus bus)
    {
        if (settings == null)
        {
            return;
        }

        float normalized = settings.GetVolume(bus);

        // Preferred: write the exposed mixer parameter (decibels).
        if (audioMixerRef != null && audioMixerRef.Mixer != null)
        {
            string param = audioMixerRef.GetVolumeParam(bus);
            if (!string.IsNullOrEmpty(param))
            {
                float dB = settings.MapToDecibels(normalized);
                bool set = audioMixerRef.Mixer.SetFloat(param, dB);
                if (set)
                {
                    return; // mixer handled this bus; nothing else to do
                }
            }
        }

        // Fallback: drive the AudioSources we own on that bus directly (linear amplitude).
        ApplySourceVolume(bus, normalized);
    }

    private void ApplySourceVolume(AudioBus bus, float normalized)
    {
        float linear = settings.MapToLinear(normalized);

        switch (bus)
        {
            case AudioBus.Sfx:
                if (sfxSources != null)
                {
                    for (int i = 0; i < sfxSources.Length; i++)
                    {
                        if (sfxSources[i] != null) sfxSources[i].volume = linear;
                    }
                }
                break;
            case AudioBus.Music:
                if (musicSource != null) musicSource.volume = linear;
                break;
            case AudioBus.Ui:
                // No dedicated UI source this phase; UI one-shots share the SFX pool. When a UI group
                // is wired later, route UI volume to the mixer (handled above) rather than a source.
                break;
        }
    }

    private AudioMixerGroup ResolveGroup(AudioBus bus)
    {
        if (audioMixerRef == null)
        {
            return null;
        }
        return audioMixerRef.GetGroup(bus);
    }
}
