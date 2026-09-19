using UnityEngine;

namespace ShikiShiro
{
    public static class UiSprites
    {
        public static Sprite Ring { get; private set; }
        public static Sprite Knob { get; private set; }
        public static Sprite Fire { get; private set; }
        public static Sprite Reload { get; private set; }
        public static Sprite Sprint { get; private set; }
        public static Sprite Weapon { get; private set; }
        public static Sprite PistolIcon { get; private set; }
        public static Sprite SmgIcon { get; private set; }
        public static Sprite ShotgunIcon { get; private set; }
        public static Sprite Pause { get; private set; }
        public static Sprite Crosshair { get; private set; }
        public static Sprite Panel { get; private set; }
        public static Sprite White { get; private set; }
        public static Sprite Blip { get; private set; }
        public static Sprite Arrow { get; private set; }
        public static Sprite FacingCone { get; private set; }
        public static Sprite HurtVignette { get; private set; }
        public static Sprite BiteMark { get; private set; }
        public static Sprite BloodSplat { get; private set; }

        public static void Ensure()
        {
            if (Ring == null)
            {
                Ring = LoadSprite("Ui/stick_base") ?? MakeRing();
                Knob = LoadSprite("Ui/stick_knob") ?? MakeKnob();
                Fire = LoadSprite("Ui/fire_button") ?? MakeFire();
                Reload = MakeReload();
                Sprint = MakeSprint();
                Weapon = MakeWeapon();
                PistolIcon = LoadSprite("Ui/icon_pistol") ?? MakeWeapon();
                SmgIcon = LoadSprite("Ui/icon_smg") ?? MakeWeapon();
                ShotgunIcon = LoadSprite("Ui/icon_shotgun") ?? MakeWeapon();
                Pause = MakePause();
                Crosshair = MakeCrosshair();
                Panel = MakePanel();
            }

            if (White == null)
            {
                White = MakeWhite();
            }

            if (Blip == null)
            {
                Blip = MakeBlip();
            }

            if (Arrow == null)
            {
                Arrow = MakeArrow();
            }

            if (FacingCone == null)
            {
                FacingCone = MakeFacingCone();
            }

            if (HurtVignette == null)
            {
                HurtVignette = MakeHurtVignette();
            }

            if (BiteMark == null)
            {
                BiteMark = MakeBiteMark();
            }

            if (BloodSplat == null)
            {
                BloodSplat = MakeBloodSplat();
            }
        }

        private static Sprite MakeRing()
        {
            const int s = 256;
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float n = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Band(n, 0.7f, 0.76f, 0.9f, 0.99f) * 0.88f;
                    float rim = Band(n, 0.86f, 0.9f, 0.93f, 0.99f);
                    var col = Color.Lerp(new Color(0.08f, 0.1f, 0.12f, a), new Color(0.92f, 0.95f, 1f, a), rim);
                    px[y * s + x] = col;
                }
            }

