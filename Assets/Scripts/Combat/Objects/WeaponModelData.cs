using UnityEngine;
using System;

public class WeaponModelData : MonoBehaviour
{
    [SerializeField] private SocketType socketType;
    [SerializeField] private Vector3 offsetPosition;
    [SerializeField] private Vector3 offsetRotation;
    public SocketType SocketType { get { return socketType; } }
    public Vector3 OffsetPosition { get { return offsetPosition; } }
    public Vector3 OffsetRotation { get { return offsetRotation; } }
}