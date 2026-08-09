using System.Collections.Generic;
using UnityEngine;

namespace DissolveExample
{
    public class DissolveChilds : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();

        private void Start()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer childRenderer in renderers)
            {
                materials.AddRange(childRenderer.materials);
            }
        }

        private void Update()
        {
            float value = Mathf.PingPong(Time.time * 0.5f, 1f);
            SetValue(value);
        }

        public void SetValue(float value)
        {
            foreach (Material material in materials)
            {
                material.SetFloat("_Dissolve", value);
            }
        }
    }
}
