using _02.Script.Combat;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00.ChoiHeesu._03.WeaponChangeSystem
{
    public class WeaponRuntimeManager : MonoBehaviour
    {
        #region singleton

        public static WeaponRuntimeManager Instance { get; private set; }

        private bool InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WeaponRuntimeManager] 중복 인스턴스가 생성되어 제거합니다.", this);
                Destroy(gameObject);
                return false;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        private const int MaxWeaponCount = 5;

        [Header("S.O Data")]
        [SerializeField] private WeaponData[] weaponDataArray = new WeaponData[MaxWeaponCount];

        [Header("Weapon Runtime")]
        [SerializeField] private WeaponRuntime[] weaponRuntimes = new WeaponRuntime[MaxWeaponCount];

        [Header("Weapon Controller")]
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private string currentWeaponId;

        [Header("Event Channel")]
        [SerializeField] private SingleStringEventChannel itemIDEventChannel;

        [Header("Weapon Ammo")]
        [SerializeField] private int pistolAmmo;
        [SerializeField] private int smgAmmo;
        [SerializeField] private int sgAmmo;
        [SerializeField] private int arAmmo;
        [SerializeField] private int mgAmmo;

        public WeaponData[] WeaponDataArray => weaponDataArray;
        public WeaponRuntime[] WeaponRuntimes => weaponRuntimes;
        public WeaponController WeaponController => weaponController;
        public IReadOnlyDictionary<WeaponClass, int> WeaponAmmoByClass => weaponAmmoByClass;

        private readonly Dictionary<WeaponClass, int> weaponAmmoByClass = new Dictionary<WeaponClass, int>();
        private bool missingItemIDEventChannelLogged;

        private void Awake()
        {
            if (!InitializeSingleton())
                return;

            EnsureWeaponDataArray();
            EnsureWeaponRuntimeArray();
            SyncWeaponRuntimesWithWeaponDataArray();
            InitializeWeaponRuntimeMagazines();
            SyncWeaponAmmoDictionary();
            ValidateWeaponRuntimeData();
            FindAndBindWeaponController();
        }

        private void OnEnable()
        {
            if (Instance != this)
                return;

            if (itemIDEventChannel != null)
                itemIDEventChannel.Register(HandleItemIDReceived);
            else
                ReportMissingItemIDEventChannel();

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (Instance != this)
                return;

            if (itemIDEventChannel != null)
                itemIDEventChannel.Unregister(HandleItemIDReceived);

            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnValidate()
        {
            EnsureWeaponDataArray();
            EnsureWeaponRuntimeArray();
            SyncWeaponRuntimesWithWeaponDataArray();
            SyncWeaponAmmoDictionary();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindAndBindWeaponController();
        }

        public bool FindAndBindWeaponController()
        {
            if (weaponController == null)
                weaponController = FindFirstObjectByType<WeaponController>(FindObjectsInactive.Include);

            if (weaponController == null)
            {
                Debug.LogWarning("[WeaponRuntimeManager] 현재 씬에서 WeaponController를 찾지 못했습니다. 플레이어가 생성된 뒤 FindAndBindWeaponController를 다시 호출해주세요.", this);
                return false;
            }

            return ApplyCurrentRuntimeToWeaponController();
        }

        private void EnsureWeaponDataArray()
        {
            if (weaponDataArray == null || weaponDataArray.Length != MaxWeaponCount)
            {
                System.Array.Resize(ref weaponDataArray, MaxWeaponCount);
            }
        }

        private void EnsureWeaponRuntimeArray()
        {
            if (weaponRuntimes == null || weaponRuntimes.Length != MaxWeaponCount)
            {
                System.Array.Resize(ref weaponRuntimes, MaxWeaponCount);
            }

            for (int i = 0; i < weaponRuntimes.Length; i++)
            {
                if (weaponRuntimes[i] == null)
                    weaponRuntimes[i] = new WeaponRuntime(null);
            }
        }

        private void SyncWeaponRuntimesWithWeaponDataArray()
        {
            for (int i = 0; i < MaxWeaponCount; i++)
            {
                WeaponData weaponData = weaponDataArray[i];

                if (weaponRuntimes[i] != null && weaponRuntimes[i].data == weaponData)
                {
                    weaponRuntimes[i].RefreshCachedData();
                    continue;
                }

                weaponRuntimes[i] = new WeaponRuntime(weaponData);
            }
        }

        private void InitializeWeaponRuntimeMagazines()
        {
            for (int i = 0; i < weaponRuntimes.Length; i++)
            {
                if (weaponRuntimes[i] == null || weaponRuntimes[i].data == null)
                    continue;

                weaponRuntimes[i].InitializeAmmo();
            }
        }

        private void SyncWeaponAmmoDictionary()
        {
            weaponAmmoByClass[WeaponClass.Pistol] = Mathf.Max(pistolAmmo, 0);
            weaponAmmoByClass[WeaponClass.SMG] = Mathf.Max(smgAmmo, 0);
            weaponAmmoByClass[WeaponClass.SG] = Mathf.Max(sgAmmo, 0);
            weaponAmmoByClass[WeaponClass.AR] = Mathf.Max(arAmmo, 0);
            weaponAmmoByClass[WeaponClass.MG] = Mathf.Max(mgAmmo, 0);
        }

        private void ValidateWeaponRuntimeData()
        {
            for (int i = 0; i < weaponRuntimes.Length; i++)
            {
                if (weaponRuntimes[i] == null)
                {
                    Debug.LogError($"[WeaponRuntimeManager] weaponRuntimes[{i}]가 null입니다. WeaponRuntime 슬롯을 확인하세요.", this);
                    continue;
                }

                if (weaponRuntimes[i].data == null)
                    Debug.LogError($"[WeaponRuntimeManager] weaponRuntimes[{i}].data가 null입니다. Inspector에서 WeaponData S.O를 연결해주세요.", this);
            }
        }

        private void ReportMissingItemIDEventChannel()
        {
            if (missingItemIDEventChannelLogged)
                return;

            Debug.LogError("[WeaponRuntimeManager] itemIDEventChannel이 null입니다. ItemIDEventChannel.asset을 Inspector에 연결해야 픽업한 무기를 해금할 수 있습니다.", this);
            missingItemIDEventChannelLogged = true;
        }

        public bool TryUnlockWeaponByItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogError("[WeaponRuntimeManager] 전달받은 itemId가 비어 있습니다. RaycastInteractor에서 WeaponData.WeaponId를 올바르게 전달하는지 확인하세요.", this);
                return false;
            }

            if (!TryGetWeaponRuntimeByItemId(itemId, out WeaponRuntime runtime))
            {
                Debug.LogError($"[WeaponRuntimeManager] WeaponId '{itemId}'와 일치하는 WeaponRuntime을 찾을 수 없습니다. Weapon Runtimes 배열의 data.WeaponId를 확인하세요.", this);
                return false;
            }

            runtime.UnLocked = true;
            return true;
        }

        private void HandleItemIDReceived(string itemId)
        {
            TryUnlockWeaponByItemId(itemId);
        }

        public bool TryUnlockWeapon(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogError("[WeaponRuntimeManager] TryUnlockWeapon에 전달된 WeaponData가 null입니다.", this);
                return false;
            }

            return TryUnlockWeaponByItemId(weaponData.WeaponId);
        }

        public bool IsWeaponUnlocked(string itemId)
        {
            return TryGetWeaponRuntimeByItemId(itemId, out WeaponRuntime runtime) && runtime.UnLocked;
        }

        public bool CanSelectWeaponByLock(string itemId, out string blockMessage)
        {
            blockMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(itemId))
            {
                blockMessage = "[WeaponRuntimeManager] 무기 선택 요청 itemId가 비어 있습니다.";
                return false;
            }

            if (!TryGetWeaponRuntimeByItemId(itemId, out WeaponRuntime runtime))
            {
                blockMessage = $"[WeaponRuntimeManager] WeaponId '{itemId}'와 일치하는 WeaponRuntime을 찾을 수 없습니다.";
                return false;
            }

            if (!runtime.UnLocked)
            {
                blockMessage = $"[WeaponRuntimeManager] WeaponId '{itemId}'는 아직 언락되지 않았습니다.";
                return false;
            }

            return true;
        }

        public bool TryRequestWeaponChange(string itemId, out string blockMessage)
        {
            blockMessage = string.Empty;

            if (!CanSelectWeaponByLock(itemId, out blockMessage))
                return false;

            if (!TryGetWeaponRuntimeByItemId(itemId, out WeaponRuntime runtime))
            {
                blockMessage = $"[WeaponRuntimeManager] WeaponId '{itemId}'와 일치하는 WeaponRuntime을 찾을 수 없습니다.";
                return false;
            }

            if (!TryEnsureWeaponController(out blockMessage))
                return false;

            if (!weaponController.SetCurrentWeaponRuntime(runtime))
            {
                blockMessage = $"[WeaponRuntimeManager] WeaponId '{itemId}'를 WeaponController에 적용하지 못했습니다.";
                return false;
            }

            currentWeaponId = runtime.data.WeaponId;
            return true;
        }

        public bool TryGetReloadPreview(WeaponRuntime runtime, out int reloadAmount, out int consumedAmmo, out string blockMessage)
        {
            reloadAmount = 0;
            consumedAmmo = 0;
            blockMessage = string.Empty;

            if (runtime == null || runtime.data == null)
            {
                blockMessage = "[WeaponRuntimeManager] 재장전할 WeaponRuntime 또는 WeaponData가 null입니다.";
                return false;
            }

            WeaponData weaponData = runtime.data;
            if (!weaponData.UseAmmo)
            {
                blockMessage = "[WeaponRuntimeManager] 탄약을 사용하지 않는 무기는 재장전하지 않습니다.";
                return false;
            }

            int maxAmmo = Mathf.Max(weaponData.MagazineSize, 0);
            if (runtime.currentAmmo >= maxAmmo)
            {
                blockMessage = "[WeaponRuntimeManager] 탄창이 이미 가득 차 있습니다.";
                return false;
            }

            int neededAmmo = maxAmmo - runtime.currentAmmo;
            int bulletCost = Mathf.Max(weaponData.BulletCost, 1);
            int availableWeaponAmmo = GetWeaponAmmoValue(weaponData.WeaponType);
            int reloadableAmmo = availableWeaponAmmo / bulletCost;

            reloadAmount = Mathf.Min(neededAmmo, reloadableAmmo);
            if (reloadAmount <= 0)
            {
                blockMessage = $"[WeaponRuntimeManager] {weaponData.WeaponType} 보유 탄약이 부족해 재장전할 수 없습니다.";
                return false;
            }

            consumedAmmo = reloadAmount * bulletCost;
            return true;
        }

        public bool TryReloadWeapon(WeaponRuntime runtime, out int reloadAmount, out int consumedAmmo, out string blockMessage)
        {
            if (!TryGetReloadPreview(runtime, out reloadAmount, out consumedAmmo, out blockMessage))
                return false;

            WeaponData weaponData = runtime.data;
            int availableWeaponAmmo = GetWeaponAmmoValue(weaponData.WeaponType);

            runtime.Reload(reloadAmount);
            SetWeaponAmmoValue(weaponData.WeaponType, availableWeaponAmmo - consumedAmmo);
            SyncWeaponAmmoDictionary();
            return true;
        }

        private bool TryEnsureWeaponController(out string blockMessage)
        {
            blockMessage = string.Empty;

            if (weaponController != null)
                return true;

            weaponController = FindFirstObjectByType<WeaponController>(FindObjectsInactive.Include);
            if (weaponController != null)
                return true;

            blockMessage = "[WeaponRuntimeManager] 현재 씬에서 WeaponController를 찾지 못해 무기를 변경할 수 없습니다.";
            return false;
        }

        private bool ApplyCurrentRuntimeToWeaponController()
        {
            if (weaponController == null)
                return false;

            WeaponRuntime runtime = GetCurrentOrDefaultRuntime();
            if (runtime == null || runtime.data == null)
                return false;

            currentWeaponId = runtime.data.WeaponId;
            return weaponController.SetCurrentWeaponRuntime(runtime);
        }

        private WeaponRuntime GetCurrentOrDefaultRuntime()
        {
            if (!string.IsNullOrWhiteSpace(currentWeaponId) &&
                TryGetWeaponRuntimeByItemId(currentWeaponId, out WeaponRuntime currentRuntime))
            {
                return currentRuntime;
            }

            for (int i = 0; i < weaponRuntimes.Length; i++)
            {
                WeaponRuntime runtime = weaponRuntimes[i];
                if (runtime == null || runtime.data == null || !runtime.UnLocked)
                    continue;

                return runtime;
            }

            for (int i = 0; i < weaponRuntimes.Length; i++)
            {
                WeaponRuntime runtime = weaponRuntimes[i];
                if (runtime == null || runtime.data == null)
                    continue;

                return runtime;
            }

            return null;
        }

        public bool TrySetCurrentAmmo(string itemId, int currentAmmo)
        {
            if (!TryGetWeaponRuntimeByItemId(itemId, out WeaponRuntime runtime))
            {
                Debug.LogError($"[WeaponRuntimeManager] currentAmmo를 설정할 수 없습니다. WeaponId '{itemId}'와 일치하는 WeaponRuntime이 없습니다.", this);
                return false;
            }

            runtime.currentAmmo = Mathf.Max(currentAmmo, 0);
            return true;
        }

        public bool TryGetCurrentAmmo(string itemId, out int currentAmmo)
        {
            currentAmmo = 0;

            if (!TryGetWeaponRuntimeByItemId(itemId, out WeaponRuntime runtime))
            {
                Debug.LogError($"[WeaponRuntimeManager] currentAmmo를 가져올 수 없습니다. WeaponId '{itemId}'와 일치하는 WeaponRuntime이 없습니다.", this);
                return false;
            }

            currentAmmo = runtime.currentAmmo;
            return true;
        }

        public bool TryGetWeaponAmmo(WeaponClass weaponClass, out int ammo)
        {
            ammo = GetWeaponAmmoValue(weaponClass);
            SyncWeaponAmmoDictionary();
            return true;
        }

        private int GetWeaponAmmoValue(WeaponClass weaponClass)
        {
            switch (weaponClass)
            {
                case WeaponClass.Pistol:
                    return Mathf.Max(pistolAmmo, 0);
                case WeaponClass.SMG:
                    return Mathf.Max(smgAmmo, 0);
                case WeaponClass.SG:
                    return Mathf.Max(sgAmmo, 0);
                case WeaponClass.AR:
                    return Mathf.Max(arAmmo, 0);
                case WeaponClass.MG:
                    return Mathf.Max(mgAmmo, 0);
                default:
                    return 0;
            }
        }

        private void SetWeaponAmmoValue(WeaponClass weaponClass, int ammo)
        {
            int safeAmmo = Mathf.Max(ammo, 0);

            switch (weaponClass)
            {
                case WeaponClass.Pistol:
                    pistolAmmo = safeAmmo;
                    break;
                case WeaponClass.SMG:
                    smgAmmo = safeAmmo;
                    break;
                case WeaponClass.SG:
                    sgAmmo = safeAmmo;
                    break;
                case WeaponClass.AR:
                    arAmmo = safeAmmo;
                    break;
                case WeaponClass.MG:
                    mgAmmo = safeAmmo;
                    break;
            }
        }

        private bool TryGetWeaponRuntimeByItemId(string itemId, out WeaponRuntime foundRuntime)
        {
            foundRuntime = null;

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            string normalizedItemId = NormalizeId(itemId);

            for (int i = 0; i < weaponRuntimes.Length; i++)
            {
                WeaponRuntime runtime = weaponRuntimes[i];

                if (runtime == null || runtime.data == null)
                    continue;

                if (NormalizeId(runtime.data.WeaponId) != normalizedItemId)
                    continue;

                foundRuntime = runtime;
                return true;
            }

            return false;
        }

        private string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }
}
