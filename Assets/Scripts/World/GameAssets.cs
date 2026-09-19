using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShikiShiro
{
    public static class GameAssets
    {
        public const string Player = "Quaternius/Player/Characters_Matt";
        public const string ZombieAtlas = "Quaternius/Zombie_Atlas";
        public const string ZombieAnimator = "Assets/Supercyan Character Pack Zombie Sample/AnimatorControllers/FreeZombieController.controller";
        public const string ZombieWalker = "Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE.prefab";
        public const string ZombieRunner = "Assets/ZombieMale_AAB/Prefabs/ZombieMale_AAB.prefab";
        public const string ZombieBrute = "Assets/Supercyan Character Pack Zombie Sample/Prefabs/Base/High Quality/FreeZombie.prefab";

        public const string Pistol = "Assets/4K 3D Weapons Mega Pack/Rifle 1/Prefabs/Rifle 1.prefab";
        public const string Smg = "Assets/4K 3D Weapons Mega Pack/Rifle 1/Prefabs/Rifle 1.prefab";
        public const string Shotgun = "Assets/4K 3D Weapons Mega Pack/Rifle 1/Prefabs/Rifle 1.prefab";
        public const string AmmoCrate = "KayKit/box_A";
        public const string MedkitCrate = "KayKit/box_A";
        public const string WeaponAtlas = "Kenney/Weapons/Textures/colormap";
        public const string CityAtlas = "KayKit/citybits_texture";

        public static T Load<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            T resource = Resources.Load<T>(path);
            if (resource != null)
            {
                return resource;
            }

            return LoadFromProject<T>(path);
        }

        public static GameObject TryInstantiate(string path, Transform parent)
        {
            GameObject prefab = LoadPrefab(path);
            if (prefab == null)
            {
                return null;
            }

            GameObject go = Object.Instantiate(prefab, parent, false);
            go.name = prefab.name;
            StripRuntimeJunk(go);
            if (!IsProjectAsset(path))
            {
                FixImportScale(go);
            }

            return go;
        }

        public static GameObject SpawnProp(string path, Transform parent, Vector3 position, Quaternion rotation, bool obstacle, float height = 0f)
        {
            GameObject go = TryInstantiate(path, parent);
            if (go == null)
            {
                return null;
            }

            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one;
            Paint(go, path);
            Unbake(go);
            if (height > 0.1f)
            {
                Fit(go, height, 7.5f);
            }

            if (obstacle)
            {
                MakeObstacle(go, 18f);
            }
            else if (!IsProjectAsset(path))
            {
                DisableColliders(go);
            }

            return go;
        }

        public static GameObject AttachCharacter(Transform parent, string modelPath, string texturePath)
        {
            GameObject visual = TryInstantiate(modelPath, parent);
            if (visual == null)
            {
                return null;
            }

            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            EnsureHeight(visual, 1.8f);
            DisableColliders(visual);
            if (!IsProjectAsset(modelPath))
            {
                ApplyMainTexture(visual, Load<Texture2D>(texturePath));
            }

            return visual;
        }

        public static Vector3 Measure(string path)
        {
            GameObject prefab = LoadPrefab(path);
            if (prefab == null)
            {
                return Vector3.one * 2f;
            }

            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
            if (filters.Length == 0)
            {
                return Vector3.one * 2f;
            }

            Bounds bounds = filters[0].sharedMesh != null ? filters[0].sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh == null)
                {
                    continue;
                }

                Bounds local = filters[i].sharedMesh.bounds;
                Vector3 worldSize = Vector3.Scale(local.size, filters[i].transform.lossyScale);
                bounds.Encapsulate(new Bounds(filters[i].transform.localPosition, worldSize));
            }

            Vector3 size = bounds.size;
            float mag = Mathf.Max(size.x, size.y, size.z);
            if (mag > 0.0001f && mag < 0.5f)
            {
                size *= 100f;
            }

            return size;
        }

        public static Bounds? WorldBounds(GameObject go)
        {
            return CombinedBounds(go);
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
            Paint(go, atlasPath);
        }

        public static void Paint(GameObject go, string path)
        {
            Texture2D atlas = GuessAtlas(path) == WeaponAtlas
                ? Load<Texture2D>(WeaponAtlas)
                : LoadCityAtlas();
            Material mat = MaterialFactory.Create(Color.white, atlas);
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = mat;
            }
        }

        public static void FitFootprint(GameObject go, float width, float depth)
        {
            Vector3 size = MeshSize(go);
            if (size.x < 0.001f || size.z < 0.001f)
            {
                return;
            }

            Vector3 scale = go.transform.localScale;
            scale.x *= width / size.x;
            scale.z *= depth / size.z;
            go.transform.localScale = scale;
        }

        public static void Fit(GameObject go, float height, float maxXZ)
        {
            Vector3 size = MeshSize(go);
            if (size.y < 0.001f)
            {
                return;
            }

            go.transform.localScale *= height / size.y;
            size = MeshSize(go);
            float xz = Mathf.Max(size.x, size.z);
            if (xz > maxXZ)
            {
                go.transform.localScale *= maxXZ / xz;
            }
        }

        private static Vector3 MeshSize(GameObject go)
        {
            MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0)
            {
                return Vector3.zero;
            }

            Bounds? bounds = null;
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Vector3[] corners = new Vector3[8];
                Bounds mb = mesh.bounds;
                Vector3 c = mb.center;
                Vector3 e = mb.extents;
                corners[0] = c + new Vector3(e.x, e.y, e.z);
                corners[1] = c + new Vector3(e.x, e.y, -e.z);
                corners[2] = c + new Vector3(e.x, -e.y, e.z);
                corners[3] = c + new Vector3(e.x, -e.y, -e.z);
                corners[4] = c + new Vector3(-e.x, e.y, e.z);
                corners[5] = c + new Vector3(-e.x, e.y, -e.z);
                corners[6] = c + new Vector3(-e.x, -e.y, e.z);
                corners[7] = c + new Vector3(-e.x, -e.y, -e.z);
                for (int k = 0; k < 8; k++)
                {
                    Vector3 w = filters[i].transform.TransformPoint(corners[k]);
                    if (!bounds.HasValue)
                    {
                        bounds = new Bounds(w, Vector3.zero);
                    }
                    else
                    {
                        Bounds b = bounds.Value;
                        b.Encapsulate(w);
                        bounds = b;
                    }
                }
            }

            return bounds?.size ?? Vector3.zero;
        }

        private static Texture2D _cityAtlas;

        public static Texture2D LoadCityAtlas()
        {
            if (_cityAtlas != null)
            {
                return _cityAtlas;
            }

            _cityAtlas = Load<Texture2D>(CityAtlas);
            if (_cityAtlas != null)
            {
                return _cityAtlas;
            }

            string png = System.IO.Path.Combine(Application.dataPath, "Resources/KayKit/citybits_texture.png");
            if (System.IO.File.Exists(png))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (tex.LoadImage(System.IO.File.ReadAllBytes(png)))
                {
                    tex.name = "citybits_texture";
                    _cityAtlas = tex;
                }
            }

            return _cityAtlas;
        }

        public static void Unbake(GameObject go)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.lightmapIndex = -1;
                renderer.realtimeLightmapIndex = -1;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }
        }

        public static void MakeObstacle(GameObject go, float maxFootprint = 14f)
        {
            int layer = LayerMask.NameToLayer("Obstacle");
            if (layer < 0)
            {
                layer = 0;
            }

            SetLayerRecursively(go, layer);
            DisableColliders(go);
            ClampFootprint(go, maxFootprint);

            Bounds? bounds = CombinedBounds(go);
            if (!bounds.HasValue)
            {
                return;
            }

            var hit = new GameObject("HitBox");
            hit.layer = layer;
            hit.transform.SetParent(null, false);
            hit.transform.position = bounds.Value.center;
            hit.transform.rotation = Quaternion.identity;
            hit.transform.localScale = Vector3.one;
            var box = hit.AddComponent<BoxCollider>();
            box.size = Vector3.Max(bounds.Value.size, new Vector3(1.2f, 1.2f, 1.2f));
            hit.transform.SetParent(go.transform, true);
        }

        public static void DisableColliders(GameObject go)
        {
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        public static void EnsureHeight(GameObject go, float height)
        {
            Bounds? bounds = CombinedBounds(go);
            if (!bounds.HasValue || bounds.Value.size.y < 0.05f)
            {
                go.transform.localScale *= height;
                return;
            }

            if (bounds.Value.size.y < height * 0.55f || bounds.Value.size.y > height * 1.8f)
            {
                go.transform.localScale *= height / bounds.Value.size.y;
            }
        }

        private static string GuessAtlas(string path)
        {
            if (path.Contains("/KayKit/") || path.StartsWith("KayKit/"))
            {
                return CityAtlas;
            }

            if (path.Contains("/Quaternius/"))
            {
                return ZombieAtlas;
            }

            if (path.Contains("/Weapons/"))
            {
                return WeaponAtlas;
            }

            return CityAtlas;
        }

        private static void ClampFootprint(GameObject go, float maxXZ)
        {
            Bounds? bounds = CombinedBounds(go);
            if (!bounds.HasValue)
            {
                return;
            }

            float xz = Mathf.Max(bounds.Value.size.x, bounds.Value.size.z);
            if (xz > maxXZ)
            {
                go.transform.localScale *= maxXZ / xz;
            }
        }

        private static Bounds? CombinedBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            Bounds? bounds = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled || !renderers[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!bounds.HasValue)
                {
                    bounds = renderers[i].bounds;
                }
                else
                {
                    Bounds b = bounds.Value;
                    b.Encapsulate(renderers[i].bounds);
                    bounds = b;
                }
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
            foreach (Camera camera in go.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }

            foreach (AudioListener listener in go.GetComponentsInChildren<AudioListener>(true))
            {
                listener.enabled = false;
            }

            foreach (Light light in go.GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Rectangle || light.type == LightType.Disc || light.bakingOutput.isBaked)
                {
                    light.enabled = false;
                }
            }

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.lightmapIndex = -1;
                renderer.realtimeLightmapIndex = -1;
            }
        }

        private static bool IsProjectAsset(string path)
        {
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/");
        }

        private static GameObject LoadPrefab(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            GameObject resource = Resources.Load<GameObject>(path);
            if (resource != null)
            {
                return resource;
            }

            return LoadFromProject<GameObject>(path);
        }

        private static T LoadFromProject<T>(string path) where T : Object
        {
            if (!IsProjectAsset(path))
            {
                return null;
            }

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
