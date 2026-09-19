using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShikiShiro
{
    public sealed class CombatFx : MonoBehaviour
    {
        private const int TracerCount = 40;

        private readonly TracerGhost[] _tracers = new TracerGhost[TracerCount];
        private ParticleSystem _muzzleBurst;
        private ParticleSystem _muzzleFlare;
        private ParticleSystem _sparks;
        private ParticleSystem _smoke;
        private ParticleSystem _blood;
        private ParticleSystem _bloodSpray;
        private ParticleSystem _bloodMist;
        private ParticleSystem _explodeFire;
        private ParticleSystem _explodeFlash;
        private ParticleSystem _explodeSmoke;
        private ParticleSystem _explodeShock;
        private ParticleSystem _explodeGibs;
        private ParticleSystem _explodeEmbers;
        private Coroutine _hitstop;
        private Light _muzzleLight;
        private Light _hitLight;
        private float _muzzleLightUntil;
        private float _hitLightUntil;
        private float _hitPeak;
        private Texture2D _glow;
        private int _cursor;

        private struct TracerGhost
        {
            public LineRenderer Core;
            public LineRenderer Bloom;
            public float DieAt;
        }

        public void Initialize()
        {
            _glow = MakeGlow();
            var root = new GameObject("FxPool").transform;
            root.SetParent(transform, false);

            Material additive = MakeParticleMaterial(true);
            Material alpha = MakeParticleMaterial(false);

            _muzzleBurst = BuildParticles(root, "MuzzleBurst", additive, 48, ParticleSystemRenderMode.Billboard);
            ConfigureBurst(_muzzleBurst, cone: 18f, speed: 9f, size: 0.07f, life: 0.07f, gravity: 0f, new Color(1f, 0.92f, 0.55f, 1f));
            _muzzleFlare = BuildParticles(root, "MuzzleFlare", additive, 8, ParticleSystemRenderMode.Billboard);
            ConfigureBurst(_muzzleFlare, cone: 0f, speed: 0.05f, size: 0.42f, life: 0.045f, gravity: 0f, new Color(1f, 0.78f, 0.28f, 1f));

            _sparks = BuildParticles(root, "Sparks", additive, 96, ParticleSystemRenderMode.Stretch);
            var sparkRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.lengthScale = 2.4f;
            sparkRenderer.velocityScale = 0.12f;
            ConfigureBurst(_sparks, cone: 42f, speed: 11f, size: 0.035f, life: 0.18f, gravity: 1.6f, new Color(1f, 0.86f, 0.45f, 1f));

            _smoke = BuildParticles(root, "Smoke", alpha, 48, ParticleSystemRenderMode.Billboard);
            ConfigureBurst(_smoke, cone: 28f, speed: 1.1f, size: 0.22f, life: 0.55f, gravity: -0.15f, new Color(0.55f, 0.53f, 0.48f, 0.45f));

            _blood = BuildParticles(root, "Blood", alpha, 160, ParticleSystemRenderMode.Billboard);
            ConfigureBurst(_blood, cone: 55f, speed: 7.5f, size: 0.09f, life: 0.55f, gravity: 2.6f, new Color(0.55f, 0.02f, 0.02f, 0.95f));
            _bloodSpray = BuildParticles(root, "BloodSpray", alpha, 120, ParticleSystemRenderMode.Stretch);
            var sprayR = _bloodSpray.GetComponent<ParticleSystemRenderer>();
            sprayR.lengthScale = 3.2f;
            sprayR.velocityScale = 0.18f;
            ConfigureBurst(_bloodSpray, cone: 28f, speed: 14f, size: 0.05f, life: 0.32f, gravity: 1.8f, new Color(0.7f, 0.0f, 0.0f, 1f));
            _bloodMist = BuildParticles(root, "BloodMist", alpha, 64, ParticleSystemRenderMode.Billboard);
            ConfigureBurst(_bloodMist, cone: 70f, speed: 2.2f, size: 0.35f, life: 0.7f, gravity: 0.4f, new Color(0.35f, 0.0f, 0.0f, 0.55f));
            _explodeFire = BuildParticles(root, "ExplodeFire", additive, 420, ParticleSystemRenderMode.Billboard);
            ConfigureBurst(_explodeFire, cone: 90f, speed: 14f, size: 0.55f, life: 0.42f, gravity: -0.8f, new Color(1f, 0.42f, 0.05f, 1f));
            _explodeFlash = BuildParticles(root, "ExplodeFlash", additive, 48, ParticleSystemRenderMode.Billboard);
            ConfigureBurst(_explodeFlash, cone: 0f, speed: 0.15f, size: 2.4f, life: 0.1f, gravity: 0f, new Color(1f, 0.92f, 0.55f, 1f));
            _explodeSmoke = BuildParticles(root, "ExplodeSmoke", alpha, 220, ParticleSystemRenderMode.Billboard);
            ConfigureBurst(_explodeSmoke, cone: 75f, speed: 3.6f, size: 1.15f, life: 1.15f, gravity: -0.45f, new Color(0.16f, 0.1f, 0.08f, 0.72f));
            _explodeShock = BuildParticles(root, "ExplodeShock", additive, 8, ParticleSystemRenderMode.Billboard);
            ConfigureShock(_explodeShock, new Color(1f, 0.7f, 0.25f, 0.85f));
            _explodeGibs = BuildParticles(root, "ExplodeGibs", alpha, 180, ParticleSystemRenderMode.Stretch);
            var gibR = _explodeGibs.GetComponent<ParticleSystemRenderer>();
            gibR.lengthScale = 2.8f;
            gibR.velocityScale = 0.16f;
            ConfigureBurst(_explodeGibs, cone: 70f, speed: 16f, size: 0.09f, life: 0.55f, gravity: 3.4f, new Color(0.55f, 0.04f, 0.02f, 1f));
            _explodeEmbers = BuildParticles(root, "ExplodeEmbers", additive, 220, ParticleSystemRenderMode.Stretch);
            var emberR = _explodeEmbers.GetComponent<ParticleSystemRenderer>();
            emberR.lengthScale = 3.6f;
            emberR.velocityScale = 0.2f;
            ConfigureBurst(_explodeEmbers, cone: 85f, speed: 18f, size: 0.07f, life: 0.38f, gravity: 1.2f, new Color(1f, 0.55f, 0.12f, 1f));

            var hitLightGo = new GameObject("HitLight");
            hitLightGo.transform.SetParent(root, false);
            _hitLight = hitLightGo.AddComponent<Light>();
            _hitLight.type = LightType.Point;
            _hitLight.range = 14f;
            _hitLight.shadows = LightShadows.None;
            _hitLight.enabled = false;

            var lightGo = new GameObject("MuzzleLight");
            lightGo.transform.SetParent(root, false);
            _muzzleLight = lightGo.AddComponent<Light>();
            _muzzleLight.type = LightType.Point;
            _muzzleLight.range = 5.5f;
            _muzzleLight.color = new Color(1f, 0.72f, 0.28f);
            _muzzleLight.intensity = 0f;
            _muzzleLight.shadows = LightShadows.None;
            _muzzleLight.enabled = false;

            for (int i = 0; i < TracerCount; i++)
            {
                _tracers[i] = new TracerGhost
                {
                    Core = MakeTracerLine(root, "TracerCore", additive, 0.018f, new Color(1f, 0.98f, 0.85f, 1f)),
                    Bloom = MakeTracerLine(root, "TracerBloom", additive, 0.07f, new Color(1f, 0.55f, 0.12f, 0.35f))
                };
            }
        }

        public void MuzzleFlash(Vector3 position, Vector3 forward)
        {
            Quaternion rot = Quaternion.LookRotation(forward);
            Emit(_muzzleBurst, position, rot, 22);
            Emit(_muzzleFlare, position + forward * 0.04f, rot, 2);
            _muzzleLight.transform.position = position + forward * 0.06f;
            _muzzleLight.enabled = true;
            _muzzleLight.intensity = 7.5f;
            _muzzleLightUntil = Time.time + 0.06f;
        }

        public void Impact(Vector3 point, Vector3 normal)
        {
            Quaternion rot = Quaternion.LookRotation(normal);
            Emit(_sparks, point + normal * 0.03f, rot, 28);
            Emit(_smoke, point + normal * 0.02f, rot, 10);
            PulseHitLight(point, new Color(1f, 0.75f, 0.3f), 4.5f, 0.07f);
        }

        public void Blood(Vector3 point, Vector3 direction, bool headshot)
        {
            Vector3 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.up;
            Quaternion rot = Quaternion.LookRotation(dir);
            Emit(_bloodSpray, point, rot, headshot ? 48 : 28);
            Emit(_blood, point, rot, headshot ? 36 : 22);
            Emit(_bloodMist, point, rot, headshot ? 18 : 10);
            PulseHitLight(point, new Color(0.7f, 0.05f, 0.02f), headshot ? 8f : 4.5f, 0.09f);
            if (headshot)
            {
                Explosion(point, dir);
            }
        }

        public void PlayerWound(Vector3 point, Vector3 incoming)
        {
            Vector3 dir = incoming.sqrMagnitude > 0.01f ? -incoming.normalized : Vector3.up;
            dir = (dir + Vector3.up * 0.25f).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            Vector3 origin = point + dir * 0.18f;
            Emit(_bloodSpray, origin, rot, 22);
            Emit(_blood, origin, rot, 16);
            Emit(_bloodMist, origin, rot, 8);
            PulseHitLight(origin, new Color(0.55f, 0.02f, 0.02f), 3.2f, 0.08f);
        }

        public void Explosion(Vector3 point, Vector3 direction, float scale = 1f)
        {
            float s = Mathf.Max(0.5f, scale);
            Quaternion rot = Quaternion.LookRotation(direction.sqrMagnitude > 0.01f ? direction : Vector3.up);
            Emit(_explodeFlash, point, rot, Mathf.RoundToInt(8f * s));
            Emit(_explodeShock, point, Quaternion.identity, 2);
            Emit(_explodeFire, point, rot, Mathf.RoundToInt(90f * s));
            Emit(_explodeSmoke, point, rot, Mathf.RoundToInt(36f * s));
            Emit(_explodeEmbers, point, rot, Mathf.RoundToInt(70f * s));
            Emit(_explodeGibs, point, rot, Mathf.RoundToInt(48f * s));
            Emit(_blood, point, rot, Mathf.RoundToInt(28f * s));
            Emit(_bloodMist, point, rot, Mathf.RoundToInt(16f * s));
            Emit(_sparks, point, rot, Mathf.RoundToInt(64f * s));
            PulseHitLight(point, new Color(1f, 0.48f, 0.1f), 22f * s, 0.22f);
        }

        public void KillPunch(float scale)
        {
            if (Time.timeScale <= 0.001f)
            {
                return;
            }
            TpsCamera cam = Camera.main != null ? Camera.main.GetComponent<TpsCamera>() : null;
            cam?.Shake(0.32f * scale, 0.28f);
            if (_hitstop != null)
            {
                StopCoroutine(_hitstop);
            }

            _hitstop = StartCoroutine(Hitstop(0.055f, 0.08f));
        }

        public void Tracer(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.04f)
            {
                return;
            }

            int i = _cursor++ % TracerCount;
            TracerGhost g = _tracers[i];
            Vector3[] pts = { from, to };
            ApplyLine(g.Core, pts, 0.022f, 0.006f);
            ApplyLine(g.Bloom, pts, 0.09f, 0.02f);
            g.DieAt = Time.time + 0.08f;
            _tracers[i] = g;
        }

        private void Update()
        {
            float now = Time.time;
            if (_muzzleLight.enabled)
            {
                float t = Mathf.InverseLerp(_muzzleLightUntil, _muzzleLightUntil - 0.06f, now);
                _muzzleLight.intensity = 7.5f * Mathf.Clamp01(t);
                if (now >= _muzzleLightUntil)
                {
                    _muzzleLight.enabled = false;
                    _muzzleLight.intensity = 0f;
                }
            }

            if (_hitLight.enabled)
            {
                float t = Mathf.InverseLerp(_hitLightUntil, _hitLightUntil - 0.12f, now);
                _hitLight.intensity = _hitPeak * Mathf.Clamp01(t);
                if (now >= _hitLightUntil)
                {
                    _hitLight.enabled = false;
                }
            }

            for (int i = 0; i < TracerCount; i++)
            {
                TracerGhost g = _tracers[i];
                if (g.DieAt <= 0f)
                {
                    continue;
                }

                float remain = g.DieAt - now;
                if (remain <= 0f)
                {
                    g.Core.enabled = false;
                    g.Bloom.enabled = false;
                    g.DieAt = 0f;
                    _tracers[i] = g;
                    continue;
                }

                float a = Mathf.Clamp01(remain / 0.08f);
                SetLineAlpha(g.Core, a);
                SetLineAlpha(g.Bloom, a * 0.45f);
            }
        }

        private static void ApplyLine(LineRenderer lr, Vector3[] pts, float startWidth, float endWidth)
        {
            lr.enabled = true;
            lr.positionCount = 2;
            lr.SetPositions(pts);
            lr.startWidth = startWidth;
            lr.endWidth = endWidth;
            lr.startColor = lr.startColor;
            Color c0 = lr.startColor;
            Color c1 = lr.endColor;
            c0.a = 1f;
            c1.a = 0.35f;
            lr.startColor = c0;
            lr.endColor = c1;
        }

        private static void SetLineAlpha(LineRenderer lr, float a)
        {
            Color s = lr.startColor;
            Color e = lr.endColor;
            s.a = a;
            e.a = a * 0.25f;
            lr.startColor = s;
            lr.endColor = e;
        }

        private static void Emit(ParticleSystem ps, Vector3 pos, Quaternion rot, int count)
        {
            if (ps == null)
            {
                return;
            }

            ps.transform.SetPositionAndRotation(pos, rot);
            ps.Emit(count);
        }

        private void PulseHitLight(Vector3 point, Color color, float intensity, float life)
        {
            _hitLight.transform.position = point;
            _hitLight.color = color;
            _hitLight.intensity = intensity;
            _hitPeak = intensity;
            _hitLight.enabled = true;
            _hitLightUntil = Time.time + life;
        }

        private static ParticleSystem BuildParticles(Transform parent, string name, Material mat, int max, ParticleSystemRenderMode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = mode;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var emission = ps.emission;
            emission.enabled = false;
            return ps;
        }

        private static void ConfigureBurst(ParticleSystem ps, float cone, float speed, float size, float life, float gravity, Color color)
        {
            var main = ps.main;
            main.startLifetime = life;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = cone;
            shape.radius = 0.01f;
            shape.radiusThickness = 1f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var sizeMod = ps.sizeOverLifetime;
            sizeMod.enabled = true;
            sizeMod.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));
        }

        private static void ConfigureShock(ParticleSystem ps, Color color)
        {
            var main = ps.main;
            main.startLifetime = 0.28f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startColor = color;
            main.gravityModifier = 0f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sizeMod = ps.sizeOverLifetime;
            sizeMod.enabled = true;
            sizeMod.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.2f, 1f, 6.5f));
        }

        private IEnumerator Hitstop(float duration, float scale)
        {
            if (Time.timeScale <= 0.001f)
            {
                _hitstop = null;
                yield break;
            }

            float previous = Time.timeScale;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(duration);
            if (Time.timeScale <= 0.001f)
            {
                _hitstop = null;
                yield break;
            }

            Time.timeScale = previous <= 0.001f ? 1f : previous;
            _hitstop = null;
        }

        private static LineRenderer MakeTracerLine(Transform parent, string name, Material mat, float width, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.startWidth = width;
            lr.endWidth = width * 0.35f;
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0.2f);
            lr.enabled = false;
            return lr;
        }

        private Material MakeParticleMaterial(bool additive)
        {
            Shader shader = Shader.Find(additive ? "Mobile/Particles/Additive" : "Mobile/Particles/Alpha Blended");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var material = new Material(shader)
            {
                mainTexture = _glow,
                color = Color.white,
                renderQueue = 3000
            };
            if (material.HasProperty("_ColorMode"))
            {
                material.SetFloat("_ColorMode", additive ? 1f : 0f);
            }

            return material;
        }

        private static Texture2D MakeGlow()
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float n = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Mathf.Clamp01(1f - n);
                    a *= a;
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
