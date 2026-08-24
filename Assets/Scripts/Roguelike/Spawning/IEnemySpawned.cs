using System;

/// <summary>
/// Death-only integration contract between SpawnSystem and whatever it spawns.
///
/// The ONE responsibility of this seam is: "tell SpawnSystem when an enemy spawned by it has died",
/// so SpawnSystem can decrement AliveCount and raise FloorCleared at the right time.
///
/// Implementations surface the Enemy system's authoritative death notification — EnemyEntity.OnDied,
/// bridged by EnemyController — rather than maintaining a separate death event. TestEnemy (the test
/// double) implements it too.
///
/// Floor scaling is deliberately NOT part of this contract: SpawnSystem owns floor/growth/floor
/// calculation and applies the resulting scaled stats through the separate, optional
/// ISpawnStatConfig seam. The Enemy side never learns about Roguelike floor progression.
/// </summary>
public interface IEnemySpawned
{
    event Action OnDied;
}
