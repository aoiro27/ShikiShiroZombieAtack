using System;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class GameSession : MonoBehaviour
    {
        public event Action<int> WaveChanged;
        public event Action<int> ScoreChanged;
        public event Action<int> ComboChanged;
        public event Action<HitPopupInfo> HitPopup;
        public event Action<SessionState> StateChanged;
        public event Action PlayerDied;

        public SessionState State { get; private set; } = SessionState.Boot;
        public int Wave { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Combo { get; private set; }
        public int HighScore { get; private set; }
        public float ComboTimer { get; private set; }

        private GameConfig _config;
        private float _comboWindow = 3.4f;

        public void Initialize(GameConfig config)
        {
            _config = config;
            HighScore = PlayerPrefs.GetInt(config.HighScoreKey, 0);
        }

        public void BeginRun()
        {
            Wave = 1;
            Score = 0;
            Kills = 0;
            Combo = 0;
            ComboTimer = 0f;
            SetState(SessionState.Countdown);
            WaveChanged?.Invoke(Wave);
            ScoreChanged?.Invoke(Score);
            ComboChanged?.Invoke(Combo);
        }

        public void NotifyWaveClear()
        {
            if (State == SessionState.GameOver)
            {
                return;
            }

            SetState(SessionState.WaveClear);
        }

        public void AdvanceWave()
        {
            Wave++;
            BeginCountdown();
            WaveChanged?.Invoke(Wave);
        }

        public void BeginCountdown()
        {
            if (State == SessionState.GameOver)
            {
                return;
            }

            SetState(SessionState.Countdown);
        }

        public void BeginCombat()
        {
            if (State == SessionState.GameOver)
            {
                return;
            }

            SetState(SessionState.Playing);
        }

        public int RegisterCombatHit(float damage, bool headshot, bool kill)
        {
            if (State != SessionState.Playing)
            {
                return 0;
            }

            ComboTimer = _comboWindow;
            Combo++;
            if (kill)
            {
                Kills++;
            }

            int gained = AwardDamage(damage);
            ComboChanged?.Invoke(Combo);
            return gained;
        }

        public int RegisterBalloonPop(float damage)
        {
            if (State != SessionState.Playing)
            {
                return 0;
            }

            ComboTimer = _comboWindow;
            Combo++;
            int gained = AwardDamage(damage);
            ComboChanged?.Invoke(Combo);
            return gained;
        }

        private int AwardDamage(float damage)
        {
            int raw = Mathf.Max(1, Mathf.RoundToInt(damage));
            int comboBonus = 1 + Mathf.Min(Combo / 5, 4);
            int gained = raw * comboBonus;
            AddScore(gained);
            return gained;
        }

        public void NotifyHitPopup(in HitPopupInfo info)
        {
            HitPopup?.Invoke(info);
        }

        public void BreakCombo()
        {
            if (Combo <= 0 && ComboTimer <= 0f)
            {
                return;
            }

            Combo = 0;
            ComboTimer = 0f;
            ComboChanged?.Invoke(Combo);
        }

        public void NotifyPlayerDeath()
        {
            if (State == SessionState.GameOver)
            {
                return;
            }

            if (Score > HighScore)
            {
                HighScore = Score;
                PlayerPrefs.SetInt(_config.HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }

            SetState(SessionState.GameOver);
            Time.timeScale = 0f;
            PlayerDied?.Invoke();
        }

        private void Update()
        {
            if (State != SessionState.Playing || Combo <= 0)
            {
                return;
            }

            ComboTimer -= Time.deltaTime;
            if (ComboTimer <= 0f)
            {
                Combo = 0;
                ComboTimer = 0f;
                ComboChanged?.Invoke(Combo);
            }
        }

        private void AddScore(int amount)
        {
            Score += amount;
            ScoreChanged?.Invoke(Score);
        }

        private void SetState(SessionState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(State);
        }
    }
}
