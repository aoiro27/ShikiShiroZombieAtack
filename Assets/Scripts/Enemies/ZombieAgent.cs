using UnityEngine;

namespace ShikiShiro
{
    public sealed class ZombieAgent : MonoBehaviour, IDamageable
    {
        public bool IsAlive { get; private set; }
        public ZombieKind Kind { get; private set; }

        private CharacterController _controller;
        private Transform _visual;
        private Transform _target;
        private GameSession _session;
        private CombatFx _fx;
        private ProceduralSfx _sfx;
        private HordeDirector _horde;
        private float _health;
        private float _maxHealth;
        private float _moveSpeed;
        private float _damage;
        private float _attackRange;
        private float _attackCooldown;
        private float _nextAttack;
        private float _stagger;
        private float _despawnAt;
        private float _bob;
        private Color _baseColor;
        private MeshRenderer[] _renderers;

        public void BuildVisual()
        {
            _controller = gameObject.AddComponent<CharacterController>();
            _controller.height = 1.8f;
            _controller.radius = 0.38f;
            _controller.center = new Vector3(0f, 0.9f, 0f);
            _controller.minMoveDistance = 0f;
            gameObject.layer = LayerMask.NameToLayer("Zombie");
            gameObject.tag = "Zombie";

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(_visual, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            Destroy(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(_visual, false);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0.05f);
            head.transform.localScale = Vector3.one * 0.42f;
            Destroy(head.GetComponent<Collider>());

            var jaw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jaw.name = "Jaw";
            jaw.transform.SetParent(_visual, false);
            jaw.transform.localPosition = new Vector3(0f, 1.55f, 0.18f);
            jaw.transform.localScale = new Vector3(0.22f, 0.08f, 0.16f);
            Destroy(jaw.GetComponent<Collider>());

            _renderers = _visual.GetComponentsInChildren<MeshRenderer>();
        }

        public void Spawn(ZombieKind kind, Vector3 position, Transform target, GameSession session, CombatFx fx, ProceduralSfx sfx, HordeDirector horde)
        {
            Kind = kind;
            _target = target;
            _session = session;
            _fx = fx;
            _sfx = sfx;
            _horde = horde;
            transform.position = position + Vector3.up * 0.05f;
            IsAlive = true;
            _stagger = 0f;
            _despawnAt = 0f;
            _controller.enabled = true;
            _visual.localScale = Vector3.one;
            ApplyKind(kind);
            gameObject.SetActive(true);
        }

        public void ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive)
            {
                return;
            }

            _health -= info.Amount;
            _fx.Blood(info.Point);
            _stagger = info.IsHeadshot ? 0.35f : 0.12f;
            Flash(info.IsHeadshot ? Color.white : new Color(1f, 0.4f, 0.4f));
            if (_health <= 0f)
            {
                Die(info.IsHeadshot);
            }
        }

        private void Update()
        {
            if (!IsAlive)
            {
                if (_despawnAt > 0f && Time.time >= _despawnAt)
                {
                    _horde.Despawn(this);
                }

                return;
            }

            if (_target == null)
            {
                return;
            }

            _bob += Time.deltaTime * (_moveSpeed * 2.2f);
            _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bob)) * 0.05f, 0f);
            _visual.localRotation = Quaternion.Euler(8f, 0f, Mathf.Sin(_bob) * 4f);

            if (_stagger > 0f)
            {
                _stagger -= Time.deltaTime;
                return;
            }

            Vector3 to = _target.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > 0.05f)
            {
                Vector3 dir = to / dist;
                AvoidNeighbors(ref dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
                Vector3 motion = dir * _moveSpeed + Vector3.down * 8f;
                _controller.Move(motion * Time.deltaTime);
            }

            if (dist <= _attackRange && Time.time >= _nextAttack)
            {
                _nextAttack = Time.time + _attackCooldown;
                var vitality = _target.GetComponent<PlayerVitality>();
                if (vitality != null && vitality.IsAlive)
                {
                    vitality.ApplyDamage(new DamageInfo(_damage, _target.position + Vector3.up, transform.forward, false, WeaponId.Pistol));
                    _sfx.PlayHit();
                }
            }
        }

        private void AvoidNeighbors(ref Vector3 dir)
        {
            Vector3 sep = Vector3.zero;
            Collider[] hits = Physics.OverlapSphere(transform.position, 1.1f, LayerMask.GetMask("Zombie"));
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform == transform)
                {
                    continue;
                }

                Vector3 away = transform.position - hits[i].transform.position;
                away.y = 0f;
                float mag = away.magnitude;
                if (mag > 0.01f)
                {
                    sep += away / mag;
                }
            }

            if (sep.sqrMagnitude > 0.01f)
            {
                dir = (dir + sep.normalized * 0.45f).normalized;
            }
        }

        private void Die(bool headshot)
        {
            IsAlive = false;
            _controller.enabled = false;
            _despawnAt = Time.time + 2.4f;
            _visual.localRotation = Quaternion.Euler(-80f, 0f, 0f);
            _visual.localPosition = new Vector3(0f, 0.2f, 0f);
            _session.RegisterKill(Kind, headshot);
            _horde.NotifyKilled(this);
            if (Random.value < 0.18f)
            {
                _horde.DropPickup(transform.position);
            }
        }

        private void ApplyKind(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Runner:
                    _maxHealth = 55f;
                    _moveSpeed = 5.6f;
                    _damage = 8f;
                    _attackRange = 1.35f;
                    _attackCooldown = 0.85f;
                    _baseColor = new Color(0.42f, 0.28f, 0.18f);
                    transform.localScale = new Vector3(0.9f, 0.95f, 0.9f);
                    break;
                case ZombieKind.Brute:
                    _maxHealth = 320f;
                    _moveSpeed = 2.35f;
                    _damage = 28f;
                    _attackRange = 1.7f;
                    _attackCooldown = 1.4f;
                    _baseColor = new Color(0.18f, 0.22f, 0.16f);
                    transform.localScale = new Vector3(1.35f, 1.25f, 1.35f);
                    break;
                default:
                    _maxHealth = 100f;
                    _moveSpeed = 2.9f;
                    _damage = 12f;
                    _attackRange = 1.45f;
                    _attackCooldown = 1.05f;
                    _baseColor = new Color(0.32f, 0.38f, 0.24f);
                    transform.localScale = Vector3.one;
                    break;
            }

            _health = _maxHealth;
            var mat = MaterialFactory.Create(_baseColor, 0.05f, 0.12f);
            foreach (MeshRenderer r in _renderers)
            {
                r.sharedMaterial = mat;
            }
        }

        private void Flash(Color color)
        {
            foreach (MeshRenderer r in _renderers)
            {
                r.material.color = color;
            }

            Invoke(nameof(RestoreColor), 0.08f);
        }

        private void RestoreColor()
        {
            if (_renderers == null)
            {
                return;
            }

            foreach (MeshRenderer r in _renderers)
            {
                if (r != null)
                {
                    r.material.color = _baseColor;
                }
            }
        }
    }
}
