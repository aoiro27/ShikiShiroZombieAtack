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
        private HudController _hud;

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

        public void StartWaves()
        {
            StartCoroutine(RunWaves());
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
            Vector3 pos = PlaceAround(_arena.SpawnPoint + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(20f, 34f), 0.5f, 2.2f, origin, 16f);
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

        private IEnumerator RunWaves()
        {
            yield return null;
            Physics.SyncTransforms();

            while (_session.State != SessionState.GameOver)
            {
                int wave = _session.Wave;
                ResetPlayerToStart();
                Physics.SyncTransforms();
                _alive = 0;
                if (_balloons != null)
                {
                    _balloons.Respawn();
                }

                SpawnWave(WaveSize(wave), wave);

                yield return WaveCountdown(wave);
                if (_session.State == SessionState.GameOver)
                {
                    yield break;
                }

                _session.BeginCombat();
                _sfx.PlayRoar();
                _sfx.PlayWaveStart();

                while (_alive > 0 && _session.State == SessionState.Playing)
                {
                    yield return null;
                }

                if (_session.State == SessionState.GameOver)
                {
                    yield break;
                }

                _session.NotifyWaveClear();
                _hud.ShowWaveClear(wave);
                _sfx.PlayWaveClear();
                if (_player != null)
                {
                    _fx.WaveClearBurst(_player.position);
                }
                yield return new WaitForSeconds(3.6f);
                if (_session.State == SessionState.GameOver)
                {
                    yield break;
                }

                _session.AdvanceWave();
            }
        }

        private IEnumerator WaveCountdown(int wave)
        {
            for (int n = 3; n >= 1; n--)
            {
                if (_session.State == SessionState.GameOver)
                {
                    _hud.HideCountdown();
                    yield break;
                }

                _hud.ShowCountdown(n, wave);
                _sfx.PlayCountdownTick(n);
                yield return new WaitForSeconds(1f);
            }

            _hud.HideCountdown();
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

        private int WaveSize(int wave)
        {
            int size = 16 + wave * 8;
            int cap = _config != null ? _config.ZombiePoolSize : 96;
            return Mathf.Min(size, cap);
        }

        private void SpawnWave(int count, int wave)
        {
            Vector3 spawn = _arena.SpawnPoint;
            float minR = 20f;
            float maxR = Mathf.Min(_arena.PlayHalf * 0.82f, 40f);
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * 360f + Random.Range(-12f, 12f);
                Vector3 ring = spawn + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Random.Range(minR, maxR);
                Vector3 pos = PlaceAround(ring, 0.4f, 2.8f, spawn, 18f);
                ZombieAgent zombie = _zombies.Get();
                zombie.Spawn(SelectKind(wave), pos, _player, _session, _fx, _sfx, this);
                _alive++;
            }
        }

        private Vector3 PlaceAround(Vector3 center, float minJitter, float maxJitter, Vector3 keepAway, float minKeepAway)
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

                if (Physics.CheckCapsule(p + Vector3.up * 0.6f, p + Vector3.up * 1.6f, 0.4f, LayerMask.GetMask("Obstacle"), QueryTriggerInteraction.Ignore))
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

                if (!Physics.CheckCapsule(fallback + Vector3.up * 0.6f, fallback + Vector3.up * 1.6f, 0.4f, obstacle, QueryTriggerInteraction.Ignore))
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
