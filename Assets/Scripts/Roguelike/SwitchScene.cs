using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the dramatic Region 2 scene switch sequence:
/// 1. Player triggers the switch (via trigger collider or FloorTransitionManager).
/// 2. Camera shakes violently with drift-free LateUpdate offsets.
/// 3. Ominous rumble and select sound effects play for a few seconds.
/// 4. Ominous banner with "The boss has spawned..." appears soon after for a few seconds.
/// 5. Screen fades to black, gameplay cleans up, and loads the DemoEnd scene.
/// </summary>
public class SwitchScene : MonoBehaviour
{
    [Header("Scene Transition Target")]
    [SerializeField] private string targetSceneName = "DemoEnd";

    [Header("Cinematic Timing")]
    [SerializeField] private float initialDelayBeforeBossText = 2.0f;
    [SerializeField] private float bossTextDisplayDuration = 3.0f;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Camera Shake Tuning")]
    [SerializeField] private float shakeIntensity = 0.35f;
    [SerializeField] private float bossSpawnShakeIntensity = 0.55f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip rumbleSound;
    [SerializeField] private AudioClip bossAlertSound;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1.0f;

    [Header("Text & Styling")]
    [SerializeField] private string bossSpawnText = "The boss has spawned...";
    [SerializeField] private Color bossTextColor = new Color(0.95f, 0.22f, 0.22f, 1f);

    [Header("Trigger Options")]
    [SerializeField] private bool triggerOnPlayerTouch = true;

    public event Action OnSequenceStarted;
    public event Action OnBossTextShown;
    public event Action OnSequenceCompleted;

    private bool hasTriggered = false;
    private AudioSource audioSource;
    private CameraShaker cameraShaker;
    private Canvas cinematicCanvas;
    private CanvasGroup bossTextGroup;
    private Image fadeOverlay;

    public bool HasTriggered => hasTriggered;

    void Awake()
    {
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // Auto-load fallback sounds from Resources if not serialized
        if (rumbleSound == null)
        {
            rumbleSound = Resources.Load<AudioClip>("Audio/BossRumble");
        }
        if (bossAlertSound == null)
        {
            bossAlertSound = Resources.Load<AudioClip>("Audio/BossEncounter");
        }
        if (selectSound == null)
        {
            selectSound = Resources.Load<AudioClip>("Audio/BossSelect");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!triggerOnPlayerTouch || hasTriggered) return;

        // Check if other is player
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
        {
            TriggerSwitchScene();
        }
    }

