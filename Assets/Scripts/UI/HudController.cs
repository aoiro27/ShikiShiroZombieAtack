using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShikiShiro
{
    public sealed class HudController : MonoBehaviour
    {
        private Text _health;
        private Text _ammo;
        private Text _wave;
        private Text _score;
        private Text _announce;
        private Text _gameOver;
        private Image _hurt;
        private float _announceUntil;
        private PlayerVitality _vitality;
        private WeaponController _weapons;
        private GameSession _session;
        private GameInput _input;
        private CanvasGroup _pauseGroup;
        private bool _paused;

        public VirtualJoystick MoveStick { get; private set; }
        public VirtualJoystick LookStick { get; private set; }

        public void Initialize(PlayerVitality vitality, WeaponController weapons, GameSession session, GameInput input)
        {
            _vitality = vitality;
            _weapons = weapons;
            _session = session;
            _input = input;
            BuildCanvas();
            vitality.HealthChanged += OnHealth;
            weapons.MagazineChanged += RefreshAmmo;
            session.ScoreChanged += _ => RefreshScore();
            session.WaveChanged += _ => RefreshWave();
            session.StateChanged += OnState;
            OnHealth(vitality.CurrentHealth, vitality.MaxHealth);
            RefreshAmmo();
            RefreshScore();
            RefreshWave();
        }

        public void Announce(string text)
        {
            _announce.text = text;
            _announceUntil = Time.unscaledTime + 2.2f;
        }

        private void Update()
        {
            if (_announceUntil > 0f && Time.unscaledTime > _announceUntil)
            {
                _announce.text = string.Empty;
                _announceUntil = 0f;
            }

            if (_input != null && _input.PausePressed && _session.State == SessionState.Playing)
            {
                TogglePause();
            }

            if (_hurt != null)
            {
                Color c = _hurt.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 0.8f);
                _hurt.color = c;
            }
        }

        private void OnHealth(float current, float max)
        {
            _health.text = $"HP  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            if (_hurt != null && current < max)
            {
                Color c = _hurt.color;
                c.a = 0.35f;
                _hurt.color = c;
            }
        }

        private void RefreshAmmo()
        {
            string reload = _weapons.Reloading ? "  RELOAD" : string.Empty;
            _ammo.text = $"{_weapons.Current.DisplayName}\n{_weapons.Mag} / {_weapons.Reserve}{reload}";
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
                _gameOver.gameObject.SetActive(true);
                _gameOver.text = $"GAME OVER\nWAVE {_session.Wave}   KILLS {_session.Kills}\nSCORE {_session.Score}\nタップ / クリックで再開";
            }
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
        }

        private void BuildCanvas()
        {
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

            var es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
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

            _health = CreateText(safeGo.transform, "Health", new Vector2(40, -40), new Vector2(520, 80), 36, TextAnchor.UpperLeft);
            _wave = CreateText(safeGo.transform, "Wave", new Vector2(0, -36), new Vector2(400, 70), 40, TextAnchor.UpperCenter);
            var waveRt = _wave.rectTransform;
            waveRt.anchorMin = new Vector2(0.5f, 1f);
            waveRt.anchorMax = new Vector2(0.5f, 1f);
            waveRt.pivot = new Vector2(0.5f, 1f);
            _score = CreateText(safeGo.transform, "Score", new Vector2(-40, -40), new Vector2(480, 90), 30, TextAnchor.UpperRight);
            var scoreRt = _score.rectTransform;
            scoreRt.anchorMin = new Vector2(1f, 1f);
            scoreRt.anchorMax = new Vector2(1f, 1f);
            scoreRt.pivot = new Vector2(1f, 1f);
            _ammo = CreateText(safeGo.transform, "Ammo", new Vector2(-48, 210), new Vector2(460, 110), 32, TextAnchor.LowerRight);
            var ammoRt = _ammo.rectTransform;
            ammoRt.anchorMin = new Vector2(1f, 0f);
            ammoRt.anchorMax = new Vector2(1f, 0f);
            ammoRt.pivot = new Vector2(1f, 0f);

            _announce = CreateText(safeGo.transform, "Announce", Vector2.zero, new Vector2(900, 120), 54, TextAnchor.MiddleCenter);
            var anRt = _announce.rectTransform;
            anRt.anchorMin = new Vector2(0.5f, 0.62f);
            anRt.anchorMax = new Vector2(0.5f, 0.62f);
            anRt.pivot = new Vector2(0.5f, 0.5f);
            _announce.color = new Color(1f, 0.82f, 0.45f);

            _gameOver = CreateText(safeGo.transform, "GameOver", Vector2.zero, new Vector2(1100, 420), 44, TextAnchor.MiddleCenter);
            var goRt = _gameOver.rectTransform;
            goRt.anchorMin = new Vector2(0.5f, 0.5f);
            goRt.anchorMax = new Vector2(0.5f, 0.5f);
            goRt.pivot = new Vector2(0.5f, 0.5f);
            _gameOver.gameObject.SetActive(false);
            var restart = _gameOver.gameObject.AddComponent<Button>();
            restart.onClick.AddListener(TryRestart);

            _hurt = CreatePanel(safeGo.transform, "Hurt", new Color(0.7f, 0.05f, 0.05f, 0f)).GetComponent<Image>();
            var hurtRt = _hurt.rectTransform;
            hurtRt.anchorMin = Vector2.zero;
            hurtRt.anchorMax = Vector2.one;
            hurtRt.offsetMin = Vector2.zero;
            hurtRt.offsetMax = Vector2.zero;
            _hurt.raycastTarget = false;

            var cross = CreatePanel(safeGo.transform, "Crosshair", new Color(1f, 1f, 1f, 0.75f));
            var cRt = cross.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0.52f);
            cRt.sizeDelta = new Vector2(8, 8);

            MoveStick = CreateJoystick(safeGo.transform, new Vector2(220, 210), "MoveStick");
            LookStick = CreateJoystick(safeGo.transform, new Vector2(-420, 210), "LookStick");
            var lookRt = LookStick.GetComponent<RectTransform>();
            lookRt.anchorMin = lookRt.anchorMax = lookRt.pivot = new Vector2(1f, 0f);

            CreateHoldButton(safeGo.transform, new Vector2(-150, 120), "FIRE", _input.SetTouchFire);
            CreateTapButton(safeGo.transform, new Vector2(-150, 280), "RELOAD", _input.PulseReload);
            var sprint = CreateHoldButton(safeGo.transform, new Vector2(420, 380), "SPRINT", _input.SetTouchSprint);
            var sprintRt = sprint.GetComponent<RectTransform>();
            sprintRt.anchorMin = sprintRt.anchorMax = sprintRt.pivot = new Vector2(0f, 0f);
            CreateTapButton(safeGo.transform, new Vector2(-320, 380), "WEAPON", _input.PulseSwap);

            var pause = CreateTapButton(safeGo.transform, new Vector2(-80, -40), "II", () =>
            {
                if (_session.State == SessionState.Playing)
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
        }

        private VirtualJoystick CreateJoystick(Transform parent, Vector2 anchored, string name)
        {
            var root = CreatePanel(parent, name, new Color(1f, 1f, 1f, 0.12f));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(240, 240);
            var handle = CreatePanel(root.transform, "Handle", new Color(1f, 1f, 1f, 0.35f));
            var hRt = handle.GetComponent<RectTransform>();
            hRt.anchorMin = hRt.anchorMax = hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(90, 90);
            var joy = root.AddComponent<VirtualJoystick>();
            joy.Configure(hRt, 90f);
            return joy;
        }

        private GameObject CreateHoldButton(Transform parent, Vector2 pos, string label, System.Action<bool> onHold)
        {
            var go = CreatePanel(parent, label, new Color(0.85f, 0.2f, 0.15f, 0.55f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(150, 150);
            CreateText(go.transform, "L", Vector2.zero, new Vector2(150, 150), 28, TextAnchor.MiddleCenter).text = label;
            var trigger = go.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => onHold(true));
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => onHold(false));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => onHold(false));
            trigger.triggers.Add(down);
            trigger.triggers.Add(up);
            trigger.triggers.Add(exit);
            return go;
        }

        private GameObject CreateTapButton(Transform parent, Vector2 pos, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = CreatePanel(parent, label, new Color(0.15f, 0.15f, 0.18f, 0.55f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(140, 70);
            CreateText(go.transform, "L", Vector2.zero, new Vector2(140, 70), 24, TextAnchor.MiddleCenter).text = label;
            if (onClick != null)
            {
                go.AddComponent<Button>().onClick.AddListener(onClick);
            }

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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
