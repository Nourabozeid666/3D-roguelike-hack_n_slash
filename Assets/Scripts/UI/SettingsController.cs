using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Serializable]
    public class ResolutionPreset
    {
        public int width = 1920;
        public int height = 1080;
    }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button resolutionButton;
    [SerializeField] private Text resolutionLabel;
    [SerializeField] private Button backButton;

    [Header("Options")]
    [SerializeField] private List<ResolutionPreset> resolutionPresets = new List<ResolutionPreset>
    {
        new ResolutionPreset { width = 1920, height = 1080 },
        new ResolutionPreset { width = 1600, height = 900 },
        new ResolutionPreset { width = 1280, height = 720 },
        new ResolutionPreset { width = 1024, height = 768 },
    };

    private int resolutionIndex = 0;

    void Start()
    {
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        resolutionLabel.text = CurrentResolutionLabel();

        masterVolumeSlider.onValueChanged.AddListener(_ => Apply());
        fullscreenToggle.onValueChanged.AddListener(_ => Apply());
        resolutionButton.onClick.AddListener(CycleResolution);
        backButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    public void CycleResolution()
    {
        resolutionIndex = (resolutionIndex + 1) % resolutionPresets.Count;
        resolutionLabel.text = CurrentResolutionLabel();
        Apply();
    }

    private void Apply()
    {
        AudioListener.volume = masterVolumeSlider.value;

        bool fullscreen = fullscreenToggle.isOn;
        Screen.fullScreen = fullscreen;

        ResolutionPreset preset = resolutionPresets[Mathf.Clamp(resolutionIndex, 0, resolutionPresets.Count - 1)];
        Screen.SetResolution(preset.width, preset.height,
            fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    private string CurrentResolutionLabel()
    {
        ResolutionPreset preset = resolutionPresets[Mathf.Clamp(resolutionIndex, 0, resolutionPresets.Count - 1)];
        return preset.width + " x " + preset.height;
    }
}
