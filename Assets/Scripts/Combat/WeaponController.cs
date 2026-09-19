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
        private Transform _viewRoot;
        private Vector3[] _hipPos;
        private Vector3[] _hipEuler;
        private float _kick;
        private int _zombieMask;
        private GameObject[] _gunVisuals;

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
            BuildGunVisual();
            Equip(0);
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

            if (_input.FireHeld)
            {
                TryFire();
            }

            AnimateViewmodel();
        }

        public int EquippedIndex => _index;

        public void SelectWeapon(int index)
        {
            if (_arsenal.Length == 0)
            {
                return;
            }

            Equip((index % _arsenal.Length + _arsenal.Length) % _arsenal.Length);
        }

        private void Equip(int index)
        {
            _index = index;
            Current = _arsenal[_index];
            Mag = _mags[_index];
            Reserve = _reserves[_index];
            Reloading = false;
            MagazineChanged?.Invoke();
            ShowEquippedGun();
        }

        private void ShowEquippedGun()
        {
            if (_gunVisuals == null)
            {
                return;
            }

            for (int i = 0; i < _gunVisuals.Length; i++)
            {
                if (_gunVisuals[i] != null)
                {
                    _gunVisuals[i].SetActive(i == _index);
                }
            }
        }

        private void TryFire()
        {
            if (Time.time < _nextFire)
            {
                return;
            }

            _nextFire = Time.time + Current.FireInterval;
            _motor.AddRecoil(Current.Recoil);
            _camera.Shake(0.035f * Current.Pellets, 0.08f);
            _kick = Mathf.Max(_kick, 0.12f + Current.Recoil * 0.04f);
            _sfx.PlayShot(Current.Id);
            Vector3 muzzlePos = _muzzle.position;
            Vector3 muzzleFwd = _camera.UnityCamera.transform.forward;
            _fx.MuzzleFlash(muzzlePos, muzzleFwd);
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
                    _fx.Tracer(muzzlePos, origin + dir * Mathf.Min(Current.Range, 40f));
                    continue;
                }

                _fx.Tracer(muzzlePos, hit.point);
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    _fx.Impact(hit.point, hit.normal);
                    _sfx.PlayImpact();
                    continue;
                }

                bool headshot = hit.point.y - hit.collider.bounds.min.y > hit.collider.bounds.size.y * 0.72f;
                float amount = Current.Damage * (headshot ? 2.4f : 1f);
                damageable.ApplyDamage(new DamageInfo(amount, hit.point, dir, headshot, Current.Id));
                _camera.Shake(headshot ? 0.16f : 0.06f, headshot ? 0.16f : 0.08f);
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
            _viewRoot = new GameObject("Viewmodel").transform;
            _viewRoot.SetParent(_camera.transform, false);

            int viewLayer = LayerMask.NameToLayer("ViewModel");
            if (viewLayer >= 0)
            {
                var vmCamGo = new GameObject("ViewmodelCamera");
                vmCamGo.transform.SetParent(_camera.transform, false);
                vmCamGo.transform.localPosition = Vector3.zero;
                vmCamGo.transform.localRotation = Quaternion.identity;
                var vmCam = vmCamGo.AddComponent<Camera>();
                vmCam.clearFlags = CameraClearFlags.Depth;
                vmCam.depth = _camera.UnityCamera.depth + 1f;
                vmCam.fieldOfView = _camera.UnityCamera.fieldOfView;
                vmCam.nearClipPlane = 0.02f;
                vmCam.farClipPlane = 8f;
                vmCam.allowHDR = false;
                vmCam.cullingMask = 1 << viewLayer;
                _camera.UnityCamera.cullingMask &= ~(1 << viewLayer);
                var vmLight = vmCamGo.AddComponent<Light>();
                vmLight.type = LightType.Directional;
                vmLight.color = new Color(1f, 0.97f, 0.92f);
                vmLight.intensity = 1.15f;
                vmLight.cullingMask = 1 << viewLayer;
                vmLight.shadows = LightShadows.None;
            }

            _hipPos = new[]
            {
                new Vector3(0.22f, -0.16f, 0.36f),
                new Vector3(0.20f, -0.24f, 0.46f),
                new Vector3(0.14f, -0.30f, 0.58f)
            };
            _hipEuler = new[]
            {
                new Vector3(2f, 8f, -4f),
                new Vector3(3f, 4f, -3f),
                new Vector3(8f, 2f, -6f)
            };
            float[] scales = { 0.22f, 0.28f, 0.34f };

            string[] paths = { GameAssets.Pistol, GameAssets.Smg, GameAssets.Shotgun };
            _gunVisuals = new GameObject[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                GameObject gun = GameAssets.TryInstantiate(paths[i], _viewRoot);
                if (gun == null)
                {
                    continue;
                }

                foreach (Collider collider in gun.GetComponentsInChildren<Collider>())
                {
                    Destroy(collider);
                }

                foreach (Renderer renderer in gun.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                StyleWeapon(gun, i);
                AlignViewmodel(gun, scales[i]);
                if (viewLayer >= 0)
                {
                    GameAssets.SetLayerRecursively(gun, viewLayer);
                }

                _gunVisuals[i] = gun;
                gun.SetActive(false);
            }

            if (_gunVisuals[0] == null)
            {
                var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gun.name = "Rifle";
                gun.transform.SetParent(_viewRoot, false);
                gun.transform.localPosition = new Vector3(0.2f, -0.2f, 0.45f);
                gun.transform.localRotation = Quaternion.identity;
                gun.transform.localScale = new Vector3(0.05f, 0.05f, 0.45f);
                gun.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Create(new Color(0.08f, 0.08f, 0.09f), 0.6f, 0.4f);
                Destroy(gun.GetComponent<Collider>());
                if (viewLayer >= 0)
                {
                    GameAssets.SetLayerRecursively(gun, viewLayer);
                }

                _gunVisuals[0] = gun;
            }

            _muzzle = new GameObject("Muzzle").transform;
            _muzzle.SetParent(_viewRoot, false);
            _muzzle.localPosition = new Vector3(0.05f, -0.02f, 0.72f);
        }

        private static void StyleWeapon(GameObject gun, int index)
        {
            string[] hide = index switch
            {
                0 => new[] { "Scope", "Stock", "Cage", "Foregrip" },
                1 => new[] { "Scope" },
                _ => new[] { "Scope", "Cage" }
            };

            Transform[] parts = gun.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == gun.transform)
                {
                    continue;
                }

                string n = parts[i].name;
                for (int h = 0; h < hide.Length; h++)
                {
                    if (n.IndexOf(hide[h], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        parts[i].gameObject.SetActive(false);
                        break;
                    }
                }
            }

            Color tint = index switch
            {
                0 => new Color(0.12f, 0.12f, 0.14f, 1f),
                1 => new Color(0.18f, 0.22f, 0.28f, 1f),
                _ => new Color(0.28f, 0.18f, 0.10f, 1f)
            };
            foreach (Renderer renderer in gun.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.gameObject.activeSelf)
                {
                    continue;
                }

                Material mat = renderer.material;
                mat.color = tint;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", tint);
                }

                renderer.material = mat;
            }
        }

        private static void AlignViewmodel(GameObject gun, float scale)
        {
            Transform t = gun.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * scale;
            Bounds? bounds = GameAssets.WorldBounds(gun);
            if (!bounds.HasValue)
            {
                return;
            }

            Vector3 size = bounds.Value.size;
            if (size.x >= size.z && size.x >= size.y)
            {
                t.localRotation = Quaternion.Euler(0f, -90f, 0f);
            }
            else if (size.y >= size.z && size.y >= size.x)
            {
                t.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }

            bounds = GameAssets.WorldBounds(gun);
            if (!bounds.HasValue || t.parent == null)
            {
                return;
            }

            Vector3 localCenter = t.parent.InverseTransformPoint(bounds.Value.center);
            t.localPosition -= localCenter;
        }

        private void AnimateViewmodel()
        {
            if (_viewRoot == null || _hipPos == null)
            {
                return;
            }

            _kick = Mathf.MoveTowards(_kick, 0f, Time.deltaTime * 2.4f);
            Vector3 hip = _hipPos[_index];
            Vector3 euler = _hipEuler[_index];
            float bob = Time.time * (_input.SprintHeld ? 10f : 7f);
            float move = _input.Move.magnitude;
            hip += new Vector3(Mathf.Sin(bob) * 0.012f, Mathf.Abs(Mathf.Cos(bob)) * -0.01f, 0f) * move;
            hip += new Vector3(0f, _kick * 0.08f, -_kick * 0.12f);
            euler += new Vector3(-_kick * 18f, 0f, _kick * 6f);
            if (Reloading)
            {
                hip += new Vector3(0.04f, -0.12f, -0.06f);
                euler += new Vector3(18f, -8f, 12f);
            }

            _viewRoot.localPosition = hip;
            _viewRoot.localRotation = Quaternion.Euler(euler);
        }
    }
}
