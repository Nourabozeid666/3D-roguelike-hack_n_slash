using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDissolve : MonoBehaviour
{
    [SerializeField] private float spawnDuration = 1.5f;
    [SerializeField] private float invisibleOffset = -0.5f;
    [SerializeField] private float visibleOffset = 1.1f;

    private readonly List<Material> enemyMaterials = new List<Material>();

    private static readonly int DissolveOffset =
        Shader.PropertyToID("_DissolveOffest");

    private void Awake()
    {
        Renderer[] enemyRenderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer enemyRenderer in enemyRenderers)
        {
            foreach (Material material in enemyRenderer.materials)
            {
                if (material.HasProperty(DissolveOffset))
                {
                    enemyMaterials.Add(material);
                }
            }
        }
    }

    private void Start()
    {
        PlaySpawnEffect();
    }

    public void PlaySpawnEffect()
    {
        StopAllCoroutines();
        StartCoroutine(SpawnAnimation());
    }

    private IEnumerator SpawnAnimation()
    {
        SetDissolveOffset(invisibleOffset);

        float time = 0f;

        while (time < spawnDuration)
        {
            time += Time.deltaTime;
            float percentage = Mathf.Clamp01(time / spawnDuration);

            float offsetY =
                Mathf.Lerp(invisibleOffset, visibleOffset, percentage);

            SetDissolveOffset(offsetY);

            yield return null;
        }

        SetDissolveOffset(visibleOffset);
    }

    private void SetDissolveOffset(float offsetY)
    {
        foreach (Material enemyMaterial in enemyMaterials)
        {
            enemyMaterial.SetVector(
                DissolveOffset,
                new Vector4(0f, offsetY, 0f, 0f)
            );
        }
    }
}
