using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Orchestrates in-arena round advancement, cross-scene region transitions,
/// player healing (+25% per round, 100% per region), and the "To be continued..." finale screen.
/// </summary>
public class FloorTransitionManager : MonoBehaviour
{
    [Header("Region Configuration")]
    [Tooltip("Exposes region definitions (scenes, round counts, budgets) for easy designer editing in Inspector.")]
    [SerializeField] private RunRegionSettings regionSettings;

    [Header("References")]
    [SerializeField] private RunBootstrap runBootstrap;
    [SerializeField] private SpawnSystem spawnSystem;
    [SerializeField] private RoguelikeProgressionBootstrap progressionBootstrap;
    [SerializeField] private FloorExitPortal exitPortal;
    [SerializeField] private Transform arenaEntrancePoint;
    [SerializeField] private PlayerController playerController;

    [Header("Tuning")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float partialHealPercent = 0.25f; // +25% Max HP per round

    // Transition Canvas Elements
    private Canvas transitionCanvas;
    private CanvasGroup fadeCanvasGroup;
    private Text bannerText;
    private GameObject finalePanel;
    private Text finaleTitleText;
    private Text finaleStatsText;
    private Button finaleMenuButton;

    private bool isTransitioning = false;

    public RunRegionSettings RegionSettings => regionSettings;
    public FloorExitPortal ExitPortal => exitPortal;
    public bool IsTransitioning => isTransitioning;

    void Awake()
    {
        EnsureReferences();
        CreateTransitionOverlay();
    }

    void Start()
    {
        BindEvents();
    }

    void OnDestroy()
    {
        UnbindEvents();
    }

    void EnsureReferences()
    {
        if (regionSettings == null)
        {
            regionSettings = Resources.Load<RunRegionSettings>("RunRegionSettings");
            if (regionSettings == null)
            {
                regionSettings = ScriptableObject.CreateInstance<RunRegionSettings>();
            }
        }

        if (runBootstrap == null) runBootstrap = FindFirstObjectByType<RunBootstrap>();
        if (spawnSystem == null) spawnSystem = FindFirstObjectByType<SpawnSystem>();
        if (progressionBootstrap == null) progressionBootstrap = FindFirstObjectByType<RoguelikeProgressionBootstrap>();
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();

        if (exitPortal == null)
        {
            exitPortal = FindFirstObjectByType<FloorExitPortal>();
            if (exitPortal == null)
            {
                // Create a default portal GameObject in the arena
                GameObject portalGo = new GameObject("FloorExitPortal");
                portalGo.transform.position = new Vector3(0, 0, 10f);
                exitPortal = portalGo.AddComponent<FloorExitPortal>();
            }
        }

        if (arenaEntrancePoint == null)
        {
            GameObject entranceGo = GameObject.Find("ArenaEntrancePoint");
            if (entranceGo != null)
            {
                arenaEntrancePoint = entranceGo.transform;
            }
            else if (playerController != null)
            {
                GameObject newEntrance = new GameObject("ArenaEntrancePoint");
                newEntrance.transform.position = playerController.transform.position;
                newEntrance.transform.rotation = playerController.transform.rotation;
                arenaEntrancePoint = newEntrance.transform;
            }
        }
    }

    void BindEvents()
    {
        if (spawnSystem != null)
        {
            spawnSystem.FloorCleared += HandleFloorCleared;
        }

        if (exitPortal != null)
        {
            exitPortal.PortalEntered += HandlePortalEntered;
        }
    }

    void UnbindEvents()
    {
        if (spawnSystem != null)
        {
            spawnSystem.FloorCleared -= HandleFloorCleared;
        }

        if (exitPortal != null)
        {
            exitPortal.PortalEntered -= HandlePortalEntered;
        }
    }

    void HandleFloorCleared()
    {
        if (isTransitioning) return;

        RunData data = GetRunData();
        int regionIdx = data.currentRegionIndex;
        RegionConfig currentRegion = regionSettings.GetRegion(regionIdx);
        int totalRounds = currentRegion != null ? currentRegion.totalRounds : 10;
        bool isLastRound = data.currentRoundInRegion >= totalRounds;
        bool isLastRegion = regionSettings.IsFinalRegion(regionIdx);

        // Heal player partial health on round clear
        ApplyHealPercent(partialHealPercent);

        if (exitPortal != null)
        {
            if (isLastRound && isLastRegion)
            {
                exitPortal.Activate(FloorExitPortal.PortalVisualState.VictoryGateway, "Step into the gateway to conclude your journey...");
            }
            else if (isLastRound)
            {
                RegionConfig nextRegion = regionSettings.GetRegion(regionIdx + 1);
                string nextName = nextRegion != null ? nextRegion.regionName : "Next Region";
                exitPortal.Activate(FloorExitPortal.PortalVisualState.RegionGateway, $"Step into the gateway to enter {nextName}");
            }
            else
            {
                exitPortal.Activate(FloorExitPortal.PortalVisualState.RoundPortal, $"Step into portal to enter Round {data.currentRoundInRegion + 1}");
            }
        }
    }

    void HandlePortalEntered()
    {
        if (isTransitioning) return;

        RunData data = GetRunData();
        int regionIdx = data.currentRegionIndex;
        RegionConfig currentRegion = regionSettings.GetRegion(regionIdx);
        int totalRounds = currentRegion != null ? currentRegion.totalRounds : 10;
        bool isLastRound = data.currentRoundInRegion >= totalRounds;
        bool isLastRegion = regionSettings.IsFinalRegion(regionIdx);

        if (exitPortal != null) exitPortal.Deactivate();

        if (isLastRound && isLastRegion)
        {
            StartCoroutine(VictoryFinaleRoutine());
        }
        else if (isLastRound)
        {
            StartCoroutine(RegionTransitionRoutine(regionIdx + 1));
        }
        else
        {
            StartCoroutine(InRegionRoundRoutine());
        }
    }

    IEnumerator InRegionRoundRoutine()
    {
        isTransitioning = true;

        // 1. Fade out to black
        yield return FadeTo(1f, fadeDuration);

        // 2. Teleport player to entrance
        RepositionPlayerToEntrance();

        // 3. Advance round counter & data
        RunData data = GetRunData();
        data.AdvanceFloor();

        // 4. Save checkpoint
        SaveCheckpoint();

        // 5. Announce Round
        RegionConfig currentRegion = regionSettings.GetRegion(data.currentRegionIndex);
        string regName = currentRegion != null ? currentRegion.regionName : "Region";
        ShowBanner($"{regName}\nRound {data.currentRoundInRegion}", 1.5f);

        // 6. Populate new round of enemies
        if (spawnSystem != null)
        {
            spawnSystem.Populate(data.enemyBudget, data.floor);
        }

        if (runBootstrap != null && runBootstrap.Run != null)
        {
            runBootstrap.Run.BeginFloor();
        }

        // 7. Fade back in
        yield return FadeTo(0f, fadeDuration);

        isTransitioning = false;
    }

    IEnumerator RegionTransitionRoutine(int nextRegionIndex)
    {
        isTransitioning = true;

        // 1. Fade out to black
        yield return FadeTo(1f, fadeDuration);

        // 2. Full heal player for the new region
        ApplyHealPercent(1.0f);

        // 3. Advance region in RunData
        RunData data = GetRunData();
        RegionConfig nextRegion = regionSettings.GetRegion(nextRegionIndex);
        float nextBudget = nextRegion != null ? nextRegion.startingBudget : 25f;
        float nextGrowth = nextRegion != null ? nextRegion.budgetGrowthPerRound : 1.35f;
        float nextStatGrowth = nextRegion != null ? nextRegion.statGrowthPerRound : 1.12f;

        data.AdvanceRegion(nextBudget, nextGrowth, nextStatGrowth);

        // 4. Save full checkpoint including player build
        SaveCheckpoint();

        // 5. Load next region scene
        string targetScene = nextRegion != null ? nextRegion.sceneName : "basic game scene";
        RunSession.EnterFromMenu = true;
        Time.timeScale = 1f;

        Debug.Log($"[FloorTransitionManager] Loading scene for Region {nextRegionIndex + 1}: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }

    IEnumerator VictoryFinaleRoutine()
    {
        isTransitioning = true;

        // 1. Fade to solid black
        yield return FadeTo(1f, fadeDuration * 1.5f);

        // 2. Freeze time
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 3. Delete save file (run successfully completed)
        RunSaveService saves = new RunSaveService();
        saves.Delete();

        // 4. Show "To be continued..." finale UI
        if (finalePanel != null)
        {
            finalePanel.SetActive(true);

            int totalDefeated = spawnSystem != null ? spawnSystem.TotalDefeated : 0;
            int totalRounds = GetRunData().floor;
            int level = progressionBootstrap != null ? progressionBootstrap.Progression.CurrentLevel : 1;

            if (finaleTitleText != null)
            {
                finaleTitleText.text = "To be continued...";
            }

            if (finaleStatsText != null)
            {
                finaleStatsText.text = $"You have conquered both Regions!\n\n" +
                                       $"Total Rounds Survived: {totalRounds}\n" +
                                       $"Enemies Defeated: {totalDefeated}\n" +
                                       $"Hero Level Achieved: {level}";
            }
        }
    }

    void RepositionPlayerToEntrance()
    {
        if (playerController == null || arenaEntrancePoint == null) return;

        CharacterController cc = playerController.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerController.transform.position = arenaEntrancePoint.position;
        playerController.transform.rotation = arenaEntrancePoint.rotation;

        if (cc != null) cc.enabled = true;

        // Reset Rigidbody velocity if any
        Rigidbody rb = playerController.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ApplyHealPercent(float percent)
    {
        if (playerController != null && playerController.Entity is PlayerEntity pe && !pe.IsDead)
        {
            float healAmt = pe.MaxHealth * percent;
            pe.Heal(healAmt);
            progressionBootstrap?.UpdateHud();
            Debug.Log($"[FloorTransitionManager] Healed player for {healAmt} HP ({percent * 100}%). Current HP: {pe.Health}/{pe.MaxHealth}");
        }
    }

    void SaveCheckpoint()
    {
        RunSaveService saves = new RunSaveService();
        RunData data = GetRunData();

        SaveData save = new SaveData
        {
            floor = data.floor,
            clearedRooms = data.clearedRooms,
            currentRegionIndex = data.currentRegionIndex,
            currentRoundInRegion = data.currentRoundInRegion,
            totalRoundsPerRegion = data.totalRoundsPerRegion,
            enemyBudget = data.enemyBudget,
            enemyBudgetGrowth = data.enemyBudgetGrowth,
            enemyStatGrowth = data.enemyStatGrowth
        };

        if (progressionBootstrap != null)
        {
            progressionBootstrap.CaptureProgression(save);
        }

        saves.Save(save);
    }

    RunData GetRunData()
    {
        if (runBootstrap != null && runBootstrap.Run != null)
        {
            return runBootstrap.Run.Data;
        }
        return new RunData();
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.05f;
    }

    void ShowBanner(string message, float duration)
    {
        if (bannerText == null) return;
        bannerText.text = message;
        bannerText.gameObject.SetActive(true);
        StartCoroutine(HideBannerAfter(duration));
    }

    IEnumerator HideBannerAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (bannerText != null) bannerText.gameObject.SetActive(false);
    }

    void CreateTransitionOverlay()
    {
        GameObject canvasGo = new GameObject("FloorTransitionOverlay");
        canvasGo.layer = LayerMask.NameToLayer("UI");
        transitionCanvas = canvasGo.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 999;

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject groupGo = new GameObject("FadeGroup");
        groupGo.transform.SetParent(canvasGo.transform, false);
        RectTransform groupRect = groupGo.AddComponent<RectTransform>();
        groupRect.anchorMin = Vector2.zero;
        groupRect.anchorMax = Vector2.one;
        groupRect.sizeDelta = Vector2.zero;

        fadeCanvasGroup = groupGo.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        // Black Image
        GameObject bgGo = new GameObject("BlackOverlay");
        bgGo.transform.SetParent(groupGo.transform, false);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = Color.black;

        // Banner Text
        GameObject bannerGo = new GameObject("BannerText");
        bannerGo.transform.SetParent(groupGo.transform, false);
        RectTransform bannerRect = bannerGo.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.1f, 0.4f);
        bannerRect.anchorMax = new Vector2(0.9f, 0.6f);
        bannerRect.sizeDelta = Vector2.zero;
        bannerText = bannerGo.AddComponent<Text>();
        bannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bannerText.fontSize = 42;
        bannerText.fontStyle = FontStyle.Bold;
        bannerText.alignment = TextAnchor.MiddleCenter;
        bannerText.color = Color.white;
        bannerGo.SetActive(false);

        // Finale Panel (To be continued...)
        finalePanel = new GameObject("FinalePanel");
        finalePanel.transform.SetParent(groupGo.transform, false);
        RectTransform finaleRect = finalePanel.AddComponent<RectTransform>();
        finaleRect.anchorMin = Vector2.zero;
        finaleRect.anchorMax = Vector2.one;
        finaleRect.sizeDelta = Vector2.zero;

        // Finale Title ("To be continued...")
        GameObject titleGo = new GameObject("FinaleTitle");
        titleGo.transform.SetParent(finalePanel.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.6f);
        titleRect.anchorMax = new Vector2(0.9f, 0.85f);
        titleRect.sizeDelta = Vector2.zero;
        finaleTitleText = titleGo.AddComponent<Text>();
        finaleTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        finaleTitleText.fontSize = 56;
        finaleTitleText.fontStyle = FontStyle.Bold;
        finaleTitleText.alignment = TextAnchor.MiddleCenter;
        finaleTitleText.color = new Color(0.95f, 0.85f, 0.3f); // Gold

        // Finale Stats
        GameObject statsGo = new GameObject("FinaleStats");
        statsGo.transform.SetParent(finalePanel.transform, false);
        RectTransform statsRect = statsGo.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.1f, 0.3f);
        statsRect.anchorMax = new Vector2(0.9f, 0.58f);
        statsRect.sizeDelta = Vector2.zero;
        finaleStatsText = statsGo.AddComponent<Text>();
        finaleStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        finaleStatsText.fontSize = 24;
        finaleStatsText.alignment = TextAnchor.MiddleCenter;
        finaleStatsText.color = Color.white;

        // Finale Menu Button
        GameObject btnGo = new GameObject("MainMenuButton");
        btnGo.transform.SetParent(finalePanel.transform, false);
        RectTransform btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, 0.15f);
        btnRect.anchorMax = new Vector2(0.65f, 0.25f);
        btnRect.sizeDelta = Vector2.zero;
        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        finaleMenuButton = btnGo.AddComponent<Button>();
        finaleMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        });

        GameObject btnTextGo = new GameObject("ButtonText");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        RectTransform btnTextRect = btnTextGo.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        Text btnText = btnTextGo.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 26;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = "RETURN TO MAIN MENU";

        finalePanel.SetActive(false);
    }
}
