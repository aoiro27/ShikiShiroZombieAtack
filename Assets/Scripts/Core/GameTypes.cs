using UnityEngine;

namespace ShikiShiro
{
    public enum SessionState
    {
        Boot,
        Countdown,
        Playing,
        WaveClear,
        GameOver
    }

    public enum ZombieKind
    {
        Walker,
        Runner,
        Brute,
        Boss
    }

    public enum WeaponId
    {
        Pistol,
        Smg,
        Shotgun,
        Bite
    }

    public enum PickupKind
    {
        Ammo,
        Medkit
    }

    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly bool IsHeadshot;
        public readonly WeaponId Weapon;
        public readonly int ChainDepth;

        public DamageInfo(float amount, Vector3 point, Vector3 direction, bool isHeadshot, WeaponId weapon, int chainDepth = 0)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            IsHeadshot = isHeadshot;
            Weapon = weapon;
            ChainDepth = chainDepth;
        }
    }

    public readonly struct HitPopupInfo
    {
        public readonly Vector3 WorldPoint;
        public readonly int Score;
        public readonly int Combo;
        public readonly bool Headshot;
        public readonly bool Kill;

        public HitPopupInfo(Vector3 worldPoint, int score, int combo, bool headshot, bool kill)
        {
            WorldPoint = worldPoint;
            Score = score;
            Combo = combo;
            Headshot = headshot;
            Kill = kill;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(in DamageInfo info);
    }

    [System.Serializable]
    public sealed class GameConfig
    {
        public float PlayerMoveSpeed = 6.6f;
        public float PlayerSprintMultiplier = 1.5f;
        public float PlayerLookSensitivity = 110f;
        public float GyroLookSensitivity = 1.85f;
        public float EditorLookSensitivity = 0.18f;
        public float PlayerMaxHealth = 100f;
        public int ZombiePoolSize = 96;
        public string HighScoreKey = "ssza.highscore";
    }
}
