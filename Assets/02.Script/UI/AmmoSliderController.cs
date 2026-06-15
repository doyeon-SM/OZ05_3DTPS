using _00.ChoiHeesu._03.WeaponChangeSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _02.Script.UI
{
    public class AmmoSliderController : MonoBehaviour
    {
        [SerializeField] private DoubleIntEventChannel AmmoChanged;
        [SerializeField] private SingleIntEventChannel WeaponAmmoEvent;
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI AmmoText;
        [SerializeField] private TextMeshProUGUI WeaponAmmoText;
        [SerializeField] private TextMeshProUGUI GrenadeCountText;
        [SerializeField] private WeaponRuntimeManager weaponRuntimeManager;

        private bool missingAmmoChangedLogged;
        private bool missingWeaponAmmoEventLogged;
        private bool missingSliderLogged;
        private bool missingAmmoTextLogged;
        private bool missingWeaponAmmoTextLogged;
        private bool missingWeaponRuntimeManagerLogged;

        private void OnEnable()
        {
            if (AmmoChanged != null)
                AmmoChanged.Register(OnAmmoChanged);
            else
                ReportMissingReference(nameof(AmmoChanged), ref missingAmmoChangedLogged);

            if (WeaponAmmoEvent != null)
                WeaponAmmoEvent.Register(OnWeaponAmmoChanged);
            else
                ReportMissingReference(nameof(WeaponAmmoEvent), ref missingWeaponAmmoEventLogged);

            CacheWeaponRuntimeManager();
            if (weaponRuntimeManager != null)
            {
                weaponRuntimeManager.OnGrenadeCountChanged += OnGrenadeCountChanged;
                OnGrenadeCountChanged(weaponRuntimeManager.GrenadeCount);
            }
        }

        private void OnDisable()
        {
            if (AmmoChanged != null)
                AmmoChanged.Unregister(OnAmmoChanged);

            if (WeaponAmmoEvent != null)
                WeaponAmmoEvent.Unregister(OnWeaponAmmoChanged);

            if (weaponRuntimeManager != null)
                weaponRuntimeManager.OnGrenadeCountChanged -= OnGrenadeCountChanged;
        }

        private void OnAmmoChanged(int currentAmmo, int maxAmmo)
        {
            if (slider == null)
            {
                ReportMissingReference(nameof(slider), ref missingSliderLogged);
            }
            else
            {
                slider.value = maxAmmo > 0 ? Mathf.Clamp01((float)currentAmmo / maxAmmo) : 0f;
            }

            if (AmmoText == null)
            {
                ReportMissingReference(nameof(AmmoText), ref missingAmmoTextLogged);
                return;
            }

            AmmoText.text = $"{currentAmmo} / {maxAmmo}";
        }

        private void OnWeaponAmmoChanged(int weaponAmmo)
        {
            if (WeaponAmmoText == null)
            {
                ReportMissingReference(nameof(WeaponAmmoText), ref missingWeaponAmmoTextLogged);
                return;
            }

            WeaponAmmoText.text = weaponAmmo.ToString();
        }

        private void OnGrenadeCountChanged(int grenadeCount)
        {
            if (GrenadeCountText == null)
                return;

            GrenadeCountText.text = grenadeCount.ToString();
        }

        private void CacheWeaponRuntimeManager()
        {
            if (weaponRuntimeManager != null)
                return;

            weaponRuntimeManager = WeaponRuntimeManager.Instance;

            if (weaponRuntimeManager == null)
                weaponRuntimeManager = FindFirstObjectByType<WeaponRuntimeManager>(FindObjectsInactive.Include);

            if (weaponRuntimeManager == null && GrenadeCountText != null)
                ReportMissingReference(nameof(weaponRuntimeManager), ref missingWeaponRuntimeManagerLogged);
        }

        private void ReportMissingReference(string fieldName, ref bool alreadyLogged)
        {
            if (alreadyLogged)
                return;

            Debug.LogError($"[AmmoSliderController] {fieldName}가 null입니다. Inspector 연결을 확인해주세요.", this);
            alreadyLogged = true;
        }
    }
}
