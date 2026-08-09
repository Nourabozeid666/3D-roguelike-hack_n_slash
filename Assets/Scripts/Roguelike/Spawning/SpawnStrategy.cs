/// <summary>
/// How the SpawnSystem resolves WHERE a floor's enemies are placed.
/// FixedPoints: designer-placed SpawnPoint children — controlled encounters and testing.
/// RandomZone: a data-driven SpawnZone region resolved through the placement validation pipeline —
/// the general-purpose strategy (bounds, blocking layers, NavMesh, player/enemy distance, attempts).
/// </summary>
public enum SpawnStrategy
{
    FixedPoints,
    RandomZone
}
