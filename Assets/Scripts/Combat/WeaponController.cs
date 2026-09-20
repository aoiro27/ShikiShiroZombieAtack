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
        private GameSession _session;
        private Transform _muzzle;
        private Transform _viewRoot;
        private Vector3[] _hipPos;
        private Vector3[] _hipEuler;
        private float _kick;
        private int _hurtMask;
        private int _blockMask;
        private readonly RaycastHit[] _hurtHits = new RaycastHit[16];
        private readonly IDamageable[] _shotVictims = new IDamageable[8];
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

        public void Initialize(GameInput input, PlayerMotor motor, TpsCamera camera, CombatFx fx, ProceduralSfx sfx, GameSession session)
        {
            _input = input;
            _motor = motor;
            _camera = camera;
            _fx = fx;
            _sfx = sfx;
            _session = session;
            _hurtMask = LayerMask.GetMask("Zombie");
            _blockMask = LayerMask.GetMask("Obstacle", "Ground", "Default");
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
            if (_input == null || Time.timeScale <= 0.001f)
            {
                return;
            }

            if (_session != null && _session.State != SessionState.Playing)
            {
                AnimateViewmodel();
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
            bool thick = Current.Pellets <= 1;
            int victimCount = 0;
            int wallFx = 0;
            for (int i = 0; i < Current.Pellets; i++)
            {
                Vector3 dir = Quaternion.Euler(
                    UnityEngine.Random.Range(-Current.SpreadDegrees, Current.SpreadDegrees),
                    UnityEngine.Random.Range(-Current.SpreadDegrees, Current.SpreadDegrees),
                    0f) * center;

                if (!TryHit(origin, dir, Current.Range, thick, out RaycastHit hit, out IDamageable damageable))
                {
                    if (i < 3)
                    {
                        _fx.Tracer(muzzlePos, origin + dir * Mathf.Min(Current.Range, 28f));
                    }

                    continue;
                }

                if (i < 5)
                {
                    _fx.Tracer(muzzlePos, hit.point);
                }

                if (damageable == null || !damageable.IsAlive)
                {
                    if (wallFx < 2)
                    {
                        _fx.Impact(hit.point, hit.normal);
                        if (wallFx == 0)
                        {
                            _sfx.PlayImpact();
                        }

                        wallFx++;
                    }

                    continue;
                }

                if (AlreadyHit(damageable, victimCount) && Current.Pellets <= 1)
                {
                    continue;
                }

                if (victimCount < _shotVictims.Length)
                {
                    _shotVictims[victimCount++] = damageable;
                }

                bool headshot = hit.point.y - hit.collider.bounds.min.y > hit.collider.bounds.size.y * 0.72f;
                float amount = Current.Damage * (headshot ? 2.4f : 1f);
                damageable.ApplyDamage(new DamageInfo(amount, hit.point, dir, headshot, Current.Id));
            }

            if (victimCount > 0)
            {
                _camera.Shake(0.1f + 0.04f * victimCount, 0.12f);
            }
        }

        private bool AlreadyHit(IDamageable hurt, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_shotVictims[i] == hurt)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryHit(Vector3 origin, Vector3 dir, float range, bool thick, out RaycastHit hit, out IDamageable damageable)
        {
            hit = default;
            damageable = null;
            origin += dir * 0.05f;

            float zombieDist = float.PositiveInfinity;
            RaycastHit zombieHit = default;
            IDamageable zombie = null;
            if (_hurtMask != 0)
            {
                if (thick)
                {
                    int hurtCount = Physics.SphereCastNonAlloc(origin, 0.16f, dir, _hurtHits, range, _hurtMask, QueryTriggerInteraction.Collide);
                    if (hurtCount <= 0)
                    {
                        hurtCount = Physics.RaycastNonAlloc(origin, dir, _hurtHits, range, _hurtMask, QueryTriggerInteraction.Collide);
                    }

                    for (int i = 0; i < hurtCount; i++)
                    {
                        ConsiderZombie(_hurtHits[i], ref zombieDist, ref zombieHit, ref zombie);
                    }
                }
                else if (Physics.Raycast(origin, dir, out RaycastHit zh, range, _hurtMask, QueryTriggerInteraction.Collide))
                {
                    ConsiderZombie(zh, ref zombieDist, ref zombieHit, ref zombie);
                }
            }

            float blockDist = float.PositiveInfinity;
            RaycastHit blockHit = default;
            if (_blockMask != 0 && Physics.Raycast(origin, dir, out RaycastHit bh, range, _blockMask, QueryTriggerInteraction.Ignore))
            {
                blockDist = bh.distance;
                blockHit = bh;
            }

            const float wallSlop = 0.6f;
            if (zombie != null && zombieDist <= blockDist + wallSlop)
            {
                hit = zombieHit;
                damageable = zombie;
                return true;
            }

            if (blockDist < float.PositiveInfinity)
            {
                hit = blockHit;
                return true;
            }

            return false;
        }

        private static void ConsiderZombie(RaycastHit candidate, ref float zombieDist, ref RaycastHit zombieHit, ref IDamageable zombie)
        {
            if (candidate.collider == null)
            {
                return;
            }

            var hurt = candidate.collider.GetComponentInParent<IDamageable>();
            if (hurt == null || !hurt.IsAlive || hurt is PlayerVitality)
            {
                return;
            }

            if (candidate.distance < zombieDist)
            {
                zombieDist = candidate.distance;
                zombieHit = candidate;
                zombie = hurt;
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
                vmCam.allowMSAA = false;
                vmCam.enabled = true;
                vmCam.targetDisplay = 0;
                vmCam.stereoTargetEye = StereoTargetEyeMask.None;
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

            _gunVisuals = new GameObject[3];
            _gunAnimators = new Animator[3];
            if (TryAttachInfimaViewmodel(viewLayer))
            {
                _muzzle = new GameObject("Muzzle").transform;
                _muzzle.SetParent(_camera.transform, false);
                PlaceMuzzle();
                return;
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
            GameAssets.RepairBrokenShaders(character);
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
            foreach (Camera camera in character.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }

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
