using UnityEngine;

namespace ShikiShiro
{
    public static class GameAssets
    {
        public const string Character = "Kenney/Characters/characterMedium";
        public const string SkinHuman = "Kenney/Characters/Skins/humanMaleA";
        public const string SkinWalker = "Kenney/Characters/Skins/zombieMaleA";
        public const string SkinRunner = "Kenney/Characters/Skins/zombieFemaleA";
        public const string SkinBrute = "Kenney/Characters/Skins/zombieMaleA";

        public const string Pistol = "Kenney/Weapons/blaster-a";
        public const string Smg = "Kenney/Weapons/blaster-k";
        public const string Shotgun = "Kenney/Weapons/blaster-r";
        public const string AmmoCrate = "Kenney/Weapons/crate-small";
        public const string MedkitCrate = "Kenney/Weapons/crate-medium";
        public const string Clip = "Kenney/Weapons/clip-small";
        public const string WeaponAtlas = "Kenney/Weapons/Textures/colormap";

        public const string BuildingAtlas = "Kenney/Buildings/Textures/colormap";
        public const string CityAtlas = "Kenney/City/Textures/colormap";

        public static readonly string[] Buildings =
        {
            "Kenney/Buildings/building-a",
            "Kenney/Buildings/building-c",
            "Kenney/Buildings/building-e",
            "Kenney/Buildings/building-g",
            "Kenney/Buildings/building-i",
            "Kenney/Buildings/building-l",
            "Kenney/Buildings/building-q",
            "Kenney/Buildings/building-t"
        };

        public static T Load<T>(string path) where T : Object
        {
            return Resources.Load<T>(path);
        }

        public static GameObject TryInstantiate(string path, Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                return null;
            }

            GameObject go = Object.Instantiate(prefab, parent, false);
            go.name = prefab.name;
            StripRuntimeJunk(go);
            FixImportScale(go);
            return go;
        }

        public static GameObject SpawnProp(string path, Transform parent, Vector3 position, Quaternion rotation, bool obstacle)
        {
            GameObject go = TryInstantiate(path, parent);
            if (go == null)
            {
                return null;
            }

            go.transform.SetPositionAndRotation(position, rotation);
            BindColormap(go, GuessAtlas(path));
            if (obstacle)
            {
                MakeObstacle(go);
            }
            else
            {
                StripColliders(go);
            }

            return go;
        }

        public static GameObject AttachCharacter(Transform parent, string skinResource)
        {
            GameObject visual = TryInstantiate(Character, parent);
            if (visual == null)
            {
                return null;
            }

            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            FitHeight(visual, 1.8f);
            StripColliders(visual);
            ApplyMainTexture(visual, Resources.Load<Texture2D>(skinResource));
            return visual;
        }

        public static Vector3 Measure(string path)
        {
            GameObject sample = TryInstantiate(path, null);
            if (sample == null)
            {
                return Vector3.one * 2f;
            }

            sample.transform.position = new Vector3(0f, 5000f, 0f);
            Bounds? bounds = CombinedBounds(sample);
            Vector3 size = bounds.HasValue ? bounds.Value.size : Vector3.one * 2f;
            Object.DestroyImmediate(sample);
            return size;
        }

        public static void ApplyMainTexture(GameObject go, Texture2D texture)
        {
            if (go == null || texture == null)
            {
                return;
            }

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetTexture("_MainTex", texture);
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseMap"))
                {
                    block.SetTexture("_BaseMap", texture);
                }

                renderer.SetPropertyBlock(block);
            }
        }

        public static void BindColormap(GameObject go, string atlasPath)
        {
            ApplyMainTexture(go, Resources.Load<Texture2D>(atlasPath));
        }

        public static void MakeObstacle(GameObject go)
        {
            int layer = LayerMask.NameToLayer("Obstacle");
            if (layer < 0)
            {
                layer = 0;
            }

            SetLayerRecursively(go, layer);
            StripColliders(go);
            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }

            if (go.GetComponentInChildren<Collider>() == null)
            {
                Bounds? bounds = CombinedBounds(go);
                if (bounds.HasValue)
                {
                    var box = go.AddComponent<BoxCollider>();
                    box.center = go.transform.InverseTransformPoint(bounds.Value.center);
                    Vector3 lossy = go.transform.lossyScale;
                    box.size = new Vector3(
                        bounds.Value.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                        bounds.Value.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                        bounds.Value.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
                }
            }
        }

        private static string GuessAtlas(string path)
        {
            if (path.Contains("/Buildings/"))
            {
                return BuildingAtlas;
            }

            if (path.Contains("/Weapons/"))
            {
                return WeaponAtlas;
            }

            return CityAtlas;
        }

        private static void FitHeight(GameObject go, float height)
        {
            Bounds? bounds = CombinedBounds(go);
            if (!bounds.HasValue || bounds.Value.size.y < 0.01f)
            {
                return;
            }

            go.transform.localScale *= height / bounds.Value.size.y;
            bounds = CombinedBounds(go);
            if (!bounds.HasValue || go.transform.parent == null)
            {
                return;
            }

            go.transform.position += Vector3.up * (go.transform.parent.position.y - bounds.Value.min.y);
        }

        private static Bounds? CombinedBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return null;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void FixImportScale(GameObject go)
        {
            Bounds? bounds = CombinedBounds(go);
            if (!bounds.HasValue)
            {
                return;
            }

            float mag = Mathf.Max(bounds.Value.size.x, bounds.Value.size.y, bounds.Value.size.z);
            if (mag > 0.0001f && mag < 0.5f)
            {
                go.transform.localScale *= 100f;
            }
        }

        private static void StripRuntimeJunk(GameObject go)
        {
            foreach (Animator animator in go.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }

            foreach (Camera camera in go.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void StripColliders(GameObject go)
        {
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
        }
    }
}
