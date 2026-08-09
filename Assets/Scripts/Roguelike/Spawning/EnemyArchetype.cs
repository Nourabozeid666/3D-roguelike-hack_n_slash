using UnityEngine;

/// <summary>
/// One affordable enemy type: prefab + spawn cost + per-floor growth multipliers.
/// Base stats live on the enemy itself (TestEnemy for now; EnemyEntity for the real enemy),
/// not here, so this file stays decoupled from the Enemy System.
/// </summary>
[CreateAssetMenu(fileName = "EnemyArchetype", menuName = "Roguelike/Enemy Archetype")]
public class EnemyArchetype : ScriptableObject
{
    [SerializeField] string displayName;
    [SerializeField] GameObject prefab;
    [SerializeField] int cost = 3;
    [SerializeField] float healthGrowthPerFloor = 0.12f;
    [SerializeField] float damageGrowthPerFloor = 0.08f;

    /// <summary>Editor-friendly name for debug/HUD summaries. Empty is allowed (falls back to cost in summaries).</summary>
    public string DisplayName => displayName;

    public GameObject Prefab => prefab;
    public int Cost => cost;
    public float HealthGrowthPerFloor => healthGrowthPerFloor;
    public float DamageGrowthPerFloor => damageGrowthPerFloor;
}
