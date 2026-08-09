using UnityEngine;

public class DealDamage : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 0f;
    [SerializeField] EnemyController enemyController;
    private void OnTriggerEnter(Collider other)
    {
        PlayerController playerController = other.GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            return;
        }
        if ()
        {
            playerController.Entity.TakeDamage(damage);
            Debug.Log($"Player took {damage} damage. Remaining health: {playerController.Entity.Health}", this);
        }
    }
}
