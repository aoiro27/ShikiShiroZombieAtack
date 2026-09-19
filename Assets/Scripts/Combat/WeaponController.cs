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
        private Animator[] _gunAnimators;
        private Camera _viewCam;
        private Transform _packView;
        private Vector3 _packBasePos;
        private Quaternion _packBaseRot;
        private Animator _packAnimator;
        private int _packFireLayer;

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
            PlaceMuzzle();
        }

        private void ShowEquippedGun()
        {
            if (_gunVisuals == null)
            {
                return;
            }

            GameObject shown = _index >= 0 && _index < _gunVisuals.Length ? _gunVisuals[_index] : null;
            for (int i = 0; i < _gunVisuals.Length; i++)
            {
                if (_gunVisuals[i] != null && _gunVisuals[i] != shown)
                {
                    _gunVisuals[i].SetActive(false);
                }
            }

            if (shown != null)
            {
                shown.SetActive(true);
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
            PlayPackFire();
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
                _viewCam = vmCam;
                vmCam.clearFlags = CameraClearFlags.Depth;
                vmCam.depth = _camera.UnityCamera.depth + 1f;
                vmCam.fieldOfView = 50f;
                vmCam.nearClipPlane = 0.01f;
                vmCam.farClipPlane = 3.5f;
                vmCam.allowHDR = false;
                vmCam.cullingMask = 1 << viewLayer;
                _camera.UnityCamera.cullingMask &= ~(1 << viewLayer);
                var vmLight = vmCamGo.AddComponent<Light>();
                vmLight.type = LightType.Directional;
                vmLight.color = new Color(1f, 0.97f, 0.92f);
                vmLight.intensity = 1.55f;
                vmLight.cullingMask = 1 << viewLayer;
                vmLight.shadows = LightShadows.None;
                var fillGo = new GameObject("ViewmodelFill");
                fillGo.transform.SetParent(vmCamGo.transform, false);
                fillGo.transform.localPosition = new Vector3(0.25f, 0.2f, 0.15f);
                var fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Point;
                fill.range = 4f;
                fill.intensity = 1.1f;
                fill.color = new Color(1f, 0.95f, 0.88f);
                fill.cullingMask = 1 << viewLayer;
                fill.shadows = LightShadows.None;
            }

            _hipPos = new[]
            {
                new Vector3(0.34f, -0.24f, 0.42f),
                new Vector3(0.30f, -0.28f, 0.46f),
                new Vector3(0.28f, -0.32f, 0.52f)
            };
            _hipEuler = new[]
            {
                new Vector3(6f, -20f, 4f),
                new Vector3(5f, -16f, 3f),
                new Vector3(8f, -14f, 6f)
            };
            float[] lengths = { 0.55f, 0.78f, 0.88f };

            string[] paths = { GameAssets.Pistol, GameAssets.Smg, GameAssets.Shotgun };
            _gunVisuals = new GameObject[paths.Length];
            _gunAnimators = new Animator[paths.Length];
            if (TryAttachInfimaViewmodel(viewLayer))
            {
                _muzzle = new GameObject("Muzzle").transform;
                _muzzle.SetParent(_camera.transform, false);
                PlaceMuzzle();
                return;
            }

            TryAttachAlterunaPistol(viewLayer, lengths[0]);
            for (int i = 0; i < paths.Length; i++)
            {
                if (_gunVisuals[i] != null)
                {
                    continue;
                }

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
                AlignViewmodel(gun, lengths[i]);
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

        private void TryAttachAlterunaPistol(int viewLayer, float length)
        {
            GameObject player = GameAssets.TryInstantiate("Assets/AlterunaFPS/Prefab/Player.prefab", _viewRoot);
            if (player == null)
            {
                return;
            }

            Transform gunRoot = FindDeep(player.transform, "GunRoot");
            if (gunRoot == null)
            {
                Destroy(player);
                return;
            }

            foreach (CharacterController controller in player.GetComponentsInChildren<CharacterController>())
            {
                Destroy(controller);
            }

            gunRoot.SetParent(_viewRoot, false);
            gunRoot.localPosition = Vector3.zero;
            gunRoot.localRotation = Quaternion.identity;
            gunRoot.localScale = Vector3.one;
            Destroy(player);

            foreach (Collider collider in gunRoot.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }

            foreach (Renderer renderer in gunRoot.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            AlignViewmodel(gunRoot.gameObject, length);
            if (viewLayer >= 0)
            {
                GameAssets.SetLayerRecursively(gunRoot.gameObject, viewLayer);
            }

            _gunVisuals[0] = gunRoot.gameObject;
            _gunAnimators[0] = gunRoot.GetComponentInChildren<Animator>();
            gunRoot.gameObject.SetActive(false);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        }

        private static void AlignViewmodel(GameObject gun, float length)
        {
            Transform t = gun.transform;
            Transform parent = t.parent;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            Transform barrel = FindPart(gun, "Main_Barrel", "Muzzle", "Barrel_Upper");
            Transform stock = FindPart(gun, "Stock_Main1", "Stock_Lower_Part", "Stock_holder");
            if (barrel != null && stock != null && parent != null)
            {
                Vector3 dir = parent.InverseTransformDirection(barrel.position - stock.position);
                if (dir.sqrMagnitude > 0.0001f)
                {
                    t.localRotation = Quaternion.FromToRotation(dir.normalized, Vector3.forward);
                }
            }
            else
            {
                Bounds? bounds = GameAssets.WorldBounds(gun);
                if (bounds.HasValue)
                {
                    Vector3 size = bounds.Value.size;
                    if (size.x >= size.z && size.x >= size.y)
                    {
                        t.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    }
                    else if (size.y >= size.z && size.y >= size.x)
                    {
                        t.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    }
                }
            }

            if (parent == null || !LocalAabb(gun, parent, out Vector3 min, out Vector3 max))
            {
                return;
            }

            float current = Mathf.Max(0.001f, max.z - min.z);
            t.localScale *= length / current;
            if (!LocalAabb(gun, parent, out min, out max))
            {
                return;
            }

            Vector3 pivot = new Vector3(
                (min.x + max.x) * 0.5f,
                min.y + (max.y - min.y) * 0.2f,
                min.z + (max.z - min.z) * 0.16f);
            t.localPosition -= pivot;
        }

        private void PlaceMuzzle()
        {
            if (_muzzle == null || _viewRoot == null || _gunVisuals == null)
            {
                return;
            }

            GameObject gun = _gunVisuals[_index];
            if (gun == null)
            {
                return;
            }

            Transform barrel = FindPart(gun, "Muzzle", "Main_Barrel", "SOCKET");
            if (barrel != null)
            {
                _muzzle.position = barrel.position;
                _muzzle.rotation = _viewRoot.rotation;
                return;
            }

            if (LocalAabb(gun, _viewRoot, out Vector3 min, out Vector3 max))
            {
                _muzzle.localPosition = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.55f, max.z);
                _muzzle.localRotation = Quaternion.identity;
            }
        }

        private static Transform FindPart(GameObject root, params string[] names)
        {
            Transform[] parts = root.GetComponentsInChildren<Transform>(true);
            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].name.IndexOf(names[n], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return parts[i];
                    }
                }
            }

            return null;
        }

        private static bool LocalAabb(GameObject go, Transform space, out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            bool any = false;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled || !renderers[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds b = renderers[i].bounds;
                Vector3 c = b.center;
                Vector3 e = b.extents;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 local = space.InverseTransformPoint(c + new Vector3(e.x * x, e.y * y, e.z * z));
                            min = Vector3.Min(min, local);
                            max = Vector3.Max(max, local);
                            any = true;
                        }
                    }
                }
            }

            return any;
        }

        private void AnimateViewmodel()
        {
            _kick = Mathf.MoveTowards(_kick, 0f, Time.deltaTime * 2.4f);
            float bob = Time.time * (_input.SprintHeld ? 10f : 7f);
            float move = _input == null ? 0f : _input.Move.magnitude;
            if (_packView != null)
            {
                Vector3 sway = new Vector3(Mathf.Sin(bob) * 0.006f, Mathf.Abs(Mathf.Cos(bob)) * -0.004f, 0f) * move;
                sway += new Vector3(0f, _kick * 0.02f, -_kick * 0.04f);
                _packView.localPosition = _packBasePos + sway;
                _packView.localRotation = _packBaseRot * Quaternion.Euler(-_kick * 10f, 0f, _kick * 4f);
                return;
            }

            if (_viewRoot == null || _hipPos == null)
            {
                return;
            }

            Vector3 hip = _hipPos[_index];
            Vector3 euler = _hipEuler[_index];
            hip += new Vector3(Mathf.Sin(bob) * 0.018f, Mathf.Abs(Mathf.Cos(bob)) * -0.014f, 0f) * move;
            hip += new Vector3(0f, _kick * 0.05f, -_kick * 0.18f);
            euler += new Vector3(-_kick * 22f, 0f, _kick * 8f);
            if (Reloading)
            {
                hip += new Vector3(0.04f, -0.12f, -0.06f);
                euler += new Vector3(18f, -8f, 12f);
            }

            _viewRoot.localPosition = hip;
            _viewRoot.localRotation = Quaternion.Euler(euler);
        }

        private void PlayPackFire()
        {
            if (_packAnimator != null)
            {
                if (_packFireLayer >= 0)
                {
                    _packAnimator.CrossFade("Fire", 0.05f, _packFireLayer, 0f);
                }
                else
                {
                    _packAnimator.Play("Fire", 0, 0f);
                }
            }

            if (_gunAnimators != null && _gunAnimators[_index] != null)
            {
                _gunAnimators[_index].Play("Fire", 0, 0f);
            }

            GameObject gun = _gunVisuals != null ? _gunVisuals[_index] : null;
            if (gun == null)
            {
                return;
            }

            ParticleSystem[] flashes = gun.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < flashes.Length; i++)
            {
                flashes[i].Play(true);
            }
        }

        private bool TryAttachInfimaViewmodel(int viewLayer)
        {
            GameObject character = GameAssets.TryInstantiate(GameAssets.InfimaFps, _camera.transform);
            if (character == null)
            {
                return false;
            }

            StripInfimaRuntime(character);
            StripMissingScripts(character);
            BindAnimEvents(character);
            Transform socket = FindDeep(character.transform, "SOCKET_Camera");
            if (socket != null)
            {
                AlignChildSoSocketMatchesParent(character.transform, socket, _camera.transform);
            }
            else
            {
                character.transform.localPosition = Vector3.zero;
                character.transform.localRotation = Quaternion.identity;
            }

            if (viewLayer >= 0)
            {
                GameAssets.SetLayerRecursively(character, viewLayer);
            }

            if (_viewCam != null)
            {
                _viewCam.fieldOfView = 90f;
                _viewCam.nearClipPlane = 0.01f;
            }

            GameObject handgun = FindNamed(character.transform, "P_LPSP_WEP_Handgun");
            if (handgun == null)
            {
                handgun = FindNamed(character.transform, "Handgun");
            }

            GameObject rifle = FindNamed(character.transform, "P_LPSP_WEP_AR");
            if (rifle == null)
            {
                rifle = FindNamed(character.transform, "AR_01");
            }

            if (handgun == null && rifle == null)
            {
                Destroy(character);
                return false;
            }

            _gunVisuals = new GameObject[3];
            _gunAnimators = new Animator[3];
            _gunVisuals[0] = handgun != null ? handgun : rifle;
            _gunVisuals[1] = rifle != null ? rifle : handgun;
            Transform gunParent = _gunVisuals[1] != null ? _gunVisuals[1].transform.parent : character.transform;
            _gunVisuals[2] = AttachAlterunaShotgun(gunParent, _gunVisuals[1], viewLayer);
            if (_gunVisuals[2] == null)
            {
                _gunVisuals[2] = _gunVisuals[1];
            }
            for (int i = 0; i < _gunVisuals.Length; i++)
            {
                if (_gunVisuals[i] != null)
                {
                    _gunAnimators[i] = _gunVisuals[i].GetComponentInChildren<Animator>(true);
                }
            }

            _packView = character.transform;
            _packBasePos = _packView.localPosition;
            _packBaseRot = _packView.localRotation;
            _packAnimator = character.GetComponent<Animator>();
            if (_packAnimator == null)
            {
                _packAnimator = character.GetComponentInChildren<Animator>();
            }

            _packFireLayer = _packAnimator != null ? _packAnimator.GetLayerIndex("Layer Overlay") : -1;
            BindAnimEvents(character);
            ShowEquippedGun();
            return true;
        }

        private static GameObject AttachAlterunaShotgun(Transform parent, GameObject poseSource, int viewLayer)
        {
            GameObject gun = GameAssets.TryInstantiate(GameAssets.ShotgunPump, parent);
            if (gun == null)
            {
                gun = GameAssets.TryInstantiate(GameAssets.ShotgunAuto, parent);
            }

            if (gun == null)
            {
                return null;
            }

            gun.name = "Shotgun_Pump";
            foreach (Collider collider in gun.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.Destroy(collider);
            }

            foreach (Renderer renderer in gun.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            if (poseSource != null)
            {
                gun.transform.localPosition = poseSource.transform.localPosition;
                gun.transform.localRotation = poseSource.transform.localRotation;
                gun.transform.localScale = Vector3.one;
                Bounds? poseBounds = GameAssets.WorldBounds(poseSource);
                Bounds? gunBounds = GameAssets.WorldBounds(gun);
                if (poseBounds.HasValue && gunBounds.HasValue)
                {
                    float poseLen = Longest(poseBounds.Value.size);
                    float gunLen = Longest(gunBounds.Value.size);
                    if (gunLen > 0.001f)
                    {
                        gun.transform.localScale *= poseLen / gunLen;
                    }
                }
            }

            if (viewLayer >= 0)
            {
                GameAssets.SetLayerRecursively(gun, viewLayer);
            }

            return gun;
        }

        private static float Longest(Vector3 size)
        {
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        private static void BindAnimEvents(GameObject root)
        {
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i].GetComponent<ViewmodelAnimEvents>() == null)
                {
                    animators[i].gameObject.AddComponent<ViewmodelAnimEvents>();
                }
            }
        }

        private static void StripMissingScripts(GameObject root)
        {
#if UNITY_EDITOR
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
            }
#endif
        }

        private static void StripInfimaRuntime(GameObject character)
        {
            foreach (Canvas canvas in character.GetComponentsInChildren<Canvas>(true))
            {
                canvas.enabled = false;
            }

            foreach (AudioListener listener in character.GetComponentsInChildren<AudioListener>(true))
            {
                listener.enabled = false;
            }

            foreach (Rigidbody body in character.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            foreach (Collider collider in character.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Behaviour behaviour in character.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName.Contains("PlayerInput") || typeName.Contains("AudioReverbZone") || typeName.Contains("PostProcess"))
                {
                    behaviour.enabled = false;
                }
            }
        }

        private static void AlignChildSoSocketMatchesParent(Transform character, Transform socket, Transform camera)
        {
            character.SetParent(null, true);
            character.rotation = camera.rotation * Quaternion.Inverse(socket.rotation) * character.rotation;
            character.position = camera.position + (character.position - socket.position);
            character.SetParent(camera, true);
        }

        private static GameObject FindNamed(Transform root, string token)
        {
            Transform[] parts = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return parts[i].gameObject;
                }
            }

            return null;
        }
    }
}
