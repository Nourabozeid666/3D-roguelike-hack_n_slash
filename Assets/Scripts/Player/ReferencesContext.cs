using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ReferencesContext
{
    [Header("References")]
    [SerializeField] internal LayerMask groundLayer;
    [SerializeField] internal LayerMask wallLayer;
    [SerializeField] internal Rigidbody rb;
    [SerializeField] internal Transform playerCamera;
    [SerializeField] internal Transform playerModel;
    [SerializeField] internal Text debugText;
    [SerializeField] internal Animator animator;
    [SerializeField] internal Text combatDebugText;
    [SerializeField] internal Text attackDebugText;
}