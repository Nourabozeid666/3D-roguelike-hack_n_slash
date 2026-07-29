using System.Collections.Generic;
using UnityEngine;
// scriptable objects do not need any logic
// any scriptable object is a collection of data to be saved in one place
[CreateAssetMenu(
    fileName = "PatrolRoute'A'",
    menuName = "AI/Patrol Route"
)]
public class PatrolRoute : ScriptableObject
{
    [SerializeField] List<Transform> wayPoints;
    public IReadOnlyList<Transform> WayPoints => wayPoints;
}
