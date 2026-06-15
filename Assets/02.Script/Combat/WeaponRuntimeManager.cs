using _02.Script.Combat;
using System;
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

        private bool EnsureInitialized()
        {
            if (!InitializeSingleton())
                return false;

            if (isInitialized)
                return true;

            EnsureWeaponDataArray();
            EnsureWeaponRuntimeArray();
            SyncWeaponRuntimesWithWeaponDataArray();
            InitializeWeaponRuntimeMagazines();
            SyncWeaponAmmoDictionary();
            ValidateWeaponRuntimeData();
            isInitialized = true;
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        private const int MaxWeaponCount = 5;
        private const string PistolAmmoItemId = "pistolammo";
        private const string SmgAmmoItemId = "smgammo";
        private const string SgAmmoItemId = "sgammo";
        private const string ArAmmoItemId = "arammo";
        private const string MgAmmoItemId = "mgammo";

        [Serializable]
        private class WeaponAmmoEntry
        {
            public string itemId;
            public int ammo;

            public WeaponAmmoEntry()
            {
            }

            public WeaponAmmoEntry(string itemId, int ammo)
            {
                this.itemId = itemId;
                this.ammo = ammo;
            }
        }

        [Serializable]
        private class WeaponAmmoIdMapping
        {
            public WeaponClass weaponClass;
            public string ammoItemId;

            public WeaponAmmoIdMapping()
            {
            }

            public WeaponAmmoIdMapping(WeaponClass weaponClass, string ammoItemId)
            {
                this.weaponClass = weaponClass;
                this.ammoItemId = ammoItemId;
            }
        }

        [Header("S.O Data")]
        [SerializeField] private WeaponData[] weaponDataArray = new WeaponData[MaxWeaponCount];

        [Header("Weapon Runtime")]
        [SerializeField] private WeaponRuntime[] weaponRuntimes = new WeaponRuntime[MaxWeaponCount];

        [Header("Weapon Controller")]
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private string currentWeaponId;

        [Header("Event Channel")]
        [SerializeField] private SingleStringEventChannel itemIDEventChannel;
        [SerializeField] private SingleIntEventChannel weaponAmmoEvent;

        [Header("Weapon Ammo")]
        [SerializeField] private WeaponAmmoEntry[] weaponAmmoEntries;
        [SerializeField] private WeaponAmmoIdMapping[] weaponAmmoIdMappings;

        [Header("Grenade")]
        [SerializeField] private int grenadeCount;

        public WeaponData[] WeaponDataArray => weaponDataArray;
        public WeaponRuntime[] WeaponRuntimes => weaponRuntimes;
        public WeaponController WeaponController => weaponController;
        public IReadOnlyDictionary<string, int> WeaponAmmoByItemId => weaponAmmoByItemId;
        public int GrenadeCount => grenadeCount;
        public bool HasGrenades => grenadeCount > 0;

        public event Action<int> OnGrenadeCountChanged;

        private readonly Dictionary<string, int> weaponAmmoByItemId = new Dictionary<string, int>();
        private readonly Dictionary<WeaponClass, string> ammoItemIdByWeaponClass = new Dictionary<WeaponClass, string>();
        private bool missingItemIDEventChannelLogged;
        private bool missingWeaponAmmoEventLogged;
        private bool isInitialized;

        private void Awake()
        {
            if (!EnsureInitialized())
                return;

            FindAndBindWeaponController();
        }

        private void OnEnable()
        {
            if (!EnsureInitialized())
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
            grenadeCount = Mathf.Max(grenadeCount, 0);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindAndBindWeaponController();
        }

        public bool FindAndBindWeaponController()
        {
            if (!EnsureInitialized())
                return false;

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

        private void EnsureWeaponAmmoSettings()
        {
            if (weaponAmmoEntries == null || weaponAmmoEntries.Length == 0)
            {
                weaponAmmoEntries = new[]
                {
                    new WeaponAmmoEntry(PistolAmmoItemId, 100),
                    new WeaponAmmoEntry(SmgAmmoItemId, 100),
                    new WeaponAmmoEntry(SgAmmoItemId, 100),
                    new WeaponAmmoEntry(ArAmmoItemId, 100),
                    new WeaponAmmoEntry(MgAmmoItemId, 500)
                };
            }

            if (weaponAmmoIdMappings == null || weaponAmmoIdMappings.Length == 0)
            {
                weaponAmmoIdMappings = new[]
                {
                    new WeaponAmmoIdMapping(WeaponClass.Pistol, PistolAmmoItemId),
                    new WeaponAmmoIdMapping(WeaponClass.SMG, SmgAmmoItemId),
                    new WeaponAmmoIdMapping(WeaponClass.SG, SgAmmoItemId),
                    new WeaponAmmoIdMapping(WeaponClass.AR, ArAmmoItemId),
                    new WeaponAmmoIdMapping(WeaponClass.MG, MgAmmoItemId)
                };
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
            EnsureWeaponAmmoSettings();
            weaponAmmoByItemId.Clear();
            ammoItemIdByWeaponClass.Clear();

            for (int i = 0; i < weaponAmmoEntries.Length; i++)
            {
                WeaponAmmoEntry entry = weaponAmmoEntries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.itemId))
                    continue;

                string normalizedItemId = NormalizeId(entry.itemId);
                int safeAmmo = Mathf.Max(entry.ammo, 0);
                entry.itemId = normalizedItemId;
                entry.ammo = safeAmmo;
                weaponAmmoByItemId[normalizedItemId] = safeAmmo;
            }

            for (int i = 0; i < weaponAmmoIdMappings.Length; i++)
            {
                WeaponAmmoIdMapping mapping = weaponAmmoIdMappings[i];
                if (mapping == null || string.IsNullOrWhiteSpace(mapping.ammoItemId))
                    continue;

                string normalizedItemId = NormalizeId(mapping.ammoItemId);
                mapping.ammoItemId = normalizedItemId;
                ammoItemIdByWeaponClass[mapping.weaponClass] = normalizedItemId;

                if (!weaponAmmoByItemId.ContainsKey(normalizedItemId))
                    weaponAmmoByItemId.Add(normalizedItemId, 0);
            }
        }

        private void SyncWeaponAmmoEntriesFromDictionary()
        {
            EnsureWeaponAmmoSettings();
            HashSet<string> syncedItemIds = new HashSet<string>();

            for (int i = 0; i < weaponAmmoEntries.Length; i++)
            {
                WeaponAmmoEntry entry = weaponAmmoEntries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.itemId))
                    continue;

                string normalizedItemId = NormalizeId(entry.itemId);
                entry.itemId = normalizedItemId;

                if (weaponAmmoByItemId.TryGetValue(normalizedItemId, out int ammo))
                    entry.ammo = Mathf.Max(ammo, 0);

                syncedItemIds.Add(normalizedItemId);
            }

            foreach (KeyValuePair<string, int> ammoPair in weaponAmmoByItemId)
            {
                if (syncedItemIds.Contains(ammoPair.Key))
                    continue;

                AddWeaponAmmoEntry(ammoPair.Key, ammoPair.Value);
                syncedItemIds.Add(ammoPair.Key);
            }
        }

        private void AddWeaponAmmoEntry(string itemId, int ammo)
        {
            int entryCount = weaponAmmoEntries != null ? weaponAmmoEntries.Length : 0;
            Array.Resize(ref weaponAmmoEntries, entryCount + 1);
            weaponAmmoEntries[entryCount] = new WeaponAmmoEntry(itemId, Mathf.Max(ammo, 0));
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

            if (!EnsureInitialized())
            {
                blockMessage = "[WeaponRuntimeManager] 초기화가 완료되지 않아 무기를 변경할 수 없습니다.";
                return false;
            }

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
            RaiseCurrentWeaponAmmo();
            return true;
        }

        public bool TryConsumeGrenade()
        {
            if (grenadeCount <= 0)
                return false;

            grenadeCount--;
            OnGrenadeCountChanged?.Invoke(grenadeCount);
            return true;
        }

        public void AddGrenade(int amount)
        {
            if (amount <= 0)
                return;

            grenadeCount += amount;
            OnGrenadeCountChanged?.Invoke(grenadeCount);
        }

        public bool TryGetReloadPreview(WeaponRuntime runtime, out int reloadAmount, out int consumedAmmo, out string blockMessage)
        {
            reloadAmount = 0;
            consumedAmmo = 0;
            blockMessage = string.Empty;

            if (!EnsureInitialized())
            {
                blockMessage = "[WeaponRuntimeManager] 초기화가 완료되지 않아 재장전 정보를 계산할 수 없습니다.";
                return false;
            }

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
            RaiseCurrentWeaponAmmoIfChangedWeapon(weaponData.WeaponType);
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
            bool applied = weaponController.SetCurrentWeaponRuntime(runtime);
            if (applied)
                RaiseCurrentWeaponAmmo();

            return applied;
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
            ammo = 0;

            if (!TryGetAmmoItemId(weaponClass, out string itemId))
                return false;

            return TryGetWeaponAmmo(itemId, out ammo);
        }

        public bool TryGetWeaponAmmo(string itemId, out int ammo)
        {
            ammo = 0;

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            ammo = GetWeaponAmmoValue(itemId);
            return true;
        }

        public bool TryGetAmmoItemId(WeaponClass weaponClass, out string itemId)
        {
            itemId = string.Empty;

            if (!ammoItemIdByWeaponClass.TryGetValue(weaponClass, out string mappedItemId))
                return false;

            itemId = mappedItemId;
            return !string.IsNullOrWhiteSpace(itemId);
        }

        public bool TryIncreaseWeaponAmmo(string itemId, int amount, out int currentAmmo)
        {
            currentAmmo = 0;

            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            string normalizedItemId = NormalizeId(itemId);
            int currentValue = GetWeaponAmmoValue(normalizedItemId);
            long nextValue = (long)currentValue + amount;
            currentAmmo = nextValue > int.MaxValue ? int.MaxValue : (int)nextValue;
            SetWeaponAmmoValue(normalizedItemId, currentAmmo);
            RaiseCurrentWeaponAmmoIfChangedAmmoItem(normalizedItemId);
            return true;
        }

        public bool TryConsumeWeaponAmmo(string itemId, int amount, out int currentAmmo)
        {
            currentAmmo = 0;

            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            string normalizedItemId = NormalizeId(itemId);
            int currentValue = GetWeaponAmmoValue(normalizedItemId);
            if (currentValue < amount)
                return false;

            currentAmmo = currentValue - amount;
            SetWeaponAmmoValue(normalizedItemId, currentAmmo);
            RaiseCurrentWeaponAmmoIfChangedAmmoItem(normalizedItemId);
            return true;
        }

        private void RaiseCurrentWeaponAmmoIfChangedWeapon(WeaponClass changedWeaponClass)
        {
            WeaponRuntime currentRuntime = GetCurrentOrDefaultRuntime();
            if (currentRuntime == null || currentRuntime.data == null)
                return;

            if (currentRuntime.data.WeaponType != changedWeaponClass)
                return;

            RaiseCurrentWeaponAmmo();
        }

        private void RaiseCurrentWeaponAmmoIfChangedAmmoItem(string changedAmmoItemId)
        {
            WeaponRuntime currentRuntime = GetCurrentOrDefaultRuntime();
            if (currentRuntime == null || currentRuntime.data == null)
                return;

            if (!TryGetAmmoItemId(currentRuntime.data.WeaponType, out string currentAmmoItemId))
                return;

            if (NormalizeId(currentAmmoItemId) != NormalizeId(changedAmmoItemId))
                return;

            RaiseCurrentWeaponAmmo();
        }

        private void RaiseCurrentWeaponAmmo()
        {
            if (weaponAmmoEvent == null)
            {
                ReportMissingWeaponAmmoEvent();
                return;
            }

            WeaponRuntime currentRuntime = GetCurrentOrDefaultRuntime();
            if (currentRuntime == null || currentRuntime.data == null)
                return;

            int currentWeaponAmmo = GetWeaponAmmoValue(currentRuntime.data.WeaponType);
            weaponAmmoEvent.Raise(currentWeaponAmmo);
        }

        private void ReportMissingWeaponAmmoEvent()
        {
            if (missingWeaponAmmoEventLogged)
                return;

            Debug.LogError("[WeaponRuntimeManager] weaponAmmoEvent가 null입니다. WeaponAmmoEvent.asset을 Inspector에 연결해야 CombatUI 보유 탄약 UI를 갱신할 수 있습니다.", this);
            missingWeaponAmmoEventLogged = true;
        }

        private int GetWeaponAmmoValue(WeaponClass weaponClass)
        {
            if (!TryGetAmmoItemId(weaponClass, out string itemId))
                return 0;

            return GetWeaponAmmoValue(itemId);
        }

        private int GetWeaponAmmoValue(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0;

            if (weaponAmmoByItemId.TryGetValue(NormalizeId(itemId), out int ammo))
            {
                return Mathf.Max(ammo, 0);
            }

            return 0;
        }

        private void SetWeaponAmmoValue(WeaponClass weaponClass, int ammo)
        {
            if (!TryGetAmmoItemId(weaponClass, out string itemId))
                return;

            SetWeaponAmmoValue(itemId, ammo);
        }

        private void SetWeaponAmmoValue(string itemId, int ammo)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            string normalizedItemId = NormalizeId(itemId);
            weaponAmmoByItemId[normalizedItemId] = Mathf.Max(ammo, 0);
            SyncWeaponAmmoEntriesFromDictionary();
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
