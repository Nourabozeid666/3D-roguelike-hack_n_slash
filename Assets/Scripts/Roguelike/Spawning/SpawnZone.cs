using UnityEngine;

/// <summary>
/// A rectangular, designer-configured spawning region for the RandomZone strategy. All values are
/// scene-agnostic and editable in the Inspector; nothing scene-specific is hardcoded. The zone is
/// centered on its GameObject's position (plus an optional center offset) and lays on that
/// horizontal plane. Candidate generation + the validation pipeline live in
/// <see cref="SpawnPlacementValidator"/>; this class only provides the configuration, the bounds
/// (Center/Size/Contains via the validator) and deterministic random candidates.
///
/// Validation pipeline for a RandomZone candidate (docs/ROGUELIKE_SPAWNING_SPRINT_7_8.md):
///   candidate inside bounds -> ground/NavMesh validation (when enabled) -> blocking-layer overlap
///   -> distance from Player -> distance from already-placed enemies -> PASS (spawn) / FAIL (retry),
///   bounded by MaxAttempts. If no candidate passes, the SpawnSystem skips that enemy with a
///   diagnostic log — it never spawns inside invalid geometry and never hangs.
/// </summary>
public class SpawnZone : MonoBehaviour
{
    [Tooltip("Full extents (width x height x depth) of the rectangular region, centered on Center.")]
    [SerializeField] Vector3 size = new Vector3(20f, 2f, 20f);

    [Tooltip("Offset added to this object's position to place the region center.")]
    [SerializeField] Vector3 centerOffset;

    [Tooltip("LayerMask of blocking geometry (walls/obstacles/environment). 0 = no blocking check. Layer numbers are never hardcoded — set this in the Inspector.")]
    [SerializeField] LayerMask blockingLayers = 0;

    [Tooltip("When true, a candidate must map to a walkable NavMesh location (NavMesh.SamplePosition) before it is accepted. When false, the ground check is skipped. In a test scene with no baked NavMesh this stays deterministic: no candidate can pass, so no enemy spawns there.")]
    [SerializeField] bool useNavMeshValidation = true;

    [Tooltip("Radius around a candidate in which NavMesh.SamplePosition must find a walkable location.")]
    [SerializeField] float groundSampleRadius = 1f;

    [Tooltip("Minimum distance from the player reference before a candidate is accepted (0 = no player-distance rule).")]
    [SerializeField] float minPlayerDistance = 5f;

    [Tooltip("Minimum distance between spawned enemies' centers. Set it >= 2x the largest footprint radius.")]
    [SerializeField] float minEnemyDistance = 2f;

    [Tooltip("Maximum candidate attempts per enemy before failing gracefully (guards against unbounded retry loops).")]
    [SerializeField] int maxAttempts = 20;

    [Tooltip("Estimated radius of an enemy's horizontal footprint used by the blocking-geometry overlap check (matches the largest enemy's body, e.g. 0.5 for the test capsule). 0 = the blocking check is skipped. Footprint radius lives here (placement concern), not on the archetype, so it scales with the zone's environs.")]
    [SerializeField] float footprintRadius = 0.5f;

    public Vector3 Center => transform.position + centerOffset;
    public Vector3 Size => size;
    public LayerMask BlockingLayers => blockingLayers;
    public bool UseNavMeshValidation => useNavMeshValidation;
    public float GroundSampleRadius => Mathf.Max(0.1f, groundSampleRadius);
    public float MinPlayerDistance => Mathf.Max(0f, minPlayerDistance);
    public float MinEnemyDistance => Mathf.Max(0f, minEnemyDistance);
    public int MaxAttempts => Mathf.Max(1, maxAttempts);
    public float FootprintRadius => Mathf.Max(0f, footprintRadius);

    /// <summary>
    /// A deterministic random candidate on the zone's horizontal plane (same y as Center). Bounded:
    /// the placement validator never draws more than MaxAttempts of these per enemy.
    /// </summary>
    public Vector3 RandomPoint() => Center + new Vector3(
        Random.Range(-size.x * 0.5f, size.x * 0.5f),
        0f,
        Random.Range(-size.z * 0.5f, size.z * 0.5f));

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Center, Size);
    }
}
