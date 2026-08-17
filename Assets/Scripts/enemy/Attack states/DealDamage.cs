using UnityEngine;

public class DealDamage : MonoBehaviour
{
    [SerializeField] EnemyController enemyController;
    private void OnTriggerEnter(Collider other)
    {
        PlayerController playerController = other.GetComponentInParent<PlayerController>();
        if (playerController == null)
            return;
        if (playerController.Entity.Health <= 0f)
            return;
        playerController.Entity.TakeDamage(enemyController.RuntimeDamage);
        Debug.Log($"Player took {enemyController.RuntimeDamage} damage. Remaining health: {playerController.Entity.Health}", this);
    }
}
