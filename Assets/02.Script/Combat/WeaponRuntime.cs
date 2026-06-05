using System;

namespace _02.Script.Combat
{
    [Serializable]
    public class WeaponRuntime
    {
        public WeaponData data;
        public int currentAmmo;
        public bool UnLocked;
        public float ShotDelayTime;

        public WeaponRuntime(WeaponData data)
        {
            this.data = data;
            InitializeAmmo();
        }

        public void InitializeAmmo()
        {
            if (data == null)
            {
                ShotDelayTime = 0f;
                return;
            }

            currentAmmo = 0;
            CalculateShotDelayTime();
        }

        public void RefreshCachedData()
        {
            if (data == null)
            {
                ShotDelayTime = 0f;
                return;
            }

            CalculateShotDelayTime();
        }

        private void CalculateShotDelayTime()
        {
            ShotDelayTime = data.RPM > 0f ? 60.0f / data.RPM : 0f;
        }

        public bool HasAmmo()
        {
            if (data == null) return false;
            if (!data.UseAmmo) return true;

            return currentAmmo > 0;
        }

        public bool hasEnoughAmmo()
        {
            if (data == null) return false;
            if (!data.UseAmmo) return true;

            return currentAmmo > 0;
        }

        public void ConsumeAmmo()
        {
            if (data == null || !data.UseAmmo) return;

            currentAmmo = Math.Max(currentAmmo - 1, 0);
        }

        public void Reload(int reloadAmount)
        {
            if (data == null || !data.UseAmmo) return;

            currentAmmo = Math.Min(currentAmmo + Math.Max(reloadAmount, 0), data.MagazineSize);
        }
    }
}
