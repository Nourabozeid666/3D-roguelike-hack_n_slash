using UnityEngine;

public class ComboAttackComponents: MonoBehaviour // what is gonna be on the enemy as for a script
{
    [SerializeField] private ComboAttackConfig config;
    public ComboAttackConfig Config => config;
}
