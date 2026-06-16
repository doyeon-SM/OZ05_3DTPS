using _02.Script.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace _00.ChoiHeesu._03.WeaponChangeSystem
{
    [DisallowMultipleComponent]
    public class WeaponSwitcher : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private Transform weaponHolder;

        [Header("Initial Weapon Prefabs")]
        [SerializeField] private WeaponPrefabSetting[] initialWeaponPrefabs;

        private readonly Dictionary<string, WeaponPrefabSetting> weaponPrefabById = new Dictionary<string, WeaponPrefabSetting>();

        [SerializeField]private WeaponRuntimeManager weaponRuntimeManager;
        [SerializeField]private WeaponController weaponController;
        [SerializeField]private bool referencesReady;
        [SerializeField]private bool initialized;

        private void Awake()
        {
            referencesReady = CacheReferences();
        }

        private void Start()
        {
            Initialize();
        }

        public bool Initialize()
        {
            if (initialized)
            {
                Debug.LogWarning("[WeaponSwitcher] 이미 초기화가 완료된 상태에서 Initialize가 다시 호출되었습니다.", this);
                return false;
            }

            if (!referencesReady)
                return false;

            weaponPrefabById.Clear();

            RegisterInitialWeaponPrefabs();
            CreateMissingRuntimeWeapons();
            DeactivateAllWeapons();

            initialized = true;
            RefreshCurrentWeapon();
            return true;
        }

        public bool TryRequestWeaponChange(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                Debug.LogWarning("[WeaponSwitcher] WeaponSelect UI에서 비어 있는 WeaponId가 전달되었습니다.", this);
                return false;
            }

            if (!EnsureInitialized())
                return false;

            if (!weaponRuntimeManager.TryRequestWeaponChange(weaponId, out string blockMessage))
            {
                Debug.LogError($"[WeaponSwitcher] WeaponRuntimeManager를 통한 무기 변경 요청이 실패했습니다. {blockMessage}", this);
                return false;
            }

            return RefreshCurrentWeapon();
        }

        public bool RefreshCurrentWeapon()
        {
            if (!EnsureInitialized())
                return false;

            WeaponRuntime currentWeapon = weaponController.CurrentWeaponRuntime;
            if (currentWeapon == null)
            {
                DeactivateAllWeapons();
                Debug.LogWarning("[WeaponSwitcher] CurrentWeapon이 null이라 모든 무기를 비활성화했습니다.", this);
                return false;
            }

            if (!TryGetValidWeaponData(currentWeapon.data, "CurrentWeapon", out string weaponId))
            {
                DeactivateAllWeapons();
                return false;
            }

            if (!weaponPrefabById.TryGetValue(weaponId, out WeaponPrefabSetting activeWeaponPrefab) || activeWeaponPrefab == null)
            {
                DeactivateAllWeapons();
                Debug.LogError($"[WeaponSwitcher] CurrentWeapon의 WeaponId '{weaponId}'와 일치하는 WeaponPrefabSetting을 찾지 못했습니다.", this);
                return false;
            }

            ActivateOnly(activeWeaponPrefab);
            activeWeaponPrefab.StopMuzzleEffect();
            return UpdateWeaponControllerMuzzle(activeWeaponPrefab, weaponId);
        }

        public bool SetWeaponsVisible(bool isVisible)
        {
            if (!EnsureInitialized())
                return false;

            if (isVisible)
                return RefreshCurrentWeapon();

            DeactivateAllWeapons();
            return true;
        }

        private bool CacheReferences()
        {
            weaponRuntimeManager = FindFirstObjectByType<WeaponRuntimeManager>(FindObjectsInactive.Include);
            if (weaponRuntimeManager == null)
            {
                Debug.LogError("[WeaponSwitcher] 씬 내에서 WeaponRuntimeManager를 찾지 못했습니다.", this);
                return false;
            }

            if (!TryGetComponent(out weaponController))
                weaponController = GetComponentInParent<WeaponController>();

            if (weaponController == null)
            {
                Debug.LogError("[WeaponSwitcher] 같은 오브젝트와 부모 오브젝트에서 WeaponController를 찾지 못했습니다.", this);
                return false;
            }

            if (weaponHolder == null)
            {
                Debug.LogError("[WeaponSwitcher] WeaponHolder가 null입니다. Inspector에서 플레이어 스켈레톤 내부의 WeaponHolder Transform을 연결해주세요.", this);
                return false;
            }

            return true;
        }

        private bool EnsureInitialized()
        {
            if (initialized)
                return true;

            return Initialize();
        }

        private void RegisterInitialWeaponPrefabs()
        {
            if (initialWeaponPrefabs == null)
                return;

            for (int i = 0; i < initialWeaponPrefabs.Length; i++)
            {
                WeaponPrefabSetting weaponPrefabSetting = initialWeaponPrefabs[i];
                if (weaponPrefabSetting == null)
                {
                    Debug.LogWarning($"[WeaponSwitcher] Inspector에 등록된 WeaponPrefabSetting 배열의 {i}번 요소가 null입니다.", this);
                    continue;
                }

                TryRegisterWeaponPrefab(weaponPrefabSetting, $"Inspector[{i}]");
            }
        }

        private void CreateMissingRuntimeWeapons()
        {
            WeaponRuntime[] weaponRuntimes = weaponRuntimeManager.WeaponRuntimes;
            if (weaponRuntimes == null)
            {
                Debug.LogError("[WeaponSwitcher] WeaponRuntimeManager의 WeaponRuntimes가 null입니다.", this);
                return;
            }

            for (int i = 0; i < weaponRuntimes.Length; i++)
            {
                WeaponRuntime runtime = weaponRuntimes[i];
                if (runtime == null)
                {
                    Debug.LogError($"[WeaponSwitcher] WeaponRuntimeManager.WeaponRuntimes[{i}]가 null입니다.", this);
                    continue;
                }

                if (!TryGetValidWeaponData(runtime.data, $"WeaponRuntimes[{i}]", out string weaponId))
                    continue;

                if (weaponPrefabById.ContainsKey(weaponId))
                    continue;

                CreateRuntimeWeaponPrefab(runtime.data, weaponId);
            }
        }

        private void CreateRuntimeWeaponPrefab(WeaponData weaponData, string weaponId)
        {
            if (weaponData.WeaponPrefab == null)
            {
                Debug.LogError($"[WeaponSwitcher] WeaponData '{weaponId}'의 WeaponPrefab이 null입니다.", this);
                return;
            }

            GameObject weaponObject = Instantiate(weaponData.WeaponPrefab, weaponHolder);
            weaponObject.transform.localPosition = weaponData.HolderPosition;
            weaponObject.transform.localRotation = Quaternion.Euler(weaponData.HolderRotation);
            weaponObject.SetActive(false);

            WeaponPrefabSetting weaponPrefabSetting = weaponObject.GetComponent<WeaponPrefabSetting>();
            if (weaponPrefabSetting == null)
                weaponPrefabSetting = weaponObject.GetComponentInChildren<WeaponPrefabSetting>(true);

            if (weaponPrefabSetting == null)
            {
                Debug.LogError($"[WeaponSwitcher] 생성한 WeaponPrefab '{weaponData.WeaponPrefab.name}'에 WeaponPrefabSetting이 없습니다.", weaponObject);
                return;
            }

            string prefabWeaponId = NormalizeId(weaponPrefabSetting.WeaponId);
            if (prefabWeaponId != weaponId)
            {
                Debug.LogError($"[WeaponSwitcher] WeaponPrefabSetting의 WeaponId '{prefabWeaponId}'와 WeaponData의 WeaponId '{weaponId}'가 일치하지 않습니다.", weaponPrefabSetting);
                return;
            }

            TryRegisterWeaponPrefab(weaponPrefabSetting, $"Runtime[{weaponId}]");
        }

        private bool TryRegisterWeaponPrefab(WeaponPrefabSetting weaponPrefabSetting, string source)
        {
            string weaponId = NormalizeId(weaponPrefabSetting.WeaponId);
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                Debug.LogError($"[WeaponSwitcher] {source} WeaponPrefabSetting의 WeaponId가 비어 있습니다.", weaponPrefabSetting);
                return false;
            }

            if (weaponPrefabById.ContainsKey(weaponId))
            {
                Debug.LogError($"[WeaponSwitcher] Dictionary에 중복된 WeaponId '{weaponId}'가 등록되려 했습니다. Source: {source}", weaponPrefabSetting);
                return false;
            }

            weaponPrefabById.Add(weaponId, weaponPrefabSetting);
            return true;
        }

        private bool TryGetValidWeaponData(WeaponData weaponData, string source, out string weaponId)
        {
            weaponId = string.Empty;

            if (weaponData == null)
            {
                Debug.LogError($"[WeaponSwitcher] {source}의 WeaponData가 null입니다.", this);
                return false;
            }

            weaponId = NormalizeId(weaponData.WeaponId);
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                Debug.LogError($"[WeaponSwitcher] {source}의 WeaponData.WeaponId가 비어 있습니다.", this);
                return false;
            }

            return true;
        }

        private void ActivateOnly(WeaponPrefabSetting activeWeaponPrefab)
        {
            foreach (WeaponPrefabSetting weaponPrefabSetting in weaponPrefabById.Values)
            {
                if (weaponPrefabSetting == null)
                    continue;

                weaponPrefabSetting.gameObject.SetActive(weaponPrefabSetting == activeWeaponPrefab);
            }
        }

        private void DeactivateAllWeapons()
        {
            foreach (WeaponPrefabSetting weaponPrefabSetting in weaponPrefabById.Values)
            {
                if (weaponPrefabSetting == null)
                    continue;

                weaponPrefabSetting.gameObject.SetActive(false);
            }
        }

        private bool UpdateWeaponControllerMuzzle(WeaponPrefabSetting activeWeaponPrefab, string weaponId)
        {
            if (!activeWeaponPrefab.muzzleOut(out Transform muzzle) || muzzle == null)
            {
                Debug.LogError($"[WeaponSwitcher] 현재 무기 '{weaponId}'의 Muzzle이 null입니다.", activeWeaponPrefab);
                return false;
            }

            if (!weaponController.SetMuzzle(muzzle))
                return false;

            weaponController.SetCurrentWeaponPrefabSetting(activeWeaponPrefab);
            return true;
        }

        private string NormalizeId(string weaponId)
        {
            return string.IsNullOrWhiteSpace(weaponId) ? string.Empty : weaponId.Trim();
        }
    }
}
