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
        private PlayerVitality _playerVitality;
        private static Collider[] NeighborBuffer;
        private static int ZombieMask = -1;
        private float _health;
        private float _maxHealth;
        private float _moveSpeed;
        private float _damage;
        private float _attackRange;
        private float _attackCooldown;
        private float _nextAttack;
        private float _nextGroan;
        private float _stagger;
        private float _despawnAt;
        private float _bob;
        private ZombieBodyMotion _motion;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _flashBlock;
        private Texture2D _skin;
        private GameObject _walkerVisual;
        private GameObject _runnerVisual;
        private GameObject _bruteVisual;

        public void BuildVisual()
        {
            _controller = gameObject.AddComponent<CharacterController>();
            _controller.height = 1.8f;
            _controller.radius = 0.38f;
            _controller.center = new Vector3(0f, 0.9f, 0f);
            _controller.minMoveDistance = 0f;
            int zombieLayer = LayerMask.NameToLayer("Zombie");
            gameObject.layer = zombieLayer >= 0 ? zombieLayer : 0;
            try
            {
                gameObject.tag = "Zombie";
            }
            catch (UnityException)
            {
            }

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            _motion = new ZombieBodyMotion();
            GameAssets.SetLayerRecursively(gameObject, gameObject.layer);
        }

        public void Spawn(ZombieKind kind, Vector3 position, Transform target, GameSession session, CombatFx fx, ProceduralSfx sfx, HordeDirector horde)
        {
            Kind = kind;
            _target = target;
            _session = session;
            _fx = fx;
            _sfx = sfx;
            _horde = horde;
            _playerVitality = target.GetComponent<PlayerVitality>();
            _controller.enabled = false;
            transform.position = position + Vector3.up * 0.05f;
            IsAlive = true;
            _stagger = 0f;
            _despawnAt = 0f;
            _nextAttack = Time.time + 0.75f;
            _nextGroan = Time.time + Random.Range(1.2f, 4.5f);
            _controller.enabled = true;
            _visual.gameObject.SetActive(true);
            _visual.localScale = Vector3.one;
            _visual.localRotation = Quaternion.identity;
            _visual.localPosition = Vector3.zero;
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
            _fx.Blood(info.Point, info.Direction, info.IsHeadshot);
            _sfx.PlayFlesh(info.IsHeadshot);
            if (info.IsHeadshot)
            {
                _sfx.PlayBoom();
            }

            _stagger = info.IsHeadshot ? 0.45f : 0.12f;
            Flash(info.IsHeadshot ? Color.white : new Color(1f, 0.4f, 0.4f));
            if (_health <= 0f)
            {
                Die(info);
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

            _bob += Time.deltaTime * (_moveSpeed * 1.4f);
            _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bob)) * 0.03f, 0f);
            _visual.localRotation = Quaternion.identity;

            if (_stagger > 0f)
            {
                _stagger -= Time.deltaTime;
                _motion?.Tick(false, false, false, 0f);
                return;
            }

            Vector3 to = _target.position - transform.position;
            float vertical = Mathf.Abs(to.y);
            to.y = 0f;
            float dist = to.magnitude;
            if (vertical > 2.4f)
            {
                if (transform.position.y < _target.position.y - 1.5f)
                {
                    _controller.enabled = false;
                    Vector3 p = transform.position;
                    p.y = _target.position.y;
                    transform.position = p;
                    _controller.enabled = true;
                }

                return;
            }

            bool chasing = dist > _attackRange * 0.92f;
            if (chasing)
            {
                Vector3 dir = to / Mathf.Max(dist, 0.05f);
                AvoidNeighbors(ref dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
                Vector3 motion = dir * _moveSpeed + Vector3.down * 8f;
                _controller.Move(motion * Time.deltaTime);
                if (Time.time >= _nextGroan)
                {
                    _sfx.PlayGroan();
                    _nextGroan = Time.time + Random.Range(2.8f, 7.5f);
                }
            }

            bool swing = false;
            if (dist <= _attackRange && Time.time >= _nextAttack && CanSeeTarget())
            {
                _nextAttack = Time.time + _attackCooldown;
                swing = true;
                if (_playerVitality != null && _playerVitality.IsAlive)
                {
                    _playerVitality.ApplyDamage(new DamageInfo(_damage, _target.position + Vector3.up, transform.forward, false, WeaponId.Pistol));
                    _sfx.PlayHit();
                }
            }

            _motion?.Tick(chasing, swing, false, _moveSpeed);
        }

        private bool CanSeeTarget()
        {
            Vector3 from = transform.position + Vector3.up * 1.15f;
            Vector3 to = _target.position + Vector3.up * 1.15f;
            Vector3 delta = to - from;
            float len = delta.magnitude;
            if (len < 0.05f)
            {
                return true;
            }

            int mask = LayerMask.GetMask("Obstacle", "Ground", "Player");
            if (mask == 0)
            {
                mask = ~0;
            }

            if (!Physics.Raycast(from, delta / len, out RaycastHit hit, len + 0.05f, mask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider.transform == _target || hit.collider.transform.IsChildOf(_target);
        }

        private void AvoidNeighbors(ref Vector3 dir)
        {
            if (ZombieMask < 0)
            {
                ZombieMask = LayerMask.GetMask("Zombie");
            }

            if (NeighborBuffer == null)
            {
                NeighborBuffer = new Collider[16];
            }

            Vector3 sep = Vector3.zero;
            int count = Physics.OverlapSphereNonAlloc(transform.position, 1.1f, NeighborBuffer, ZombieMask);
            for (int i = 0; i < count; i++)
            {
                if (NeighborBuffer[i].transform == transform)
                {
                    continue;
                }

                Vector3 away = transform.position - NeighborBuffer[i].transform.position;
                away.y = 0f;
                float mag = away.magnitude;
                if (mag > 0.01f)
                {
                    sep += away / mag;
                }
            }

            if (sep.sqrMagnitude > 0.01f)
            {
                dir = (dir + sep.normalized * 0.18f).normalized;
            }
        }

        private void Die(in DamageInfo info)
        {
            IsAlive = false;
            _controller.enabled = false;
            CancelInvoke(nameof(RestoreColor));
            Vector3 pos = transform.position;
            Vector3 boom = pos + Vector3.up * 0.9f;
            Vector3 dir = info.Direction.sqrMagnitude > 0.01f ? info.Direction : Vector3.up;
            float scale = Kind == ZombieKind.Brute ? 1.65f : 1f;
            _fx.Explosion(boom, dir, scale);
            _fx.Blood(info.Point, dir, true);
            _sfx.PlayExplosion();
            _session.RegisterKill(Kind, info.IsHeadshot);
            _horde.NotifyKilled(this);
            if (Random.value < 0.18f)
            {
                _horde.DropPickup(pos);
            }

            _visual.gameObject.SetActive(false);
            _horde.Despawn(this);
        }

        private void ApplyKind(ZombieKind kind)
        {
            string model = GameAssets.ZombieWalker;
            switch (kind)
            {
                case ZombieKind.Runner:
                    _maxHealth = 55f;
                    _moveSpeed = 2.4f;
                    _damage = 8f;
                    _attackRange = 1.35f;
                    _attackCooldown = 0.95f;
                    transform.localScale = new Vector3(0.92f, 0.95f, 0.92f);
                    model = GameAssets.ZombieRunner;
                    break;
                case ZombieKind.Brute:
                    _maxHealth = 320f;
                    _moveSpeed = 1.05f;
                    _damage = 28f;
                    _attackRange = 1.7f;
                    _attackCooldown = 1.55f;
                    transform.localScale = new Vector3(1.25f, 1.2f, 1.25f);
                    model = GameAssets.ZombieBrute;
                    break;
                default:
                    _maxHealth = 100f;
                    _moveSpeed = 1.35f;
                    _damage = 12f;
                    _attackRange = 1.45f;
                    _attackCooldown = 1.2f;
                    transform.localScale = Vector3.one;
                    model = GameAssets.ZombieWalker;
                    break;
            }

            _health = _maxHealth;
            ReplaceVisual(model);
        }

        private void ReplaceVisual(string modelPath)
        {
            EnsureCached(ref _walkerVisual, GameAssets.ZombieWalker);
            EnsureCached(ref _runnerVisual, GameAssets.ZombieRunner);
            EnsureCached(ref _bruteVisual, GameAssets.ZombieBrute);
            if (_walkerVisual != null)
            {
                _walkerVisual.SetActive(modelPath == GameAssets.ZombieWalker);
            }

            if (_runnerVisual != null)
            {
                _runnerVisual.SetActive(modelPath == GameAssets.ZombieRunner);
            }

            if (_bruteVisual != null)
            {
                _bruteVisual.SetActive(modelPath == GameAssets.ZombieBrute);
            }

            _renderers = _visual.GetComponentsInChildren<Renderer>();
            _skin = GameAssets.Load<Texture2D>(GameAssets.ZombieAtlas);
            GameAssets.SetLayerRecursively(gameObject, gameObject.layer);
            _motion?.Bind(_visual);
        }

        private void EnsureCached(ref GameObject slot, string path)
        {
            if (slot != null)
            {
                return;
            }

            slot = GameAssets.AttachCharacter(_visual, path, GameAssets.ZombieAtlas);
        }

        private void Flash(Color color)
        {
            if (_renderers == null)
            {
                return;
            }

            CancelInvoke(nameof(RestoreColor));
            if (_flashBlock == null)
            {
                _flashBlock = new MaterialPropertyBlock();
            }

            foreach (Renderer r in _renderers)
            {
                r.GetPropertyBlock(_flashBlock);
                _flashBlock.SetColor("_Color", color);
                _flashBlock.SetColor("_BaseColor", color);
                r.SetPropertyBlock(_flashBlock);
            }

            Invoke(nameof(RestoreColor), 0.08f);
        }

        private void RestoreColor()
        {
            if (_renderers == null)
            {
                return;
            }

            foreach (Renderer r in _renderers)
            {
                if (r != null)
                {
                    r.SetPropertyBlock(null);
                }
            }

            if (_skin != null)
            {
                GameAssets.ApplyMainTexture(_visual.gameObject, _skin);
            }
        }
    }
}
