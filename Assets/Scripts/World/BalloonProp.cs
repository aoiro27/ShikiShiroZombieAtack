using UnityEngine;

namespace ShikiShiro
{
    public sealed class BalloonProp : MonoBehaviour, IDamageable
    {
        private HordeDirector _horde;
        private CombatFx _fx;
        private ProceduralSfx _sfx;
        private SpriteRenderer _sprite;
        private Collider _hitbox;
        private Rigidbody _body;
        private Camera _cam;
        private Vector3 _anchor;
        private float _phase;
        private float _bobSpeed;
        private float _bobAmp;
        private float _spin;

        public bool IsAlive { get; private set; }

        public void Setup(Sprite sprite, Vector3 position, HordeDirector horde, CombatFx fx, ProceduralSfx sfx)
        {
            _horde = horde;
            _fx = fx;
            _sfx = sfx;
            _anchor = position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _bobSpeed = Random.Range(0.55f, 1.15f);
            _bobAmp = Random.Range(0.18f, 0.42f);
            _spin = Random.Range(-12f, 12f);
            _cam = Camera.main;
            transform.position = position;
            ApplySprite(sprite);
            IsAlive = true;
            gameObject.SetActive(true);
            if (_hitbox != null)
            {
                _hitbox.enabled = true;
            }
        }

        private void ApplySprite(Sprite sprite)
        {
            if (_sprite == null)
            {
                var visual = new GameObject("Sprite");
                visual.transform.SetParent(transform, false);
                _sprite = visual.AddComponent<SpriteRenderer>();
                _sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _sprite.receiveShadows = false;
                var hit = gameObject.AddComponent<SphereCollider>();
                hit.isTrigger = true;
                hit.radius = 0.58f;
                hit.center = Vector3.up * 0.15f;
                _hitbox = hit;
                _body = gameObject.AddComponent<Rigidbody>();
                _body.isKinematic = true;
                _body.useGravity = false;
                _body.interpolation = RigidbodyInterpolation.Interpolate;
                int zombie = LayerMask.NameToLayer("Zombie");
                GameAssets.SetLayerRecursively(gameObject, zombie >= 0 ? zombie : 0);
            }

            _sprite.sprite = sprite;
            float height = sprite != null ? sprite.bounds.size.y : 1.6f;
            float target = Random.Range(1.45f, 2.15f);
            float scale = target / Mathf.Max(0.2f, height);
            transform.localScale = Vector3.one * scale;
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                return;
            }

            Vector3 pos = _anchor;
            pos.x += Mathf.Sin(Time.time * 0.21f + _phase) * 0.35f;
            pos.z += Mathf.Cos(Time.time * 0.17f + _phase) * 0.35f;
            pos.y += Mathf.Sin(Time.time * _bobSpeed + _phase) * _bobAmp;
            Quaternion rot = BillboardRotation(pos);
            if (_body != null)
            {
                _body.MovePosition(pos);
                _body.MoveRotation(rot);
            }
            else
            {
                transform.SetPositionAndRotation(pos, rot);
            }
        }

        private Quaternion BillboardRotation(Vector3 pos)
        {
            if (_cam == null)
            {
                _cam = Camera.main;
            }

            if (_cam == null)
            {
                return transform.rotation;
            }

            Vector3 toCam = _cam.transform.position - pos;
            if (toCam.sqrMagnitude < 0.001f)
            {
                return transform.rotation;
            }

            return Quaternion.LookRotation(toCam.normalized, Vector3.up)
                * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 0.8f + _phase) * _spin);
        }

        public void ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            if (_hitbox != null)
            {
                _hitbox.enabled = false;
            }

            Vector3 boom = transform.position;
            Vector3 dir = info.Direction.sqrMagnitude > 0.01f ? info.Direction : Vector3.up;
            _fx?.Explosion(boom, dir, info.ChainDepth > 0 ? 0.75f : 0.95f);
            _sfx?.PlayBalloonBurst();
            _horde?.ChainBurst(boom, info);
            gameObject.SetActive(false);
        }
    }
}
