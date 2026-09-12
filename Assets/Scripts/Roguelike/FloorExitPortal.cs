using System;
using UnityEngine;

/// <summary>
/// State-aware Exit Portal placed in the arena.
/// Activates when the round or region is cleared, emitting a color-coded beacon (Cyan, Gold, Crimson)
/// and detecting when the player steps into the gateway to advance.
/// </summary>
public class FloorExitPortal : MonoBehaviour
{
    public enum PortalVisualState
    {
        Dormant,        // Combat active: hidden / non-interactive
        RoundPortal,    // In-region round advance (Cyan beacon)
        RegionGateway,  // Region boundary advance (Gold beacon)
        VictoryGateway  // Final game completion (Crimson beacon)
    }

    [Header("Visual Elements")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Light portalLight;
    [SerializeField] private ParticleSystem portalParticles;
    [SerializeField] private Renderer beaconRenderer;

    [Header("Color Palettes")]
    [SerializeField] private Color roundColor = new Color(0.15f, 0.8f, 1f, 1f);      // Cyan
    [SerializeField] private Color regionColor = new Color(1f, 0.75f, 0.1f, 1f);     // Gold
    [SerializeField] private Color victoryColor = new Color(0.9f, 0.1f, 0.3f, 1f);    // Crimson

    [Header("Trigger Detection")]
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private float triggerRadius = 2.5f;

    public event Action PortalEntered;

    private PortalVisualState currentState = PortalVisualState.Dormant;
    private bool hasTriggeredThisActivation = false;
    private string currentPrompt = string.Empty;

    public PortalVisualState CurrentState => currentState;
    public bool IsActive => currentState != PortalVisualState.Dormant;
    public string CurrentPrompt => currentPrompt;

    void Awake()
    {
        EnsureComponents();
        Deactivate();
    }

    void EnsureComponents()
    {
        if (visualRoot == null)
        {
            // If no separate visual root assigned, use children or this GameObject
            visualRoot = transform.Find("Visuals")?.gameObject ?? gameObject;
        }

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider == null)
            {
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = triggerRadius;
                triggerCollider = sc;
            }
            else
            {
                triggerCollider.isTrigger = true;
            }
        }

        if (portalLight == null)
        {
            portalLight = GetComponentInChildren<Light>();
            if (portalLight == null && visualRoot != null)
            {
                GameObject lightGo = new GameObject("PortalLight");
                lightGo.transform.SetParent(visualRoot.transform, false);
                lightGo.transform.localPosition = new Vector3(0, 1.5f, 0);
                portalLight = lightGo.AddComponent<Light>();
                portalLight.type = LightType.Point;
                portalLight.range = 8f;
                portalLight.intensity = 3f;
            }
        }
    }

    /// <summary>
    /// Activates the portal with the specified state and prompt.
    /// </summary>
    public void Activate(PortalVisualState state, string prompt = "")
    {
        currentState = state;
        currentPrompt = prompt;
        hasTriggeredThisActivation = false;

        if (triggerCollider != null) triggerCollider.enabled = true;
        if (visualRoot != null) visualRoot.SetActive(true);

        Color targetColor = roundColor;
        switch (state)
        {
            case PortalVisualState.RoundPortal:
                targetColor = roundColor;
                break;
            case PortalVisualState.RegionGateway:
                targetColor = regionColor;
                break;
            case PortalVisualState.VictoryGateway:
                targetColor = victoryColor;
                break;
            case PortalVisualState.Dormant:
                Deactivate();
                return;
        }

        ApplyColor(targetColor);

        if (portalParticles != null)
        {
            var main = portalParticles.main;
            main.startColor = targetColor;
            portalParticles.Play();
        }

        Debug.Log($"[FloorExitPortal] Activated ({state}): {prompt}");
    }

    public void Deactivate()
    {
        currentState = PortalVisualState.Dormant;
        hasTriggeredThisActivation = false;
        currentPrompt = string.Empty;

        if (triggerCollider != null) triggerCollider.enabled = false;
        if (visualRoot != null && visualRoot != gameObject) visualRoot.SetActive(false);
        if (portalParticles != null) portalParticles.Stop();
        if (portalLight != null) portalLight.enabled = false;
    }

    void ApplyColor(Color color)
    {
        if (portalLight != null)
        {
            portalLight.enabled = true;
            portalLight.color = color;
        }

        if (beaconRenderer != null && beaconRenderer.material != null)
        {
            beaconRenderer.material.color = color;
            if (beaconRenderer.material.HasProperty("_EmissionColor"))
            {
                beaconRenderer.material.EnableKeyword("_EMISSION");
                beaconRenderer.material.SetColor("_EmissionColor", color * 2f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsActive || hasTriggeredThisActivation) return;

        // Check if player entered the portal
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
        {
            hasTriggeredThisActivation = true;
            Debug.Log($"[FloorExitPortal] Player entered portal in state: {currentState}");
            PortalEntered?.Invoke();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsActive ? roundColor : Color.gray;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
