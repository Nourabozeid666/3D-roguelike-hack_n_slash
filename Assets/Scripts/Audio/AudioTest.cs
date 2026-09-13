using UnityEngine;
using UnityEngine.InputSystem;

public class AudioTest : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            audioSource.Play();
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            audioSource.Stop();
        }
    }
}