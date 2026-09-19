using UnityEngine;

namespace ShikiShiro
{
    [DefaultExecutionOrder(-100)]
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;

            foreach (Camera existing in FindObjectsOfType<Camera>())
            {
                existing.gameObject.SetActive(false);
            }

            var config = new GameConfig();
            var services = new GameObject("Systems");

            var session = services.AddComponent<GameSession>();
            session.Initialize(config);

            var input = services.AddComponent<GameInput>();
            var fx = services.AddComponent<CombatFx>();
            fx.Initialize();
            var sfx = services.AddComponent<ProceduralSfx>();
            sfx.Initialize();

            var world = new GameObject("World").transform;
            var arena = new ArenaBuilder(world);
            arena.Build();

            var sunGo = new GameObject("Moon");
            sunGo.transform.rotation = Quaternion.Euler(28f, 140f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.45f, 0.52f, 0.7f);
            sun.intensity = 0.35f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.06f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.028f;
            RenderSettings.ambientLight = new Color(0.08f, 0.07f, 0.07f);

            int playerLayer = LayerMask.NameToLayer("Player");
            var playerGo = new GameObject("Player");
            if (playerLayer >= 0)
            {
                playerGo.layer = playerLayer;
            }

            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0f, 0.1f, -4f);
            var motor = playerGo.AddComponent<PlayerMotor>();
            motor.Initialize(input, config);
            var vitality = playerGo.AddComponent<PlayerVitality>();
            vitality.Initialize(config, session);

            var camGo = new GameObject("TpsCamera");
            var cameraRig = camGo.AddComponent<TpsCamera>();
            cameraRig.Initialize(motor, config);

            var weapons = playerGo.AddComponent<WeaponController>();
            weapons.Initialize(input, motor, cameraRig, fx, sfx);

            var hudGo = new GameObject("HudSystem");
            var hud = hudGo.AddComponent<HudController>();
            hud.Initialize(vitality, weapons, session, input);
            input.Bind(hud.MoveStick, hud.LookStick);

            var horde = services.AddComponent<HordeDirector>();
            horde.Initialize(config, session, arena, playerGo.transform, fx, sfx, hud);

            services.AddComponent<RestartOnTap>().Bind(session);

            session.BeginRun();
            horde.StartWaves();
            hud.Announce("式城ゾンビアタック");
        }
    }
}
