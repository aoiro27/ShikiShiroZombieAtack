using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class HordeDirector : MonoBehaviour
    {
        private GameConfig _config;
        private GameSession _session;
        private ArenaBuilder _arena;
        private Transform _player;
        private CombatFx _fx;
        private ProceduralSfx _sfx;
        private ObjectPool<ZombieAgent> _zombies;
        private ObjectPool<WorldPickup> _pickups;
        private BalloonField _balloons;
        private int _alive;
        private readonly List<ZombieAgent> _waveTrash = new List<ZombieAgent>(96);
        private readonly List<ZombieAgent> _waveBosses = new List<ZombieAgent>(8);
        private static HordeDirector RunOwner;
        private static int BossWaveIssued;
        private WaveBeat _beat;
        private int _beatWave;
        private Phase _phase;
        private int _wave;
        private int _countNum;
        private float _timer;
        private bool _trashSpawned;
        private HudController _hud;

        private enum WaveBeat
        {
            None,
            Trash,
            Boss,
            Clear
        }

        private enum Phase
        {
            Idle,
            Countdown,
            Trash,
            PauseBeforeBoss,
            Boss,
            Clear
        }

        public IReadOnlyList<ZombieAgent> AllZombies => _zombies != null ? _zombies.All : System.Array.Empty<ZombieAgent>();

        public void Initialize(GameConfig config, GameSession session, ArenaBuilder arena, Transform player, CombatFx fx, ProceduralSfx sfx, HudController hud)
        {
            _config = config;
            _session = session;
            _arena = arena;
            _player = player;
            _fx = fx;
            _sfx = sfx;
            _hud = hud;

            var zombieRoot = new GameObject("ZombiePool").transform;
            zombieRoot.SetParent(transform, false);
            _zombies = new ObjectPool<ZombieAgent>(CreateZombie, zombieRoot, config.ZombiePoolSize);

            var pickupRoot = new GameObject("PickupPool").transform;
            pickupRoot.SetParent(transform, false);
            _pickups = new ObjectPool<WorldPickup>(CreatePickup, pickupRoot, 12);

            _balloons = BalloonField.Spawn(transform, arena, this, fx, sfx, session);
        }

        public static void ResetStaticRun()
        {
            RunOwner = null;
            BossWaveIssued = 0;
        }

        public void StartWaves()
        {
            if (RunOwner != null && RunOwner != this)
            {
                enabled = false;
                return;
            }

            if (RunOwner == this && _phase != Phase.Idle)
            {
                return;
            }

            RunOwner = this;
            enabled = true;
            EnterCountdown();
        }

        private void Update()
        {
            if (_session == null || _phase == Phase.Idle || _session.State == SessionState.GameOver)
            {
                return;
            }

            switch (_phase)
            {
                case Phase.Countdown:
                    TickCountdown();
                    break;
                case Phase.Trash:
                    if (_trashSpawned && CountAlive(false) <= 0)
                    {
                        _phase = Phase.PauseBeforeBoss;
                        _timer = 0.9f;
                    }
                    break;
                case Phase.PauseBeforeBoss:
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        _beat = WaveBeat.Boss;
                        _phase = Phase.Boss;
                        TrySpawnBossRound(_wave);
                    }
                    break;
                case Phase.Boss:
                    if (CountSceneBosses() <= 0 && !AnyAlive(_waveBosses))
                    {
                        EnterClear();
                    }
                    break;
                case Phase.Clear:
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        _hud.HideCountdown();
                        _session.AdvanceWave();
                        EnterCountdown();
                    }
                    break;
            }
        }

        private void LateUpdate()
        {
            CullIllegalBosses();
        }

        public void NotifyKilled(ZombieAgent agent)
        {
            _alive = Mathf.Max(0, _alive - 1);
        }

        public void Rescue(ZombieAgent agent)
        {
            if (agent == null || !agent.IsAlive)
            {
                return;
            }

            Vector3 origin = _player != null ? _player.position : _arena.SpawnPoint;
            float body = agent.Kind == ZombieKind.Boss ? 1.9f : 0.4f;
            Vector3 pos = PlaceAround(_arena.SpawnPoint + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(20f, 34f), 0.5f, 2.2f, origin, 16f, body);
            agent.Warp(pos);
        }

        public bool IsInside(Vector3 point)
        {
            return _arena == null || _arena.Contains(point, 0.8f);
        }

        public void Despawn(ZombieAgent agent)
        {
            _zombies.Release(agent);
        }

        private void ClearLiving()
        {
            var all = AllZombies;
            for (int i = 0; i < all.Count; i++)
            {
                ZombieAgent z = all[i];
                if (z != null && z.IsAlive)
                {
                    z.ForceDespawn();
                }
            }

            _alive = 0;
        }

        public void DropPickup(Vector3 position)
        {
            WorldPickup pickup = _pickups.Get();
            pickup.Setup(PickupKind.Medkit, position, _player, _sfx);
        }

        public void ChainBurst(ZombieAgent source, in DamageInfo info)
        {
            if (source == null)
            {
                return;
            }

            ChainBurst(source.transform.position, info);
        }

        public void ChainBurst(Vector3 origin, in DamageInfo info)
        {
            if (info.ChainDepth >= 2)
            {
                return;
            }

            StartCoroutine(ChainBurstRoutine(origin, info));
        }

        private IEnumerator ChainBurstRoutine(Vector3 origin, DamageInfo info)
        {
            const float radius = 6.2f;
            const int maxVictims = 8;
            int mask = LayerMask.GetMask("Zombie");
            var hits = new Collider[32];
            int count = Physics.OverlapSphereNonAlloc(origin, radius, hits, mask, QueryTriggerInteraction.Collide);
            var victims = new List<IDamageable>(maxVictims);
            for (int i = 0; i < count && victims.Count < maxVictims; i++)
            {
                if (hits[i] == null)
                {
                    continue;
                }

                var hurt = hits[i].GetComponentInParent<IDamageable>();
                if (hurt == null || !hurt.IsAlive || hurt is PlayerVitality || victims.Contains(hurt))
                {
                    continue;
                }

                if (hurt is ZombieAgent zombie && zombie.Kind == ZombieKind.Boss)
                {
                    continue;
                }

                victims.Add(hurt);
            }

            int nextDepth = info.ChainDepth + 1;
            for (int i = 0; i < victims.Count; i++)
            {
                if (_session.State == SessionState.GameOver)
                {
                    yield break;
                }

                yield return new WaitForSeconds(0.045f);
                IDamageable hurt = victims[i];
                if (hurt == null || !hurt.IsAlive)
                {
                    continue;
                }

                var mb = hurt as MonoBehaviour;
                Vector3 pos = mb != null ? mb.transform.position : origin;
                Vector3 boom = pos + Vector3.up * 0.4f;
                Vector3 dir = pos - origin;
                dir.y = 0.4f;
                if (dir.sqrMagnitude < 0.01f)
                {
                    dir = Vector3.up;
                }

                hurt.ApplyDamage(new DamageInfo(999f, boom, dir.normalized, false, info.Weapon, nextDepth));
            }
        }

        private void EnterCountdown()
        {
            _wave = _session.Wave;
            _beatWave = _wave;
            _beat = WaveBeat.None;
            _phase = Phase.Countdown;
            _countNum = 3;
            _timer = 1f;
            _trashSpawned = false;
            PrepareWave();
            _session.BeginCountdown();
            _hud.ShowCountdown(_countNum, _wave);
            _sfx.PlayCountdownTick(_countNum);
        }

        private void TickCountdown()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            _countNum--;
            if (_countNum >= 1)
            {
                _timer = 1f;
                _hud.ShowCountdown(_countNum, _wave);
                _sfx.PlayCountdownTick(_countNum);
                return;
            }

            _hud.HideCountdown();
            _session.BeginCombat();
            _beat = WaveBeat.Trash;
            _phase = Phase.Trash;
            SpawnWave(WaveCombatRules.TrashCount(_wave, PoolCap()), _wave);
            if (CountAlive(false) == 0)
            {
                SpawnWave(WaveCombatRules.TrashCount(_wave, PoolCap()), _wave);
            }

            _trashSpawned = CountAlive(false) > 0;
            _sfx.PlayRoar();
            _sfx.PlayWaveStart();
        }

        private void EnterClear()
        {
            _beat = WaveBeat.Clear;
            _phase = Phase.Clear;
            _timer = 3.6f;
            CullIllegalBosses();
            _session.NotifyWaveClear();
            _hud.ShowWaveClear(_wave);
            _sfx.PlayWaveClear();
            if (_player != null)
            {
                _fx.WaveClearBurst(_player.position);
            }
        }

        private void PrepareWave()
        {
            _waveTrash.Clear();
            _waveBosses.Clear();
            ResetPlayerToStart();
            Physics.SyncTransforms();
            ClearLiving();
            if (_balloons != null)
            {
                _balloons.Respawn();
            }
        }

        private void TrySpawnBossRound(int wave)
        {
            if (_phase != Phase.Boss || !_trashSpawned || CountAlive(false) > 0)
            {
                return;
            }

            if (wave <= BossWaveIssued)
            {
                return;
            }

            BossWaveIssued = wave;
            int livingBosses = CountSceneBosses();
            int bosses = WaveCombatRules.BossCount(wave);
            int need = bosses - livingBosses;
            if (need <= 0)
            {
                return;
            }
            for (int i = 0; i < need; i++)
            {
                SpawnBoss(livingBosses + i, bosses);
            }

            _hud.ShowBossAppear(wave, bosses);
            _sfx.PlayBossWarning();
        }

        private static bool AnyAlive(List<ZombieAgent> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ZombieAgent z = list[i];
                if (z != null && z.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountAlive(bool bosses)
        {
            int count = 0;
            var all = AllZombies;
            for (int i = 0; i < all.Count; i++)
            {
                ZombieAgent z = all[i];
                if (z == null || !z.IsAlive)
                {
                    continue;
                }

                if ((z.Kind == ZombieKind.Boss) == bosses)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSceneBosses()
        {
            int count = 0;
            ZombieAgent[] all = FindObjectsByType<ZombieAgent>(FindObjectsInactive.Exclude);
            for (int i = 0; i < all.Length; i++)
            {
                ZombieAgent z = all[i];
                if (z != null && z.IsAlive && z.Kind == ZombieKind.Boss)
                {
                    count++;
                }
            }

            return count;
        }

        private void CullIllegalBosses()
        {
            int allowed = 0;
            if (_beat == WaveBeat.Boss)
            {
                allowed = WaveCombatRules.BossCount(Mathf.Max(1, _beatWave));
            }

            int kept = 0;
            ZombieAgent[] all = FindObjectsByType<ZombieAgent>(FindObjectsInactive.Exclude);
            for (int i = 0; i < all.Length; i++)
            {
                ZombieAgent z = all[i];
                if (z == null || !z.IsAlive || z.Kind != ZombieKind.Boss)
                {
                    continue;
                }

                if (_beat != WaveBeat.Boss || !IsTrackedBoss(z) || kept >= allowed)
                {
                    z.ForceDespawn();
                    continue;
                }

                kept++;
            }
        }

        private bool IsTrackedBoss(ZombieAgent agent)
        {
            for (int i = 0; i < _waveBosses.Count; i++)
            {
                if (_waveBosses[i] == agent)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetPlayerToStart()
        {
            if (_player == null)
            {
                return;
            }

            var motor = _player.GetComponent<PlayerMotor>();
            if (motor != null)
            {
                motor.ResetToSpawn();
            }
            else
            {
                _player.SetPositionAndRotation(_arena.SpawnPoint, Quaternion.identity);
            }
        }

        private int PoolCap()
        {
            return _config != null ? _config.ZombiePoolSize : 96;
        }

        private void SpawnWave(int count, int wave)
        {
            Vector3 spawn = _arena.SpawnPoint;
            float farMax = Mathf.Min(_arena.PlayHalf * 0.82f, 40f);
            for (int i = 0; i < count; i++)
            {
                bool close = i < 10;
                float minR = close ? 8f : 20f;
                float maxR = close ? 14f : farMax;
                float keepAway = close ? 6f : 18f;
                float angle = (i / (float)count) * 360f + Random.Range(-12f, 12f);
                Vector3 ring = spawn + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Random.Range(minR, maxR);
                Vector3 pos = PlaceAround(ring, 0.4f, 2.8f, spawn, keepAway);
                ZombieAgent zombie = TakeZombie();
                zombie.Spawn(SelectKind(wave), pos, _player, _session, _fx, _sfx, this);
                _waveTrash.Add(zombie);
                _alive++;
            }
        }

        private void SpawnBoss(int index, int total)
        {
            Vector3 pos = PlaceBossAtCenter(index, total);
            ZombieAgent boss = CreateZombie();
            boss.name = "Boss";
            boss.Spawn(ZombieKind.Boss, pos, _player, _session, _fx, _sfx, this);
            _waveBosses.Add(boss);
            _alive++;
            if (_balloons != null)
            {
                int balloons = Mathf.Max(40, 110 / Mathf.Max(1, total));
                _balloons.SwarmAround(pos + Vector3.up * 3.8f, balloons);
            }
        }

        private ZombieAgent TakeZombie()
        {
            ZombieAgent zombie = _zombies.Get();
            if (zombie.IsAlive)
            {
                zombie.ForceDespawn();
                zombie = _zombies.Get();
            }

            return zombie;
        }

        private Vector3 PlaceBossAtCenter(int index, int total)
        {
            const float body = 2.05f;
            Vector3 center = _arena.SpawnPoint;
            Vector3 candidate = center;
            if (total > 1)
            {
                float slice = 360f / total;
                candidate += Quaternion.Euler(0f, slice * index, 0f) * Vector3.forward * 2.2f;
                candidate = _arena.SnapToStreet(candidate);
            }

            if (BossFits(candidate, body))
            {
                return SnapBossToGround(candidate);
            }

            for (int i = 0; i < 28; i++)
            {
                float dist = 1.1f + (i / 4) * 1.15f;
                Vector3 p = center + StreetAxis(i % 4) * dist;
                p = _arena.SnapToStreet(p);
                if (BossFits(p, body))
                {
                    return SnapBossToGround(p);
                }
            }

            return SnapBossToGround(center);
        }

        private static Vector3 StreetAxis(int dir)
        {
            switch (dir)
            {
                case 0: return Vector3.forward;
                case 1: return Vector3.right;
                case 2: return Vector3.back;
                default: return Vector3.left;
            }
        }

        private bool BossFits(Vector3 pos, float body)
        {
            if (_arena == null || !_arena.IsStreet(pos))
            {
                return false;
            }

            int obstacle = LayerMask.GetMask("Obstacle");
            Vector3 bottom = pos + Vector3.up * (body + 0.2f);
            Vector3 top = pos + Vector3.up * 7.2f;
            return !Physics.CheckCapsule(bottom, top, body, obstacle, QueryTriggerInteraction.Ignore);
        }

        private Vector3 SnapBossToGround(Vector3 pos)
        {
            int mask = LayerMask.GetMask("Ground");
            if (mask == 0)
            {
                mask = LayerMask.GetMask("Obstacle", "Ground");
            }

            Vector3 probe = pos + Vector3.up * 10f;
            if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 24f, mask, QueryTriggerInteraction.Ignore))
            {
                pos.y = hit.point.y + 0.08f;
            }
            else
            {
                pos.y = _arena.SpawnPoint.y;
            }

            return pos;
        }

        private Vector3 PlaceAround(Vector3 center, float minJitter, float maxJitter, Vector3 keepAway, float minKeepAway, float bodyRadius = 0.4f)
        {
            int mask = LayerMask.GetMask("Obstacle", "Ground");
            float minSqr = minKeepAway * minKeepAway;
            Vector3 heightRef = _arena.SpawnPoint;
            for (int i = 0; i < 24; i++)
            {
                Vector2 jitter = Random.insideUnitCircle * Random.Range(minJitter, maxJitter);
                Vector3 p = center + new Vector3(jitter.x, 0f, jitter.y);
                Vector3 probe = p + Vector3.up * 8f;
                if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 24f, mask == 0 ? ~0 : mask, QueryTriggerInteraction.Ignore))
                {
                    p = hit.point + Vector3.up * 0.08f;
                }
                else
                {
                    p.y = heightRef.y;
                }

                if (!_arena.IsStreet(p))
                {
                    p = _arena.SnapToStreet(p);
                }

                if (Mathf.Abs(p.y - heightRef.y) > 2.5f)
                {
                    continue;
                }

                Vector2 away = new Vector2(p.x - keepAway.x, p.z - keepAway.z);
                if (away.sqrMagnitude < minSqr)
                {
                    continue;
                }

                if (!_arena.Contains(p, 2.2f))
                {
                    continue;
                }

                if (Physics.CheckCapsule(p + Vector3.up * (bodyRadius + 0.2f), p + Vector3.up * (bodyRadius + 1.4f), bodyRadius, LayerMask.GetMask("Obstacle"), QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                return p;
            }

            int obstacle = LayerMask.GetMask("Obstacle");
            for (int i = 0; i < 10; i++)
            {
                Vector3 fallback = _arena.SnapToStreet(_arena.SpawnPoint + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(22f, 36f));
                fallback.y = heightRef.y;
                fallback = _arena.ClampInside(fallback, 2.2f);
                Vector2 away = new Vector2(fallback.x - keepAway.x, fallback.z - keepAway.z);
                if (away.sqrMagnitude < minSqr)
                {
                    continue;
                }

                if (!Physics.CheckCapsule(fallback + Vector3.up * (bodyRadius + 0.2f), fallback + Vector3.up * (bodyRadius + 1.4f), bodyRadius, obstacle, QueryTriggerInteraction.Ignore))
                {
                    return fallback;
                }
            }

            Vector3 safe = _arena.SnapToStreet(_arena.SpawnPoint + Vector3.forward * 24f);
            safe.y = heightRef.y;
            return _arena.ClampInside(safe, 2.2f);
        }

        private static ZombieKind SelectKind(int wave)
        {
            float roll = Random.value;
            if (wave >= 3 && roll < 0.08f + wave * 0.01f)
            {
                return ZombieKind.Brute;
            }

            if (wave >= 2 && roll < 0.35f)
            {
                return ZombieKind.Runner;
            }

            return ZombieKind.Walker;
        }

        private ZombieAgent CreateZombie()
        {
            var go = new GameObject("Zombie");
            var agent = go.AddComponent<ZombieAgent>();
            agent.BuildVisual();
            return agent;
        }

        private WorldPickup CreatePickup()
        {
            var go = new GameObject("Pickup");
            var pickup = go.AddComponent<WorldPickup>();
            pickup.BuildVisual();
            return pickup;
        }
    }
}
