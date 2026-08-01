using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class EnemyDissolve : MonoBehaviour
{
    [Header("Dissolve")]
    [SerializeField] private Material dissolveMaterial;
    [SerializeField, Min(0f)] private float spawnDuration = 2f;
    [SerializeField, Min(0f)] private float boundsPadding = 0.3f;
    [SerializeField] private string[] excludedMaterialKeywords = { "EyeCornea" };

    [Header("Edge")]
    [SerializeField, ColorUsage(true, true)] private Color edgeColor = new Color(8f, 0.125f, 0f, 1f);
    [SerializeField, Min(0f)] private float edgeWidth = 0.183f;
    [SerializeField, Min(0f)] private float edgeIntensity = 2.18f;
    [SerializeField, Min(0.01f)] private float noiseScale = 171.18f;
    [SerializeField] private Vector2 noiseSpeed = new Vector2(1f, 1.27f);

    [Header("Edge Particles")]
    [SerializeField] private bool useEdgeParticles = true;
    [SerializeField, Min(1f)] private float particlesPerSecond = 140f;
    [SerializeField, Min(0.01f)] private float particleBand = 0.22f;
    [SerializeField, Min(0.01f)] private float particleLifetime = 0.45f;
    [SerializeField, Min(0.001f)] private float particleSize = 0.09f;
    [SerializeField, Min(0f)] private float particleSpeed = 0.18f;

    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int NormalMap = Shader.PropertyToID("_NormalMap");
    private static readonly int NormalScale = Shader.PropertyToID("_NormalScale");
    private static readonly int PackedMap = Shader.PropertyToID("_R_Metallic_G_Occulsion_A_Smoothness");
    private static readonly int Metallic = Shader.PropertyToID("_Metallic");
    private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
    private static readonly int OcclusionStrength = Shader.PropertyToID("_OcclusionStrength");
    private static readonly int DissolveOffset = Shader.PropertyToID("_DissolveOffest");
    private static readonly int DissolveDirection = Shader.PropertyToID("_DissolveDirection");
    private static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
    private static readonly int EdgeWidth = Shader.PropertyToID("_EdgeWidth");
    private static readonly int EdgeIntensity = Shader.PropertyToID("_EdgeColorIntensity");
    private static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeed = Shader.PropertyToID("_NoiseUVSpeed");

    private readonly List<Material> runtimeMaterials = new List<Material>();
    private readonly List<RendererSetup> rendererSetups = new List<RendererSetup>();
    private readonly List<ParticleMeshSource> particleMeshSources = new List<ParticleMeshSource>();
    private Coroutine spawnRoutine;
    private ParticleSystem edgeParticleSystem;
    private Material edgeParticleMaterial;
    private Texture2D edgeParticleTexture;
    private float particleTimer;
    private float hiddenOffset;
    private float visibleOffset;

    private void Awake()
    {
        if (dissolveMaterial == null || !dissolveMaterial.HasProperty(DissolveOffset))
        {
            Debug.LogError($"{nameof(EnemyDissolve)} on '{name}' is missing a compatible dissolve material.", this);
            enabled = false;
            return;
        }

        foreach (Renderer targetRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!HasMesh(targetRenderer))
            {
                continue;
            }

            Material[] originals = targetRenderer.sharedMaterials;
            Material[] dissolveCopies = new Material[originals.Length];
            rendererSetups.Add(new RendererSetup(targetRenderer, originals));
            bool rendererUsesDissolve = false;

            for (int i = 0; i < originals.Length; i++)
            {
                if (ShouldExclude(originals[i]))
                {
                    dissolveCopies[i] = originals[i];
                    continue;
                }

                Material dissolveCopy = new Material(dissolveMaterial)
                {
                    name = originals[i] != null
                        ? $"{originals[i].name} (Runtime Dissolve)"
                        : "Runtime Dissolve"
                };

                CopySurface(originals[i], dissolveCopy);
                ConfigureDissolve(dissolveCopy);

                dissolveCopies[i] = dissolveCopy;
                runtimeMaterials.Add(dissolveCopy);
                rendererUsesDissolve = true;
            }

            targetRenderer.sharedMaterials = dissolveCopies;

            if (rendererUsesDissolve)
            {
                particleMeshSources.Add(new ParticleMeshSource(targetRenderer));
            }
        }

        if (!TryComputeVerticalRange(out hiddenOffset, out visibleOffset))
        {
            Debug.LogError($"{nameof(EnemyDissolve)} on '{name}' could not calculate a mesh range.", this);
            enabled = false;
            return;
        }

        hiddenOffset -= boundsPadding;
        visibleOffset += boundsPadding;

        if (useEdgeParticles)
        {
            CreateEdgeParticleSystem();
        }

        SetOffset(hiddenOffset);
    }

    private void Start()
    {
        PlaySpawnEffect();
    }

    public void PlaySpawnEffect()
    {
        if (!enabled)
        {
            return;
        }

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        particleTimer = 0f;
        if (edgeParticleSystem != null)
        {
            edgeParticleSystem.Clear(true);
            edgeParticleSystem.Play(true);
        }

        spawnRoutine = StartCoroutine(AnimateSpawn());
    }

    private IEnumerator AnimateSpawn()
    {
        SetOffset(hiddenOffset);

        if (spawnDuration <= 0f)
        {
            SetOffset(visibleOffset);
            spawnRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / spawnDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float currentOffset = Mathf.Lerp(hiddenOffset, visibleOffset, easedProgress);
            SetOffset(currentOffset);
            EmitEdgeParticles(currentOffset, Time.deltaTime);
            yield return null;
        }

        SetOffset(visibleOffset);
        spawnRoutine = null;
    }

    private void CreateEdgeParticleSystem()
    {
        GameObject particleObject = new GameObject("Dissolve Edge Particles");
        particleObject.transform.SetParent(transform, false);

        edgeParticleSystem = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = edgeParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1500;
        main.startLifetime = particleLifetime;
        main.startSize = particleSize;
        main.startSpeed = 0f;
        main.startColor = Color.white;

        ParticleSystem.EmissionModule emission = edgeParticleSystem.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = edgeParticleSystem.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = edgeParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient particleGradient = new Gradient();
        particleGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.65f, 0.05f), 0f),
                new GradientColorKey(new Color(1f, 0.05f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = particleGradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = edgeParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        ParticleSystem.NoiseModule noise = edgeParticleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.4f;

        ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = CreateEdgeParticleMaterial();
        particleRenderer.sortMode = ParticleSystemSortMode.Distance;
    }

    private Material CreateEdgeParticleMaterial()
    {
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (particleShader == null)
        {
            Debug.LogError("A URP particle shader could not be found for the dissolve edge particles.", this);
            return null;
        }

        edgeParticleMaterial = new Material(particleShader)
        {
            name = "Dissolve Edge Particles (Runtime)",
            renderQueue = (int)RenderQueue.Transparent
        };

        edgeParticleTexture = CreateSoftParticleTexture();
        SetTextureIfPresent(edgeParticleMaterial, BaseMap, edgeParticleTexture);
        SetTextureIfPresent(edgeParticleMaterial, Shader.PropertyToID("_MainTex"), edgeParticleTexture);
        SetColorIfPresent(edgeParticleMaterial, BaseColor, Color.white);

        SetFloatIfPresent(edgeParticleMaterial, Shader.PropertyToID("_Surface"), 1f);
        SetFloatIfPresent(edgeParticleMaterial, Shader.PropertyToID("_Blend"), 2f);
        SetFloatIfPresent(edgeParticleMaterial, Shader.PropertyToID("_SrcBlend"), (float)BlendMode.SrcAlpha);
        SetFloatIfPresent(edgeParticleMaterial, Shader.PropertyToID("_DstBlend"), (float)BlendMode.One);
        SetFloatIfPresent(edgeParticleMaterial, Shader.PropertyToID("_ZWrite"), 0f);
        edgeParticleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return edgeParticleMaterial;
    }

    private static Texture2D CreateSoftParticleTexture()
    {
        const int resolution = 32;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            name = "Dissolve Particle Glow (Runtime)",
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
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void EmitEdgeParticles(float edgeOffset, float deltaTime)
    {
        if (edgeParticleSystem == null || particleMeshSources.Count == 0)
        {
            return;
        }

        particleTimer += deltaTime;
        const float updateInterval = 0.05f;
        if (particleTimer < updateInterval)
        {
            return;
        }

        int particleCount = Mathf.Max(1, Mathf.RoundToInt(particlesPerSecond * particleTimer));
        particleTimer = 0f;

        int remainingParticles = particleCount;

        foreach (ParticleMeshSource source in particleMeshSources)
        {
            if (remainingParticles <= 0 || !source.RefreshVertices())
            {
                continue;
            }

            int targetCount = remainingParticles;
            int emittedCount = 0;
            int maximumAttempts = Mathf.Min(source.Vertices.Count * 2, targetCount * 250);

            for (int attempt = 0; attempt < maximumAttempts && emittedCount < targetCount; attempt++)
            {
                Vector3 localVertex = source.Vertices[Random.Range(0, source.Vertices.Count)];
                Vector3 worldPosition = source.Renderer.transform.TransformPoint(localVertex);
                float rootLocalY = transform.InverseTransformPoint(worldPosition).y;

                if (Mathf.Abs(rootLocalY - edgeOffset) > particleBand)
                {
                    continue;
                }

                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    position = worldPosition,
                    velocity = Random.insideUnitSphere * particleSpeed,
                    startLifetime = particleLifetime * Random.Range(0.75f, 1.25f),
                    startSize = particleSize * Random.Range(0.65f, 1.35f),
                    startColor = Color.Lerp(
                        new Color(1f, 0.04f, 0f, 1f),
                        new Color(1f, 0.55f, 0.02f, 1f),
                        Random.value)
                };

                edgeParticleSystem.Emit(emitParams, 1);
                emittedCount++;
                remainingParticles--;
            }
        }
    }

    private bool TryComputeVerticalRange(out float minimumY, out float maximumY)
    {
        minimumY = float.PositiveInfinity;
        maximumY = float.NegativeInfinity;

        foreach (ParticleMeshSource source in particleMeshSources)
        {
            if (!source.RefreshVertices())
            {
                continue;
            }

            foreach (Vector3 localVertex in source.Vertices)
            {
                Vector3 worldPosition = source.Renderer.transform.TransformPoint(localVertex);
                float rootLocalY = transform.InverseTransformPoint(worldPosition).y;
                minimumY = Mathf.Min(minimumY, rootLocalY);
                maximumY = Mathf.Max(maximumY, rootLocalY);
            }
        }

        return !float.IsInfinity(minimumY) && !float.IsInfinity(maximumY);
    }

    private bool ShouldExclude(Material material)
    {
        if (material == null || excludedMaterialKeywords == null)
        {
            return false;
        }

        foreach (string keyword in excludedMaterialKeywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) &&
                material.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ConfigureDissolve(Material material)
    {
        SetColorIfPresent(material, EdgeColor, edgeColor);
        SetFloatIfPresent(material, EdgeWidth, edgeWidth);
        SetFloatIfPresent(material, EdgeIntensity, edgeIntensity);
        SetFloatIfPresent(material, NoiseScale, noiseScale);
        SetVectorIfPresent(material, NoiseSpeed, new Vector4(noiseSpeed.x, noiseSpeed.y, 0f, 0f));
        SetVectorIfPresent(material, DissolveDirection, new Vector4(0f, -1f, 0f, 0f));
    }

    private void SetOffset(float offset)
    {
        Vector4 dissolveOffset = new Vector4(0f, offset, 0f, 0f);
        foreach (Material material in runtimeMaterials)
        {
            material.SetVector(DissolveOffset, dissolveOffset);
        }
    }

    private static void CopySurface(Material source, Material destination)
    {
        if (source == null)
        {
            return;
        }

        CopyTexture(source, BaseMap, destination, BaseMap);
        CopyTexture(source, Shader.PropertyToID("_BumpMap"), destination, NormalMap);
        CopyTexture(source, Shader.PropertyToID("_MetallicGlossMap"), destination, PackedMap);

        CopyColor(source, BaseColor, destination, BaseColor);
        CopyFloat(source, Shader.PropertyToID("_BumpScale"), destination, NormalScale);
        CopyFloat(source, Metallic, destination, Metallic);
        CopyFloat(source, Smoothness, destination, Smoothness);
        CopyFloat(source, OcclusionStrength, destination, OcclusionStrength);
    }

    private static void CopyTexture(Material source, int sourceProperty, Material destination, int destinationProperty)
    {
        if (!source.HasProperty(sourceProperty) || !destination.HasProperty(destinationProperty))
        {
            return;
        }

        destination.SetTexture(destinationProperty, source.GetTexture(sourceProperty));
        destination.SetTextureScale(destinationProperty, source.GetTextureScale(sourceProperty));
        destination.SetTextureOffset(destinationProperty, source.GetTextureOffset(sourceProperty));
    }

    private static void CopyColor(Material source, int sourceProperty, Material destination, int destinationProperty)
    {
        if (source.HasProperty(sourceProperty) && destination.HasProperty(destinationProperty))
        {
            destination.SetColor(destinationProperty, source.GetColor(sourceProperty));
        }
    }

    private static void CopyFloat(Material source, int sourceProperty, Material destination, int destinationProperty)
    {
        if (source.HasProperty(sourceProperty) && destination.HasProperty(destinationProperty))
        {
            destination.SetFloat(destinationProperty, source.GetFloat(sourceProperty));
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

    private static void SetVectorIfPresent(Material material, int property, Vector4 value)
    {
        if (material.HasProperty(property))
        {
            material.SetVector(property, value);
        }
    }

    private static void SetTextureIfPresent(Material material, int property, Texture texture)
    {
        if (material.HasProperty(property))
        {
            material.SetTexture(property, texture);
        }
    }

    private static bool HasMesh(Renderer targetRenderer)
    {
        if (targetRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            return skinnedMeshRenderer.sharedMesh != null;
        }

        MeshFilter meshFilter = targetRenderer.GetComponent<MeshFilter>();
        return meshFilter != null && meshFilter.sharedMesh != null;
    }

    private void OnDestroy()
    {
        foreach (RendererSetup setup in rendererSetups)
        {
            if (setup.Renderer != null)
            {
                setup.Renderer.sharedMaterials = setup.OriginalMaterials;
            }
        }

        foreach (Material material in runtimeMaterials)
        {
            Destroy(material);
        }

        foreach (ParticleMeshSource source in particleMeshSources)
        {
            source.Dispose();
        }

        if (edgeParticleMaterial != null)
        {
            Destroy(edgeParticleMaterial);
        }

        if (edgeParticleTexture != null)
        {
            Destroy(edgeParticleTexture);
        }
    }

    private readonly struct RendererSetup
    {
        public Renderer Renderer { get; }
        public Material[] OriginalMaterials { get; }

        public RendererSetup(Renderer renderer, Material[] originalMaterials)
        {
            Renderer = renderer;
            OriginalMaterials = originalMaterials;
        }
    }

    private sealed class ParticleMeshSource
    {
        private readonly SkinnedMeshRenderer skinnedRenderer;
        private readonly MeshFilter meshFilter;
        private readonly Mesh bakedMesh;

        public Renderer Renderer { get; }
        public List<Vector3> Vertices { get; } = new List<Vector3>();

        public ParticleMeshSource(Renderer renderer)
        {
            Renderer = renderer;
            skinnedRenderer = renderer as SkinnedMeshRenderer;
            meshFilter = renderer.GetComponent<MeshFilter>();

            if (skinnedRenderer != null)
            {
                bakedMesh = new Mesh
                {
                    name = $"{renderer.name} Particle Sampling Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }

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
                Object.Destroy(bakedMesh);
            }
        }
    }
}
