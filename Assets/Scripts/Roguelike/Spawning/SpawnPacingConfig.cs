using System;
using UnityEngine;

/// <summary>
/// Configurable high-floor spawn pacing. A floor's composition is selected ONCE (cached); this config
/// decides whether that existing composition is released all at once or in waves.
/// Off by default: floors below <c>waveStartFloor</c> (or with <c>waveSize</c> == 0) spawn their whole
/// composition at once. When active, waves are contiguous slices of the SAME composition — no
/// re-selection, no budget recompute (see <see cref="WavePlan"/>).
/// </summary>
[Serializable]
public class SpawnPacingConfig
{
    [Tooltip("First floor at which the already-selected composition is released in waves. Floors below this spawn their whole composition at once. Default (int.MaxValue) = waves off for every floor.")]
    [SerializeField] int waveStartFloor = int.MaxValue;

    [Tooltip("Enemies released per wave. 0 = no wave splitting (one wave containing the whole composition).")]
    [SerializeField] int waveSize = 0;

    [Tooltip("Optional seconds between the moment a wave is fully cleared and the next wave spawns.")]
    [SerializeField] float waveDelaySeconds = 0f;

    public int WaveStartFloor => Mathf.Max(1, waveStartFloor);
    public int WaveSize => Mathf.Max(0, waveSize);
    public float WaveDelaySeconds => Mathf.Max(0f, waveDelaySeconds);

    /// <summary>True when <paramref name="floor"/> uses wave release (threshold reached AND a wave size is configured).</summary>
    public bool UsesWavesOn(int floor) => floor >= WaveStartFloor && WaveSize > 0;
}
