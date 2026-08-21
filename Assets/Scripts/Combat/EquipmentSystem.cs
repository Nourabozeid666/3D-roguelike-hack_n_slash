using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

[Serializable]
public class EquipmentSystem
{
    public CombatController _owner;

    [Header("Equipment System Data")]
    [SerializeField] private WeaponModelData currentWeaponModelData;
    [SerializeField] private SerializableDictionary<SocketType, GameObject> availableSockets;
    [SerializeField] private bool enableEquipmentSystem = false;
    [Header("Weapon Equipment Data")]
    [SerializeField] private WeaponObject currentWeapon;
    [SerializeField] private GameObject currentWeaponModel;
    [SerializeField] private List<GameObject> accessoryModels;

    public WeaponModelData CurrentWeaponModelData { get { return currentWeaponModelData; } }
    public SerializableDictionary<SocketType, GameObject> AvailableSockets { get { return availableSockets; } }
    public bool EnableEquipmentSystem { get { return enableEquipmentSystem; } }
    public WeaponObject CurrentWeapon { get { return currentWeapon; } }
    public GameObject CurrentWeaponModel { get { return currentWeaponModel; } }
    public List<GameObject> AccessoryModels { get { return accessoryModels; } }
    
    public EquipmentSystem(CombatController owner)
    {
        _owner = owner;
    }

    UniTask WaitForQueueCooldown()
    {
        return UniTask.WaitUntil(() => _owner.CombatContext.queuedAttack == null && _owner.StateMachine.CheckState<CombatIdleState>());
    }

    void EquipAcessories(WeaponObject weapon)
    {
        if (weapon.Accessories != null)
        {
            foreach (GameObject accessoryPrefab in weapon.Accessories)
            {
                WeaponModelData accessoryModelData = accessoryPrefab.GetComponent<WeaponModelData>();
                if (accessoryModelData == null)
                {
                    Debug.LogError("Accessory prefab does not have a WeaponModelData component.");
                    continue;
                }
                GameObject socketGameObject = availableSockets[accessoryModelData.SocketType];
                GameObject accessoryModel = GameObject.Instantiate(accessoryPrefab, socketGameObject.transform);
                accessoryModel.transform.localPosition += accessoryModelData.OffsetPosition;
                accessoryModel.transform.localEulerAngles += accessoryModelData.OffsetRotation;
                accessoryModels.Add(accessoryModel);
            }
        }
        else
        {
            Debug.Log("No accessories to equip for weapon: " + weapon.name);
        }
    }

    public void EquipWeapon(WeaponObject weapon)
    {
        if (!enableEquipmentSystem) return;
        _owner.CombatContext.canAttack = false;
        WaitForQueueCooldown();
        GameObject weaponModel = weapon.WeaponPrefab;
        if (weaponModel == null)
        {
            Debug.LogError("Weapon prefab is not assigned for weapon: " + weapon.name);
            return;
        }
        currentWeapon = weapon;
        if (currentWeaponModel != null)
        {
            GameObject.Destroy(currentWeaponModel);
        }
        if (accessoryModels != null)
        {
            foreach (GameObject accessory in accessoryModels)
            {
                GameObject.Destroy(accessory);
            }
            accessoryModels.Clear();
        }
        currentWeaponModelData = weaponModel.GetComponent<WeaponModelData>();
        if (currentWeaponModelData == null)
        {
            Debug.LogError("Weapon model does not have a WeaponModelData component.");
            return;
        }
        GameObject socketGameObject = availableSockets[currentWeaponModelData.SocketType];
        currentWeaponModel = GameObject.Instantiate(currentWeaponModelData.gameObject, socketGameObject.transform);
        currentWeaponModel.transform.localPosition += currentWeaponModelData.OffsetPosition;
        currentWeaponModel.transform.localEulerAngles += currentWeaponModelData.OffsetRotation;
        EquipAcessories(weapon);
        _owner.CombatContext.canAttack = true;
    }

}