using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

/// <summary>
/// Bakes the runtime NavMesh at scene load from the enabled NavMeshSurfaces in this subtree, then
/// re-enables scene-placed NavMeshAgents that were disabled in the Inspector (disabled so they never
/// try to register before a NavMesh exists and log "Failed to create agent").
///
/// Attach this to the GameObject holding the scene's NavMeshSurface(s). Works for TestingScene and
/// any scene using runtime-baked navigation; disabled surfaces holding stale baked data are skipped.
/// </summary>
public class RuntimeNavMeshBuilder : MonoBehaviour
{
    void Awake()
    {
        NavMeshSurface[] surfaces = GetComponentsInChildren<NavMeshSurface>(true);
        foreach (NavMeshSurface surface in surfaces)
            surface.BuildNavMesh();

        NavMeshAgent[] agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        foreach (NavMeshAgent agent in agents)
        {
            // Re-register every scene agent against the freshly baked NavMesh (even agents the
            // Inspector left enabled): toggling re-runs OnEnable so any agent that came up before
            // a NavMesh existed attaches now instead of staying "not placed".
            if (agent.enabled)
                agent.enabled = false;
            agent.enabled = true;
        }
    }
}