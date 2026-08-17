using UnityEngine;

/// <summary>
/// A rectangular, designer-configured spawning region for the RandomZone strategy. All values are
/// scene-agnostic and editable in the Inspector; nothing scene-specific is hardcoded.
///
/// Two definition modes:
///   CenterSize — the original mode: a box centered on this GameObject's position (+ offset) with
///                a full-extents Size vector.
///   TwoPoints  — the zone is defined by two arbitrary corner points (startPoint / endPoint). The
///                order does not matter; the system normalizes to min/max. Random candidates are
///                generated inside the resulting rectangle. Y is taken from the ground level
///                (Center.y) so horizontal coordinates (X/Z) define the spawn area.
///
/// Candidate generation + the validation pipeline live in <see cref="SpawnPlacementValidator"/>;
/// this class only provides the configuration, the bounds (Center/Size/Contains via the validator)
/// and deterministic random candidates.
///
/// Validation pipeline for a RandomZone candidate (docs/ROGUELIKE_SPAWNING_SPRINT_7_8.md):
///   candidate inside bounds -> ground/NavMesh validation (when enabled) -> blocking-layer overlap
///   (using footprint + safety margin) -> distance from Player -> distance from already-placed
///   enemies -> PASS (spawn) / FAIL (retry), bounded by MaxAttempts. If no candidate passes, the
///   SpawnSystem skips that enemy with a diagnostic log — it never spawns inside invalid geometry
///   and never hangs.
/// </summary>
public class SpawnZone : MonoBehaviour
{
    public enum ZoneMode { CenterSize, TwoPoints }

    [Header("Definition mode")]
    [Tooltip("CenterSize: box defined by Center + full-extents Size (original mode). TwoPoints: box defined by two arbitrary corner points (order does not matter).")]
    [SerializeField] ZoneMode zoneMode = ZoneMode.CenterSize;

    [Tooltip("Corner point A (used only in TwoPoints mode). Order does not matter.")]
    [SerializeField] Vector3 startPoint;

    [Tooltip("Corner point B (used only in TwoPoints mode). Order does not matter.")]
    [SerializeField] Vector3 endPoint;

    [Header("CenterSize mode")]
    [Tooltip("Full extents (width x height x depth) of the rectangular region, centered on Center.")]
    [SerializeField] Vector3 size = new Vector3(20f, 2f, 20f);

    [Tooltip("Offset added to this object's position to place the region center.")]
    [SerializeField] Vector3 centerOffset;

    [Header("Validation")]
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

    [Header("Footprint / clearance")]
    [Tooltip("Estimated radius of an enemy's horizontal footprint used by the blocking-geometry overlap check (matches the largest enemy's body, e.g. 0.72 for TreeEntAsh, 0.5 for the test capsule). 0 = the blocking check is skipped. Footprint radius lives here (placement concern), not on the archetype, so it scales with the zone's environs.")]
    [SerializeField] float footprintRadius = 0.5f;

    [Tooltip("Additional clearance added to the enemy footprint radius when checking obstacle overlap. This creates a safety buffer around obstacles so enemies do not spawn too close to walls. Effective clearance = footprintRadius + safetyMargin.")]
    [SerializeField] float safetyMargin = 0f;

    // --- Computed bounds ---

    /// <summary>Effective center of the rectangular zone (world space). In TwoPoints mode this is
    /// the midpoint of the two normalized corner points.</summary>
    public Vector3 Center
    {
        get
        {
            if (zoneMode == ZoneMode.TwoPoints)
            {
                Vector3 min = MinPoint(startPoint, endPoint);
                Vector3 max = MaxPoint(startPoint, endPoint);
                return (min + max) * 0.5f;
            }
            return transform.position + centerOffset;
        }
    }

    /// <summary>Effective full-extents size of the rectangular zone. In TwoPoints mode this is
    /// the absolute difference of the two normalized corner points (Y uses the serialized size.y
    /// for ground height tolerance).</summary>
    public Vector3 Size
    {
        get
        {
            if (zoneMode == ZoneMode.TwoPoints)
            {
                Vector3 diff = MaxPoint(startPoint, endPoint) - MinPoint(startPoint, endPoint);
                return new Vector3(Mathf.Max(diff.x, 0.001f), Mathf.Max(size.y, 0.001f), Mathf.Max(diff.z, 0.001f));
            }
            return size;
        }
    }

    public ZoneMode Mode => zoneMode;
    public LayerMask BlockingLayers => blockingLayers;
    public bool UseNavMeshValidation => useNavMeshValidation;
    public float GroundSampleRadius => Mathf.Max(0.1f, groundSampleRadius);
    public float MinPlayerDistance => Mathf.Max(0f, minPlayerDistance);
    public float MinEnemyDistance => Mathf.Max(0f, minEnemyDistance);
    public int MaxAttempts => Mathf.Max(1, maxAttempts);
    public float FootprintRadius => Mathf.Max(0f, footprintRadius);
    public float SafetyMargin => Mathf.Max(0f, safetyMargin);

    /// <summary>
    /// A deterministic random candidate on the zone's horizontal plane (same y as Center). Bounded:
    /// the placement validator never draws more than MaxAttempts of these per enemy.
    /// </summary>
    public Vector3 RandomPoint()
    {
        Vector3 half = Size * 0.5f;
        return Center + new Vector3(
            Random.Range(-half.x, half.x),
            0f,
            Random.Range(-half.z, half.z));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Center, Size);
    }

    static Vector3 MinPoint(Vector3 a, Vector3 b) => new Vector3(
        Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));

    static Vector3 MaxPoint(Vector3 a, Vector3 b) => new Vector3(
        Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
}
