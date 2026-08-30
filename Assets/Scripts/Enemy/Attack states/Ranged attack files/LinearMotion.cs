// Suggested path: Assets/Scripts/enemy/Attack states/LinearMotion.cs
// Attach to bullet-type projectile prefabs alongside Projectile.cs.
using UnityEngine;

public class LinearMotion : MonoBehaviour, IProjectileMotion
{
    private Vector3 start;
    private Vector3 direction;
    private float speed;

    public void Initialize(Vector3 startPosition, Vector3 targetPosition, float projectileSpeed)
    {
        start = startPosition;
        direction = (targetPosition - startPosition).normalized;
        speed = projectileSpeed;
    }

    public Vector3 Evaluate(float elapsedTime)
    {
        return start + direction * speed * elapsedTime;
    }

    // A bullet doesn't arrive anywhere on its own - it flies until it hits
    // something or Projectile's lifeTime safety-net expires it.
    public bool HasFinished(float elapsedTime) => false;
}
