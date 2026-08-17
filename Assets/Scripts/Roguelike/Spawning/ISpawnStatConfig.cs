/// <summary>
/// Minimal spawn-time stat configuration seam (optional; NOT part of the death contract).
///
/// SpawnSystem owns floor progression, composition, budget and the floor-scaling CALCULATION. It
/// reads each spawned enemy's BASE stats through this seam, computes the floor-scaled ABSOLUTE
/// values, and writes them back through ConfigureForSpawn. Implementers simply store/apply the
/// resulting stats through their own stat storage:
///
///   - TestEnemy (test double)   -> local baseHealth/baseDamage fields (both applied).
///   - real enemy (EnemyController) -> HEALTH via the authoritative EnemyEntity.SetMaxHealth;
///     DAMAGE stored as a per-instance `runtimeDamage` so attacks (DealDamage, SacrificeAttack)
///     read the scaled value instead of the shared ScriptableObject base. Shared SOs are never
///     mutated.
///
/// Nothing here is floor-aware: no multipliers, no growth rates, no Roguelike concepts leak into
/// the Enemy system. SpawnSystem is the only place that knows what "floor 7" means.
/// </summary>
public interface ISpawnStatConfig
{
    /// <summary>Base max health of this enemy instance (serialized/prefab stats, before floor scaling).</summary>
    float BaseMaxHealth { get; }

    /// <summary>Base damage of this enemy instance, read from the actual runtime damage configuration
    /// the Enemy/Combat system uses (per-attack ScriptableObject), before floor scaling.</summary>
    float BaseDamage { get; }

    /// <summary>Apply the resulting (floor-scaled absolute) stats before the enemy initializes.
    /// Implementers apply what their stat storage supports (health via SetMaxHealth; damage stored
    /// as a per-instance runtime value read by attacks).</summary>
    void ConfigureForSpawn(float maxHealth, float baseDamage);
}
