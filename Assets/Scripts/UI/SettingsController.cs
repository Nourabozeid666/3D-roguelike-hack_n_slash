using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared settings logic used by BOTH the Main Menu settings panel and the Pause Menu
/// settings panel (same script, two scene/prefab instances). All settings rows (section headers,
/// Master/Music/SFX/UI sliders, Mute toggle, Resolution dropdown, Fullscreen toggle) are built and
/// positioned at runtime from ONE code path so both panels can never drift apart.
///
/// Layout: rows are placed into a scroll container that sits between the static "SETTINGS" title
/// and the static Back button (which stays OUTSIDE the scroll content so it is always reachable).
/// The canvas is match-height referenced (1920x1080), so reference-unit coordinates scale with the
/// screen; the scroll view height adapts to the available region and its scrollbar auto-hides when
/// the content fits, so the same layout serves normal/wide/smaller aspect ratios.
///
/// Theme: project red accent (same red used by the volume slider fills). Toggle on-state + dropdown
/// highlight/selection use the red accent while the normal/off state stays dark and readable.</summary>
public class SettingsController : MonoBehaviour
{
    // ---- layout (reference units; canvas is match-height 1920x1080) ----
    public const float RowPitch = 44f;
    public const float HeaderPitch = 50f;
    public const float TopPadding = 8f;
    public const float LabelX = -380f;
    public const float LabelWidth = 320f;
    public const float LabelHeight = 40f;
    public const float ControlX = 130f;
    public const float SliderWidth = 500f;
    public const float SliderHeight = 20f;
    public const float ToggleSize = 40f;
    public const float DropdownWidth = 260f;
    public const float DropdownHeight = 48f;
    public const float HeaderWidth = 500f;
    public const float HeaderHeight = 42f;

    // Scene-authored constants: title spans up to y=272 (center-based), Back button is 200x175.
    // The scroll region fits between them; Back stays below it, outside the scroll content.
    private const float TitleBottomY = 272f;
    public const float BackY = -240f;
    private const float BackHalfHeight = 87.5f;
    private const float RegionInset = 14f;

