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
        private int _remainingToSpawn;
        private int _alive;
        private HudController _hud;

        private float _lastPackAngle = 999f;

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
            if (!_arena.Contains(origin, 2f))
            {
                origin = _arena.SpawnPoint;
            }

            Vector3 pos = PlaceAround(origin, 8f, 16f);
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
            if (source == null || info.ChainDepth >= 2)
            {
                return;
            }

            StartCoroutine(ChainBurstRoutine(source.transform.position, info));
        }

        private IEnumerator ChainBurstRoutine(Vector3 origin, DamageInfo info)
        {
            const float radius = 6.2f;
            const int maxVictims = 12;
            var victims = new List<ZombieAgent>(maxVictims);
            var all = AllZombies;
            for (int i = 0; i < all.Count && victims.Count < maxVictims; i++)
            {
                ZombieAgent z = all[i];
                if (z == null || !z.IsAlive || !z.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 delta = z.transform.position - origin;
                delta.y = 0f;
                if (delta.sqrMagnitude <= radius * radius)
                {
                    victims.Add(z);
                }
            }

            int nextDepth = info.ChainDepth + 1;
            for (int i = 0; i < victims.Count; i++)
            {
                if (_session.State != SessionState.Playing)
                {
                    yield break;
                }

                yield return new WaitForSeconds(0.045f);
                ZombieAgent z = victims[i];
                if (z == null || !z.IsAlive)
                {
                    continue;
                }

                Vector3 boom = z.transform.position + Vector3.up * 0.9f;
                Vector3 dir = (z.transform.position - origin);
                dir.y = 0.4f;
                if (dir.sqrMagnitude < 0.01f)
                {
                    dir = Vector3.up;
                }

                z.ApplyDamage(new DamageInfo(999f, boom, dir.normalized, false, info.Weapon, nextDepth));
            }
        }

        private IEnumerator RunWaves()
        {
            yield return null;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(3f);

            while (_session.State != SessionState.GameOver)
            {
                int wave = _session.Wave;
                _remainingToSpawn = 16 + wave * 8;
                _alive = 0;
                _hud.Announce($"WAVE {wave}");
                _sfx.PlayRoar();
                _lastPackAngle = 999f;

                while (_remainingToSpawn > 0 && _session.State == SessionState.Playing)
                {
                    while (_alive >= _config.MaxAliveZombies && _session.State == SessionState.Playing)
                    {
                        yield return null;
                    }

                    if (_session.State != SessionState.Playing)
                    {
                        break;
                    }

                    int pack = Mathf.Clamp(8 + wave, 8, 18);
                    pack = Mathf.Min(pack, _remainingToSpawn);
                    yield return SpawnPack(pack, wave);
                    if (_remainingToSpawn > 0 && _session.State == SessionState.Playing)
                    {
                        yield return new WaitForSeconds(Mathf.Lerp(3.4f, 1.8f, Mathf.Clamp01(wave / 8f)));
                    }
                }

                while (_alive > 0 && _session.State == SessionState.Playing)
                {
                    yield return null;
                }

                if (_session.State == SessionState.GameOver)
                {
                    yield break;
                }

                _session.NotifyWaveClear();
                _hud.Announce("エリア確保");
                yield return new WaitForSeconds(4.5f);
                if (_session.State == SessionState.GameOver)
                {
                    yield break;
                }

                _session.AdvanceWave();
            }
        }

        private IEnumerator SpawnPack(int count, int wave)
        {
            Vector3 anchor = PickPackAnchor();
            for (int i = 0; i < count; i++)
            {
                if (_session.State != SessionState.Playing)
                {
                    yield break;
                }

                Vector3 pos = PlaceAround(anchor, 0.6f, 2.4f);
                ZombieAgent zombie = _zombies.Get();
                zombie.Spawn(SelectKind(wave), pos, _player, _session, _fx, _sfx, this);
                _alive++;
                _remainingToSpawn--;
                yield return new WaitForSeconds(0.05f);
            }
        }

        private Vector3 PickPackAnchor()
        {
            Vector3 origin = _player != null ? _player.position : _arena.SpawnPoint;
            float angle;
            int guard = 0;
            do
            {
                angle = Random.Range(0f, 360f);
                guard++;
            }
            while (guard < 12 && Mathf.Abs(Mathf.DeltaAngle(_lastPackAngle, angle)) < 70f);

            _lastPackAngle = angle;
            Vector3 raw = origin + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Random.Range(14f, 24f);
            return PlaceAround(raw, 0f, 1.2f);
        }

        private Vector3 PlaceAround(Vector3 center, float minJitter, float maxJitter)
        {
            int mask = LayerMask.GetMask("Obstacle", "Ground");
            Vector3 origin = _player != null ? _player.position : _arena.SpawnPoint;
            for (int i = 0; i < 20; i++)
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
                    p.y = origin.y;
                }

                if (!_arena.IsStreet(p))
                {
                    p = _arena.SnapToStreet(p);
                }

                if (Mathf.Abs(p.y - origin.y) > 2.5f)
                {
                    continue;
                }

                Vector2 fromPlayer = new Vector2(p.x - origin.x, p.z - origin.z);
                if (fromPlayer.sqrMagnitude < 11f * 11f)
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
            for (int i = 0; i < 8; i++)
            {
                Vector3 fallback = _arena.SnapToStreet(origin + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(12f, 22f));
                fallback.y = origin.y;
                fallback = _arena.ClampInside(fallback, 2.2f);
                if (!Physics.CheckCapsule(fallback + Vector3.up * 0.6f, fallback + Vector3.up * 1.6f, 0.4f, obstacle, QueryTriggerInteraction.Ignore))
                {
                    return fallback;
                }
            }

            Vector3 safe = _arena.SnapToStreet(_arena.SpawnPoint + Vector3.back * 8f);
            safe.y = origin.y;
            return safe;
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
