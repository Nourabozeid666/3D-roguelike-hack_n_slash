using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TEST-ONLY runtime HUD + console logger for the SpawnSystem in test scenes (TestingScene.unity).
/// Not production code: no production UI involvement, free use of FindObjectsOfType is fine here.
/// Builds its own Screen-Space-Overlay Canvas + legacy UI Text at runtime, styled to match the
/// existing Run/State debug texts already in the scene (Overlay Canvas, CanvasScaler 1920x1080
/// match-height, Arial bold). All numbers come from the live SpawnSystem, TestEnemy and the
/// driver's real RunController (AliveCount, IsFloorCleared, Died events, Run state/floor) -
/// nothing is hardcoded. Run number is NOT shown: no run-number field exists in the codebase
/// (RunData has floor/clearedRooms/enemyBudget only), so it is reported as unavailable.
/// </summary>
public class SpawnTestDebugDisplay : MonoBehaviour
{
    [SerializeField] SpawnSystem spawnSystem;

    readonly HashSet<TestEnemy> tracked = new();
    int spawned;
    int dead;
    bool wasCleared;

    Text hudText;

    string SceneName => SceneManager.GetActiveScene().name;

    /// <summary>Last text written to the HUD (test-observable, e.g. dotnet harness).</summary>
    public string CurrentHudText => hudText != null ? hudText.text : string.Empty;

    void Awake()
    {
        BuildHud();
    }

    void Start()
    {
        Debug.Log($"[SpawnTest] Debug display active in scene '{SceneName}'");
    }

    void Update()
    {
        if (spawnSystem == null) return;

        // A fresh Populate after a clear starts a new floor: reset the per-floor counters.
        int alive = spawnSystem.AliveCount();
        if (wasCleared && alive > 0)
        {
            spawned = 0;
            dead = 0;
            tracked.Clear();
            wasCleared = false;
        }

        foreach (TestEnemy enemy in FindObjectsOfType<TestEnemy>())
        {
            if (tracked.Add(enemy))
            {
                spawned++;
                LogSpawn(enemy);
                enemy.Died += () => LogDeath(enemy);
            }
        }

        if (spawnSystem.IsFloorCleared && !wasCleared)
        {
            wasCleared = true;
            Debug.Log($"[SpawnTest] FloorCleared=true in scene '{SceneName}' | Spawned={spawned} Dead={dead}");
        }
        else if (!spawnSystem.IsFloorCleared)
        {
            wasCleared = false;
        }

        RefreshHud();
    }

    void LogSpawn(TestEnemy enemy)
    {
        SpawnPoint nearest = NearestPoint(enemy.transform.position);
        string pointDesc = nearest != null
            ? $"SpawnPoint '{nearest.name}' at {nearest.Position}"
            : "no SpawnPoint";
        Debug.Log($"[SpawnTest] Spawned #{spawned} in scene '{SceneName}' at {pointDesc} | enemy pos {enemy.transform.position} | AliveCount={spawnSystem.AliveCount()}");
    }

    void LogDeath(TestEnemy enemy)
    {
        dead++;
        Debug.Log($"[SpawnTest] Enemy died at {enemy.transform.position} | Spawned={spawned} Alive={spawnSystem.AliveCount()} Dead={dead} | FloorCleared={spawnSystem.IsFloorCleared}");
    }

    SpawnPoint NearestPoint(Vector3 position)
    {
        SpawnPoint nearest = null;
        float best = float.MaxValue;
        foreach (SpawnPoint p in spawnSystem.GetComponentsInChildren<SpawnPoint>(true))
        {
            float d = Vector3.Distance(position, p.Position);
            if (d < best) { best = d; nearest = p; }
        }
        return nearest;
    }

    void RefreshHud()
    {
        if (hudText == null) return;
        int alive = spawnSystem != null ? spawnSystem.AliveCount() : 0;
        hudText.text = $"SPAWN TEST / Scene: {SceneName}\n" +
                       $"{ReadRunInfo()}\n" +
                       $"Spawned: {spawned} / Alive: {alive} / Dead: {dead}\n" +
                       $"Wave: {ReadWaveInfo()}\n" +
                       $"Budget: {ReadBudgetInfo()}\n" +
                       $"Floor Cleared: {(spawnSystem != null && spawnSystem.IsFloorCleared ? "YES" : "NO")}" +
                       $"{ReadCompositionInfo()}";
    }

    /// <summary>
    /// Live wave info from the SpawnSystem's pacing plan: current wave / total waves and how many
    /// unspawned composition entries remain. Waves are reported even when off (1/1, 0 left) because
    /// the numbers come from the real plan, never hardcoded.
    /// </summary>
    string ReadWaveInfo()
    {
        if (spawnSystem == null) return "n/a";
        return $"{spawnSystem.CurrentWave}/{spawnSystem.WaveCount} (waves) | {spawnSystem.RemainingInComposition} composition entries left";
    }

    string ReadBudgetInfo()
    {
        if (spawnSystem == null) return "n/a";
        return spawnSystem.CurrentBudget.ToString("0.##");
    }

    /// <summary>
    /// Last spawn composition summary (floor / available types / target / composition / cost) read
    /// from the live SpawnSystem. Shown only when a floor has been populated. Test/debug only.
    /// </summary>
    string ReadCompositionInfo()
    {
        if (spawnSystem == null || string.IsNullOrEmpty(spawnSystem.LastCompositionInfo)) return "";
        return $"\nComp: {spawnSystem.LastCompositionInfo}";
    }

    /// <summary>
    /// Run state + floor come from the REAL RunController that the scene's run owner drives — the
    /// test driver (SpawnSystemTestDriver) in a test scene, or the production bootstrap
    /// (RunBootstrap) when the scene was entered from the Main Menu. If neither is present, both are
    /// reported as n/a - they are never invented.
    /// </summary>
    string ReadRunInfo()
    {
        SpawnSystemTestDriver driver = FindObjectOfType<SpawnSystemTestDriver>();
        if (driver != null && driver.Run != null)
            return $"Run state: {driver.Run.CurrentState} / Floor: {driver.Run.Data.floor}";

        RunBootstrap bootstrap = FindObjectOfType<RunBootstrap>();
        if (bootstrap != null && bootstrap.Run != null)
            return $"Run state: {bootstrap.Run.CurrentState} / Floor: {bootstrap.Run.Data.floor}";

        return "Run: n/a (no driver or bootstrap)";
    }

    /// <summary>
    /// Build a HUD that matches the scene's existing debug texts: Screen-Space-Overlay Canvas,
    /// CanvasScaler (ScaleWithScreenSize, 1920x1080, match height), Arial bold legacy UI Text,
    /// anchored top-left. A translucent panel keeps it readable over any background.
    /// </summary>
    void BuildHud()
    {
        GameObject canvasGo = new GameObject("SpawnTestDebugCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(16, -16);
        panelRect.sizeDelta = new Vector2(760, 230);
        panelGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(0, 1);
        textRect.pivot = new Vector2(0, 1);
        textRect.anchoredPosition = new Vector2(24, -24);
        textRect.sizeDelta = new Vector2(740, 210);
        hudText = textGo.AddComponent<Text>();
        hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudText.fontSize = 34;
        hudText.fontStyle = FontStyle.Bold;
        hudText.color = Color.white;
        hudText.alignment = TextAnchor.UpperLeft;
        hudText.horizontalOverflow = HorizontalWrapMode.Wrap;
        hudText.verticalOverflow = VerticalWrapMode.Overflow;
        hudText.raycastTarget = false;
    }
}
