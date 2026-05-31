using System;

namespace _02.Script.Combat
{
    [Serializable]
    public class WeaponRuntime
    {
        public WeaponData data;
        public int currentAmmo;             // 현재 Ammo
        public bool UnLocked ;              // 현재 Lock되어있는가
        public WeaponRuntime(WeaponData data) // 생성자. 새로운 WeaponRuntime이 입력되면 총알을 채움
        {
            this.data = data;
            InitializeAmmo();
        }

        public void InitializeAmmo()
        {
            if (data == null) return;

            currentAmmo = data.UseAmmo ? data.MagazineSize : 0;
        }

        public bool HasAmmo() // 총알을 가지고 있나
        {
            if (data == null) return false;
            if (!data.UseAmmo) return true;

            return currentAmmo > 0;
        }

        public bool hasEnoughAmmo() // 발사에 충분한 탄약을 가지고 있는가?
        {
            if (data == null) return false;
            if (!data.UseAmmo) return true;

            return currentAmmo > 0;
        }

        public void ConsumeAmmo() // 발사 1회당 현재 탄창에서 1발 소모
        {
            if (data == null || !data.UseAmmo) return;

            currentAmmo = Math.Max(currentAmmo - 1, 0);
        }

        public void Reload()
        {
            if (data == null || !data.UseAmmo) return;

            currentAmmo = data.MagazineSize;
        }
    }
}
