// Suggested path: Assets/Scripts/enemy/Attack states/ArcMotion.cs
// Attach to rock-type projectile prefabs alongside Projectile.cs.
using UnityEngine;

public class ArcMotion : MonoBehaviour, IProjectileMotion
{
    [Tooltip("How high above the straight line to start-target the arc peaks.")]
    [SerializeField] private float arcHeight = 3f;

    private Vector3 start;
    private Vector3 target;
    private float flightDuration;

    public void Initialize(Vector3 startPosition, Vector3 targetPosition, float projectileSpeed)
    {
        start = startPosition;
        target = targetPosition;

        float distance = Vector3.Distance(start, target);
        flightDuration = projectileSpeed > 0f ? distance / projectileSpeed : 1f;
    }

    public Vector3 Evaluate(float elapsedTime)
    {
        float t = flightDuration > 0f ? Mathf.Clamp01(elapsedTime / flightDuration) : 1f;

        Vector3 flatPosition = Vector3.Lerp(start, target, t);

        // Parabola that's 0 at t=0, peaks at t=0.5, back to 0 at t=1.
        float height = 4f * arcHeight * t * (1f - t);

        return flatPosition + Vector3.up * height;
    }

    // The rock has "landed" once it reaches the end of its arc, regardless
    // of whether it hit the player on the way - Projectile decides what to
    // do with that (right now: return to pool; easy to extend into splash
    // damage on landing later if you want that).
    public bool HasFinished(float elapsedTime) => elapsedTime >= flightDuration;
}
