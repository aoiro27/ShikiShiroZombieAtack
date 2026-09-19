using UnityEngine;
using UnityEngine.UI;

namespace ShikiShiro
{
    public sealed class HitPopupHud : MonoBehaviour
    {
        private const int PoolSize = 28;
        private const float Life = 0.95f;

        private readonly Slot[] _slots = new Slot[PoolSize];
        private RectTransform _root;
        private Canvas _canvas;
        private Camera _camera;
        private int _cursor;

        private struct Slot
        {
            public RectTransform Rt;
            public Text Score;
            public Text Combo;
            public Vector3 World;
            public Vector2 Jitter;
            public float Born;
            public float Peak;
            public bool Alive;
        }

        public static HitPopupHud Create(Transform canvasRoot, Camera camera)
        {
            var go = new GameObject("HitPopups", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var hud = go.AddComponent<HitPopupHud>();
            hud._root = rt;
            hud._canvas = canvasRoot.GetComponent<Canvas>();
            hud._camera = camera;
            hud.BuildPool();
            return hud;
        }

        public void Spawn(in HitPopupInfo info)
        {
            int i = _cursor;
            _cursor = (_cursor + 1) % PoolSize;
            Slot slot = _slots[i];
            slot.World = info.WorldPoint + Vector3.up * 0.12f;
            slot.Jitter = new Vector2(Random.Range(-36f, 36f), Random.Range(8f, 28f));
            slot.Born = Time.unscaledTime;
            slot.Peak = info.Kill ? 1.35f : 1.08f;
            slot.Alive = true;
            slot.Rt.gameObject.SetActive(true);
            slot.Rt.SetAsLastSibling();

            bool gold = info.Kill || info.Headshot;
            slot.Score.text = info.Headshot && info.Kill ? $"HEAD +{info.Score}" : $"+{info.Score}";
            slot.Score.fontSize = info.Kill ? 48 : 34;
            slot.Score.color = gold ? new Color(1f, 0.92f, 0.28f, 1f) : new Color(1f, 1f, 1f, 1f);

            if (info.Combo >= 2)
            {
                slot.Combo.gameObject.SetActive(true);
                slot.Combo.text = $"COMBO x{info.Combo}";
                slot.Combo.fontSize = info.Combo >= 10 ? 36 : 30;
                float heat = Mathf.Clamp01((info.Combo - 2) / 18f);
                slot.Combo.color = Color.Lerp(new Color(1f, 0.55f, 0.15f, 1f), new Color(1f, 0.2f, 0.35f, 1f), heat);
            }
            else
            {
                slot.Combo.gameObject.SetActive(false);
            }

            _slots[i] = slot;
            Place(ref _slots[i], 0f);
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < PoolSize; i++)
            {
                if (!_slots[i].Alive)
                {
                    continue;
                }

                float age = now - _slots[i].Born;
                if (age >= Life)
                {
                    _slots[i].Alive = false;
                    _slots[i].Rt.gameObject.SetActive(false);
                    continue;
                }

                Place(ref _slots[i], age);
            }
        }

        private void Place(ref Slot slot, float age)
        {
            Camera cam = _camera;
            if (cam == null)
            {
                return;
            }

            Vector3 screen = cam.WorldToScreenPoint(slot.World);
            if (screen.z < 0.08f)
            {
                SetAlpha(ref slot, 0f);
                return;
            }

            RectTransform canvasRt = _canvas.transform as RectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local))
            {
                return;
            }

            float t = Mathf.Clamp01(age / Life);
            float rise = Mathf.Lerp(0f, 88f, 1f - Mathf.Pow(1f - t, 2f));
            float punch = age < 0.12f
                ? Mathf.Lerp(0.45f, slot.Peak, age / 0.12f)
                : Mathf.Lerp(slot.Peak, 1f, Mathf.Clamp01((age - 0.12f) / 0.16f));
            float alpha = t > 0.55f ? Mathf.Clamp01(1f - (t - 0.55f) / 0.45f) : 1f;
            slot.Rt.anchoredPosition = local + slot.Jitter + new Vector2(0f, rise);
            slot.Rt.localScale = Vector3.one * punch;
            SetAlpha(ref slot, alpha);
        }

        private static void SetAlpha(ref Slot slot, float a)
        {
            Color s = slot.Score.color;
            s.a = a;
            slot.Score.color = s;
            if (slot.Combo.gameObject.activeSelf)
            {
                Color c = slot.Combo.color;
                c.a = a;
                slot.Combo.color = c;
            }
        }

        private void BuildPool()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Popup", typeof(RectTransform));
                go.transform.SetParent(_root, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(420f, 120f);
                go.SetActive(false);

                Text score = MakeLabel(go.transform, "Score", 48, TextAnchor.LowerCenter, new Vector2(0f, 8f));
                Text combo = MakeLabel(go.transform, "Combo", 30, TextAnchor.UpperCenter, new Vector2(0f, -10f));
                _slots[i] = new Slot { Rt = rt, Score = score, Combo = combo };
            }
        }

        private static Text MakeLabel(Transform parent, string name, int size, TextAnchor align, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = pos;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }
}
