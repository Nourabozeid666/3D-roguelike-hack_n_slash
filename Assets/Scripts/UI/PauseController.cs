using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [Tooltip("Temporary target until the build scene list is finalized.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    /// <summary>Cached, lazily-resolved progression host used to block pause while the upgrade
    /// selection modal owns the screen (Time.timeScale is already frozen there).</summary>
    private RoguelikeProgressionBootstrap progression;

    void Start()
    {
        resumeButton.onClick.AddListener(Resume);
        settingsButton.onClick.AddListener(OpenSettings);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    void Update()
    {
        if (SceneTransitioner.IsBusy) return;

        // While an upgrade offer is on screen the pause menu must not stack on top of it (both
        // freeze time and own the cursor). Resolve lazily so creation order never matters.
        if (progression == null)
        {
            progression = FindFirstObjectByType<RoguelikeProgressionBootstrap>();
        }
        if (progression != null && progression.IsSelectingUpgrade) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
            return;
        }

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (progression != null && progression.IsSelectingUpgrade) return;

        if (isPaused)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
                return;
            }
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        pausePanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Quit-to-menu relies on the run's checkpoint save (written by RunBootstrap at the START of
    /// every floor, including floor 1), so returning here never silently destroys the run. A mid-floor
    /// quit resumes the current floor from its start — enemy state is not persisted yet, so the floor
    /// is repopulated fresh. No extra save is needed at this point: the current floor's checkpoint is
    /// already on disk. The transitioner unfreezes time and frees the cursor for the menu destination.
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (SceneTransitioner.IsBusy) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        SceneTransitioner.RequestScene(mainMenuSceneName, SceneTransitioner.Destination.Menu);
    }
}
