using UnityEngine;

namespace ShikiShiro
{
    public enum SessionState
    {
        Boot,
        Playing,
        WaveClear,
        GameOver
    }

    public enum ZombieKind
    {
        Walker,
        Runner,
        Brute
    }

    public enum WeaponId
    {
        Pistol,
        Smg,
        Shotgun
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

        public DamageInfo(float amount, Vector3 point, Vector3 direction, bool isHeadshot, WeaponId weapon)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            IsHeadshot = isHeadshot;
            Weapon = weapon;
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
        public float GyroLookSensitivity = 1f;
        public float EditorLookSensitivity = 0.18f;
        public float PlayerMaxHealth = 100f;
        public float CameraDistance = 3.6f;
        public float CameraHeight = 1.55f;
        public float CameraCollisionRadius = 0.22f;
        public int MaxAliveZombies = 18;
        public int ZombiePoolSize = 48;
        public float PickupDropChance = 0.18f;
        public string HighScoreKey = "ssza.highscore";
    }
}
