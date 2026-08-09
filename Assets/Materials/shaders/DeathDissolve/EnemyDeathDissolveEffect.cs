using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
[DefaultExecutionOrder(100)]
public sealed class EnemyDeathDissolveEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Material deathMaterial;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float deathDelay = 0.45f;
    [SerializeField, Min(0.01f)] private float dissolveDuration = 2.15f;

    [Header("Sweep")]
    [SerializeField] private Vector3 worldDirection = Vector3.up;
    [SerializeField, Min(0f)] private float boundsPadding = 0.15f;
    [SerializeField] private string[] excludedMaterialKeywords = { "EyeCornea" };
    [SerializeField] private bool disableRenderersWhenFinished = true;

    [Header("Green Death Particles")]
    [SerializeField] private bool useDeathParticles = true;
    [SerializeField, Min(1f)] private float particlesPerSecond = 240f;
    [SerializeField, Min(0.01f)] private float particleBand = 0.35f;
    [SerializeField, Min(0.01f)] private float particleLifetime = 0.75f;
    [SerializeField, Min(0.001f), Tooltip("Size of each green particle in world units.")]
    private float particleSize = 0.45f;
    [SerializeField, Min(0f)] private float particleSpeed = 0.45f;
    [SerializeField, ColorUsage(true, true)]
    private Color particleColor = new Color(0.05f, 1f, 0.12f, 1f);
    [SerializeField, Min(0f)] private float particleGlow = 4f;

    private static readonly BindingFlags PrivateInstance =
        BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo DieStateAgentField =
        typeof(DieState).GetField("agent", PrivateInstance);
    private static readonly FieldInfo DieStateAnimatorField =
        typeof(DieState).GetField("animator", PrivateInstance);

    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColor = Shader.PropertyToID("_Color");
    private static readonly int NormalMap = Shader.PropertyToID("_NormalMap");
    private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
    private static readonly int NormalScale = Shader.PropertyToID("_NormalScale");
    private static readonly int BumpScale = Shader.PropertyToID("_BumpScale");
    private static readonly int PackedMap =
        Shader.PropertyToID("_R_Metallic_G_Occulsion_A_Smoothness");
    private static readonly int Metallic = Shader.PropertyToID("_Metallic");
    private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
    private static readonly int OcclusionStrength = Shader.PropertyToID("_OcclusionStrength");

    private static readonly int DissolveAmount = Shader.PropertyToID("_DissolveAmount");
    private static readonly int DissolveDirection = Shader.PropertyToID("_DissolveDirection");
    private static readonly int DissolveOrigin = Shader.PropertyToID("_DissolveOrigin");
    private static readonly int DissolveRange = Shader.PropertyToID("_DissolveRange");
    private static readonly int UseWorldSpace = Shader.PropertyToID("_UseWorldSpace");

    // These are the properties used by the already-working spawn dissolve.
    // Reversing that material avoids a visible shader/material handoff at death.
    private static readonly int ExistingDissolveOffset = Shader.PropertyToID("_DissolveOffest");
    private static readonly int ExistingEdgeColor = Shader.PropertyToID("_EdgeColor");
    private static readonly int ExistingEdgeWidth = Shader.PropertyToID("_EdgeWidth");
    private static readonly int ExistingEdgeIntensity = Shader.PropertyToID("_EdgeColorIntensity");
    private static readonly int ExistingNoiseScale = Shader.PropertyToID("_NoiseScale");
    private static readonly int ExistingNoiseSpeed = Shader.PropertyToID("_NoiseUVSpeed");
    private static readonly int DeathEdgeIntensity = Shader.PropertyToID("_EdgeIntensity");
    private static readonly int DeathNoiseSpeed = Shader.PropertyToID("_NoiseSpeed");

    private readonly List<Material> ownedRuntimeMaterials = new List<Material>();
    private readonly List<Material> customDissolveMaterials = new List<Material>();
    private readonly List<ExistingDissolveTarget> existingDissolveTargets =
        new List<ExistingDissolveTarget>();
    private readonly List<Renderer> affectedRenderers = new List<Renderer>();
    private readonly HashSet<Material> registeredMaterials = new HashSet<Material>();
    private readonly List<ParticleMeshSource> particleMeshSources =
        new List<ParticleMeshSource>();
    private readonly List<ParticleMeshSource> activeParticleMeshSources =
        new List<ParticleMeshSource>();

    private bool deathStateReferencesBound;
    private bool deathStarted;
    private ParticleSystem deathParticleSystem;
    private Material deathParticleMaterial;
    private Texture2D deathParticleTexture;
    private float particleTimer;
    private float particleMinimumLocalY = -1f;
    private float particleMaximumLocalY = 1f;

    private void Reset()
    {
        enemyController = GetComponent<EnemyController>();
    }

    private void Awake()
    {
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        if (enemyController == null)
        {
            Debug.LogError(
                $"{nameof(EnemyDeathDissolveEffect)} on '{name}' needs an EnemyController.",
                this);
            enabled = false;
        }
    }

    private void Start()
    {
        // EnemyController creates DieState in its Start method. This component
        // runs later, so it can safely supply the references that DieState needs
        // without changing the controller or state source files.
        deathStateReferencesBound = TryBindDeathStateReferences();
    }

    private void Update()
    {
        if (!deathStateReferencesBound)
        {
            deathStateReferencesBound = TryBindDeathStateReferences();
        }

        if (deathStarted || enemyController.EnemyEntity == null)
        {
            return;
        }

        if (enemyController.EnemyEntity.CurrentHealth <= 0f)
        {
            PlayDeathEffect();
        }
    }

    public void PlayDeathEffect()
    {
        if (deathStarted || !enabled)
        {
            return;
        }

        deathStarted = true;
        StartCoroutine(AnimateDeathDissolve());
    }

    private bool TryBindDeathStateReferences()
    {
        if (enemyController == null ||
            enemyController.Agent == null ||
            enemyController.Animator == null ||
            DieStateAgentField == null ||
            DieStateAnimatorField == null)
        {
            return false;
        }

        Dictionary<Type, EnemyState> states = enemyController.EnemyStates;
        if (states == null ||
            !states.TryGetValue(typeof(DieState), out EnemyState state) ||
            !(state is DieState dieState))
        {
            return false;
        }

        DieStateAgentField.SetValue(dieState, enemyController.Agent);
        DieStateAnimatorField.SetValue(dieState, enemyController.Animator);
        return true;
    }

    private IEnumerator AnimateDeathDissolve()
    {
        if (deathDelay > 0f)
        {
            yield return new WaitForSeconds(deathDelay);
        }

        PrepareDeathMaterials();
        StartDeathParticles();

        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / dissolveDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetDissolveProgress(easedProgress);
            EmitDeathParticles(easedProgress, Time.deltaTime);
            yield return null;
        }

        SetDissolveProgress(1f);
        StopDeathParticles();

        if (disableRenderersWhenFinished)
        {
            foreach (Renderer targetRenderer in affectedRenderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = false;
                }
            }
        }

        // DieState remains responsible for destroying the enemy. Keeping that
        // ownership in one place prevents duplicate animation and destroy calls.
    }

    private void PrepareDeathMaterials()
    {
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> meshRenderers = new List<Renderer>();

        foreach (Renderer childRenderer in childRenderers)
        {
            if ((childRenderer is MeshRenderer || childRenderer is SkinnedMeshRenderer) &&
                childRenderer.gameObject.activeInHierarchy &&
                childRenderer.enabled)
            {
                meshRenderers.Add(childRenderer);
            }
        }

        GetWorldSweepRange(meshRenderers, out Vector3 direction, out Vector3 origin, out Vector2 range);
        GetLocalVerticalRange(
            meshRenderers,
            out particleMinimumLocalY,
            out particleMaximumLocalY);

        foreach (Renderer targetRenderer in meshRenderers)
        {
            Material[] sourceMaterials = targetRenderer.sharedMaterials;
            Material[] deathMaterials = null;
            bool rendererUsesDissolve = false;

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material sourceMaterial = sourceMaterials[i];
                if (sourceMaterial == null || ShouldExclude(sourceMaterial))
                {
                    continue;
                }

                if (sourceMaterial.HasProperty(ExistingDissolveOffset))
                {
                    RegisterExistingDissolve(
                        sourceMaterial,
                        particleMinimumLocalY - boundsPadding);
                    rendererUsesDissolve = true;
                    continue;
                }

                if (deathMaterial == null)
                {
                    continue;
                }

                if (deathMaterials == null)
                {
                    deathMaterials = (Material[])sourceMaterials.Clone();
                }

                Material runtimeMaterial = new Material(deathMaterial)
                {
                    name = $"{sourceMaterial.name} (Runtime Death Dissolve)"
                };

                CopySurface(sourceMaterial, runtimeMaterial);
                runtimeMaterial.SetFloat(DissolveAmount, 0f);
                runtimeMaterial.SetFloat(UseWorldSpace, 1f);
                runtimeMaterial.SetVector(DissolveDirection, direction);
                runtimeMaterial.SetVector(DissolveOrigin, origin);
                runtimeMaterial.SetVector(
                    DissolveRange,
                    new Vector4(range.x, range.y, 0f, 0f));

                deathMaterials[i] = runtimeMaterial;
                ownedRuntimeMaterials.Add(runtimeMaterial);
                customDissolveMaterials.Add(runtimeMaterial);
                rendererUsesDissolve = true;
            }

            if (deathMaterials != null)
            {
                targetRenderer.sharedMaterials = deathMaterials;
            }

            if (rendererUsesDissolve)
            {
                affectedRenderers.Add(targetRenderer);

                if (useDeathParticles)
                {
                    particleMeshSources.Add(new ParticleMeshSource(targetRenderer));
                }
            }
        }

        if (useDeathParticles && particleMeshSources.Count > 0)
        {
            CreateDeathParticleSystem();
        }
    }

    private void RegisterExistingDissolve(Material material, float hiddenOffset)
    {
        if (!registeredMaterials.Add(material))
        {
            return;
        }

        CopyDeathEdgeSettings(material);

        Vector4 visibleOffset = material.GetVector(ExistingDissolveOffset);
        Vector4 invisibleOffset = visibleOffset;
        invisibleOffset.y = hiddenOffset;
        existingDissolveTargets.Add(
            new ExistingDissolveTarget(material, visibleOffset, invisibleOffset));
    }

    private void CopyDeathEdgeSettings(Material material)
    {
        if (deathMaterial == null)
        {
            return;
        }

        CopyColor(deathMaterial, material, ExistingEdgeColor, ExistingEdgeColor);
        CopyFloat(deathMaterial, material, ExistingEdgeWidth, ExistingEdgeWidth);
        CopyFloat(deathMaterial, material, ExistingEdgeIntensity, DeathEdgeIntensity);
        CopyFloat(deathMaterial, material, ExistingNoiseScale, ExistingNoiseScale);

        if (material.HasProperty(ExistingNoiseSpeed) &&
            deathMaterial.HasProperty(DeathNoiseSpeed))
        {
            material.SetVector(ExistingNoiseSpeed, deathMaterial.GetVector(DeathNoiseSpeed));
        }
    }

    private void GetWorldSweepRange(
        IReadOnlyList<Renderer> renderers,
        out Vector3 direction,
        out Vector3 origin,
        out Vector2 range)
    {
        Vector3 sweepDirection = worldDirection.sqrMagnitude > 0.0001f
            ? worldDirection.normalized
            : Vector3.up;
        Vector3 sweepOrigin = transform.position;

        float minimum = float.PositiveInfinity;
        float maximum = float.NegativeInfinity;

        foreach (Renderer targetRenderer in renderers)
        {
            VisitBoundsCorners(targetRenderer.bounds, corner =>
            {
                float projectedDistance = Vector3.Dot(corner - sweepOrigin, sweepDirection);
                minimum = Mathf.Min(minimum, projectedDistance);
                maximum = Mathf.Max(maximum, projectedDistance);
            });
        }

        if (float.IsInfinity(minimum) || float.IsInfinity(maximum))
        {
            minimum = -1f;
            maximum = 1f;
        }

        direction = sweepDirection;
        origin = sweepOrigin;
        range = new Vector2(minimum - boundsPadding, maximum + boundsPadding);
    }

    private void GetLocalVerticalRange(
        IReadOnlyList<Renderer> renderers,
        out float minimum,
        out float maximum)
    {
        float localMinimum = float.PositiveInfinity;
        float localMaximum = float.NegativeInfinity;

        foreach (Renderer targetRenderer in renderers)
        {
            VisitBoundsCorners(targetRenderer.bounds, corner =>
            {
                float localY = transform.InverseTransformPoint(corner).y;
                localMinimum = Mathf.Min(localMinimum, localY);
                localMaximum = Mathf.Max(localMaximum, localY);
            });
        }

        if (float.IsInfinity(localMinimum) || float.IsInfinity(localMaximum))
        {
            localMinimum = -1f;
            localMaximum = 1f;
        }

        minimum = localMinimum;
        maximum = localMaximum;
    }

    private static void VisitBoundsCorners(Bounds bounds, Action<Vector3> visitor)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    visitor(center + Vector3.Scale(extents, new Vector3(x, y, z)));
                }
            }
        }
    }

    private void CreateDeathParticleSystem()
    {
        GameObject particleObject = new GameObject("Green Death Dissolve Particles");
        particleObject.layer = gameObject.layer;
        particleObject.transform.SetParent(transform, false);

        deathParticleSystem = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = deathParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 3000;
        main.startLifetime = particleLifetime;
        main.startSize = particleSize;
        main.startSpeed = 0f;
        main.startColor = Color.white;

        ParticleSystem.EmissionModule emission = deathParticleSystem.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = deathParticleSystem.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            deathParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient particleGradient = new Gradient();
        particleGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = particleGradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
            deathParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.65f, 0.8f),
                new Keyframe(1f, 0f)));

        ParticleSystem.NoiseModule noise = deathParticleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 0.9f;
        noise.scrollSpeed = 0.5f;

        ParticleSystemRenderer particleRenderer =
            particleObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = CreateDeathParticleMaterial();
        particleRenderer.sortMode = ParticleSystemSortMode.Distance;
        particleRenderer.sortingFudge = 2f;
    }

    private Material CreateDeathParticleMaterial()
    {
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (particleShader == null)
        {
            Debug.LogWarning(
                "A URP particle shader could not be found for the green death particles.",
                this);
            return null;
        }

        deathParticleMaterial = new Material(particleShader)
        {
            name = "Green Death Dissolve Particles (Runtime)",
            renderQueue = (int)RenderQueue.Transparent
        };

        deathParticleTexture = CreateSoftParticleTexture();
        SetTextureIfPresent(deathParticleMaterial, BaseMap, deathParticleTexture);
        SetTextureIfPresent(deathParticleMaterial, MainTex, deathParticleTexture);

        Color glowColor = particleColor;
        glowColor.r *= particleGlow;
        glowColor.g *= particleGlow;
        glowColor.b *= particleGlow;
        SetColorIfPresent(deathParticleMaterial, BaseColor, glowColor);
        SetColorIfPresent(deathParticleMaterial, LegacyColor, glowColor);

        SetFloatIfPresent(deathParticleMaterial, Shader.PropertyToID("_Surface"), 1f);
        SetFloatIfPresent(deathParticleMaterial, Shader.PropertyToID("_Blend"), 2f);
        SetFloatIfPresent(
            deathParticleMaterial,
            Shader.PropertyToID("_SrcBlend"),
            (float)BlendMode.SrcAlpha);
        SetFloatIfPresent(
            deathParticleMaterial,
            Shader.PropertyToID("_DstBlend"),
            (float)BlendMode.One);
        SetFloatIfPresent(deathParticleMaterial, Shader.PropertyToID("_ZWrite"), 0f);
        deathParticleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return deathParticleMaterial;
    }

    private static Texture2D CreateSoftParticleTexture()
    {
        const int resolution = 32;
        Texture2D texture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false)
        {
            name = "Green Death Particle Glow (Runtime)",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[resolution * resolution];
        Vector2 center = new Vector2((resolution - 1) * 0.5f, (resolution - 1) * 0.5f);
        float radius = resolution * 0.5f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.6f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void StartDeathParticles()
    {
        if (deathParticleSystem == null)
        {
            return;
        }

        particleTimer = 0f;
        deathParticleSystem.Clear(true);
        deathParticleSystem.Play(true);
    }

    private void EmitDeathParticles(float progress, float deltaTime)
    {
        if (deathParticleSystem == null || particleMeshSources.Count == 0)
        {
            return;
        }

        particleTimer += deltaTime;
        const float updateInterval = 0.05f;
        if (particleTimer < updateInterval)
        {
            return;
        }

        int targetCount = Mathf.Max(1, Mathf.RoundToInt(particlesPerSecond * particleTimer));
        particleTimer = 0f;

        activeParticleMeshSources.Clear();
        foreach (ParticleMeshSource source in particleMeshSources)
        {
            if (source.RefreshVertices())
            {
                activeParticleMeshSources.Add(source);
            }
        }

        if (activeParticleMeshSources.Count == 0)
        {
            return;
        }

        float edgeOffset = GetParticleEdgeOffset(progress);
        int emittedCount = 0;
        int maximumAttempts = targetCount * 250;

        for (int attempt = 0;
             attempt < maximumAttempts && emittedCount < targetCount;
             attempt++)
        {
            ParticleMeshSource source = activeParticleMeshSources[
                UnityEngine.Random.Range(0, activeParticleMeshSources.Count)];
            Vector3 localVertex = source.Vertices[
                UnityEngine.Random.Range(0, source.Vertices.Count)];
            Vector3 worldPosition = source.Renderer.transform.TransformPoint(localVertex);
            float rootLocalY = transform.InverseTransformPoint(worldPosition).y;

            if (Mathf.Abs(rootLocalY - edgeOffset) > particleBand)
            {
                continue;
            }

            Vector3 velocityDirection =
                UnityEngine.Random.insideUnitSphere * 0.65f + Vector3.up * 0.8f;
            if (velocityDirection.sqrMagnitude < 0.0001f)
            {
                velocityDirection = Vector3.up;
            }

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = worldPosition,
                velocity = velocityDirection.normalized *
                    particleSpeed * UnityEngine.Random.Range(0.65f, 1.35f),
                startLifetime = particleLifetime * UnityEngine.Random.Range(0.8f, 1.25f),
                startSize = particleSize * UnityEngine.Random.Range(0.75f, 1.35f),
                startColor = Color.white
            };

            deathParticleSystem.Emit(emitParams, 1);
            emittedCount++;
        }
    }

    private float GetParticleEdgeOffset(float progress)
    {
        if (existingDissolveTargets.Count > 0)
        {
            ExistingDissolveTarget target = existingDissolveTargets[0];
            return Mathf.Lerp(
                target.VisibleOffset.y,
                target.InvisibleOffset.y,
                progress);
        }

        return Mathf.Lerp(
            particleMinimumLocalY - boundsPadding,
            particleMaximumLocalY + boundsPadding,
            progress);
    }

    private void StopDeathParticles()
    {
        if (deathParticleSystem != null)
        {
            deathParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private bool ShouldExclude(Material material)
    {
        if (excludedMaterialKeywords == null)
        {
            return false;
        }

        foreach (string keyword in excludedMaterialKeywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) &&
                material.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetTextureIfPresent(Material material, int property, Texture texture)
    {
        if (material.HasProperty(property))
        {
            material.SetTexture(property, texture);
        }
    }

    private static void SetColorIfPresent(Material material, int property, Color value)
    {
        if (material.HasProperty(property))
        {
            material.SetColor(property, value);
        }
    }

    private static void SetFloatIfPresent(Material material, int property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }

    private static void CopySurface(Material source, Material destination)
    {
        CopyTexture(source, destination, BaseMap, BaseMap, MainTex);
        CopyColor(source, destination, BaseColor, BaseColor, LegacyColor);
        CopyTexture(source, destination, NormalMap, NormalMap, BumpMap);
        CopyFloat(source, destination, NormalScale, NormalScale, BumpScale);
        CopyTexture(source, destination, PackedMap, PackedMap);
        CopyFloat(source, destination, Metallic, Metallic);
        CopyFloat(source, destination, Smoothness, Smoothness);
        CopyFloat(source, destination, OcclusionStrength, OcclusionStrength);
    }

    private static void CopyTexture(
        Material source,
        Material destination,
        int destinationProperty,
        params int[] sourceProperties)
    {
        if (!destination.HasProperty(destinationProperty))
        {
            return;
        }

        foreach (int sourceProperty in sourceProperties)
        {
            if (!source.HasProperty(sourceProperty))
            {
                continue;
            }

            Texture texture = source.GetTexture(sourceProperty);
            if (texture == null)
            {
                continue;
            }

            destination.SetTexture(destinationProperty, texture);
            destination.SetTextureScale(destinationProperty, source.GetTextureScale(sourceProperty));
            destination.SetTextureOffset(destinationProperty, source.GetTextureOffset(sourceProperty));
            return;
        }
    }

    private static void CopyColor(
        Material source,
        Material destination,
        int destinationProperty,
        params int[] sourceProperties)
    {
        if (!destination.HasProperty(destinationProperty))
        {
            return;
        }

        foreach (int sourceProperty in sourceProperties)
        {
            if (source.HasProperty(sourceProperty))
            {
                destination.SetColor(destinationProperty, source.GetColor(sourceProperty));
                return;
            }
        }
    }

    private static void CopyFloat(
        Material source,
        Material destination,
        int destinationProperty,
        params int[] sourceProperties)
    {
        if (!destination.HasProperty(destinationProperty))
        {
            return;
        }

        foreach (int sourceProperty in sourceProperties)
        {
            if (source.HasProperty(sourceProperty))
            {
                destination.SetFloat(destinationProperty, source.GetFloat(sourceProperty));
                return;
            }
        }
    }

    private void SetDissolveProgress(float progress)
    {
        foreach (ExistingDissolveTarget target in existingDissolveTargets)
        {
            if (target.Material != null)
            {
                target.Material.SetVector(
                    ExistingDissolveOffset,
                    Vector4.Lerp(target.VisibleOffset, target.InvisibleOffset, progress));
            }
        }

        foreach (Material runtimeMaterial in customDissolveMaterials)
        {
            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat(DissolveAmount, progress);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (ParticleMeshSource source in particleMeshSources)
        {
            source.Dispose();
        }

        foreach (Material runtimeMaterial in ownedRuntimeMaterials)
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        if (deathParticleMaterial != null)
        {
            Destroy(deathParticleMaterial);
        }

        if (deathParticleTexture != null)
        {
            Destroy(deathParticleTexture);
        }
    }

    private sealed class ParticleMeshSource
    {
        private readonly SkinnedMeshRenderer skinnedRenderer;
        private readonly MeshFilter meshFilter;
        private readonly Mesh bakedMesh;

        public ParticleMeshSource(Renderer renderer)
        {
            Renderer = renderer;
            skinnedRenderer = renderer as SkinnedMeshRenderer;
            meshFilter = renderer.GetComponent<MeshFilter>();

            if (skinnedRenderer != null)
            {
                bakedMesh = new Mesh
                {
                    name = $"{renderer.name} Death Particle Sampling Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }

        public Renderer Renderer { get; }
        public List<Vector3> Vertices { get; } = new List<Vector3>();

        public bool RefreshVertices()
        {
            Vertices.Clear();

            if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
            {
                skinnedRenderer.BakeMesh(bakedMesh);
                bakedMesh.GetVertices(Vertices);
                return Vertices.Count > 0;
            }

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshFilter.sharedMesh.GetVertices(Vertices);
                return Vertices.Count > 0;
            }

            return false;
        }

        public void Dispose()
        {
            if (bakedMesh != null)
            {
                UnityEngine.Object.Destroy(bakedMesh);
            }
        }
    }

    private sealed class ExistingDissolveTarget
    {
        public ExistingDissolveTarget(
            Material material,
            Vector4 visibleOffset,
            Vector4 invisibleOffset)
        {
            Material = material;
            VisibleOffset = visibleOffset;
            InvisibleOffset = invisibleOffset;
        }

        public Material Material { get; }
        public Vector4 VisibleOffset { get; }
        public Vector4 InvisibleOffset { get; }
    }
}
