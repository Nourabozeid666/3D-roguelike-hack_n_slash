using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu: Continue + New Run + Settings + Quit. Continue is driven by the REAL save file —
/// disabled (label "CONTINUE") when there is no save, enabled with "CONTINUE — FLOOR N" when a valid
/// save exists. New Run deletes the save before entering the game scene so the scene bootstrap starts
/// fresh at floor 1. Opening the menu never writes or overwrites the save. Refresh() is the single
/// data -> UI sync point (no reactive framework): it re-reads the save and updates the button.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newRunButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Temporary target until the build scene list is finalized.")]
    [SerializeField] private string gameSceneName = "TestingScene";

    readonly RunSaveService saves = new();

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        continueButton.onClick.AddListener(Continue);
        newRunButton.onClick.AddListener(StartNewRun);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(Quit);

        Refresh();
    }

    /// <summary>Re-read the save and sync the Continue button (label + interactable).</summary>
    public void Refresh()
    {
        if (continueButton == null) return;

        SaveData save;
        if (saves.TryLoad(out save))
        {
            continueButton.interactable = true;
            continueButton.GetComponentInChildren<Text>().text = $"CONTINUE — FLOOR {save.floor}";
        }
        else
        {
            continueButton.interactable = false;
            continueButton.GetComponentInChildren<Text>().text = "CONTINUE";
        }
    }

    /// <summary>Resume the saved run. Guarded: without a valid save it does nothing and never loads
    /// the scene, so a corrupt/missing save can never start a fabricated run.</summary>
    public void Continue()
    {
        if (!saves.TryLoad(out _)) return;
        EnterGameScene();
    }

    /// <summary>Discard the save and start a fresh run at floor 1.</summary>
    public void StartNewRun()
    {
        saves.Delete();
        EnterGameScene();
    }

    void EnterGameScene()
    {
        RunSession.EnterFromMenu = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
