using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent scene-transition owner: the single reusable path for changing scenes. Owns the visual
/// fade to/from black, the asynchronous scene load (LoadSceneAsync + allowSceneActivation gating),
/// the loading label, and a full-screen input blackout while a transition is in flight.
///
/// It only EXECUTES a requested scene change. Save deletion/restoration, run state, Continue/Retry
/// availability and Main-Menu semantics stay with the existing owners (RunBootstrap,
/// MainMenuController, PauseController) — this class never decides whether a transition is allowed,
/// it just performs it, exactly once per request.
///
/// Ownership: created programmatically before the first scene loads and marked DontDestroyOnLoad, so
/// exactly one owner survives every single-scene load/unload (same conservative duplicate-guard
/// pattern as AudioCore). The overlay Canvas is rebuilt in code and carries no scene/prefab reference,
/// so no GUID/meta surface exists to break.
/// </summary>
public sealed class SceneTransitioner : MonoBehaviour
{
    /// <summary>Destination kind: the only transition-owned decision (drives cursor state).</summary>
    public enum Destination { Game, Menu }

    private static SceneTransitioner instance;
    public static SceneTransitioner Instance => instance;

    /// <summary>True once the persistent owner exists (always in a live Unity session).</summary>
    public static bool IsAvailable => instance != null;

    /// <summary>True while a transition is running; callers can consult it to avoid starting a new one.</summary>
    public static bool IsBusy => instance != null && instance.isTransitioning;

    [Tooltip("Fade to/from black, in seconds.")]
    [SerializeField] private float fadeSeconds = 0.35f;

    [Tooltip("How long the overlay stays fully black after the destination activates, so the scene "
        + "never flashes before the fade-in.")]
    [SerializeField] private float holdAfterActivationSeconds = 0.15f;

    private Canvas overlayCanvas;
    private RectTransform fadeRect;
    private Image fadeImage;
    private Text loadingLabel;
    private bool isTransitioning;

    /// <summary>Guarantees a single persistent owner before the first scene loads. Registry of the
    /// registered-owner token; a no-op once one is already present. Requires the UnityEngine
    /// RuntimeInitialize attribute; inert in the dotnet stub build.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInitialized()
    {
        if (instance != null)
        {
            return;
        }

        GameObject owner = new GameObject("SceneTransitioner");
        owner.AddComponent<SceneTransitioner>();
    }

    /// <summary>
    /// Request a scene change through the transition system. Single-flight: a second request while a
    /// transition is already running is ignored. Time is unfrozen first so the fade and the load can
    /// never stall on a paused/game-over freeze. When no persistent owner exists (headless harness
    /// build) the request is executed synchronously instead of fading.
    /// </summary>
    public static void RequestScene(string sceneName, Destination destination)
    {
        Time.timeScale = 1f;

        if (instance == null)
        {
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            ApplyDestinationCursor(destination);
            return;
        }

        instance.Begin(sceneName, destination);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"SceneTransitioner: duplicate instance '{name}' destroyed; keeping the existing owner.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();
        overlayCanvas.gameObject.SetActive(false);
    }

    private void Begin(string sceneName, Destination destination)
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        StartCoroutine(Run(sceneName, destination));
    }

    private IEnumerator Run(string sceneName, Destination destination)
    {
        Time.timeScale = 1f;
        overlayCanvas.gameObject.SetActive(true);
        loadingLabel.gameObject.SetActive(false);

        try
        {
            yield return FadeTo(1f, fadeSeconds);

            loadingLabel.gameObject.SetActive(true);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogWarning($"SceneTransitioner: LoadSceneAsync rejected '{sceneName}'; fading back in.");
            }
            else
            {
                op.allowSceneActivation = false;
                int dotCount = 0;
                while (op.progress < 0.9f)
                {
                    dotCount = (dotCount + 1) % 3;
                    loadingLabel.text = "LOADING" + new string('.', dotCount + 1);
                    yield return null;
                }
                loadingLabel.text = "LOADING...";
                op.allowSceneActivation = true;
                while (!op.isDone)
                {
                    yield return null;
                }
            }

            ApplyDestinationCursor(destination);
            yield return Hold(holdAfterActivationSeconds);

            loadingLabel.gameObject.SetActive(false);
            yield return FadeTo(0f, fadeSeconds);
        }
        finally
        {
            overlayCanvas.gameObject.SetActive(false);
            isTransitioning = false;
        }
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed >= duration ? 1f : elapsed / duration;
            SetOverlayAlpha(startAlpha + (target - startAlpha) * t);
            yield return null;
        }
        SetOverlayAlpha(target);
    }

    private IEnumerator Hold(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    private static void ApplyDestinationCursor(Destination destination)
    {
        if (destination == Destination.Game)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void BuildOverlay()
    {
        GameObject canvasGo = new GameObject("SceneTransitionOverlay");
        canvasGo.transform.SetParent(transform, false);

        overlayCanvas = canvasGo.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        fadeRect = CreateRect("FadeOverlay", canvasGo.transform);
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;

        fadeImage = fadeRect.gameObject.AddComponent<Image>();
        fadeImage.raycastTarget = true;
        fadeImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform labelRect = CreateRect("LoadingLabel", canvasGo.transform);
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 56f);
        labelRect.sizeDelta = new Vector2(0f, 48f);

        loadingLabel = labelRect.gameObject.AddComponent<Text>();
        loadingLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (loadingLabel.font == null)
        {
            loadingLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        loadingLabel.text = "LOADING...";
        loadingLabel.fontSize = 36;
        loadingLabel.fontStyle = FontStyle.Bold;
        loadingLabel.alignment = TextAnchor.MiddleCenter;
        loadingLabel.color = new Color(1f, 1f, 1f, 1f);
        loadingLabel.raycastTarget = false;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }
}