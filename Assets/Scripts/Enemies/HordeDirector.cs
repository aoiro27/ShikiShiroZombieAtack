using System.Collections;
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

        public void Despawn(ZombieAgent agent)
        {
            _zombies.Release(agent);
        }

        public void DropPickup(Vector3 position)
        {
            WorldPickup pickup = _pickups.Get();
            pickup.Setup(Random.value < 0.4f ? PickupKind.Medkit : PickupKind.Ammo, position, _player, _sfx);
        }

        private IEnumerator RunWaves()
        {
            while (_session.State != SessionState.GameOver)
            {
                int wave = _session.Wave;
                _remainingToSpawn = 6 + wave * 4 + (wave >= 5 ? wave : 0);
                _alive = 0;
                _hud.Announce($"WAVE {wave}");
                _sfx.PlayRoar();

                while (_remainingToSpawn > 0 && _session.State == SessionState.Playing)
                {
                    if (_alive >= _config.MaxAliveZombies)
                    {
                        yield return null;
                        continue;
                    }

                    SpawnOne(SelectKind(wave));
                    _remainingToSpawn--;
                    float interval = Mathf.Lerp(1.1f, 0.28f, Mathf.Clamp01(wave / 12f));
                    yield return new WaitForSeconds(interval);
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

        private void SpawnOne(ZombieKind kind)
        {
            Vector3 pos = _arena.RandomSpawnOnRing(18f, _arena.Radius - 4f);
            ZombieAgent zombie = _zombies.Get();
            zombie.Spawn(kind, pos, _player, _session, _fx, _sfx, this);
            _alive++;
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
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Pickup";
            go.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            Destroy(go.GetComponent<Collider>());
            return go.AddComponent<WorldPickup>();
        }
    }
}
