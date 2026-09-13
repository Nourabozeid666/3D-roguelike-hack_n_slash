using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controller for the DemoEnd scene.
/// Unlocks the cursor, cleans up runtime state, and provides input handling to return to MainMenu.
/// </summary>
public class DemoEndController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Button returnToMenuButton;

    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Ensure run save data is cleared when reaching demo end
        RunSaveService saves = new RunSaveService();
        saves.Delete();
    }

    void Start()
    {
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }
    }

    void Update()
    {
        // Allow pressing Escape, Enter, or Space to return to main menu
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ReturnToMenu();
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