    // ---- theme ----
    private static readonly Color RedAccent = new Color(0.85f, 0.2f, 0.2f, 1f);
    private static readonly Color VolumeFillColor = RedAccent;
    private static readonly Color ControlBackgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color ControlHighlightColor = new Color(0.22f, 0.24f, 0.3f, 1f);
    private static readonly Color PressedRedColor = new Color(0.55f, 0.15f, 0.15f, 1f);
    private static readonly Color SelectedRedColor = new Color(0.32f, 0.09f, 0.09f, 1f);
    private static readonly Color ListBackgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.98f);
    private static readonly Color ListItemColor = new Color(0.12f, 0.14f, 0.18f, 1f);
    private static readonly Color DisabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Serializable]
    public class ResolutionPreset
    {
        public int width = 1920;
        public int height = 1080;
    }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Font uiFont;
    [SerializeField] private RectTransform fullscreenLabel;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button resolutionButton;
    [SerializeField] private Text resolutionLabel;
    [SerializeField] private RectTransform resolutionLabelTitle;
    [SerializeField] private Button backButton;

    [Header("Options")]
    [SerializeField] private List<ResolutionPreset> resolutionPresets = new List<ResolutionPreset>
    {
        new ResolutionPreset { width = 1920, height = 1080 },
        new ResolutionPreset { width = 1600, height = 900 },
        new ResolutionPreset { width = 1280, height = 720 },
        new ResolutionPreset { width = 1024, height = 768 },
    };

    private readonly Dictionary<AudioBus, Slider> volumeSliders = new Dictionary<AudioBus, Slider>();
    private readonly List<ResolutionPreset> resolutionOptions = new List<ResolutionPreset>();
    private Toggle muteToggle;
    private bool isMuted;
    private float lastMasterBeforeMute = 1f;
    private int resolutionIndex;

    private ScrollRect settingsScroll;
    private RectTransform scrollViewport;
    private RectTransform scrollContent;
    private RectTransform scrollbarHandle;

    private IAudioService audioService;
    private bool audioServiceCached;

    void Start()
    {
        audioService = ResolveAudioService();
        audioServiceCached = audioService != null;

        BuildLayout();

        BuildResolutionOptions();
        Dropdown resolutionDropdown = SetupResolutionDropdown();

        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        SeedVolumes();
        fullscreenToggle.onValueChanged.AddListener(_ => Apply());
        muteToggle.onValueChanged.AddListener(SetMute);
        volumeSliders[AudioBus.Master].onValueChanged.AddListener(_ => { UnmuteIfMasterRaised(); Apply(); });
        volumeSliders[AudioBus.Music].onValueChanged.AddListener(_ => Apply());
        volumeSliders[AudioBus.Sfx].onValueChanged.AddListener(_ => Apply());
        volumeSliders[AudioBus.Ui].onValueChanged.AddListener(_ => Apply());
        backButton.onClick.AddListener(Close);

        resolutionDropdown.onValueChanged.AddListener(index =>
        {
            resolutionIndex = index;
            Apply();
        });

        AdjustScrollToFit();
    }

    public void Open()
    {
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    private void Apply()
    {
        float master = volumeSliders[AudioBus.Master].value;
        if (isMuted && master > 0f)
        {
            isMuted = false;
            muteToggle.SetIsOnWithoutNotify(false);
        }

        if (audioServiceCached && audioService != null)
        {
            // Prefer the Audio Core (drives the mixer / managed sources).
            audioService.SetVolume(AudioBus.Master, master);
            audioService.SetVolume(AudioBus.Music, volumeSliders[AudioBus.Music].value);
            audioService.SetVolume(AudioBus.Sfx, volumeSliders[AudioBus.Sfx].value);
            audioService.SetVolume(AudioBus.Ui, volumeSliders[AudioBus.Ui].value);
        }
        else
        {
            // Fallback: no Audio Core present; drive the global listener volume directly (Master only).
            AudioListener.volume = master;
        }

        bool fullscreen = fullscreenToggle.isOn;
        Screen.fullScreen = fullscreen;

        if (resolutionOptions.Count > 0)
        {
            ResolutionPreset preset = resolutionOptions[Mathf.Clamp(resolutionIndex, 0, resolutionOptions.Count - 1)];
            Screen.SetResolution(preset.width, preset.height,
                fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }

    private void UnmuteIfMasterRaised()
    {
        if (isMuted && volumeSliders[AudioBus.Master].value > 0f)
        {
            isMuted = false;
            muteToggle.SetIsOnWithoutNotify(false);
        }
    }

    private void SetMute(bool muted)
    {
        isMuted = muted;
        if (muted)
        {
            lastMasterBeforeMute = volumeSliders[AudioBus.Master].value;
            volumeSliders[AudioBus.Master].SetValueWithoutNotify(0f);
        }
        else
        {
            volumeSliders[AudioBus.Master].SetValueWithoutNotify(lastMasterBeforeMute);
        }
        Apply();
    }

    private void BuildResolutionOptions()
    {
        resolutionOptions.Clear();
        for (int i = 0; i < resolutionPresets.Count; i++)
        {
            AddUniqueResolution(resolutionPresets[i]);
        }

        // Always represent the currently active resolution so the dropdown never lies about it.
        Resolution current = Screen.currentResolution;
        AddUniqueResolution(new ResolutionPreset { width = current.width, height = current.height });

        resolutionIndex = 0;
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            if (resolutionOptions[i].width == current.width && resolutionOptions[i].height == current.height)
            {
                resolutionIndex = i;
                break;
            }
        }
    }

    private void AddUniqueResolution(ResolutionPreset preset)
    {
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            if (resolutionOptions[i].width == preset.width && resolutionOptions[i].height == preset.height)
            {
                return;
            }
        }
        resolutionOptions.Add(new ResolutionPreset { width = preset.width, height = preset.height });
    }

    private Dropdown SetupResolutionDropdown()
    {
        RectTransform ddRT = resolutionButton.transform as RectTransform;
        ddRT.sizeDelta = new Vector2(DropdownWidth, DropdownHeight);

        Image ddBackground = ddRT.GetComponent<Image>();
        if (ddBackground != null)
        {
            ddBackground.color = ControlBackgroundColor;
            ddBackground.sprite = (Sprite)null;
            ddBackground.type = Image.Type.Simple;
        }

        Selectable ddSelectable = ddRT.GetComponent<Selectable>();
        if (ddSelectable != null)
        {
            ddSelectable.targetGraphic = ddBackground;
            ddSelectable.colors = BuildSelectableColors();
        }

        // The old click behavior is being replaced by the Dropdown; drop the Button component.
        Destroy(resolutionButton);

        // Caption: reuse the existing button label text ("1920 x 1080").
        // Dropdown arrow: small red indicator so the control reads as an openable list.
        Text arrow = CreateTextChild(ddRT.gameObject, "Arrow", new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(40f, 40f), "\u25BC", RedAccent, 22, TextAnchor.MiddleCenter);
        arrow.transform.SetAsLastSibling();

        Dropdown dropdown = ddRT.gameObject.AddComponent<Dropdown>();
        dropdown.targetGraphic = ddBackground;
        dropdown.captionText = resolutionLabel;
        dropdown.options = BuildDropdownOptions();

        RectTransform template = CreateDropdownTemplate(ddRT, dropdown);
        dropdown.template = template;
        dropdown.value = resolutionIndex;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private List<Dropdown.OptionData> BuildDropdownOptions()
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(resolutionOptions.Count);
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            options.Add(new Dropdown.OptionData(ResolutionLabel(resolutionOptions[i])));
        }
        return options;
    }

    private static string ResolutionLabel(ResolutionPreset preset)
    {
        return preset.width + " x " + preset.height;
    }

    private RectTransform CreateDropdownTemplate(RectTransform dropdownRoot, Dropdown dropdown)
    {
        GameObject templateGo = new GameObject("Template", typeof(RectTransform));
        RectTransform templateRt = templateGo.GetComponent<RectTransform>();
        templateRt.SetParent(dropdownRoot, false);
        templateRt.anchorMin = new Vector2(0f, 1f);
        templateRt.anchorMax = new Vector2(1f, 1f);
        templateRt.pivot = new Vector2(0.5f, 1f);
        templateRt.anchoredPosition = Vector2.zero;
        templateRt.sizeDelta = Vector2.zero;
        templateGo.SetActive(false); // dropdown templates must be inactive

        CanvasRenderer templateRenderer = templateGo.AddComponent<CanvasRenderer>();
        Image templateImage = templateGo.AddComponent<Image>();
        templateImage.color = ListBackgroundColor;
        templateGo.AddComponent<RectMask2D>();

        // Content (item list).
        RectTransform viewport = CreateRectChild(templateGo, "Viewport",
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform content = CreateRectChild(viewport.gameObject, "Content",
            new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1f);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 0f;

        // Item template (single exemplar; Dropdown clones it per option).
        RectTransform item = CreateRectChild(content.gameObject, "Item",
            new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 36f));
        item.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
        item.anchorMin = new Vector2(0f, 0f);
        item.anchorMax = new Vector2(1f, 0f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.anchoredPosition = new Vector2(0f, 18f);

        Image itemBackground = CreateImageChild(item.gameObject, "Item Background",
            new Vector2(0f, 0f), new Vector2(1f, 1f), ListItemColor).GetComponent<Image>();
        RectTransform checkTransform = CreateImageChild(item.gameObject, "Item Checkmark",
            new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), RedAccent);
        Image itemCheckmark = checkTransform.GetComponent<Image>();
        Text itemLabel = CreateTextChild(item.gameObject, "Item Label",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            string.Empty, Color.white, 24, TextAnchor.MiddleLeft);

        Toggle itemToggle = item.gameObject.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBackground;
        itemToggle.graphic = itemCheckmark;
        itemToggle.colors = BuildItemToggleColors();
        itemToggle.onValueChanged.AddListener(on =>
        {
            itemBackground.color = on ? SelectedRedColor : ListItemColor;
        });

        // Scrollbar (right edge of the template).
        RectTransform scrollbarRt = CreateRectChild(templateGo, "Scrollbar",
            new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        scrollbarRt.pivot = new Vector2(1f, 0.5f);
        scrollbarRt.offsetMin = new Vector2(-20f, 4f);
        scrollbarRt.offsetMax = new Vector2(-6f, -4f);

        CanvasRenderer trackRenderer = scrollbarRt.gameObject.AddComponent<CanvasRenderer>();
        Image trackImage = scrollbarRt.gameObject.AddComponent<Image>();
        trackImage.color = ControlBackgroundColor;

        RectTransform slidingArea = CreateRectChild(scrollbarRt.gameObject, "Sliding Area",
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform handle = CreateRectChild(slidingArea.gameObject, "Handle",
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        CanvasRenderer handleRenderer = handle.gameObject.AddComponent<CanvasRenderer>();
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = RedAccent;

        Scrollbar scrollbar = scrollbarRt.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        scrollbar.colors = BuildSelectableColors();
        scrollbarHandle = handle;

        ScrollRect scrollRect = templateGo.AddComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 1f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        dropdown.itemText = itemLabel;
        dropdown.itemImage = itemCheckmark;
        dropdown.template = templateRt;
        return templateRt;
    }

    private void BuildLayout()
    {
        RectTransform panelRT = panel.GetComponent<RectTransform>();

        float regionTop = TitleBottomY - RegionInset;
        float regionBottom = BackY + BackHalfHeight - RegionInset;
        float maxViewportHeight = regionTop - regionBottom;

        scrollViewport = CreateRectChild(panel, "Settings Content Viewport",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, viewportCenterY(regionTop, regionBottom)), new Vector2(0f, maxViewportHeight));
        scrollContent = CreateRectChild(scrollViewport.gameObject, "Settings Content",
            new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        scrollContent.pivot = new Vector2(0.5f, 1f);
        scrollContent.anchorMin = new Vector2(0f, 1f);
        scrollContent.anchorMax = new Vector2(1f, 1f);

        Image maskImage = scrollViewport.gameObject.AddComponent<Image>();
        maskImage.color = Color.clear;
        maskImage.raycastTarget = false;
        RectMask2D mask = scrollViewport.gameObject.AddComponent<RectMask2D>();

        settingsScroll = panel.AddComponent<ScrollRect>();
        settingsScroll.viewport = scrollViewport;
        settingsScroll.content = scrollContent;
        settingsScroll.horizontal = false;
        settingsScroll.vertical = true;
        settingsScroll.movementType = ScrollRect.MovementType.Clamped;
        settingsScroll.scrollSensitivity = 25f;

        float y = 0f;
        y += TopPadding;
        CreateHeader(scrollContent, "AUDIO", y + HeaderHeight * 0.5f);
        y += HeaderPitch;

        volumeSliders[AudioBus.Master] = CreateVolumeRow(scrollContent, "Master Volume", AudioBus.Master, y + LabelHeight * 0.5f);
        y += RowPitch;
        volumeSliders[AudioBus.Music] = CreateVolumeRow(scrollContent, "Music Volume", AudioBus.Music, y + LabelHeight * 0.5f);
        y += RowPitch;
        volumeSliders[AudioBus.Sfx] = CreateVolumeRow(scrollContent, "SFX Volume", AudioBus.Sfx, y + LabelHeight * 0.5f);
        y += RowPitch;
        volumeSliders[AudioBus.Ui] = CreateVolumeRow(scrollContent, "UI Volume", AudioBus.Ui, y + LabelHeight * 0.5f);
        y += RowPitch;
        muteToggle = CreateMuteRow(scrollContent, y + LabelHeight * 0.5f);
        y += RowPitch;

        y += TopPadding;
        CreateHeader(scrollContent, "DISPLAY", y + HeaderHeight * 0.5f);
        y += HeaderPitch;

        float rowY = y + LabelHeight * 0.5f;
        ReparentIntoContent(resolutionLabelTitle, scrollContent, LabelX, rowY);
        ReparentIntoContent(resolutionButton.transform as RectTransform, scrollContent, ControlX, rowY + (DropdownHeight - LabelHeight) * 0.5f);
        y += RowPitch + (DropdownHeight - LabelHeight) * 0.5f;

        rowY = y + LabelHeight * 0.5f;
        ReparentIntoContent(fullscreenLabel, scrollContent, LabelX, rowY);
        ReparentIntoContent(fullscreenToggle.transform as RectTransform, scrollContent, ControlX, rowY);
        y += RowPitch;

        float contentHeight = y + 4f;
        scrollContent.sizeDelta = new Vector2(0f, contentHeight);

        // If content is a bit taller than the guaranteed region, let the scroll view grow upward.
        float viewportHeight = Mathf.Max(maxViewportHeight, contentHeight + 6f);
        scrollViewport.sizeDelta = new Vector2(0f, viewportHeight);
        scrollViewport.anchoredPosition = new Vector2(0f, viewportCenterY(regionTop, regionBottom) + (viewportHeight - maxViewportHeight) * 0.5f);

        // Back stays OUTSIDE the scroll region, pinned below it.
        SetY(backButton.transform as RectTransform, BackY);
    }

    private static float viewportCenterY(float regionTop, float regionBottom)
    {
        return (regionTop + regionBottom) * 0.5f;
    }

    private void CreateHeader(RectTransform parent, string text, float y)
    {
        CreateTextChild(parent.gameObject, text + " Header",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(LabelX, -y), new Vector2(HeaderWidth, HeaderHeight),
            text, RedAccent, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
    }

    private Slider CreateVolumeRow(RectTransform parent, string label, AudioBus bus, float y)
    {
        CreateTextChild(parent.gameObject, label,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(LabelX, -y), new Vector2(LabelWidth, LabelHeight),
            label, Color.white, 28, TextAnchor.MiddleLeft);
        return CreateSlider(parent, label + " Slider", new Vector2(ControlX, -y), new Vector2(SliderWidth, SliderHeight));
    }

    private Toggle CreateMuteRow(RectTransform parent, float y)
    {
        CreateTextChild(parent.gameObject, "Mute Audio",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(LabelX, -y), new Vector2(LabelWidth, LabelHeight),
            "Mute Audio", Color.white, 28, TextAnchor.MiddleLeft);
        return CreateToggle(parent, "Mute Toggle", new Vector2(ControlX, -y), new Vector2(ToggleSize, ToggleSize));
    }

    private Slider CreateSlider(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        go.AddComponent<CanvasRenderer>();
        Image background = go.AddComponent<Image>();
        background.color = ControlBackgroundColor;

        RectTransform fillArea = CreateRectChild(go, "Fill Area", new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
        RectTransform fill = CreateImageChild(fillArea.gameObject, "Fill", new Vector2(0f, 0f), new Vector2(0f, 0f), VolumeFillColor);

        RectTransform handleArea = CreateRectChild(go, "Handle Slide Area", Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform handle = CreateImageChild(handleArea.gameObject, "Handle", new Vector2(1f, 0f), new Vector2(1f, 1f), Color.white);
        handle.anchoredPosition = Vector2.zero;
        handle.sizeDelta = new Vector2(20f, 0f);

        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 1f;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle != null ? handle.GetComponent<Image>() : background;
        slider.direction = Slider.Direction.LeftToRight;

        ColorBlock colors = BuildSelectableColors();
        colors.normalColor = Color.white;
        colors.disabledColor = Color.white;
        slider.colors = colors;
        return slider;
    }

    private Toggle CreateToggle(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        go.AddComponent<CanvasRenderer>();
        Image background = go.AddComponent<Image>();
        background.color = Color.white;
        background.sprite = (Sprite)null;
        background.type = Image.Type.Simple;

        RectTransform check = CreateImageChild(go, "Checkmark", Vector2.zero, new Vector2(1f, 1f), RedAccent);
        check.sizeDelta = Vector2.zero;

        Toggle toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = check.GetComponent<Image>();
        toggle.toggleTransition = Toggle.ToggleTransition.Fade;
        toggle.colors = BuildToggleColors();
        toggle.isOn = false;
        return toggle;
    }

    private static ColorBlock BuildSelectableColors()
    {
        ColorBlock colors = new ColorBlock();
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = DisabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        return colors;
    }

    private static ColorBlock BuildToggleColors()
    {
        // The target graphic (background) is tinted through these; the red Checkmark carries the
        // on/off state, and navigation/hover states stay visible without painting everything red.
        ColorBlock colors = new ColorBlock();
        colors.normalColor = ControlBackgroundColor;
        colors.highlightedColor = ControlHighlightColor;
        colors.pressedColor = PressedRedColor;
        colors.selectedColor = SelectedRedColor;
        colors.disabledColor = DisabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        return colors;
    }

    private static ColorBlock BuildItemToggleColors()
    {
        ColorBlock colors = new ColorBlock();
        colors.normalColor = ListItemColor;
        colors.highlightedColor = PressedRedColor;
        colors.pressedColor = PressedRedColor;
        colors.selectedColor = SelectedRedColor;
        colors.disabledColor = DisabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        return colors;
    }

    private static void ReparentIntoContent(RectTransform rt, RectTransform content, float x, float y)
    {
        if (rt == null) return;
        rt.SetParent(content, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    private static RectTransform CreateRectChild(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent.transform, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform CreateRectChild(Component parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        return CreateRectChild(parent.gameObject, name, anchorMin, anchorMax, position, size);
    }

    private static RectTransform CreateImageChild(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        RectTransform rt = CreateRectChild(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        rt.gameObject.AddComponent<CanvasRenderer>();
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    private Text CreateTextChild(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, string text, Color color, int fontSize, TextAnchor alignment, FontStyle style = FontStyle.Normal)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent.transform, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        go.AddComponent<CanvasRenderer>();
        Text label = go.AddComponent<Text>();
        label.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    private static void SetY(RectTransform rt, float y)
    {
        if (rt == null) return;
        Vector2 pos = rt.anchoredPosition;
        pos.y = y;
        rt.anchoredPosition = pos;
    }

    private void AdjustScrollToFit()
    {
        // Content is built to fit the fixed layout region; the scroll view stays inert when the
        // content fits and only engages if a future change makes the content taller than the region.
        if (settingsScroll == null || scrollContent == null) return;
        settingsScroll.verticalNormalizedPosition = 0f;
    }

    private void SeedVolumes()
    {
        volumeSliders[AudioBus.Master].SetValueWithoutNotify(InitialVolume(AudioBus.Master, 1f));
        volumeSliders[AudioBus.Music].SetValueWithoutNotify(InitialVolume(AudioBus.Music, 1f));
        volumeSliders[AudioBus.Sfx].SetValueWithoutNotify(InitialVolume(AudioBus.Sfx, 1f));
        volumeSliders[AudioBus.Ui].SetValueWithoutNotify(InitialVolume(AudioBus.Ui, 1f));

        if (volumeSliders[AudioBus.Master].value <= 0f)
        {
            isMuted = true;
            muteToggle.SetIsOnWithoutNotify(true);
        }
    }

    private float InitialVolume(AudioBus bus, float fallback)
    {
        if (audioServiceCached && audioService != null)
        {
            return audioService.GetVolume(bus);
        }
        if (bus == AudioBus.Master)
        {
            return AudioListener.volume;
        }
        return fallback;
    }

    private IAudioService ResolveAudioService()
    {
        AudioCore core = FindAnyObjectByType<AudioCore>();
        return core != null ? (IAudioService)core : null;
    }

    private string CurrentResolutionLabel()
    {
        if (resolutionOptions.Count == 0) return string.Empty;
        return ResolutionLabel(resolutionOptions[Mathf.Clamp(resolutionIndex, 0, resolutionOptions.Count - 1)]);
    }
}