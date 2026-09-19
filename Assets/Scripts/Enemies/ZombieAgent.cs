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
        private float _bob;
        private Vector3 _stuckSamplePos;
        private float _stuckTimer;
        private float _stuckSampleAt;
        private float _slideSign = 1f;
        private float _nextRescue;
        private ZombieBodyMotion _motion;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _flashBlock;
        private CapsuleCollider _hurtbox;
        private GameObject _walkerVisual;
        private GameObject _runnerVisual;
        private GameObject _bruteVisual;

        public void BuildVisual()
        {
            _controller = gameObject.AddComponent<CharacterController>();
            _controller.height = 1.9f;
            _controller.radius = 0.45f;
            _controller.center = new Vector3(0f, 0.95f, 0f);
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
            EnsureHurtbox();
            GameAssets.SetLayerRecursively(gameObject, gameObject.layer);
        }

        private void EnsureHurtbox()
        {
            Transform existing = transform.Find("Hurtbox");
            GameObject go = existing != null ? existing.gameObject : new GameObject("Hurtbox");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.layer = gameObject.layer;
            _hurtbox = go.GetComponent<CapsuleCollider>();
            if (_hurtbox == null)
            {
                _hurtbox = go.AddComponent<CapsuleCollider>();
            }

            _hurtbox.isTrigger = true;
            _hurtbox.direction = 1;
            _hurtbox.height = 2.15f;
            _hurtbox.radius = 0.62f;
            _hurtbox.center = new Vector3(0f, 1.05f, 0f);
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
            _stuckTimer = 0f;
            _stuckSamplePos = transform.position;
            _stuckSampleAt = Time.time + 0.4f;
            _slideSign = Random.value < 0.5f ? -1f : 1f;
            _nextRescue = 0f;
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

        public void Warp(Vector3 position)
        {
            if (_controller != null)
            {
                _controller.enabled = false;
            }

            transform.position = position + Vector3.up * 0.05f;
            _stuckTimer = 0f;
            _stuckSamplePos = transform.position;
            _stuckSampleAt = Time.time + 0.5f;
            _nextRescue = Time.time + 2.5f;
            if (_controller != null)
            {
                _controller.enabled = true;
            }
        }

        public void ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive)
            {
                return;
            }

            _health -= info.Amount;
            if (info.ChainDepth == 0)
            {
                _fx.Blood(info.Point, info.Direction, info.IsHeadshot);
                _sfx.PlayFlesh(info.IsHeadshot);
                _stagger = info.IsHeadshot ? 0.45f : 0.12f;
                Flash(info.IsHeadshot ? Color.white : new Color(1f, 0.4f, 0.4f));
            }

            if (_health <= 0f)
            {
                Die(info);
            }
            else if (info.ChainDepth == 0)
            {
                int gained = _session.RegisterCombatHit(Kind, info.IsHeadshot, false);
                _session.NotifyHitPopup(new HitPopupInfo(info.Point, gained, _session.Combo, info.IsHeadshot, false));
            }
        }

        private void Update()
        {
            if (!IsAlive || _target == null)
            {
                return;
            }

            if (_session != null && _session.State != SessionState.Playing)
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
            if (vertical > 1.8f)
            {
                TryRescue();
            }

            bool chasing = dist > _attackRange * 0.92f;
            if (chasing)
            {
                Vector3 dir = to / Mathf.Max(dist, 0.05f);
                if (_stuckTimer > 1.1f)
                {
                    float yaw = Mathf.Lerp(40f, 100f, Mathf.InverseLerp(1.1f, 3.2f, _stuckTimer));
                    dir = Quaternion.Euler(0f, yaw * _slideSign, 0f) * dir;
                }

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

            SampleStuck(chasing, dist);
            if (_horde != null && !_horde.IsInside(transform.position))
            {
                TryRescue();
            }

            bool swing = false;
            if (dist <= _attackRange && Time.time >= _nextAttack && CanSeeTarget())
            {
                _nextAttack = Time.time + _attackCooldown;
                swing = true;
                if (_playerVitality != null && _playerVitality.IsAlive)
                {
                    float before = _playerVitality.CurrentHealth;
                    Vector3 wound = _target.position + Vector3.up * 1.52f;
                    _playerVitality.ApplyDamage(new DamageInfo(_damage, wound, transform.forward, false, WeaponId.Bite));
                    if (_playerVitality.CurrentHealth < before)
                    {
                        _fx.PlayerWound(wound, transform.forward);
                        _sfx.PlayBite();
                    }
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

        private void SampleStuck(bool chasing, float dist)
        {
            if (!chasing || dist < _attackRange * 1.5f)
            {
                _stuckTimer = 0f;
                _stuckSamplePos = transform.position;
                return;
            }

            if (Time.time < _stuckSampleAt)
            {
                return;
            }

            Vector3 pos = transform.position;
            Vector2 delta = new Vector2(pos.x - _stuckSamplePos.x, pos.z - _stuckSamplePos.z);
            if (delta.sqrMagnitude < 0.45f * 0.45f)
            {
                _stuckTimer += 0.4f;
                if (_stuckTimer > 2.2f)
                {
                    _slideSign = -_slideSign;
                }

                if (_stuckTimer > 3.8f)
                {
                    TryRescue();
                    if (_stuckTimer <= 0f)
                    {
                        return;
                    }
                }
            }
            else
            {
                _stuckTimer = Mathf.Max(0f, _stuckTimer - 0.55f);
            }

            _stuckTimer = Mathf.Min(_stuckTimer, 6f);
            _stuckSamplePos = pos;
            _stuckSampleAt = Time.time + 0.4f;
        }

        private void TryRescue()
        {
            if (_horde == null || Time.time < _nextRescue)
            {
                return;
            }

            _horde.Rescue(this);
        }

        private void AvoidNeighbors(ref Vector3 dir)
        {
            if (ZombieMask < 0)
            {
                ZombieMask = LayerMask.GetMask("Zombie");
            }

            if (NeighborBuffer == null)
            {
                NeighborBuffer = new Collider[32];
            }

            Vector3 sep = Vector3.zero;
            int count = Physics.OverlapSphereNonAlloc(transform.position, 1.1f, NeighborBuffer, ZombieMask, QueryTriggerInteraction.Ignore);
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
            float scale = Kind == ZombieKind.Brute ? 1.9f : 1.15f;
            scale *= info.ChainDepth > 0 ? 1.25f : 1f;
            _fx.Explosion(boom, dir, scale);
            if (info.ChainDepth == 0)
            {
                _sfx.PlayKill(info.IsHeadshot);
                _sfx.PlayBoom();
                _fx.KillPunch(scale);
            }

            int gained = _session.RegisterCombatHit(Kind, info.IsHeadshot, true);
            _session.NotifyHitPopup(new HitPopupInfo(info.Point, gained, _session.Combo, info.IsHeadshot, true));
            _horde.NotifyKilled(this);
            _horde.ChainBurst(this, info);
            if (info.ChainDepth == 0 && Random.value < 0.1f)
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
            GameAssets.SetLayerRecursively(gameObject, gameObject.layer);
            RestoreColor();
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
                if (r == null)
                {
                    continue;
                }

                _flashBlock.Clear();
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
        }
    }
}
