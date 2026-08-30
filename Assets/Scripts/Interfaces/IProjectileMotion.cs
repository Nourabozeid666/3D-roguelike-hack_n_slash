// Suggested path: Assets/Scripts/enemy/Attack states/IProjectileMotion.cs
// Implemented by whatever movement component sits on a given projectile prefab
// (LinearMotion for bullets, ArcMotion for lobbed rocks, etc.). Projectile.cs
// doesn't know or care which one it's talking to.
using UnityEngine;

public interface IProjectileMotion
{
    void Initialize(Vector3 startPosition, Vector3 targetPosition, float speed);

    // World-space position at this point in the projectile's flight.
    Vector3 Evaluate(float elapsedTime);

    // True once this motion considers the flight "over" on its own terms
    // (e.g. a rock landing). Straight-line motion has no natural endpoint,
    // so it can just always return false and rely on Projectile's lifeTime.
    bool HasFinished(float elapsedTime);
}
