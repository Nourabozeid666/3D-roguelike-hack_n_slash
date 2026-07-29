using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PlayerContext
{
    [Header("References")]
    [SerializeField] internal LayerMask groundLayer;
    [SerializeField] internal LayerMask wallLayer;
    [SerializeField] internal Rigidbody rb;
    [SerializeField] internal Transform playerCamera;
    [SerializeField] internal Transform playerModel;
    [SerializeField] internal Text debugText;
    [SerializeField] internal Animator animator;

    [Header("Physics Settings")]
    [SerializeField] internal bool useCustomGravity = true;
    [SerializeField] internal bool canMove = true;
    [SerializeField] internal bool useDrag = true;
    [SerializeField] internal float gravity = -25f;
    [SerializeField] internal float risingMultiplier = 1f;
    [SerializeField] internal float apexMultiplier = 0.6f;
    [SerializeField] internal float fallingMultiplier = 2.5f;
    [SerializeField] internal float apexThreshold = 0.5f;  // Velocity range for apex
    [SerializeField] internal float MaxVelocity = 12f;
    [SerializeField] internal float jumpForwardPush = 10f;
    [SerializeField] internal float airMoveSpeedMultiplier = 0.05f;
    [Header("Movement Settings")]
    [SerializeField] internal float sprintSpeed = 145f;
    [SerializeField] internal float walkSpeed = 100f;
    [SerializeField] internal float dashSpeed = 50f;
    [SerializeField] internal float dashDuration = 0.2f;
    [SerializeField] internal float jumpForce = 5f;

    [Header("Physics Running Values")]
    [SerializeField] internal float speed = 100f;
    [SerializeField] internal float customDrag = 1.0f;
    [SerializeField] internal RaycastHit RightRaycast;
    [SerializeField] internal RaycastHit LeftRaycast;

    [Header("Player Input Values")]

    internal Vector2 moveDirection = Vector2.zero;
    internal bool isSprinting = false;
    internal float lastwallrunTime = 0f;
}