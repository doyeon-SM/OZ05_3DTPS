using _00.ChoiHeesu._03.WeaponChangeSystem;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _02.Script.UI
{
    public class AmmoSliderController : MonoBehaviour
    {
        [SerializeField] private DoubleIntEventChannel AmmoChanged;
        [SerializeField] private SingleIntEventChannel WeaponAmmoEvent;
        [SerializeField] private SingleFloatEventChannel ReloadTimeEvent;

        [Header("Current Ammo UI")]
        [FormerlySerializedAs("slider")]
        [SerializeField] private Slider CurrentAmmoSlider;
        [FormerlySerializedAs("AmmoText")]
        [SerializeField] private TextMeshProUGUI CurrentAmmoText;

        [Header("Reload UI")]
        [SerializeField] private Slider ReloadSlider;

        [SerializeField] private TextMeshProUGUI WeaponAmmoText;
        [SerializeField] private Image AmmoImage;
        [SerializeField] private TextMeshProUGUI GrenadeCountText;
        [SerializeField] private WeaponRuntimeManager weaponRuntimeManager;

        private bool missingAmmoChangedLogged;
        private bool missingWeaponAmmoEventLogged;
        private bool missingReloadTimeEventLogged;
        private bool missingCurrentAmmoSliderLogged;
        private bool missingCurrentAmmoTextLogged;
        private bool missingReloadSliderLogged;
        private bool missingWeaponAmmoTextLogged;
        private bool missingAmmoImageLogged;
        private bool missingWeaponRuntimeManagerLogged;
        private Coroutine reloadCoroutine;
        private int cachedCurrentAmmo;
        private int cachedMaxAmmo;
        private bool hasCachedAmmo;
        private bool isReloadingUI;

        private void OnEnable()
        {
            CacheUIReferences();
            SetCurrentAmmoSliderVisible(true);
            SetReloadUIVisible(false);

            if (AmmoChanged != null)
                AmmoChanged.Register(OnAmmoChanged);
            else
                ReportMissingReference(nameof(AmmoChanged), ref missingAmmoChangedLogged);

            if (WeaponAmmoEvent != null)
                WeaponAmmoEvent.Register(OnWeaponAmmoChanged);
            else
                ReportMissingReference(nameof(WeaponAmmoEvent), ref missingWeaponAmmoEventLogged);

            if (ReloadTimeEvent != null)
                ReloadTimeEvent.Register(OnReloadTimeChanged);
            else
                ReportMissingReference(nameof(ReloadTimeEvent), ref missingReloadTimeEventLogged);

            CacheWeaponRuntimeManager();
            RefreshWeaponAmmoVisibility();

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

            if (ReloadTimeEvent != null)
                ReloadTimeEvent.Unregister(OnReloadTimeChanged);

            if (weaponRuntimeManager != null)
                weaponRuntimeManager.OnGrenadeCountChanged -= OnGrenadeCountChanged;

            StopReloadCoroutine();
            SetReloadUIVisible(false);
            SetCurrentAmmoTextVisible(true);
            isReloadingUI = false;
        }

        private void OnAmmoChanged(int currentAmmo, int maxAmmo)
        {
            cachedCurrentAmmo = currentAmmo;
            cachedMaxAmmo = maxAmmo;
            hasCachedAmmo = true;

            if (isReloadingUI)
            {
                SetCurrentAmmoSliderValue(0f);
                RefreshWeaponAmmoVisibility();
                return;
            }

            ApplyCurrentAmmoUI(currentAmmo, maxAmmo);
            RefreshWeaponAmmoVisibility();
        }

        private void OnReloadTimeChanged(float reloadTime)
        {
            StopReloadCoroutine();

            if (reloadTime <= 0f)
            {
                FinishReloadAnimation();
                return;
            }

            reloadCoroutine = StartCoroutine(ReloadRoutine(reloadTime));
        }

        private void OnWeaponAmmoChanged(int weaponAmmo)
        {
            RefreshWeaponAmmoVisibility();

            if (IsCurrentWeaponInfiniteAmmo())
                return;

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

        private IEnumerator ReloadRoutine(float reloadTime)
        {
            CacheUIReferences();

            if (ReloadSlider == null)
            {
                ReportMissingReference(nameof(ReloadSlider), ref missingReloadSliderLogged);
                isReloadingUI = false;
                SetCurrentAmmoTextVisible(true);
                if (hasCachedAmmo)
                    ApplyCurrentAmmoUI(cachedCurrentAmmo, cachedMaxAmmo);

                reloadCoroutine = null;
                yield break;
            }

            SetCurrentAmmoSliderVisible(true);
            SetCurrentAmmoSliderValue(0f);
            SetCurrentAmmoTextVisible(false);
            SetReloadUIVisible(true);
            ReloadSlider.value = 0f;
            isReloadingUI = true;

            float elapsedTime = 0f;
            while (elapsedTime < reloadTime)
            {
                elapsedTime += Time.deltaTime;
                ReloadSlider.value = Mathf.Clamp01(elapsedTime / reloadTime);
                yield return null;
            }

            FinishReloadAnimation();
        }

        private void FinishReloadAnimation()
        {
            if (ReloadSlider != null)
                ReloadSlider.value = 1f;

            isReloadingUI = false;
            SetReloadUIVisible(false);
            SetCurrentAmmoTextVisible(true);
            SetCurrentAmmoSliderVisible(true);

            if (hasCachedAmmo)
                ApplyCurrentAmmoUI(cachedCurrentAmmo, cachedMaxAmmo);

            reloadCoroutine = null;
        }

        private void StopReloadCoroutine()
        {
            if (reloadCoroutine == null)
                return;

            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        private void ApplyCurrentAmmoUI(int currentAmmo, int maxAmmo)
        {
            CacheUIReferences();

            if (CurrentAmmoSlider == null)
            {
                ReportMissingReference(nameof(CurrentAmmoSlider), ref missingCurrentAmmoSliderLogged);
            }
            else
            {
                CurrentAmmoSlider.value = maxAmmo > 0 ? Mathf.Clamp01((float)currentAmmo / maxAmmo) : 0f;
            }

            if (CurrentAmmoText == null)
            {
                ReportMissingReference(nameof(CurrentAmmoText), ref missingCurrentAmmoTextLogged);
                return;
            }

            CurrentAmmoText.text = $"{currentAmmo} / {maxAmmo}";
        }

        private void SetCurrentAmmoSliderValue(float value)
        {
            CacheCurrentAmmoSlider();

            if (CurrentAmmoSlider != null)
                CurrentAmmoSlider.value = Mathf.Clamp01(value);
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

        private void CacheUIReferences()
        {
            CacheCurrentAmmoSlider();
            CacheCurrentAmmoText();
            CacheReloadSlider();
            CacheAmmoImage();
        }

        private void CacheCurrentAmmoSlider()
        {
            if (CurrentAmmoSlider != null)
                return;

            Transform currentAmmoSliderTransform = FindChildRecursive(transform, "CurrentAmmoSlider");
            if (currentAmmoSliderTransform == null && transform.root != transform)
                currentAmmoSliderTransform = FindChildRecursive(transform.root, "CurrentAmmoSlider");

            if (currentAmmoSliderTransform != null && currentAmmoSliderTransform.TryGetComponent(out Slider foundSlider))
                CurrentAmmoSlider = foundSlider;
        }

        private void CacheCurrentAmmoText()
        {
            if (CurrentAmmoText != null)
                return;

            Transform currentAmmoTextTransform = FindChildRecursive(transform, "CurrentAmmoText");
            if (currentAmmoTextTransform == null && transform.root != transform)
                currentAmmoTextTransform = FindChildRecursive(transform.root, "CurrentAmmoText");

            if (currentAmmoTextTransform != null && currentAmmoTextTransform.TryGetComponent(out TextMeshProUGUI foundText))
                CurrentAmmoText = foundText;
        }

        private void CacheReloadSlider()
        {
            if (ReloadSlider != null)
                return;

            Transform reloadSliderTransform = FindChildRecursive(transform, "ReloadSlider");
            if (reloadSliderTransform == null && transform.root != transform)
                reloadSliderTransform = FindChildRecursive(transform.root, "ReloadSlider");

            if (reloadSliderTransform != null && reloadSliderTransform.TryGetComponent(out Slider foundSlider))
                ReloadSlider = foundSlider;
        }

        private void CacheAmmoImage()
        {
            if (AmmoImage != null)
                return;

            Transform ammoImageTransform = FindChildRecursive(transform, "AmmoImage");
            if (ammoImageTransform == null && transform.root != transform)
                ammoImageTransform = FindChildRecursive(transform.root, "AmmoImage");

            if (ammoImageTransform == null)
                return;

            if (ammoImageTransform.TryGetComponent(out Image foundImage))
                AmmoImage = foundImage;
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform foundChild = FindChildRecursive(root.GetChild(i), childName);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }

        private void SetReloadUIVisible(bool isVisible)
        {
            CacheReloadSlider();

            if (ReloadSlider != null && ReloadSlider.gameObject.activeSelf != isVisible)
                ReloadSlider.gameObject.SetActive(isVisible);
        }

        private void SetCurrentAmmoSliderVisible(bool isVisible)
        {
            CacheCurrentAmmoSlider();

            if (CurrentAmmoSlider != null && CurrentAmmoSlider.gameObject.activeSelf != isVisible)
                CurrentAmmoSlider.gameObject.SetActive(isVisible);
        }

        private void SetCurrentAmmoTextVisible(bool isVisible)
        {
            CacheCurrentAmmoText();

            if (CurrentAmmoText != null && CurrentAmmoText.gameObject.activeSelf != isVisible)
                CurrentAmmoText.gameObject.SetActive(isVisible);
        }

        private void RefreshWeaponAmmoVisibility()
        {
            SetWeaponAmmoVisible(!IsCurrentWeaponInfiniteAmmo());
        }

        private bool IsCurrentWeaponInfiniteAmmo()
        {
            CacheWeaponRuntimeManager();
            return weaponRuntimeManager != null && weaponRuntimeManager.IsCurrentWeaponInfiniteAmmo;
        }

        private void SetWeaponAmmoVisible(bool isVisible)
        {
            CacheAmmoImage();

            if (AmmoImage != null)
            {
                if (AmmoImage.gameObject.activeSelf != isVisible)
                    AmmoImage.gameObject.SetActive(isVisible);
            }
            else if (!isVisible)
            {
                ReportMissingReference(nameof(AmmoImage), ref missingAmmoImageLogged);
            }

            if (WeaponAmmoText != null)
            {
                if (WeaponAmmoText.gameObject.activeSelf != isVisible)
                    WeaponAmmoText.gameObject.SetActive(isVisible);
            }
            else if (!isVisible)
            {
                ReportMissingReference(nameof(WeaponAmmoText), ref missingWeaponAmmoTextLogged);
            }
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
