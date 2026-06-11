using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _02.Script.UI
{
    public class AmmoSliderController: MonoBehaviour
    {
        [SerializeField] private DoubleIntEventChannel AmmoChanged;
        [SerializeField] private SingleIntEventChannel WeaponAmmoEvent;
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI AmmoText;
        [SerializeField] private TextMeshProUGUI WeaponAmmoText;

        private bool missingAmmoChangedLogged;
        private bool missingWeaponAmmoEventLogged;
        private bool missingSliderLogged;
        private bool missingAmmoTextLogged;
        private bool missingWeaponAmmoTextLogged;
        
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
        }

        private void OnDisable()
        {
            if (AmmoChanged != null)
                AmmoChanged.Unregister(OnAmmoChanged);

            if (WeaponAmmoEvent != null)
                WeaponAmmoEvent.Unregister(OnWeaponAmmoChanged);
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

        private void ReportMissingReference(string fieldName, ref bool alreadyLogged)
        {
            if (alreadyLogged)
                return;

            Debug.LogError($"[AmmoSliderController] {fieldName}가 null입니다. Inspector 연결을 확인해주세요.", this);
            alreadyLogged = true;
        }
    }
}
