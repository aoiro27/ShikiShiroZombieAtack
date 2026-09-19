using UnityEngine;

namespace ShikiShiro
{
    public static class MaterialFactory
    {
        private static Shader _lit;

        public static Material Create(Color color, float metallic = 0.05f, float smoothness = 0.28f)
        {
            return Create(color, null, metallic, smoothness);
        }

        public static Material Create(Color color, Texture2D texture, float metallic = 0.05f, float smoothness = 0.28f)
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
                    _lit = Shader.Find("Unlit/Color");
                }

                if (_lit == null)
                {
                    _lit = Shader.Find("Sprites/Default");
                }
            }

            if (_lit == null)
            {
                return new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var material = new Material(_lit) { color = color };
            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                }

                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }
            }
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
