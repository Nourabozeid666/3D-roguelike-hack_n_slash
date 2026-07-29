using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon")]
public class WeaponObject : ScriptableObject
{
    [SerializeField] private string weaponName = "New Weapon";
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseLength = 1f;
    [SerializeField] private float baseSize = 1f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private AttackData[] entryAttacks;

    public string WeaponName { get { return weaponName; } }
    public float BaseDamage { get { return baseDamage; } }
    public float BaseLength { get { return baseLength; } }
    public float BaseSize { get { return baseSize; } }
    public float BaseAttackSpeed { get { return baseAttackSpeed; } } 
    public GameObject WeaponPrefab { get { return weaponPrefab; } }
    public AttackData[] EntryAttacks { get { return entryAttacks; } }
}