using UnityEngine;
using System;
using System.Collections.Generic;
using Drakkar.GameUtils;

public enum SocketType { RightHand, LeftHand, Hip }

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon")]
public class WeaponObject : ScriptableObject
{
    [SerializeField] private string weaponName = "New Weapon";
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseLength = 1f;
    [SerializeField] private float baseSize = 1f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private GameObject[] accessoriesPrefabs;
    [SerializeField] private DrakkarTrail trail;
    [SerializeField] private AnimationClip chargeAnimation;
    [SerializeField] private SerializableDictionary<InputType, AttackData> entryAttacks;

    public string WeaponName { get { return weaponName; } }
    public float BaseDamage { get { return baseDamage; } }
    public float BaseLength { get { return baseLength; } }
    public float BaseSize { get { return baseSize; } }
    public float BaseAttackSpeed { get { return baseAttackSpeed; } } 
    public AnimationClip ChargeAnimation { get { return chargeAnimation; } }
    public GameObject WeaponPrefab { get { return weaponPrefab; } }
    public DrakkarTrail Trail { get { return trail; } set { trail = value; } }
    public SerializableDictionary<InputType, AttackData> EntryAttacks { get { return entryAttacks; } }

    void OnValidate()
    {
        if (weaponPrefab != null)
        {
            DrakkarTrail trailComponent = weaponPrefab.GetComponentInChildren<DrakkarTrail>();
            if (trailComponent != null)
            {
                trail = trailComponent;
            }
        }
    }
}