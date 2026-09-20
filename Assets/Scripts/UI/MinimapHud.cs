using UnityEngine;
using UnityEngine.UI;

namespace ShikiShiro
{
    public sealed class MinimapHud : MonoBehaviour
    {
        private const int MaxBlips = 48;
        private const float Size = 348f;
        private const float ViewRange = 48f;

        private RectTransform _overlay;
        private RectTransform _facingCone;
        private RectTransform _playerBlip;
        private RectTransform[] _enemyBlips;
        private float _half;
        private Transform _player;
        private HordeDirector _horde;

        public static MinimapHud Create(Transform parent)
        {
            UiSprites.Ensure();
            var go = new GameObject("Minimap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-22f, -118f);
            rt.sizeDelta = new Vector2(Size, Size);

            var frame = go.GetComponent<Image>();
            frame.sprite = UiSprites.Panel;
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0.07f, 0.09f, 0.1f, 0.94f);
            frame.raycastTarget = false;

            var map = new GameObject("Field", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            map.transform.SetParent(go.transform, false);
            var mapRt = map.GetComponent<RectTransform>();
            mapRt.anchorMin = Vector2.zero;
            mapRt.anchorMax = Vector2.one;
            mapRt.offsetMin = new Vector2(12f, 12f);
            mapRt.offsetMax = new Vector2(-12f, -12f);
            var mapImg = map.GetComponent<Image>();
            mapImg.color = new Color(0.08f, 0.11f, 0.09f, 1f);
            mapImg.raycastTarget = false;

            AddGrid(mapRt);
            AddLabel(go.transform, "N", new Vector2(0f, -6f), new Vector2(0.5f, 1f));

            var overlayGo = new GameObject("Blips", typeof(RectTransform));
            overlayGo.transform.SetParent(go.transform, false);
            var overlay = overlayGo.GetComponent<RectTransform>();
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = new Vector2(12f, 12f);
            overlay.offsetMax = new Vector2(-12f, -12f);

            var hud = go.AddComponent<MinimapHud>();
            hud._overlay = overlay;
            hud._half = (Size - 24f) * 0.5f;
            hud._facingCone = MakeBlip(overlay, "Facing", UiSprites.FacingCone, Color.white, Size - 36f);
            hud._playerBlip = MakeBlip(overlay, "Player", UiSprites.Blip, new Color(0.2f, 1f, 0.55f, 1f), 14f);
            MakeBlip(hud._playerBlip, "Core", UiSprites.Blip, new Color(0.9f, 1f, 0.95f, 1f), 7f);
            hud._enemyBlips = new RectTransform[MaxBlips];
            for (int i = 0; i < MaxBlips; i++)
            {
                hud._enemyBlips[i] = MakeEnemyBlip(overlay);
                hud._enemyBlips[i].gameObject.SetActive(false);
            }

            hud._facingCone.SetAsFirstSibling();
            hud._playerBlip.SetAsLastSibling();
            return hud;
        }

        public void Bind(Transform player, ArenaBuilder arena, HordeDirector horde)
        {
            _player = player;
            _horde = horde;
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                return;
            }

            _playerBlip.gameObject.SetActive(true);
            _playerBlip.anchoredPosition = Vector2.zero;
            _playerBlip.localEulerAngles = Vector3.zero;
            if (_facingCone != null)
            {
                _facingCone.gameObject.SetActive(true);
                _facingCone.anchoredPosition = Vector2.zero;
                _facingCone.localEulerAngles = new Vector3(0f, 0f, -_player.eulerAngles.y);
            }

            _playerBlip.SetAsLastSibling();

            if (_horde == null)
            {
                return;
            }

            Vector3 origin = _player.position;
            int used = 0;
            var all = _horde.AllZombies;
            for (int i = 0; i < all.Count && used < MaxBlips; i++)
            {
                ZombieAgent z = all[i];
                if (z == null || !z.IsAlive || !z.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 delta = z.transform.position - origin;
                Vector2 ui = new Vector2(delta.x, delta.z);
                if (ui.magnitude > ViewRange)
                {
                    ui = ui.normalized * ViewRange;
                }

                float scale = (_half - 10f) / ViewRange;
                _enemyBlips[used].anchoredPosition = ui * scale;
                _enemyBlips[used].gameObject.SetActive(true);
                used++;
            }

            for (int i = used; i < MaxBlips; i++)
            {
                if (_enemyBlips[i].gameObject.activeSelf)
                {
                    _enemyBlips[i].gameObject.SetActive(false);
                }
            }
        }

        private static RectTransform MakeEnemyBlip(Transform parent)
        {
            var go = new GameObject("Enemy", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(36f, 36f);
            MakeBlip(rt, "Glow", UiSprites.Blip, new Color(1f, 0.08f, 0.04f, 0.92f), 36f);
            MakeBlip(rt, "Body", UiSprites.Blip, new Color(1f, 0.02f, 0.02f, 1f), 26f);
            MakeBlip(rt, "Core", UiSprites.Blip, new Color(1f, 0.28f, 0.22f, 1f), 12f);
            return rt;
        }

        private static RectTransform MakeBlip(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.maskable = false;
            return rt;
        }

        private static void AddGrid(RectTransform parent)
        {
            for (int i = 1; i <= 3; i++)
            {
                float t = i / 4f;
                MakeLine(parent, "H" + i, new Vector2(0f, t), new Vector2(1f, t), new Vector2(0f, -0.5f), new Vector2(0f, 0.5f));
                MakeLine(parent, "V" + i, new Vector2(t, 0f), new Vector2(t, 1f), new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f));
            }
        }

        private static void MakeLine(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.12f);
            image.raycastTarget = false;
        }

        private static void AddLabel(Transform parent, string text, Vector2 pos, Vector2 anchor)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(40f, 28f);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.UpperCenter;
            label.color = new Color(0.85f, 0.9f, 0.8f, 0.9f);
            label.text = text;
            label.raycastTarget = false;
        }
    }
}
