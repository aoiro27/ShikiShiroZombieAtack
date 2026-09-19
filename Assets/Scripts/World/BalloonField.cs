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

        private ArenaBuilder _arena;
        private HordeDirector _horde;
        private CombatFx _fx;
        private ProceduralSfx _sfx;
        private Sprite[] _sprites;
        private readonly List<BalloonProp> _balloons = new List<BalloonProp>(Count);

        public static BalloonField Spawn(Transform parent, ArenaBuilder arena, HordeDirector horde, CombatFx fx, ProceduralSfx sfx)
        {
            var root = new GameObject("BalloonField");
            root.transform.SetParent(parent, false);
            var field = root.AddComponent<BalloonField>();
            field._arena = arena;
            field._horde = horde;
            field._fx = fx;
            field._sfx = sfx;
            field._sprites = LoadSprites();
            if (field._sprites.Length == 0)
            {
                Debug.LogWarning("Balloon sprites were not found. Import Free Balloons into Resources/Balloons.");
            }

            return field;
        }

        public void Respawn()
        {
            if (_sprites == null || _sprites.Length == 0)
            {
                _sprites = LoadSprites();
            }

            if (_sprites.Length == 0 || _arena == null)
            {
                return;
            }

            EnsurePool();
            for (int i = 0; i < _balloons.Count; i++)
            {
                BalloonProp balloon = _balloons[i];
                if (balloon == null)
                {
                    continue;
                }

                if (!TryPlace(out Vector3 pos))
                {
                    balloon.gameObject.SetActive(false);
                    continue;
                }

                balloon.Setup(_sprites[Random.Range(0, _sprites.Length)], pos, _horde, _fx, _sfx);
            }
        }

        private void EnsurePool()
        {
            while (_balloons.Count < Count)
            {
                var go = new GameObject("Balloon");
                go.transform.SetParent(transform, false);
                go.SetActive(false);
                _balloons.Add(go.AddComponent<BalloonProp>());
            }
        }

        private bool TryPlace(out Vector3 pos)
        {
            pos = Vector3.zero;
            Vector3 spawn = _arena.SpawnPoint;
            float minKeepSqr = 8f * 8f;
            for (int i = 0; i < 12; i++)
            {
                float angle = Random.Range(0f, 360f);
                float radius = Random.Range(10f, Mathf.Min(_arena.PlayHalf * 0.78f, 48f));
                Vector3 ground = spawn + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                ground = _arena.SnapToStreet(ground);
                ground = _arena.ClampInside(ground, 2.4f);
                Vector2 away = new Vector2(ground.x - spawn.x, ground.z - spawn.z);
                if (away.sqrMagnitude < minKeepSqr || !_arena.Contains(ground, 2.4f))
                {
                    continue;
                }

                pos = ground + Vector3.up * Random.Range(2.2f, 5.4f);
                return true;
            }

            return false;
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
            const string folder = "Assets/Resources/Balloons";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (sprite != null)
                {
                    list.Add(sprite);
                }
            }
#endif
        }
    }
}
