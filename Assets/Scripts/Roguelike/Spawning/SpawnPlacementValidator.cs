using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validation delegate for a candidate's ground/NavMesh: returns whether the candidate maps to an
/// acceptable walkable location and snaps the accepted position (e.g. to the sampled NavMesh point).
/// SpawnSystem wires this to real NavMesh.SamplePosition; the dotnet harness injects a deterministic
/// fake, so the spawner stays testable without a baked NavMesh.
/// </summary>
public delegate bool GroundValidation(Vector3 candidate, out Vector3 snapped);

/// <summary>
/// Pure, deterministic placement validator for RandomZone spawning. Runs the documented pipeline for
/// each random candidate and gives up after the zone's MaxAttempts, so it never hangs:
///
///   candidate inside bounds
///     -> ground/NavMesh validation (only when the zone enables it; no validator = reject, never
///        invent a result)
///     -> blocking-geometry overlap (injected by the caller)
///     -> distance from Player (when a min distance is requested)
///     -> distance from already-placed enemies
///
/// No UnityEngine Physics or NavMesh calls live here — callers inject them (see SpawnSystem), which
/// keeps every rule unit-testable with plain functions and the SpawnSystem testable in the harness.
/// </summary>
public class SpawnPlacementValidator
{
    /// <summary>True when <paramref name="p"/> is inside the zone's box (Center +/- Size/2).</summary>
    public bool Contains(SpawnZone zone, Vector3 p)
    {
        if (zone == null) return false;
        Vector3 min = zone.Center - zone.Size * 0.5f;
        Vector3 max = zone.Center + zone.Size * 0.5f;
        return p.x >= min.x && p.x <= max.x
            && p.y >= min.y && p.y <= max.y
            && p.z >= min.z && p.z <= max.z;
    }

    /// <summary>
    /// Distance rules: candidate must be at least <paramref name="minPlayerDistance"/> from
    /// <paramref name="playerPosition"/> (skipped when the min is &lt;= 0) and at least
    /// <paramref name="minEnemyDistance"/> from every already-placed enemy position.
    /// </summary>
    public bool PassesDistanceRules(Vector3 candidate, Vector3 playerPosition, float minPlayerDistance,
        IReadOnlyList<Vector3> occupied, float minEnemyDistance)
    {
        if (minPlayerDistance > 0f && Vector3.Distance(candidate, playerPosition) < minPlayerDistance)
            return false;
        if (occupied == null) return true;
        for (int i = 0; i < occupied.Count; i++)
            if (Vector3.Distance(candidate, occupied[i]) < minEnemyDistance)
                return false;
        return true;
    }

    /// <summary>
    /// Resolve a valid spawn location for one enemy. Returns false after MaxAttempts failed
    /// candidates (bounded, no infinite retry). <paramref name="minPlayerDistance"/> is the
    /// effective value the caller wants enforced (e.g. 0 when no player reference exists).
    /// </summary>
    public bool TryFindLocation(SpawnZone zone, Vector3 playerPosition, float minPlayerDistance,
        IReadOnlyList<Vector3> occupied, GroundValidation ground, Func<Vector3, bool> blocking,
        out Vector3 location)
    {
        location = Vector3.zero;
        if (zone == null) return false;

        int attempts = 0;
        int maxAttempts = zone.MaxAttempts;
        while (attempts < maxAttempts)
        {
            attempts++;
            Vector3 candidate = zone.RandomPoint();
            if (!Contains(zone, candidate)) continue;

            Vector3 snapped = candidate;
            if (zone.UseNavMeshValidation)
            {
                if (ground == null) continue;                  // cannot validate -> reject, never fake a result
                if (!ground(candidate, out snapped)) continue; // invalid NavMesh location -> reject
                if (!Contains(zone, snapped)) continue;
            }

            if (blocking != null && blocking(snapped)) continue;  // overlapping blocking geometry -> reject

            if (!PassesDistanceRules(snapped, playerPosition, minPlayerDistance, occupied, zone.MinEnemyDistance))
                continue;

            location = snapped;
            return true;
        }
        return false;
    }
}
