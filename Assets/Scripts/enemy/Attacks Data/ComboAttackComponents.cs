using UnityEngine;

public class ComboAttackComponents: MonoBehaviour // what is gonna be on the enemy as for a script
{
    [SerializeField] private ComboAttackConfig config;
    [SerializeField] private DealDamage hitbox;
    public ComboAttackConfig Config => config;
    public DealDamage Hitbox => hitbox;
}
