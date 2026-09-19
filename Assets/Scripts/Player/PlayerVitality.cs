using System;
using UnityEngine;

namespace ShikiShiro
{
    public sealed class PlayerVitality : MonoBehaviour, IDamageable
    {
        public event Action<float, float> HealthChanged;
        public event Action Died;

        public bool IsAlive => CurrentHealth > 0f;
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }

        private GameSession _session;
        private float _invuln;

        public void Initialize(GameConfig config, GameSession session)
        {
            _session = session;
            MaxHealth = config.PlayerMaxHealth;
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (!IsAlive)
            {
                return;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive || _invuln > 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - info.Amount);
            _invuln = 0.35f;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            Handheld.Vibrate();
            if (!IsAlive)
            {
                Died?.Invoke();
                _session.NotifyPlayerDeath();
            }
        }

        private void Update()
        {
            if (_invuln > 0f)
            {
                _invuln -= Time.deltaTime;
            }
        }
    }
}
