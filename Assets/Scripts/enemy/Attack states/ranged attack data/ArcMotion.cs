using UnityEngine;

public class ArcMotion : MonoBehaviour,IProjectileMotion
{
    // height = 4 × arcHeight × t × (1 − t)
    [SerializeField] private float arcHeight = 3f; // How high the rock arches
    Vector3 target;
    Vector3 start;
    private float flightDuration;
    public void Intialization(Vector3 startPoint, Vector3 targetPos, float projectileSpeed)
    {
        start = startPoint;
        target = targetPos;

        float distance = Vector3.Distance(start, target);
        flightDuration = projectileSpeed > 0f? distance/projectileSpeed : 1f; 
    }

    public Vector3 Evaluate(float elapsedTime)
    {
        // t goes from 0.0 (start) to 1.0 (target)
        float t = flightDuration > 0f ? Mathf.Clamp01(elapsedTime / flightDuration) : 1f;

        // "Slide between Point A (start) and Point B (target) based on the percentage t."
        Vector3 flatPosition = Vector3.Lerp(start, target, t);
        float height = 4f * arcHeight * t * (1f - t);

        return flatPosition + (Vector3.up * height);
    }

    public bool HasFinished(float elapsedTime) => elapsedTime >= flightDuration;
}
