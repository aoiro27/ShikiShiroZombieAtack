using UnityEngine;

namespace ShikiShiro
{
    public static class MaterialFactory
    {
        private static Shader _lit;

        public static Material Create(Color color, float metallic = 0.05f, float smoothness = 0.28f)
        {
            if (_lit == null)
            {
                _lit = Shader.Find("Standard");
                if (_lit == null)
                {
                    _lit = Shader.Find("Universal Render Pipeline/Lit");
                }

                if (_lit == null)
                {
                    _lit = Shader.Find("Diffuse");
                }
            }

            var material = new Material(_lit) { color = color };
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            return material;
        }
    }
}
