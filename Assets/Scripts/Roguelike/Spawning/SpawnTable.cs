using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool of enemy archetypes the SpawnSystem may pick from.
/// The spawn budget comes from RunData.enemyBudget (single source of truth), not this asset.
/// </summary>
[CreateAssetMenu(fileName = "SpawnTable", menuName = "Roguelike/Spawn Table")]
public class SpawnTable : ScriptableObject
{
    [SerializeField] List<EnemyArchetype> archetypes = new();

    public IReadOnlyList<EnemyArchetype> Archetypes => archetypes;
}
