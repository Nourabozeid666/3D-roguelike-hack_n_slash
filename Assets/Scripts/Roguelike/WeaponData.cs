using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponData",
    menuName = "Roguelike/Weapon Data"
)]
public class WeaponData : ScriptableObject
{
    [SerializeField] WeaponType type;
    [SerializeField] string displayName;
    [Range(0f, 1f)] [SerializeField] float damage;
    [Range(0f, 1f)] [SerializeField] float range;
    [Range(0f, 1f)] [SerializeField] float attackSpeed;

    public WeaponType Type => type;
    public string DisplayName => displayName;
    public float Damage => damage;
    public float Range => range;
    public float AttackSpeed => attackSpeed;
}
