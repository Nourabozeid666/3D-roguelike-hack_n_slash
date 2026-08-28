using UnityEngine;
using System;

public class WeaponModelData : MonoBehaviour
{
    [SerializeField] private SocketType socketType;
    [SerializeField] private Vector3 offsetPosition;
    [SerializeField] private Vector3 offsetRotation;
    [SerializeField] private Transform bladePivot;
    [SerializeField] private Vector3 bladeScaleDirection;
    [SerializeField] private Transform bladeSizeParts;
    [SerializeField] private Vector3 bladeSizePartsScaleDirection;
    [SerializeField] private float scaleMultiplier = 0.5f;
    public SocketType SocketType { get { return socketType; } }
    public Vector3 OffsetPosition { get { return offsetPosition; } }
    public Vector3 OffsetRotation { get { return offsetRotation; } }
    public Transform BladePivot { get { return bladePivot; } }
    public Vector3 BladeScaleDirection { get { return bladeScaleDirection; } }
    public Transform BladeSizeParts { get { return bladeSizeParts; } }
    public Vector3 BladeSizePartsScaleDirection { get { return bladeSizePartsScaleDirection; } }
    public float ScaleMultiplier { get { return scaleMultiplier; } }
}