            return ToSprite(px, s, s);
        }

        private static Sprite MakeKnob()
        {
            const int s = 128;
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Vector2 p = new Vector2(x - c, y - c) / c;
                    float n = p.magnitude;
                    float a = 1f - Mathf.SmoothStep(0.86f, 0.99f, n);
                    float hi = Mathf.Clamp01(1.1f - Vector2.Distance(p, new Vector2(-0.22f, 0.28f)));
                    var col = Color.Lerp(new Color(0.18f, 0.2f, 0.24f, a), new Color(0.85f, 0.9f, 0.95f, a), hi * 0.65f);
                    px[y * s + x] = col;
                }
            }

            return ToSprite(px, s, s);
        }

        public static Sprite ForWeapon(WeaponId id)
        {
            return id switch
            {
                WeaponId.Smg => SmgIcon,
                WeaponId.Shotgun => ShotgunIcon,
                _ => PistolIcon
            };
        }

        private static Sprite LoadSprite(string path)
        {
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite MakeFire()
        {
            return MakeRoundButton(new Color(0.72f, 0.12f, 0.1f), DrawCrosshairIcon);
        }

        private static Sprite MakeReload()
        {
            return MakeRoundButton(new Color(0.16f, 0.22f, 0.28f), DrawReloadIcon);
        }

        private static Sprite MakeSprint()
        {
            return MakeRoundButton(new Color(0.14f, 0.28f, 0.2f), DrawSprintIcon);
        }

        private static Sprite MakeWeapon()
        {
            return MakeRoundButton(new Color(0.18f, 0.16f, 0.12f), DrawWeaponIcon);
        }

        private static Sprite MakePause()
        {
            return MakeRoundButton(new Color(0.12f, 0.12f, 0.14f), DrawPauseIcon);
        }

        private static Sprite MakeCrosshair()
        {
            const int s = 128;
            var px = new Color[s * s];
            int m = s / 2;
            DrawCrosshairArm(px, s, m, true, 5, new Color(0f, 0f, 0f, 0.85f));
            DrawCrosshairArm(px, s, m, false, 5, new Color(0f, 0f, 0f, 0.85f));
            DrawCrosshairArm(px, s, m, true, 2, new Color(1f, 0.95f, 0.2f, 1f));
            DrawCrosshairArm(px, s, m, false, 2, new Color(1f, 0.95f, 0.2f, 1f));
            Stamp(px, s, m, m, 4, new Color(0f, 0f, 0f, 0.9f));
            Stamp(px, s, m, m, 2, new Color(1f, 0.2f, 0.12f, 1f));
            return ToSprite(px, s, s);
        }

        private static void DrawCrosshairArm(Color[] px, int s, int m, bool horizontal, int thickness, Color color)
        {
            const int inner = 10;
            const int outer = 46;
            for (int i = inner; i <= outer; i++)
            {
                Stamp(px, s, horizontal ? m + i : m, horizontal ? m : m + i, thickness, color);
                Stamp(px, s, horizontal ? m - i : m, horizontal ? m : m - i, thickness, color);
            }
        }

        private static Sprite MakeWhite()
        {
            var px = new[] { Color.white };
            return ToSprite(px, 1, 1);
        }

        private static Sprite MakePanel()
        {
            const int s = 64;
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float edge = Mathf.Min(x, y, s - 1 - x, s - 1 - y) / 8f;
                    float a = Mathf.Clamp01(edge) * 0.72f;
                    px[y * s + x] = new Color(0.05f, 0.06f, 0.07f, a);
                }
            }

            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        }

        private static Sprite MakeBlip()
        {
            const int s = 32;
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float n = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = 1f - Mathf.SmoothStep(0.72f, 1f, n);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            }

            return ToSprite(px, s, s);
        }

        private static Sprite MakeArrow()
        {
            const int s = 48;
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float nx = (x / (float)(s - 1)) * 2f - 1f;
                    float ny = (y / (float)(s - 1)) * 2f - 1f;
                    bool inside = ny > -0.7f && ny < 0.85f && Mathf.Abs(nx) < (0.82f - ny) * 0.62f;
                    px[y * s + x] = inside ? Color.white : Color.clear;
                }
            }

            return ToSprite(px, s, s);
        }

        private static Sprite MakeFacingCone()
        {
            const int s = 256;
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            const float halfFov = 42f * Mathf.Deg2Rad;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / c;
                    if (dist > 0.98f || dist < 0.02f)
                    {
                        continue;
                    }

                    float ang = Mathf.Atan2(dx, dy);
                    float a = Mathf.Abs(ang);
                    if (a > halfFov)
                    {
                        continue;
                    }

                    float edge = 1f - Mathf.InverseLerp(halfFov * 0.82f, halfFov, a);
                    float fall = 1f - dist;
                    float alpha = Mathf.Clamp01(fall * 0.55f + edge * 0.35f) * 0.85f;
                    px[y * s + x] = new Color(0.25f, 1f, 0.45f, alpha);
                }
            }

            return ToSprite(px, s, s);
        }

        private static Sprite MakeHurtVignette()
        {
            const int s = 256;
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float n = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Mathf.SmoothStep(0.28f, 1.02f, n);
                    a = a * a * 0.95f;
                    px[y * s + x] = new Color(0.45f, 0.0f, 0.0f, a);
                }
            }

            return ToSprite(px, s, s);
        }

        private static Sprite MakeBiteMark()
        {
            const int s = 256;
            var px = new Color[s * s];
            DrawJaw(px, s, 0.62f, -1f);
            DrawJaw(px, s, 0.38f, 1f);
            return ToSprite(px, s, s);
        }

        private static void DrawJaw(Color[] px, int s, float yNorm, float pointDir)
        {
            const int teeth = 9;
            for (int i = 0; i < teeth; i++)
            {
                float t = (i + 0.5f) / teeth;
                float x = Mathf.Lerp(0.18f, 0.82f, t) * (s - 1);
                float y = yNorm * (s - 1);
                float w = s * 0.042f;
                float h = s * (0.11f + 0.03f * Mathf.Sin(i * 1.7f));
                DrawTooth(px, s, x, y, w, h * pointDir, new Color(0.22f, 0.0f, 0.0f, 0.95f));
            }

            int gy = Mathf.RoundToInt(yNorm * (s - 1));
            for (int x = Mathf.RoundToInt(s * 0.16f); x < Mathf.RoundToInt(s * 0.84f); x++)
            {
                for (int t = -3; t <= 3; t++)
                {
                    Set(px, s, x, gy + t, new Color(0.18f, 0.0f, 0.0f, 0.7f));
                }
            }
        }

        private static void DrawTooth(Color[] px, int s, float cx, float cy, float halfW, float height, Color col)
        {
            int steps = Mathf.Max(8, Mathf.RoundToInt(Mathf.Abs(height)));
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                float y = cy + height * u;
                float w = halfW * (1f - u);
                int y0 = Mathf.RoundToInt(y);
                int x0 = Mathf.RoundToInt(cx - w);
                int x1 = Mathf.RoundToInt(cx + w);
                for (int x = x0; x <= x1; x++)
                {
                    Set(px, s, x, y0, col);
                    Set(px, s, x, y0 + 1, col);
                }
            }
        }

        private static Sprite MakeBloodSplat()
        {
            const int s = 128;
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            DrawBlob(px, s, c, c, 0.38f * s, new Color(0.38f, 0.0f, 0.0f, 0.95f));
            DrawBlob(px, s, c + 18f, c - 10f, 0.18f * s, new Color(0.32f, 0.0f, 0.0f, 0.85f));
            DrawBlob(px, s, c - 16f, c + 8f, 0.16f * s, new Color(0.28f, 0.0f, 0.0f, 0.8f));
            DrawBlob(px, s, c + 8f, c + 22f, 0.11f * s, new Color(0.4f, 0.02f, 0.02f, 0.75f));
            DrawBlob(px, s, c - 22f, c - 14f, 0.1f * s, new Color(0.3f, 0.0f, 0.0f, 0.7f));
            return ToSprite(px, s, s);
        }

        private static void DrawBlob(Color[] px, int s, float cx, float cy, float radius, Color col)
        {
            int r = Mathf.CeilToInt(radius);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r));
            int x1 = Mathf.Min(s - 1, Mathf.CeilToInt(cx + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r));
            int y1 = Mathf.Min(s - 1, Mathf.CeilToInt(cy + r));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / radius;
                    float dy = (y - cy) / radius;
                    float n = dx * dx * 0.85f + dy * dy * 1.15f + 0.12f * Mathf.Sin(x * 0.35f + y * 0.22f);
                    if (n < 1f)
                    {
                        Color d = col;
                        d.a *= 1f - n;
                        Set(px, s, x, y, d);
                    }
                }
            }
        }

        private delegate void IconDraw(Color[] px, int s, Color ink);

        private static Sprite MakeRoundButton(Color fill, IconDraw icon)
        {
            const int s = 256;
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float n = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = 1f - Mathf.SmoothStep(0.88f, 0.99f, n);
                    float rim = Band(n, 0.82f, 0.88f, 0.93f, 0.99f);
                    Color col = Color.Lerp(fill, Color.white, rim * 0.35f);
                    col.a = a;
                    px[y * s + x] = col;
                }
            }

            icon(px, s, Color.white);
            return ToSprite(px, s, s);
        }

        private static void DrawCrosshairIcon(Color[] px, int s, Color ink)
        {
            int m = s / 2;
            for (int i = s / 5; i < s - s / 5; i++)
            {
                if (Mathf.Abs(i - m) < 18)
                {
                    continue;
                }

                Stamp(px, s, m, i, 3, ink);
                Stamp(px, s, i, m, 3, ink);
            }
        }

        private static void DrawReloadIcon(Color[] px, int s, Color ink)
        {
            float c = (s - 1) * 0.5f;
            float r = s * 0.28f;
            for (int i = 20; i < 300; i++)
            {
                float a = i * Mathf.Deg2Rad;
                int x = Mathf.RoundToInt(c + Mathf.Cos(a) * r);
                int y = Mathf.RoundToInt(c + Mathf.Sin(a) * r);
                Stamp(px, s, x, y, 4, ink);
            }

            Stamp(px, s, Mathf.RoundToInt(c + r), Mathf.RoundToInt(c), 10, ink);
        }

        private static void DrawSprintIcon(Color[] px, int s, Color ink)
        {
            int m = s / 2;
            for (int i = 0; i < 3; i++)
            {
                int ox = m - 40 + i * 28;
                DrawChevron(px, s, ox, m, ink);
            }
        }

        private static void DrawChevron(Color[] px, int s, int ox, int oy, Color ink)
        {
            for (int i = -22; i <= 22; i++)
            {
                Stamp(px, s, ox + Mathf.Abs(i) / 2, oy + i, 3, ink);
            }
        }

        private static void DrawWeaponIcon(Color[] px, int s, Color ink)
        {
            int m = s / 2;
            for (int x = m - 70; x < m + 78; x++)
            {
                Stamp(px, s, x, m, 6, ink);
            }

            for (int y = m - 18; y < m + 8; y++)
            {
                Stamp(px, s, m - 18, y, 5, ink);
            }

            Stamp(px, s, m + 70, m, 8, ink);
        }

        private static void DrawPauseIcon(Color[] px, int s, Color ink)
        {
            int m = s / 2;
            for (int y = m - 36; y <= m + 36; y++)
            {
                Stamp(px, s, m - 16, y, 6, ink);
                Stamp(px, s, m + 16, y, 6, ink);
            }
        }

        private static void Stamp(Color[] px, int s, int x, int y, int r, Color ink)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r)
                    {
                        continue;
                    }

                    Set(px, s, x + dx, y + dy, ink);
                }
            }
        }

        private static void Set(Color[] px, int s, int x, int y, Color col)
        {
            if ((uint)x >= (uint)s || (uint)y >= (uint)s)
            {
                return;
            }

            Color d = px[y * s + x];
            px[y * s + x] = Color.Lerp(d, col, col.a);
            var m = px[y * s + x];
            m.a = Mathf.Max(d.a, col.a);
            px[y * s + x] = m;
        }

        private static float Band(float n, float in0, float in1, float out0, float out1)
        {
            return Mathf.SmoothStep(in0, in1, n) * (1f - Mathf.SmoothStep(out0, out1, n));
        }

        private static Sprite ToSprite(Color[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
