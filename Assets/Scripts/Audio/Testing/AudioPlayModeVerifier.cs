using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// TEMPORARY Play-Mode verification tool for the Phase 1B audio infrastructure. NOT production code,
/// NOT referenced by any gameplay/UI logic. Attach it to a GameObject in the TestingScene; fill the
/// sfxClip field with (e.g.) Assets/Audio/SFX/Player/SwordHit.mp3.
///
/// On Start it logs a structured PASS/FAIL report for:
///   - AudioCore lifetime (exists, exactly one, IsAvailable)
///   - AudioMixerRef + MainMixer resolution
///   - group resolution (Master/Music/SFX/UI)
///   - exposed-parameter SetFloat/GetFloat round-trip for each volume param
/// Press T to play sfxClip once through AudioCore.Service.PlaySfx (routes to the SFX group).
/// Press V to print the volume round-trip report again.
/// </summary>
public class AudioPlayModeVerifier : MonoBehaviour
{
    [Tooltip("Temporary verification clip (e.g. SwordHit.mp3). Played via the SFX bus on T.")]
    [SerializeField] private AudioClip sfxClip;

    void Start()
    {
        Report();
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            var svc = AudioCore.Service;
            if (svc == null || !svc.IsAvailable)
            {
                Debug.LogWarning("[AudioVerifier] T pressed but AudioCore unavailable; nothing played.");
                return;
            }
            svc.PlaySfx(sfxClip, 1f);
            Debug.Log($"[AudioVerifier] PlaySfx called clip={(sfxClip != null ? sfxClip.name : "NULL")} -> SFX bus");
        }

        if (UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame)
        {
            Report();
        }
    }

    void Report()
    {
        string line = "=========== [AudioVerifier] Play-Mode report ===========";

        if (AudioCore.Instance == null)
        {
            line += "\n[FAIL] AudioCore.Instance is null (not created).";
            Debug.Log(line);
            return;
        }

        AudioCore core = AudioCore.Instance;
        var owners = Object.FindObjectsByType<AudioCore>(FindObjectsSortMode.None);
        line += $"\n[PASS?] AudioCore exists, IsAvailable={core.IsAvailable}, instanceCount={owners.Length}" +
                (owners.Length == 1 ? " (exactly one OK)" : " (WARNING: != 1)");

        IAudioService svc = AudioCore.Service;
        line += $"\n[PASS?] IAudioService.Service available={(svc != null)}";

        // Mixer resolution is read through AudioCore's configured ref (test-tool reflection only,
        // so production keeps its public surface free of mixer internals).
        line += "\n-- Mixer resolution (via AudioCore's configured ref) --";

        var mixer = TryGetMixer(core);
        if (mixer == null)
        {
            line += "\n[FAIL] Configured mixer is null; core is on per-source fallback.";
            Debug.Log(line);
            return;
        }

        line += $"\n[PASS?] Mixer resolved: {mixer.name}";

        AudioMixerGroup master = FindGroup(mixer, "Master");
        AudioMixerGroup music = FindGroup(mixer, "Music");
        AudioMixerGroup sfx = FindGroup(mixer, "SFX");
        AudioMixerGroup ui = FindGroup(mixer, "UI");
        line += $"\n[PASS?] groups -> Master={(master != null)} Music={(music != null)} SFX={(sfx != null)} UI={(ui != null)}";

        line += "\n-- Exposed param round-trips (SetFloat then GetFloat) --";
        line += VolRoundTrip(mixer, "MasterVolume");
        line += VolRoundTrip(mixer, "MusicVolume");
        line += VolRoundTrip(mixer, "SfxVolume");
        line += VolRoundTrip(mixer, "UiVolume");

        Debug.Log(line);
    }

    static string VolRoundTrip(AudioMixer mixer, string name)
    {
        bool ok = mixer.SetFloat(name, -10f);
        float got;
        bool read = mixer.GetFloat(name, out got);
        bool match = ok && read && Mathf.Approximately(got, -10f);
        return $"\n[{(match ? "PASS" : "FAIL")}] {name}: set={ok} get={read} value={got}";
    }

    static AudioMixer TryGetMixer(AudioCore core)
    {
        // The core's configured mixer is only exposed through its public AudioMixerRef property
        // path; reflect a safe read of the audioMixerRef field for verification only (test tool).
        var f = typeof(AudioCore).GetField("audioMixerRef",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var refAsset = f != null ? f.GetValue(core) as AudioMixerRef : null;
        return refAsset != null ? refAsset.Mixer : null;
    }

    static AudioMixerGroup FindGroup(AudioMixer mixer, string path)
    {
        var g = mixer.FindMatchingGroups(path);
        return (g != null && g.Length > 0) ? g[0] : null;
    }
}
