using UnityEngine;

/// <summary>Gameplay-scene-only seam that guarantees exactly one active Pause Menu exists.
/// Reuses an already-present (possibly inactive) PauseController, otherwise instantiates the
/// serialized prefab. Never marked DontDestroyOnLoad: it dies with its scene, so it can never
/// leak into MainMenu. Attach only to gameplay scenes, never to MainMenu.</summary>
public class PauseMenuBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPrefab;

    void Awake()
    {
        PauseController existing = FindFirstObjectByType<PauseController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return;
        }

        if (pauseMenuPrefab == null)
        {
            Debug.LogWarning("PauseMenuBootstrap: no pause menu prefab assigned.", this);
            return;
        }

        Instantiate(pauseMenuPrefab);
    }
}