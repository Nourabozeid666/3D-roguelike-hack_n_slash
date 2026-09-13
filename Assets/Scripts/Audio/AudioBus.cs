/// <summary>
/// Logical mix buses exposed by the Audio Core.
///
/// These map to mixer groups (or the per-source volume fallback when no AudioMixer asset
/// is assigned). Ambient is intentionally reserved for a later content phase; it is listed
/// here so the bus model does not need to change when Ambient shipping arrives.
///
/// Do NOT add gameplay-specific buses (e.g. "SwordHit") here. Specific sounds belong to later
/// integration/content phases and should ride the generic Music/SFX/UI buses.
/// </summary>
public enum AudioBus
{
    /// <summary>Overall output. In the mixer model this is the Master group, not a leaf bus.</summary>
    Master = 0,

    /// <summary>Background music.</summary>
    Music = 1,

    /// <summary>Gameplay one-shot effects.</summary>
    Sfx = 2,

    /// <summary>UI one-shot effects (menus, buttons, prompts).</summary>
    Ui = 3,

    /// <summary>Reserved for a future ambient/environment pass. Not yet routed.</summary>
    Ambient = 4
}
