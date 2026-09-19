using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShikiShiro
{
    public sealed class HudController : MonoBehaviour
    {
        private Image _healthFill;
        private Image _hurt;
        private Image[] _weaponIcons;
        private Image[] _weaponFrames;
        private Text _wave;
        private Text _score;
        private Text _announce;
        private Text _gameOver;
        private float _announceUntil;
        private PlayerVitality _vitality;
        private WeaponController _weapons;
        private GameSession _session;
        private GameInput _input;
        private CanvasGroup _pauseGroup;
        private MinimapHud _minimap;
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

        public void BindMinimap(Transform player, ArenaBuilder arena, HordeDirector horde)
        {
            if (_minimap != null)
            {
                _minimap.Bind(player, arena, horde);
            }
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
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (_healthFill != null)
            {
                _healthFill.fillAmount = ratio;
                _healthFill.color = HealthColor(ratio);
            }

            if (_hurt != null && current < max)
            {
                Color c = _hurt.color;
                c.a = 0.35f;
                _hurt.color = c;
            }
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
            LookStick = CreateJoystick(safeGo.transform, new Vector2(-420, 210), "LookStick", new Vector2(220, 220), 82f);
            var lookRt = LookStick.GetComponent<RectTransform>();
            lookRt.anchorMin = lookRt.anchorMax = lookRt.pivot = new Vector2(1f, 0f);

            CreateFireButton(safeGo.transform, new Vector2(-140, 176), new Vector2(228, 228), _input.SetTouchFire);
            var sprint = CreateHoldButton(safeGo.transform, new Vector2(420, 400), "SPRINT", UiSprites.Sprint, new Vector2(128, 128), _input.SetTouchSprint);
            var sprintRt = sprint.GetComponent<RectTransform>();
            sprintRt.anchorMin = sprintRt.anchorMax = sprintRt.pivot = new Vector2(0f, 0f);
            CreateWeaponRack(safeGo.transform);

            var pause = CreateIconButton(safeGo.transform, new Vector2(-72, -28), "PAUSE", UiSprites.Pause, new Vector2(88, 88), () =>
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

            _minimap = MinimapHud.Create(safeGo.transform);
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
            rackRt.anchoredPosition = new Vector2(36f, 390f);
            rackRt.sizeDelta = new Vector2(168f, 320f);

            for (int i = 0; i < icons.Length; i++)
            {
                int index = i;
                var frame = CreateSprite(rack.transform, "WeaponSlot" + i, UiSprites.Panel, new Color(1f, 1f, 1f, 0.92f));
                var rt = frame.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 8f + i * 104f);
                rt.sizeDelta = new Vector2(158f, 96f);
                var frameImg = frame.GetComponent<Image>();
                frameImg.type = Image.Type.Sliced;
                frameImg.preserveAspect = false;
                frameImg.raycastTarget = true;
                _weaponFrames[i] = frameImg;

                var icon = CreateSprite(frame.transform, "Icon", icons[i], Color.white);
                var iRt = icon.GetComponent<RectTransform>();
                iRt.anchorMin = iRt.anchorMax = iRt.pivot = new Vector2(0.5f, 0.5f);
                iRt.anchoredPosition = Vector2.zero;
                iRt.sizeDelta = new Vector2(140f, 72f);
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
