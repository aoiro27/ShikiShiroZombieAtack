#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShikiShiro.EditorTools
{
    public static class VerifyArenaCapture
    {
        public const string RequestPath = "Assets/Editor/run-verify.txt";
        public const string OutputDir = "Temp/verify";

        public static void Run()
        {
            string dir = "/tmp/shiki-arena-out";
            Directory.CreateDirectory(dir);
            Debug.Log("VERIFY_DIR " + dir);
            var log = new StringBuilder();

            var preview = EditorSceneManager.NewPreviewScene();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.85f, 0.88f, 0.92f);

            var sunGo = new GameObject("Sun");
            EditorSceneManager.MoveGameObjectToScene(sunGo, preview);
            sunGo.transform.rotation = Quaternion.Euler(48f, 130f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.4f;

            var worldGo = new GameObject("World");
            EditorSceneManager.MoveGameObjectToScene(worldGo, preview);
            var arena = new ArenaBuilder(worldGo.transform);
            arena.Build();

            int overlapping = 0;
            var origin = arena.SpawnPoint + Vector3.up * 1.6f;
            log.AppendLine($"spawn={arena.SpawnPoint} radius={arena.Radius}");
            log.AppendLine($"kaykitA={Resources.Load<GameObject>("KayKit/building_A")}");
            log.AppendLine($"rifle={AssetDatabase.LoadAssetAtPath<GameObject>("Assets/4K 3D Weapons Mega Pack/Rifle 1/Prefabs/Rifle 1.prefab")}");

            GameObject[] roots = preview.GetRootGameObjects();
            int rendererCount = 0;
            for (int r = 0; r < roots.Length; r++)
            {
                Renderer[] renderers = roots[r].GetComponentsInChildren<Renderer>(true);
                rendererCount += renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer rend = renderers[i];
                    Bounds b = rend.bounds;
                    bool hit = b.Contains(origin);
                    if (hit)
                    {
                        overlapping++;
                    }

                    if (hit || b.size.y > 20f || Mathf.Max(b.size.x, b.size.z) > 25f)
                    {
                        log.AppendLine($"BIG/OVERLAP {(hit ? "HIT" : "   ")} {rend.gameObject.name} size={b.size} center={b.center} mat={rend.sharedMaterial?.name} shader={rend.sharedMaterial?.shader?.name}");
                    }
                }
            }

            log.AppendLine($"renderers={rendererCount} overlappingSpawn={overlapping}");
            Debug.Log("VERIFY_REPORT\n" + log);
            File.WriteAllText(Path.Combine(dir, "arena-report.txt"), log.ToString());
            File.WriteAllText(Path.Combine(dir, "exit.txt"), overlapping > 2 ? "FAIL" : "OK");

            var camGo = new GameObject("ShotCam");
            EditorSceneManager.MoveGameObjectToScene(camGo, preview);
            var cam = camGo.AddComponent<Camera>();
            cam.scene = preview;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.7f, 0.95f);
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.transform.position = origin;

            Capture(cam, Path.Combine(dir, "north.png"), Vector3.forward);
            Capture(cam, Path.Combine(dir, "east.png"), Vector3.right);
            Capture(cam, Path.Combine(dir, "south.png"), Vector3.back);
            Capture(cam, Path.Combine(dir, "west.png"), Vector3.left);
            Capture(cam, Path.Combine(dir, "down.png"), Vector3.down);

            EditorSceneManager.ClosePreviewScene(preview);
        }

        private static void Capture(Camera cam, string path, Vector3 forward)
        {
            cam.transform.rotation = Quaternion.LookRotation(forward);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }

    internal sealed class VerifyArenaTrigger : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
        {
            for (int i = 0; i < imported.Length; i++)
            {
                if (imported[i] == VerifyArenaCapture.RequestPath)
                {
                    EditorApplication.delayCall += VerifyArenaCapture.Run;
                    return;
                }
            }
        }
    }
}
#endif
