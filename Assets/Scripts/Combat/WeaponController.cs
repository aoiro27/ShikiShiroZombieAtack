using System;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class WeaponController : MonoBehaviour
    {
        public event Action MagazineChanged;

        public WeaponStats Current { get; private set; }
        public int Mag { get; private set; }
        public int Reserve { get; private set; }
        public bool Reloading { get; private set; }

        private readonly WeaponStats[] _arsenal = { WeaponStats.Pistol(), WeaponStats.Smg(), WeaponStats.Shotgun() };
        private readonly int[] _mags;
        private readonly int[] _reserves;
        private int _index;
        private float _nextFire;
        private float _reloadEnd;
        private GameInput _input;
        private PlayerMotor _motor;
        private TpsCamera _camera;
        private CombatFx _fx;
        private ProceduralSfx _sfx;
        private Transform _muzzle;
        private int _zombieMask;

        public WeaponController()
        {
            _mags = new int[_arsenal.Length];
            _reserves = new int[_arsenal.Length];
            for (int i = 0; i < _arsenal.Length; i++)
            {
                _mags[i] = _arsenal[i].MagazineSize;
                _reserves[i] = _arsenal[i].ReserveAmmo;
            }
        }

        public void Initialize(GameInput input, PlayerMotor motor, TpsCamera camera, CombatFx fx, ProceduralSfx sfx)
        {
            _input = input;
            _motor = motor;
            _camera = camera;
            _fx = fx;
            _sfx = sfx;
            _zombieMask = LayerMask.GetMask("Zombie", "Obstacle", "Ground", "Default");
            Equip(0);
            BuildGunVisual();
        }

        public void AddAmmo(int amount)
        {
            _reserves[_index] += amount;
            Reserve = _reserves[_index];
            MagazineChanged?.Invoke();
        }

        private void Update()
        {
            if (_input == null)
            {
                return;
            }

            if (_input.SwapPressed)
            {
                Equip((_index + 1) % _arsenal.Length);
            }

            if (Reloading)
            {
                if (Time.time >= _reloadEnd)
                {
                    FinishReload();
                }

                return;
            }

            if (_input.ReloadPressed)
            {
                BeginReload();
                return;
            }

            if (_input.FireHeld)
            {
                TryFire();
            }
        }

        private void Equip(int index)
        {
            _index = index;
            Current = _arsenal[_index];
            Mag = _mags[_index];
            Reserve = _reserves[_index];
            Reloading = false;
            MagazineChanged?.Invoke();
        }

        private void TryFire()
        {
            if (Time.time < _nextFire)
            {
                return;
            }

            if (Mag <= 0)
            {
                BeginReload();
                return;
            }

            Mag--;
            _mags[_index] = Mag;
            _nextFire = Time.time + Current.FireInterval;
            _motor.AddRecoil(Current.Recoil);
            _camera.Shake(0.035f * Current.Pellets, 0.08f);
            _sfx.PlayShot(Current.Id);
            _fx.MuzzleFlash(_muzzle.position, _muzzle.forward);
            MagazineChanged?.Invoke();

            Camera cam = _camera.UnityCamera;
            Vector3 origin = cam.transform.position;
            Vector3 center = cam.transform.forward;
            for (int i = 0; i < Current.Pellets; i++)
            {
                Vector3 dir = Quaternion.Euler(
                    UnityEngine.Random.Range(-Current.SpreadDegrees, Current.SpreadDegrees),
                    UnityEngine.Random.Range(-Current.SpreadDegrees, Current.SpreadDegrees),
                    0f) * center;

                if (!Physics.Raycast(origin, dir, out RaycastHit hit, Current.Range, _zombieMask, QueryTriggerInteraction.Ignore))
                {
                    _fx.Tracer(origin + cam.transform.forward, origin + dir * 18f);
                    continue;
                }

                _fx.Impact(hit.point, hit.normal);
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                bool headshot = hit.point.y - hit.collider.bounds.min.y > hit.collider.bounds.size.y * 0.72f;
                float amount = Current.Damage * (headshot ? 2.4f : 1f);
                damageable.ApplyDamage(new DamageInfo(amount, hit.point, dir, headshot, Current.Id));
            }
        }

        private void BeginReload()
        {
            if (Reloading || Mag >= Current.MagazineSize || Reserve <= 0)
            {
                return;
            }

            Reloading = true;
            _reloadEnd = Time.time + Current.ReloadTime;
            _sfx.PlayReload();
            MagazineChanged?.Invoke();
        }

        private void FinishReload()
        {
            int need = Current.MagazineSize - Mag;
            int take = Mathf.Min(need, Reserve);
            Mag += take;
            Reserve -= take;
            _mags[_index] = Mag;
            _reserves[_index] = Reserve;
            Reloading = false;
            MagazineChanged?.Invoke();
        }

        private void BuildGunVisual()
        {
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "Rifle";
            gun.transform.SetParent(_motor.Head, false);
            gun.transform.localPosition = new Vector3(0.28f, -0.18f, 0.55f);
            gun.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            gun.transform.localScale = new Vector3(0.12f, 0.12f, 0.7f);
            gun.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Create(new Color(0.08f, 0.08f, 0.09f), 0.6f, 0.4f);
            Destroy(gun.GetComponent<Collider>());

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(gun.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            barrel.GetComponent<MeshRenderer>().sharedMaterial = gun.GetComponent<MeshRenderer>().sharedMaterial;
            Destroy(barrel.GetComponent<Collider>());

            _muzzle = new GameObject("Muzzle").transform;
            _muzzle.SetParent(gun.transform, false);
            _muzzle.localPosition = new Vector3(0f, 0f, 0.72f);
        }
    }
}
