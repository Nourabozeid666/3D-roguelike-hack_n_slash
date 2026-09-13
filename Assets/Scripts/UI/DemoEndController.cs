using UnityEngine;
using UnityEngine.InputSystem;
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
        // The scene may not serialize a button; build one at runtime so the end screen always has a
        // clickable way back (matches how the rest of the Roguelike UI is runtime-built).
        if (returnToMenuButton == null)
        {
            Button built = PlayerUiKit.Button("ReturnToMenuButton", transform, new Color(0.2f, 0.2f, 0.25f, 1f));
            PlayerUiKit.StyleButton(built);
            PlayerUiKit.Pin(built.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(360, 60));

            Text label = PlayerUiKit.Text("Label", built.transform, 24, TextAnchor.MiddleCenter, Color.white);
            PlayerUiKit.Stretch(label.rectTransform);
            label.text = "RETURN TO MAIN MENU";

            returnToMenuButton = built;
        }
        returnToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    void Update()
    {
        // Allow pressing Escape, Enter, or Space to return to main menu
        Keyboard kb = Keyboard.current;
        if (kb != null
            && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
        {
            ReturnToMenu();
        }
    }

    public void ReturnToMenu()
    {
        SceneTransitioner.RequestScene(mainMenuSceneName, SceneTransitioner.Destination.Menu);
    }
}
