using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShikiShiro
{
    [DefaultExecutionOrder(-100)]
    public sealed class Bootstrap : MonoBehaviour
    {
        private static bool _restarting;
        private static bool _bootLock;
        private bool _booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _restarting = false;
            _bootLock = false;
            HordeDirector.ResetStaticRun();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            _restarting = false;
            var existing = FindAnyObjectByType<Bootstrap>();
            if (existing != null)
            {
                existing.BootIfNeeded();
                return;
            }

            var root = new GameObject("GameRoot");
            root.AddComponent<Bootstrap>();
        }

        public static void RestartRun()
        {
            if (_restarting)
            {
                return;
            }

            _restarting = true;
            _bootLock = false;
            HordeDirector.ResetStaticRun();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
            {
                SceneManager.LoadScene(scene.path);
                return;
            }

            SceneManager.LoadScene("Main");
        }

        private void Awake()
        {
            BootIfNeeded();
        }

        private void BootIfNeeded()
        {
            if (_booted || _bootLock)
            {
                return;
            }

            _bootLock = true;
            _booted = true;
            _restarting = false;
            Input.simulateMouseWithTouches = false;
            StripForeignSceneObjects();
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            QualitySettings.vSyncCount = 0;

            var config = new GameConfig();
            var services = new GameObject("Systems");
            services.transform.SetParent(transform, false);

            var session = services.AddComponent<GameSession>();
            session.Initialize(config);

            var input = services.AddComponent<GameInput>();
            var fx = services.AddComponent<CombatFx>();
            fx.Initialize();
            var sfx = services.AddComponent<ProceduralSfx>();
            sfx.Initialize();

            int playerLayer = LayerMask.NameToLayer("Player");
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(transform, false);
            if (playerLayer >= 0)
            {
                playerGo.layer = playerLayer;
            }

            try
            {
                playerGo.tag = "Player";
            }
            catch (UnityException)
            {
            }

            playerGo.transform.position = new Vector3(0f, 0.1f, -8f);
            var motor = playerGo.AddComponent<PlayerMotor>();
            motor.Initialize(input, config);
            var vitality = playerGo.AddComponent<PlayerVitality>();
            vitality.Initialize(config, session);

            var camGo = new GameObject("FpsCamera");
            camGo.transform.SetParent(transform, false);
            try
            {
                camGo.tag = "MainCamera";
            }
            catch (UnityException)
            {
            }

            var cameraRig = camGo.AddComponent<TpsCamera>();
            cameraRig.Initialize(motor, config);
            sfx.StartBgm();
            sfx.BindSession(session);

            var world = new GameObject("World").transform;
            world.SetParent(transform, false);
            var arena = new ArenaBuilder(world);
            arena.Build();
            motor.BindArena(arena);
            Physics.SyncTransforms();
            var cc = playerGo.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            playerGo.transform.position = arena.SpawnPoint;
            if (cc != null)
            {
                cc.enabled = true;
            }

            DisableOtherCameras(cameraRig.UnityCamera);
            cameraRig.UnityCamera.enabled = true;
            cameraRig.UnityCamera.targetDisplay = 0;

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, 130f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.74f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.56f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.3f, 0.26f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.72f, 0.8f, 0.88f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0012f;

            var weapons = playerGo.AddComponent<WeaponController>();
            weapons.Initialize(input, motor, cameraRig, fx, sfx, session);

            var hudGo = new GameObject("HudSystem");
            hudGo.transform.SetParent(transform, false);
            var hud = hudGo.AddComponent<HudController>();
            hud.Initialize(vitality, weapons, session, input, cameraRig);
            input.Bind(hud.MoveStick, hud.LookStick);

            var horde = services.AddComponent<HordeDirector>();
            horde.Initialize(config, session, arena, playerGo.transform, fx, sfx, hud);
            hud.BindMinimap(playerGo.transform, arena, horde);
            services.AddComponent<RestartOnTap>().Bind(session);

            session.BeginRun();
            HordeDirector.ResetStaticRun();
            horde.StartWaves();
            hud.Announce("街区マップ");
            DisableDuplicateDirectors(horde);
        }

        private static void DisableDuplicateDirectors(HordeDirector keep)
        {
            HordeDirector[] all = FindObjectsByType<HordeDirector>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != keep)
                {
                    all[i].StopAllCoroutines();
                    all[i].enabled = false;
                }
            }
        }

        private void StripForeignSceneObjects()
        {
            for (int s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject go = roots[i];
                    if (go == gameObject)
                    {
                        continue;
                    }

                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void DisableOtherCameras(Camera keep)
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != keep)
                {
                    cameras[i].enabled = false;
                }
            }
        }
    }
}
