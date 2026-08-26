using UnityEngine;

public interface IProjectileMotion
{
    void Intialization(Vector3 startPoint, Vector3 targetPos, float projectileSpeed);
    Vector3 Evaluate(float elapsedTime);
    bool HasFinished(float elapsedTime);
}
