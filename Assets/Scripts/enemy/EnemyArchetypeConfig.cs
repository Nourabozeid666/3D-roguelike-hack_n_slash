using UnityEngine;

[CreateAssetMenu(fileName = "EnemyArchetype", menuName = "Enemies/Enemy Archetype")]
public class EnemyArchetypeConfig : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float baseDamage = 10f;
    public float baseDefense = 0f;

    [Header("Poise")]
    public float maxPoise = 100f;
    public float poiseRegenDelay = 1.5f;
    public float poiseRegenRate = 20f;
    public bool flinchesOnHit = true;
}