    /// <summary>
    /// Initiates the full camera shake -> sound -> "The boss has spawned..." -> DemoEnd sequence.
    /// Safe to call from FloorTransitionManager, portal interactions, or trigger colliders.
    /// </summary>
    public void TriggerSwitchScene()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        StartCoroutine(SwitchSceneRoutine());
    }

    IEnumerator SwitchSceneRoutine()
    {
        Debug.Log("[SwitchScene] Triggered Region 2 finale boss spawn sequence!");
        OnSequenceStarted?.Invoke();

        // 1. Lock player movement during the cinematic sequence
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.context.canMove = false;
            pc.ResetHorizontalVelocity();
        }

        // 2. Setup Camera Shaker on active camera
        Camera targetCam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (targetCam != null)
        {
            cameraShaker = targetCam.GetComponent<CameraShaker>();
            if (cameraShaker == null)
            {
                cameraShaker = targetCam.gameObject.AddComponent<CameraShaker>();
            }
            cameraShaker.intensity = shakeIntensity;
            cameraShaker.isShaking = true;
        }

        // 3. Play initial sounds (earthquake rumble + select sounds for a few seconds)
        if (audioSource != null)
        {
            if (rumbleSound != null)
            {
                audioSource.clip = rumbleSound;
                audioSource.volume = soundVolume;
                audioSource.loop = true;
                audioSource.Play();
            }

            if (selectSound != null)
            {
                StartCoroutine(PlaySelectSoundsRoutine());
            }
        }

        // Build cinematic canvas UI
        CreateCinematicUi();

        // 4. Wait initial few seconds with camera shake and rumble
        yield return new WaitForSecondsRealtime(initialDelayBeforeBossText);

        // 5. Increase shake intensity & play boss alert sound stinger
        if (cameraShaker != null)
        {
            cameraShaker.intensity = bossSpawnShakeIntensity;
        }

        if (audioSource != null && bossAlertSound != null)
        {
            audioSource.PlayOneShot(bossAlertSound, soundVolume);
        }

        // 6. Show "The boss has spawned..." text
        OnBossTextShown?.Invoke();
        if (bossTextGroup != null)
        {
            bossTextGroup.alpha = 1f;
        }

        // 7. Display text for a few seconds
        yield return new WaitForSecondsRealtime(bossTextDisplayDuration);

        // 8. Fade out to black
        if (fadeOverlay != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            fadeOverlay.color = Color.black;
        }

        // 9. Stop camera shake and audio
        if (cameraShaker != null)
        {
            cameraShaker.isShaking = false;
        }
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 10. Clean up run save (run completed)
        RunSaveService saves = new RunSaveService();
        saves.Delete();

        OnSequenceCompleted?.Invoke();

        // 11. Switch to DemoEnd scene through the transition system (menu destination handles cursor)
        Debug.Log($"[SwitchScene] Loading target scene: {targetSceneName}");
        SceneTransitioner.RequestScene(targetSceneName, SceneTransitioner.Destination.Menu);
    }

    IEnumerator PlaySelectSoundsRoutine()
    {
        // Play select chimes / pulses over the first 2 seconds
        float timer = 0f;
        while (timer < initialDelayBeforeBossText && audioSource != null && selectSound != null)
        {
            audioSource.PlayOneShot(selectSound, soundVolume * 0.85f);
            yield return new WaitForSecondsRealtime(0.5f);
            timer += 0.5f;
        }
    }

    void CreateCinematicUi()
    {
        if (cinematicCanvas != null) return;

        GameObject canvasGo = new GameObject("CinematicBossCanvas");
        cinematicCanvas = canvasGo.AddComponent<Canvas>();
        cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cinematicCanvas.sortingOrder = 999;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Top cinematic letterbox bar
        RectTransform topBar = PlayerUiKit.Rect("TopBar", canvasGo.transform);
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 130f);
        topBar.anchoredPosition = Vector2.zero;
        Image topImg = topBar.gameObject.AddComponent<Image>();
        topImg.color = Color.black;
        topImg.raycastTarget = false;

        // Bottom cinematic letterbox bar
        RectTransform botBar = PlayerUiKit.Rect("BottomBar", canvasGo.transform);
        botBar.anchorMin = new Vector2(0f, 0f);
        botBar.anchorMax = new Vector2(1f, 0f);
        botBar.pivot = new Vector2(0.5f, 0f);
        botBar.sizeDelta = new Vector2(0f, 130f);
        botBar.anchoredPosition = Vector2.zero;
        Image botImg = botBar.gameObject.AddComponent<Image>();
        botImg.color = Color.black;
        botImg.raycastTarget = false;

        // Text container with CanvasGroup for fade-in
        GameObject textContainer = new GameObject("BossTextContainer", typeof(RectTransform));
        textContainer.transform.SetParent(canvasGo.transform, false);
        bossTextGroup = textContainer.AddComponent<CanvasGroup>();
        bossTextGroup.alpha = 0f; // starts hidden until the prompt fires

        RectTransform textContainerRect = textContainer.GetComponent<RectTransform>();
        PlayerUiKit.Stretch(textContainerRect);

        // Darkened center banner backdrop
        RectTransform bannerRect = PlayerUiKit.Rect("BannerBackdrop", textContainer.transform);
        PlayerUiKit.Pin(bannerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 160f));
        Image bannerImg = bannerRect.gameObject.AddComponent<Image>();
        bannerImg.color = new Color(0.05f, 0.02f, 0.02f, 0.75f);
        bannerImg.raycastTarget = false;

        // "The boss has spawned..." text
        Text text = PlayerUiKit.Text("BossText", bannerRect, 56, TextAnchor.MiddleCenter, bossTextColor);
        text.fontStyle = FontStyle.Bold;
        text.text = bossSpawnText;
        PlayerUiKit.Stretch(text.rectTransform);

        // Bold black outline
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Fullscreen fade overlay (starts transparent)
        RectTransform fadeRect = PlayerUiKit.Rect("FadeOverlay", canvasGo.transform);
        PlayerUiKit.Stretch(fadeRect);
        fadeOverlay = fadeRect.gameObject.AddComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;
    }

    void OnDestroy()
    {
        if (cameraShaker != null)
        {
            cameraShaker.isShaking = false;
        }
        if (cinematicCanvas != null)
        {
            Destroy(cinematicCanvas.gameObject);
        }
    }
}

/// <summary>
/// Camera shaker component that applies jitter in LateUpdate in a clean, drift-free manner
/// on top of whatever camera follow / Cinemachine logic runs in Update.
/// </summary>
public class CameraShaker : MonoBehaviour
{
    public float intensity = 0.35f;
    public bool isShaking = false;

    private Vector3 currentOffset;

    void LateUpdate()
    {
        // Revert last frame's offset first so follow logic operates on base position
        if (currentOffset != Vector3.zero)
        {
            transform.position -= currentOffset;
            currentOffset = Vector3.zero;
        }

        if (isShaking && intensity > 0f)
        {
            currentOffset = UnityEngine.Random.insideUnitSphere * intensity;
            transform.position += currentOffset;
        }
    }

    void OnDisable()
    {
        if (currentOffset != Vector3.zero)
        {
            transform.position -= currentOffset;
            currentOffset = Vector3.zero;
        }
    }
}
