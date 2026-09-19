using System;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class GameSession : MonoBehaviour
    {
        public event Action<int> WaveChanged;
        public event Action<int> ScoreChanged;
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
            SetState(SessionState.Playing);
            WaveChanged?.Invoke(Wave);
            ScoreChanged?.Invoke(Score);
        }

        public void NotifyWaveClear()
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            SetState(SessionState.WaveClear);
        }

        public void AdvanceWave()
        {
            Wave++;
            SetState(SessionState.Playing);
            WaveChanged?.Invoke(Wave);
        }

        public void RegisterKill(ZombieKind kind, bool headshot)
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            Kills++;
            ComboTimer = _comboWindow;
            Combo++;

            int baseScore = kind switch
            {
                ZombieKind.Runner => 120,
                ZombieKind.Brute => 350,
                _ => 80
            };

            if (headshot)
            {
                baseScore = Mathf.RoundToInt(baseScore * 1.8f);
            }

            int comboBonus = 1 + Mathf.Min(Combo / 5, 4);
            AddScore(baseScore * comboBonus);
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
