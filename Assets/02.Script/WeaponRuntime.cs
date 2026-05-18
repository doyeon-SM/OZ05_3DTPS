namespace _01.Scenes.PhaseValidation._26._05._14
{
    public class WeaponRuntime
    {
        public WeaponData data;
        public int currentAmmo;

        public WeaponRuntime(WeaponData data)
        {
            this.data = data;

            if (data.useAmmo)
                currentAmmo = data.magazineSize;
            else
            {
                currentAmmo = 0;
            }
        }
        public bool HasAmmo()
        {
            if (!data.useAmmo) return true;
            
            return currentAmmo > 0;
        }

        public void ConsumeAmmo()
        {
            if(!data.useAmmo) return;
            currentAmmo--;
        }

        public void Reload()
        {
            if (!data.useAmmo) return;
            currentAmmo = data.magazineSize;
        }
    }
}