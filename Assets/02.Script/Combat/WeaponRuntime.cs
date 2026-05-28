using System;

namespace _02.Script.Combat
{
    [Serializable]
    public class WeaponRuntime
    {
        public WeaponData data;
        public int currentAmmo;
        public WeaponRuntime(WeaponData data)
        {
            this.data = data;

            if (data.UseAmmo)
                currentAmmo = data.MagazineSize;
            else
            {
                currentAmmo = 0;
            }
        }
        public bool HasAmmo()
        {
            if (!data.UseAmmo) return true;
            
            return currentAmmo >= 0;
        }

        public void ConsumeAmmo()
        {
            if(!data.UseAmmo) return;
            currentAmmo--;
        }

        public void Reload()
        {
            if (!data.UseAmmo) return;
            currentAmmo = data.MagazineSize;
        }
    }
}