using UnityEngine;

namespace ShikiShiro
{
    public readonly struct WeaponStats
    {
        public readonly WeaponId Id;
        public readonly string DisplayName;
        public readonly float Damage;
        public readonly float FireInterval;
        public readonly int MagazineSize;
        public readonly float ReloadTime;
        public readonly float SpreadDegrees;
        public readonly int Pellets;
        public readonly float Range;
        public readonly float Recoil;
        public readonly int ReserveAmmo;

        public WeaponStats(WeaponId id, string displayName, float damage, float fireInterval, int magazineSize,
            float reloadTime, float spreadDegrees, int pellets, float range, float recoil, int reserveAmmo)
        {
            Id = id;
            DisplayName = displayName;
            Damage = damage;
            FireInterval = fireInterval;
            MagazineSize = magazineSize;
            ReloadTime = reloadTime;
            SpreadDegrees = spreadDegrees;
            Pellets = pellets;
            Range = range;
            Recoil = recoil;
            ReserveAmmo = reserveAmmo;
        }

        public static WeaponStats Pistol()
        {
            return new WeaponStats(WeaponId.Pistol, "M9 ハンドガン", 28f, 0.22f, 12, 1.35f, 1.1f, 1, 70f, 1.4f, 72);
        }

        public static WeaponStats Smg()
        {
            return new WeaponStats(WeaponId.Smg, "ベクター SMG", 16f, 0.075f, 30, 1.7f, 3.2f, 1, 55f, 0.9f, 150);
        }

        public static WeaponStats Shotgun()
        {
            return new WeaponStats(WeaponId.Shotgun, "M870 ショットガン", 11f, 0.72f, 6, 2.1f, 7.5f, 8, 28f, 4.2f, 30);
        }
    }
}
