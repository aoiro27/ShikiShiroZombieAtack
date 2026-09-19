using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShikiShiro
{
    public sealed class BalloonField : MonoBehaviour
    {
        private const int Count = 72;

        public static BalloonField Spawn(Transform parent, ArenaBuilder arena, HordeDirector horde, CombatFx fx, ProceduralSfx sfx)
        {
            var root = new GameObject("BalloonField");
            root.transform.SetParent(parent, false);
            var field = root.AddComponent<BalloonField>();
            field.Build(arena, horde, fx, sfx);
            return field;
        }

        private void Build(ArenaBuilder arena, HordeDirector horde, CombatFx fx, ProceduralSfx sfx)
        {
            Sprite[] sprites = LoadSprites();
            if (sprites.Length == 0)
            {
                Debug.LogWarning("Balloon sprites were not found. Import Free Balloons into Resources/Balloons.");
                return;
            }

            Vector3 spawn = arena.SpawnPoint;
            float minKeep = 8f;
            float minKeepSqr = minKeep * minKeep;
            int placed = 0;
            int guard = 0;
            while (placed < Count && guard < Count * 12)
            {
                guard++;
                float angle = Random.Range(0f, 360f);
                float radius = Random.Range(10f, Mathf.Min(arena.PlayHalf * 0.78f, 48f));
                Vector3 ground = spawn + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                ground = arena.SnapToStreet(ground);
                ground = arena.ClampInside(ground, 2.4f);
                Vector2 away = new Vector2(ground.x - spawn.x, ground.z - spawn.z);
                if (away.sqrMagnitude < minKeepSqr)
                {
                    continue;
                }

                if (!arena.Contains(ground, 2.4f))
                {
                    continue;
                }

                Vector3 pos = ground + Vector3.up * Random.Range(2.2f, 5.4f);
                var go = new GameObject("Balloon");
                go.transform.SetParent(transform, false);
                var prop = go.AddComponent<BalloonProp>();
                prop.Setup(sprites[Random.Range(0, sprites.Length)], pos, horde, fx, sfx);
                placed++;
            }
        }

        private static Sprite[] LoadSprites()
        {
            var list = new List<Sprite>();
            Sprite[] packed = Resources.LoadAll<Sprite>("Balloons");
            if (packed != null)
            {
                for (int i = 0; i < packed.Length; i++)
                {
                    if (packed[i] != null)
                    {
                        list.Add(packed[i]);
                    }
                }
            }

            if (list.Count == 0)
            {
                CollectEditorSprites(list);
            }

            return list.ToArray();
        }

        private static void CollectEditorSprites(List<Sprite> list)
        {
#if UNITY_EDITOR
            string[] folders = { "Assets/Qookie Games/Balloons Free", "Assets/Resources/Balloons" };
            for (int f = 0; f < folders.Length; f++)
            {
                if (!AssetDatabase.IsValidFolder(folders[f]))
                {
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folders[f] });
                for (int i = 0; i < guids.Length; i++)
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (sprite != null)
                    {
                        list.Add(sprite);
                    }
                }
            }
#endif
        }
    }
}
