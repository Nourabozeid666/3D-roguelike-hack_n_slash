using System;

/// <summary>
/// Contract between SpawnSystem and whatever it spawns (the death/removal + floor-scaling seam).
/// The temporary TestEnemy implements this. The real EnemyController must surface its death the
/// same way (EnemyEntity.OnDied is the Enemy-side source) — [WAITING FOR ENEMY SYSTEM].
/// </summary>
public interface IEnemySpawned
{
    event Action Died;
    void ApplyFloorScaling(float healthScale, float damageScale);
}
