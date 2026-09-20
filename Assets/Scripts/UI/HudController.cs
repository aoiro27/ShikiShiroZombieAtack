using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShikiShiro
{
    public sealed class HudController : MonoBehaviour
    {
        private Image _healthFill;
        private RectTransform _healthPlate;
        private Image _hurt;
        private Image _vignette;
        private Image _biteMark;
        private Image[] _splats;
        private float[] _splatLife;
        private Vector2[] _splatVel;
        private int _splatCursor;
        private Image[] _weaponIcons;
        private Image[] _weaponFrames;
        private Text _wave;
        private Text _score;
        private Text _announce;
        private Text _announceSub;
        private Text _gameOver;
        private Image _clearFlash;
        private float _announceUntil;
        private float _announcePunch = 1f;
        private float _clearFlashAlpha;
        private float _flash;
        private float _biteAlpha;
        private float _biteScale;
        private float _vignettePulse;
        private float _healthPunch;
        private TpsCamera _camera;
        private PlayerVitality _vitality;
        private WeaponController _weapons;
        private GameSession _session;
        private GameInput _input;
        private CanvasGroup _pauseGroup;
        private GameObject _countdownRoot;
        private Text _countdownWave;
        private Text _countdownNumber;
        private float _countdownPunch = 1f;
        private MinimapHud _minimap;
        private HitPopupHud _hitPopups;
        private bool _paused;

        public VirtualJoystick MoveStick { get; private set; }
        public VirtualJoystick LookStick { get; private set; }

        public void Initialize(PlayerVitality vitality, WeaponController weapons, GameSession session, GameInput input, TpsCamera camera)
        {
            _vitality = vitality;
            _weapons = weapons;
            _session = session;
            _input = input;
            _camera = camera;
            BuildCanvas();
            vitality.HealthChanged += OnHealth;
            vitality.Damaged += OnDamaged;
            weapons.MagazineChanged += RefreshAmmo;
            session.ScoreChanged += OnScoreChanged;
            session.ComboChanged += OnScoreChanged;
            session.HitPopup += OnHitPopup;
            session.WaveChanged += OnWaveChanged;
            session.StateChanged += OnState;
            OnHealth(vitality.CurrentHealth, vitality.MaxHealth);
            RefreshAmmo();
            RefreshScore();
            RefreshWave();
        }

        private void OnDestroy()
        {
            if (_vitality != null)
            {
                _vitality.HealthChanged -= OnHealth;
                _vitality.Damaged -= OnDamaged;
            }

            if (_weapons != null)
            {
                _weapons.MagazineChanged -= RefreshAmmo;
            }

            if (_session != null)
            {
                _session.ScoreChanged -= OnScoreChanged;
                _session.ComboChanged -= OnScoreChanged;
                _session.WaveChanged -= OnWaveChanged;
                _session.StateChanged -= OnState;
                _session.HitPopup -= OnHitPopup;
            }
        }

        public void BindMinimap(Transform player, ArenaBuilder arena, HordeDirector horde)
        {
            if (_minimap != null)
            {
                _minimap.Bind(player, arena, horde);
            }
        }

        public void Announce(string text)
        {
            ShowBanner(text, string.Empty, new Color(1f, 0.82f, 0.45f), 54, 2.2f, 1.12f, 0f);
        }

        public void ShowWaveClear(int wave)
        {
            ClearBanners();
            if (_countdownRoot == null)
            {
                return;
            }

            _countdownRoot.SetActive(true);
            if (_countdownWave != null)
            {
                _countdownWave.text = "ウェーブ  " + wave;
                _countdownWave.resizeTextMaxSize = 110;
            }

            if (_countdownNumber != null)
            {
                _countdownNumber.text = "クリア!";
                _countdownNumber.color = new Color(1f, 0.92f, 0.38f);
                _countdownNumber.resizeTextMaxSize = 280;
                _countdownPunch = 1.22f;
                _countdownNumber.rectTransform.localScale = Vector3.one * _countdownPunch;
            }

            _clearFlashAlpha = 0.72f;
        }

        public void ShowWaveStart(int wave)
        {
            ShowBanner("ウェーブ  " + wave, "ちかづいてる", new Color(1f, 0.45f, 0.22f), 64, 2.4f, 1.22f, 0.18f);
        }

        public void ShowBossAppear(int wave, int count)
        {
            string sub = count <= 1 ? "このウェーブは1体" : "このウェーブは同時に" + count + "体";
            ShowBanner("ボス出現  WAVE " + wave, sub, new Color(1f, 0.22f, 0.18f), 70, 3.2f, 1.28f, 0.28f);
        }

        public void ShowCountdown(int seconds, int wave)
        {
            if (_countdownRoot == null)
            {
                return;
            }

            ClearBanners();
            _countdownRoot.SetActive(true);
            if (_countdownWave != null)
            {
                _countdownWave.text = "ウェーブ  " + wave;
                _countdownWave.resizeTextMaxSize = 88;
            }

            if (_countdownNumber != null)
            {
                _countdownNumber.text = seconds.ToString();
                _countdownNumber.color = Color.white;
                _countdownNumber.resizeTextMaxSize = 420;
                _countdownPunch = 1.42f;
                _countdownNumber.rectTransform.localScale = Vector3.one * _countdownPunch;
            }
        }

        public void HideCountdown()
        {
            if (_countdownRoot != null)
            {
                _countdownRoot.SetActive(false);
            }
        }

        private void ShowBanner(string title, string sub, Color color, int size, float seconds, float punch, float flash)
        {
            _announce.text = title;
            _announce.fontSize = size;
            _announce.color = color;
            _announcePunch = punch;
            if (_announceSub != null)
            {
                _announceSub.text = sub;
                _announceSub.color = new Color(color.r, color.g, color.b, 0.9f);
            }

            _clearFlashAlpha = flash;
            _announceUntil = Time.unscaledTime + seconds;
        }

        private void ClearBanners()
        {
            _announceUntil = 0f;
            if (_announce != null)
            {
                _announce.text = string.Empty;
            }

            if (_announceSub != null)
            {
                _announceSub.text = string.Empty;
            }
        }

        private void Update()
        {
            if (_session != null)
            {
                RefreshWave();
            }

            if (_announceUntil > 0f && Time.unscaledTime > _announceUntil)
            {
                _announce.text = string.Empty;
                if (_announceSub != null)
                {
                    _announceSub.text = string.Empty;
                }

                _announceUntil = 0f;
            }

            float udt = Time.unscaledDeltaTime;
            _announcePunch = Mathf.MoveTowards(_announcePunch, 1f, udt * 2.6f);
            if (_announce != null)
            {
                _announce.rectTransform.localScale = Vector3.one * _announcePunch;
            }

            _clearFlashAlpha = Mathf.MoveTowards(_clearFlashAlpha, 0f, udt * 1.15f);
            if (_clearFlash != null)
            {
                _clearFlash.color = new Color(1f, 0.86f, 0.35f, _clearFlashAlpha);
            }

            _countdownPunch = Mathf.MoveTowards(_countdownPunch, 1f, udt * 2.4f);
            if (_countdownNumber != null)
            {
                _countdownNumber.rectTransform.localScale = Vector3.one * _countdownPunch;
            }

            if (_input != null && _input.PausePressed && CanPause(_session.State))
            {
                TogglePause();
            }

            TickHurtFx();
        }

        private void OnHealth(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (_healthFill != null)
            {
                _healthFill.fillAmount = ratio;
                _healthFill.color = HealthColor(ratio);
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            float intensity = Mathf.Clamp(info.Amount / 14f, 0.7f, 1.85f);
            _flash = Mathf.Max(_flash, 0.48f * intensity);
            _vignettePulse = Mathf.Max(_vignettePulse, 0.7f * intensity);
            _biteAlpha = 1f;
            _biteScale = 1.28f;
            _healthPunch = 1.18f;
            _camera?.BiteKick(info.Direction, intensity);
            PlaceBite(info.Direction, intensity);
            SpawnSplats(info.Direction, intensity);
        }

        private void TickHurtFx()
        {
            float dt = Time.deltaTime;
            _flash = Mathf.MoveTowards(_flash, 0f, dt * 3.6f);
            _vignettePulse = Mathf.MoveTowards(_vignettePulse, 0f, dt * 1.15f);
            _biteAlpha = Mathf.MoveTowards(_biteAlpha, 0f, dt * 1.7f);
            _biteScale = Mathf.MoveTowards(_biteScale, 1f, dt * 2.4f);
            _healthPunch = Mathf.MoveTowards(_healthPunch, 1f, dt * 3.2f);

            float wound = 0f;
            if (_vitality != null && _vitality.MaxHealth > 0f)
            {
                wound = (1f - Mathf.Clamp01(_vitality.CurrentHealth / _vitality.MaxHealth)) * 0.48f;
            }

            if (_hurt != null)
            {
                _hurt.color = new Color(0.72f, 0.02f, 0.02f, _flash);
            }

            if (_vignette != null)
            {
                Color v = _vignette.color;
                v.a = Mathf.Clamp01(wound + _vignettePulse);
                _vignette.color = v;
            }

            if (_biteMark != null)
            {
                Color b = _biteMark.color;
                b.a = _biteAlpha;
                _biteMark.color = b;
                _biteMark.rectTransform.localScale = Vector3.one * _biteScale;
            }

            if (_healthPlate != null)
            {
                _healthPlate.localScale = new Vector3(_healthPunch, _healthPunch, 1f);
            }

            if (_splats == null)
            {
                return;
            }

            for (int i = 0; i < _splats.Length; i++)
            {
                if (_splatLife[i] <= 0f)
                {
                    continue;
                }

                _splatLife[i] -= dt;
                var rt = _splats[i].rectTransform;
                rt.anchoredPosition += _splatVel[i] * dt;
                _splatVel[i] += new Vector2(0f, -90f) * dt;
                Color c = _splats[i].color;
                c.a = Mathf.Clamp01(_splatLife[i] / 0.85f) * 0.82f;
                _splats[i].color = c;
                if (_splatLife[i] <= 0f)
                {
                    _splats[i].enabled = false;
                }
            }
        }

        private void PlaceBite(Vector3 incoming, float intensity)
        {
            if (_biteMark == null)
            {
                return;
            }

            Vector2 dir = ScreenDir(incoming);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var rt = _biteMark.rectTransform;
            rt.localEulerAngles = new Vector3(0f, 0f, ang - 90f);
            rt.anchoredPosition = dir * (90f + 40f * intensity);
            rt.sizeDelta = new Vector2(520f, 520f) * (0.85f + 0.2f * intensity);
        }

        private void SpawnSplats(Vector3 incoming, float intensity)
        {
            if (_splats == null)
            {
                return;
            }

            Vector2 dir = ScreenDir(incoming);
            int count = Mathf.Clamp(Mathf.RoundToInt(4f * intensity), 4, 8);
            for (int n = 0; n < count; n++)
            {
                int i = _splatCursor;
                _splatCursor = (_splatCursor + 1) % _splats.Length;
                var img = _splats[i];
                img.enabled = true;
                img.sprite = UiSprites.BloodSplat;
                float size = Random.Range(90f, 210f) * (0.7f + intensity * 0.35f);
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = dir * Random.Range(40f, 280f) + Random.insideUnitCircle * 160f;
                rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
                img.color = new Color(0.42f, 0.01f, 0.01f, 0.9f);
                _splatLife[i] = Random.Range(0.55f, 1.05f);
                _splatVel[i] = dir * Random.Range(20f, 80f) + new Vector2(Random.Range(-40f, 40f), Random.Range(-20f, 30f));
            }
        }

        private Vector2 ScreenDir(Vector3 incoming)
        {
            Transform cam = _camera != null ? _camera.transform : null;
            if (cam == null)
            {
                return Vector2.down;
            }

            Vector3 dir = incoming.sqrMagnitude > 0.0001f ? -incoming.normalized : -cam.forward;
            Vector2 screen = new Vector2(Vector3.Dot(dir, cam.right), Vector3.Dot(dir, cam.up));
            if (screen.sqrMagnitude < 0.04f)
            {
                screen = Vector2.down;
            }

            return screen.normalized;
        }

        private static Color HealthColor(float ratio)
        {
            if (ratio > 0.5f)
            {
                return Color.Lerp(new Color(0.92f, 0.78f, 0.18f), new Color(0.28f, 0.86f, 0.38f), (ratio - 0.5f) * 2f);
            }

            return Color.Lerp(new Color(0.82f, 0.12f, 0.12f), new Color(0.92f, 0.78f, 0.18f), ratio * 2f);
        }

        private void RefreshAmmo()
        {
            if (_weaponIcons == null || _weaponFrames == null || _weapons == null)
            {
                return;
            }

            int selected = _weapons.EquippedIndex;
            for (int i = 0; i < _weaponIcons.Length; i++)
            {
                bool on = i == selected;
                _weaponIcons[i].color = on ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                _weaponFrames[i].color = on ? new Color(1f, 0.85f, 0.2f, 0.95f) : new Color(0.12f, 0.12f, 0.14f, 0.7f);
            }
        }

        private void OnHitPopup(HitPopupInfo info)
        {
            _hitPopups?.Spawn(info);
        }

        private void OnScoreChanged(int _)
        {
            RefreshScore();
        }

        private void OnWaveChanged(int _)
        {
            RefreshWave();
        }

        private void RefreshScore()
        {
            string combo = _session.Combo > 1 ? $"  COMBO x{_session.Combo}" : string.Empty;
            _score.text = $"SCORE  {_session.Score}{combo}\nBEST  {_session.HighScore}";
        }

        private void RefreshWave()
        {
            _wave.text = $"WAVE {_session.Wave}";
        }

        private void OnState(SessionState state)
        {
            if (state == SessionState.GameOver)
            {
                Time.timeScale = 0f;
                _paused = false;
                HideCountdown();
                if (_pauseGroup != null)
                {
                    _pauseGroup.alpha = 0f;
                    _pauseGroup.blocksRaycasts = false;
                }

                _gameOver.gameObject.SetActive(true);
                _gameOver.text = $"GAME OVER\nWAVE {_session.Wave}   KILLS {_session.Kills}\nSCORE {_session.Score}\nタップ / クリックで再開";
            }
        }

        private static bool CanPause(SessionState state)
        {
            return state == SessionState.Playing || state == SessionState.Countdown || state == SessionState.WaveClear;
        }

        public void TryRestart()
        {
            if (_session.State == SessionState.GameOver)
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
        }

        private void TogglePause()
        {
            _paused = !_paused;
            Time.timeScale = _paused ? 0f : 1f;
            _pauseGroup.alpha = _paused ? 1f : 0f;
            _pauseGroup.blocksRaycasts = _paused;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void BuildCanvas()
        {
            UiSprites.Ensure();
            var canvasGo = new GameObject("HUD");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2388f, 1668f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                es = esGo.AddComponent<EventSystem>();
            }

            foreach (BaseInputModule module in es.GetComponents<BaseInputModule>())
            {
                if (module is StandaloneInputModule)
                {
                    continue;
                }

                module.enabled = false;
                Destroy(module);
            }

            if (es.GetComponent<StandaloneInputModule>() == null)
            {
                es.gameObject.AddComponent<StandaloneInputModule>();
            }

            Rect safe = Screen.safeArea;
            var safeGo = CreatePanel(canvasGo.transform, "Safe", new Color(0, 0, 0, 0));
            var safeRt = safeGo.GetComponent<RectTransform>();
            Vector2 min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            Vector2 max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeRt.anchorMin = min;
            safeRt.anchorMax = max;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;
            safeGo.GetComponent<Image>().raycastTarget = false;

            _healthFill = CreateHealthGauge(safeGo.transform, new Vector2(24, -24), new Vector2(460, 72));
            _wave = CreateHudPlate(safeGo.transform, "Wave", new Vector2(0, -24), new Vector2(280, 78), new Vector2(0.5f, 1f), TextAnchor.MiddleCenter);
            _score = CreateHudPlate(safeGo.transform, "Score", new Vector2(-320, -24), new Vector2(360, 96), new Vector2(1f, 1f), TextAnchor.MiddleRight);

            _announce = CreateText(safeGo.transform, "Announce", Vector2.zero, new Vector2(1200, 140), 54, TextAnchor.MiddleCenter);
            var anRt = _announce.rectTransform;
            anRt.anchorMin = new Vector2(0.5f, 0.64f);
            anRt.anchorMax = new Vector2(0.5f, 0.64f);
            anRt.pivot = new Vector2(0.5f, 0.5f);
            _announce.color = new Color(1f, 0.82f, 0.45f);
            _announce.fontStyle = FontStyle.Bold;

            _announceSub = CreateText(safeGo.transform, "AnnounceSub", Vector2.zero, new Vector2(900, 64), 32, TextAnchor.MiddleCenter);
            var subRt = _announceSub.rectTransform;
            subRt.anchorMin = new Vector2(0.5f, 0.56f);
            subRt.anchorMax = new Vector2(0.5f, 0.56f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            _announceSub.color = new Color(1f, 0.9f, 0.6f, 0.9f);

            _clearFlash = CreatePanel(safeGo.transform, "ClearFlash", new Color(1f, 0.86f, 0.35f, 0f)).GetComponent<Image>();
            var flashRt = _clearFlash.rectTransform;
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
            _clearFlash.raycastTarget = false;

            _gameOver = CreateText(safeGo.transform, "GameOver", Vector2.zero, new Vector2(1100, 420), 44, TextAnchor.MiddleCenter);
            var goRt = _gameOver.rectTransform;
            goRt.anchorMin = new Vector2(0.5f, 0.5f);
            goRt.anchorMax = new Vector2(0.5f, 0.5f);
            goRt.pivot = new Vector2(0.5f, 0.5f);
            _gameOver.gameObject.SetActive(false);
            var restart = _gameOver.gameObject.AddComponent<Button>();
            restart.onClick.AddListener(TryRestart);

            _hurt = CreatePanel(safeGo.transform, "Hurt", new Color(0.72f, 0.02f, 0.02f, 0f)).GetComponent<Image>();
            var hurtRt = _hurt.rectTransform;
            hurtRt.anchorMin = Vector2.zero;
            hurtRt.anchorMax = Vector2.one;
            hurtRt.offsetMin = Vector2.zero;
            hurtRt.offsetMax = Vector2.zero;
            _hurt.raycastTarget = false;

            _vignette = CreateSprite(safeGo.transform, "HurtVignette", UiSprites.HurtVignette, new Color(0.5f, 0.0f, 0.0f, 0f)).GetComponent<Image>();
            var vigRt = _vignette.rectTransform;
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero;
            vigRt.offsetMax = Vector2.zero;
            _vignette.preserveAspect = false;
            _vignette.raycastTarget = false;

            _biteMark = CreateSprite(safeGo.transform, "BiteMark", UiSprites.BiteMark, new Color(0.28f, 0.0f, 0.0f, 0f)).GetComponent<Image>();
            var biteRt = _biteMark.rectTransform;
            biteRt.anchorMin = biteRt.anchorMax = biteRt.pivot = new Vector2(0.5f, 0.5f);
            biteRt.sizeDelta = new Vector2(520f, 520f);
            _biteMark.raycastTarget = false;

            _splats = new Image[8];
            _splatLife = new float[8];
            _splatVel = new Vector2[8];
            for (int i = 0; i < _splats.Length; i++)
            {
                var splat = CreateSprite(safeGo.transform, "Splat" + i, UiSprites.BloodSplat, new Color(0.4f, 0.0f, 0.0f, 0f));
                var sRt = splat.GetComponent<RectTransform>();
                sRt.anchorMin = sRt.anchorMax = sRt.pivot = new Vector2(0.5f, 0.5f);
                sRt.sizeDelta = new Vector2(140f, 140f);
                var img = splat.GetComponent<Image>();
                img.raycastTarget = false;
                img.enabled = false;
                _splats[i] = img;
            }

            var cross = CreateSprite(safeGo.transform, "Crosshair", UiSprites.Crosshair, Color.white);
            var cRt = cross.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(92, 92);
            cross.GetComponent<Image>().raycastTarget = false;

            MoveStick = CreateJoystick(safeGo.transform, new Vector2(48, 56), "MoveStick", new Vector2(300, 300), 112f);
            var moveLabel = CreateText(MoveStick.transform, "MoveLabel", Vector2.zero, new Vector2(220, 40), 26, TextAnchor.MiddleCenter);
            moveLabel.text = "移動";
            moveLabel.raycastTarget = false;
            var mlRt = moveLabel.rectTransform;
            mlRt.anchorMin = mlRt.anchorMax = mlRt.pivot = new Vector2(0.5f, 0f);
            mlRt.anchoredPosition = new Vector2(0f, -36f);
            const float rightPad = 48f;
            const float fireSize = 228f;
            const float lookSize = 220f;
            const float lookGap = 16f;
            CreateFireButton(safeGo.transform, new Vector2(-rightPad, 56f), new Vector2(fireSize, fireSize), _input.SetTouchFire);
            LookStick = CreateJoystick(safeGo.transform, Vector2.zero, "LookStick", new Vector2(lookSize, lookSize), 82f);
            var lookRt = LookStick.GetComponent<RectTransform>();
            lookRt.anchorMin = lookRt.anchorMax = lookRt.pivot = new Vector2(1f, 0f);
            lookRt.anchoredPosition = new Vector2(-rightPad - (fireSize - lookSize) * 0.5f, 56f + fireSize + lookGap);
            var sprint = CreateHoldButton(safeGo.transform, new Vector2(420, 400), "SPRINT", UiSprites.Sprint, new Vector2(128, 128), _input.SetTouchSprint);
            var sprintRt = sprint.GetComponent<RectTransform>();
            sprintRt.anchorMin = sprintRt.anchorMax = sprintRt.pivot = new Vector2(0f, 0f);
            CreateWeaponRack(safeGo.transform);

            var pause = CreateIconButton(safeGo.transform, new Vector2(-72, -28), "PAUSE", UiSprites.Pause, new Vector2(88, 88), () =>
            {
                if (CanPause(_session.State))
                {
                    TogglePause();
                }
                else
                {
                    TryRestart();
                }
            });
            var pauseRt = pause.GetComponent<RectTransform>();
            pauseRt.anchorMin = pauseRt.anchorMax = pauseRt.pivot = new Vector2(1f, 1f);

            _minimap = MinimapHud.Create(safeGo.transform);
            Camera cam = _camera != null ? _camera.UnityCamera : Camera.main;
            _hitPopups = HitPopupHud.Create(safeGo.transform.parent, cam);
            Transform crosshair = safeGo.transform.Find("Crosshair");
            if (crosshair != null)
            {
                crosshair.SetAsLastSibling();
            }

            var pausePanel = CreatePanel(safeGo.transform, "Pause", new Color(0f, 0f, 0f, 0.55f));
            var pauseRtPanel = pausePanel.GetComponent<RectTransform>();
            pauseRtPanel.anchorMin = Vector2.zero;
            pauseRtPanel.anchorMax = Vector2.one;
            pauseRtPanel.offsetMin = Vector2.zero;
            pauseRtPanel.offsetMax = Vector2.zero;
            CreateText(pausePanel.transform, "PauseLabel", Vector2.zero, new Vector2(700, 120), 48, TextAnchor.MiddleCenter).text = "PAUSE";
            var pauseLabel = pausePanel.GetComponentInChildren<Text>();
            var plRt = pauseLabel.rectTransform;
            plRt.anchorMin = plRt.anchorMax = plRt.pivot = new Vector2(0.5f, 0.5f);
            plRt.anchoredPosition = Vector2.zero;
            _pauseGroup = pausePanel.AddComponent<CanvasGroup>();
            _pauseGroup.alpha = 0f;
            _pauseGroup.blocksRaycasts = false;
            pausePanel.GetComponent<Image>().raycastTarget = false;
            Transform rack = safeGo.transform.Find("WeaponRack");
            if (rack != null)
            {
                rack.SetAsLastSibling();
            }

            _hurt.transform.SetAsLastSibling();
            _vignette.transform.SetAsLastSibling();
            _biteMark.transform.SetAsLastSibling();
            for (int i = 0; i < _splats.Length; i++)
            {
                _splats[i].transform.SetAsLastSibling();
            }

            pausePanel.transform.SetAsLastSibling();
            BuildCountdown(safeGo.transform);
            _gameOver.transform.SetAsLastSibling();
        }

        private void BuildCountdown(Transform parent)
        {
            var root = CreatePanel(parent, "Countdown", new Color(0.02f, 0.02f, 0.04f, 0.72f));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = root.GetComponent<Image>();
            dim.raycastTarget = false;
            _countdownRoot = root;

            _countdownWave = CreateText(root.transform, "CountdownWave", Vector2.zero, new Vector2(2200f, 180f), 96, TextAnchor.MiddleCenter);
            var waveRt = _countdownWave.rectTransform;
            waveRt.anchorMin = new Vector2(0.05f, 0.72f);
            waveRt.anchorMax = new Vector2(0.95f, 0.88f);
            waveRt.pivot = new Vector2(0.5f, 0.5f);
            waveRt.offsetMin = Vector2.zero;
            waveRt.offsetMax = Vector2.zero;
            waveRt.anchoredPosition = Vector2.zero;
            _countdownWave.font = UiFont();
            _countdownWave.fontStyle = FontStyle.Bold;
            _countdownWave.color = new Color(1f, 0.86f, 0.42f);
            _countdownWave.resizeTextForBestFit = true;
            _countdownWave.resizeTextMinSize = 48;
            _countdownWave.resizeTextMaxSize = 110;
            _countdownWave.horizontalOverflow = HorizontalWrapMode.Wrap;
            _countdownWave.verticalOverflow = VerticalWrapMode.Truncate;

            _countdownNumber = CreateText(root.transform, "CountdownNumber", Vector2.zero, new Vector2(2200f, 820f), 420, TextAnchor.MiddleCenter);
            var numRt = _countdownNumber.rectTransform;
            numRt.anchorMin = new Vector2(0.04f, 0.18f);
            numRt.anchorMax = new Vector2(0.96f, 0.74f);
            numRt.pivot = new Vector2(0.5f, 0.5f);
            numRt.offsetMin = Vector2.zero;
            numRt.offsetMax = Vector2.zero;
            numRt.anchoredPosition = Vector2.zero;
            _countdownNumber.font = UiFont();
            _countdownNumber.fontStyle = FontStyle.Bold;
            _countdownNumber.color = Color.white;
            _countdownNumber.resizeTextForBestFit = true;
            _countdownNumber.resizeTextMinSize = 120;
            _countdownNumber.resizeTextMaxSize = 420;
            _countdownNumber.horizontalOverflow = HorizontalWrapMode.Wrap;
            _countdownNumber.verticalOverflow = VerticalWrapMode.Truncate;

            root.SetActive(false);
        }

        private void CreateWeaponRack(Transform parent)
        {
            Sprite[] icons = { UiSprites.PistolIcon, UiSprites.SmgIcon, UiSprites.ShotgunIcon };
            _weaponIcons = new Image[icons.Length];
            _weaponFrames = new Image[icons.Length];
            var rack = new GameObject("WeaponRack", typeof(RectTransform));
            rack.transform.SetParent(parent, false);
            var rackRt = rack.GetComponent<RectTransform>();
            rackRt.anchorMin = rackRt.anchorMax = rackRt.pivot = new Vector2(0f, 0f);
            rackRt.anchoredPosition = new Vector2(32f, 380f);
            rackRt.sizeDelta = new Vector2(236f, 460f);

            for (int i = 0; i < icons.Length; i++)
            {
                int index = i;
                var frame = CreateSprite(rack.transform, "WeaponSlot" + i, UiSprites.Panel, new Color(1f, 1f, 1f, 0.92f));
                var rt = frame.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 10f + i * 148f);
                rt.sizeDelta = new Vector2(220f, 136f);
                var frameImg = frame.GetComponent<Image>();
                frameImg.type = Image.Type.Sliced;
                frameImg.preserveAspect = false;
                frameImg.raycastTarget = true;
                _weaponFrames[i] = frameImg;

                var icon = CreateSprite(frame.transform, "Icon", icons[i], Color.white);
                var iRt = icon.GetComponent<RectTransform>();
                iRt.anchorMin = iRt.anchorMax = iRt.pivot = new Vector2(0.5f, 0.5f);
                iRt.anchoredPosition = Vector2.zero;
                iRt.sizeDelta = new Vector2(196f, 108f);
                var iconImg = icon.GetComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                _weaponIcons[i] = iconImg;

                var button = frame.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => _weapons.SelectWeapon(index));
                var trigger = frame.AddComponent<EventTrigger>();
                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                down.callback.AddListener(_ => _weapons.SelectWeapon(index));
                trigger.triggers.Add(down);
            }
        }

        private VirtualJoystick CreateJoystick(Transform parent, Vector2 anchored, string name, Vector2 size, float radius)
        {
            var root = CreateSprite(parent, name, UiSprites.Ring, Color.white);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
            var handle = CreateSprite(root.transform, "Handle", UiSprites.Knob, Color.white);
            var hRt = handle.GetComponent<RectTransform>();
            hRt.anchorMin = hRt.anchorMax = hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = size * 0.46f;
            handle.GetComponent<Image>().raycastTarget = false;
            var joy = root.AddComponent<VirtualJoystick>();
            joy.Configure(hRt, radius);
            return joy;
        }

        private GameObject CreateFireButton(Transform parent, Vector2 pos, Vector2 size, System.Action<bool> onHold)
        {
            var go = CreateHoldButton(parent, pos, "FIRE", UiSprites.Fire, size, onHold);
            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = true;
            return go;
        }

        private GameObject CreateHoldButton(Transform parent, Vector2 pos, string name, Sprite sprite, Vector2 size, System.Action<bool> onHold)
        {
            var go = CreateSprite(parent, name, sprite, Color.white);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            var trigger = go.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                image.color = new Color(0.78f, 0.78f, 0.78f, 1f);
                onHold(true);
            });
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ =>
            {
                image.color = Color.white;
                onHold(false);
            });
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                image.color = Color.white;
                onHold(false);
            });
            trigger.triggers.Add(down);
            trigger.triggers.Add(up);
            trigger.triggers.Add(exit);
            return go;
        }

        private GameObject CreateIconButton(Transform parent, Vector2 pos, string name, Sprite sprite, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = CreateSprite(parent, name, sprite, Color.white);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            if (onClick != null)
            {
                go.AddComponent<Button>().onClick.AddListener(onClick);
            }

            return go;
        }

        private Image CreateHealthGauge(Transform parent, Vector2 pos, Vector2 size)
        {
            var plate = CreateSprite(parent, "HealthPlate", UiSprites.Panel, Color.white);
            var rt = plate.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            _healthPlate = rt;
            var plateImage = plate.GetComponent<Image>();
            plateImage.type = Image.Type.Sliced;
            plateImage.preserveAspect = false;
            plateImage.raycastTarget = false;

            var label = CreateText(plate.transform, "HealthLabel", Vector2.zero, new Vector2(70f, size.y), 28, TextAnchor.MiddleCenter);
            label.text = "HP";
            label.raycastTarget = false;
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.offsetMin = new Vector2(10f, 8f);
            labelRt.offsetMax = new Vector2(78f, -8f);

            var track = CreatePanel(plate.transform, "HealthTrack", new Color(0.08f, 0.09f, 0.1f, 0.85f));
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(1f, 1f);
            trackRt.offsetMin = new Vector2(82f, 18f);
            trackRt.offsetMax = new Vector2(-16f, -18f);
            track.GetComponent<Image>().raycastTarget = false;

            var fillGo = CreateSprite(track.transform, "HealthFill", UiSprites.White, new Color(0.28f, 0.86f, 0.38f, 1f));
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fill = fillGo.GetComponent<Image>();
            fill.preserveAspect = false;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            return fill;
        }

        private Text CreateHudPlate(Transform parent, string name, Vector2 pos, Vector2 size, Vector2 anchor, TextAnchor align)
        {
            var plate = CreateSprite(parent, name + "Plate", UiSprites.Panel, Color.white);
            var rt = plate.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            plate.GetComponent<Image>().type = Image.Type.Sliced;
            plate.GetComponent<Image>().preserveAspect = false;
            plate.GetComponent<Image>().raycastTarget = false;
            var text = CreateText(plate.transform, name, Vector2.zero, size - new Vector2(28f, 12f), 30, align);
            var tRt = text.rectTransform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(18f, 8f);
            tRt.offsetMax = new Vector2(-18f, -8f);
            tRt.anchoredPosition = Vector2.zero;
            return text;
        }

        private static GameObject CreateSprite(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = UiFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Font UiFont()
        {
            var font = Font.CreateDynamicFontFromOSFont(new[]
            {
                "HiraginoSans-W6",
                "Hiragino Sans",
                "Hiragino Kaku Gothic ProN",
                "YuGothic-Bold",
                "Yu Gothic",
                "Noto Sans CJK JP",
                "DroidSansFallback"
            }, 64);
            if (font != null)
            {
                return font;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
