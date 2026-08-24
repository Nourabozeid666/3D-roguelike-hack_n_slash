using UnityEngine;

public class DealDamage : MonoBehaviour
{
    private float damage = 0f;

    public void SetDamage(float amount) => damage = amount;
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        player.Entity.TakeDamage(damage);
    }
}
