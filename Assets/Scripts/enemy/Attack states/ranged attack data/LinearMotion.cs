using System;
using UnityEngine;

public class LinearMotion : MonoBehaviour,IProjectileMotion
{
    float speed;
    Vector3 start;
    Vector3 direction;
    public void Intialization(Vector3 startPoint, Vector3 targetPos, float projectileSpeed)
    {
        start = startPoint;
        direction = (targetPos - startPoint).normalized;
        speed = projectileSpeed;
    }

    public Vector3 Evaluate(float elapsedTime)
    {
        //Calculate and give me back the exact 3D position where the object should be right now in this specific second
        //Distance = Speed × Time
        return start + (direction * elapsedTime * speed);
    }

    public bool HasFinished(float elapsedTime) => false;
}
