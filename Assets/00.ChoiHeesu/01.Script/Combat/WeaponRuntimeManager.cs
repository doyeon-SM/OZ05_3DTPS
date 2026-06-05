using _02.Script.Combat;
using System.Collections.Generic;
using UnityEngine;

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
            SyncWeaponAmmoDictionary();
            ValidateWeaponRuntimeData();
        }

        private void OnEnable()
        {
            if (Instance != this)
                return;

            if (itemIDEventChannel != null)
                itemIDEventChannel.Register(HandleItemIDReceived);
            else
                ReportMissingItemIDEventChannel();
        }

        private void OnDisable()
        {
            if (Instance != this)
                return;

            if (itemIDEventChannel != null)
                itemIDEventChannel.Unregister(HandleItemIDReceived);
        }

        private void OnValidate()
        {
            EnsureWeaponDataArray();
            EnsureWeaponRuntimeArray();
            SyncWeaponRuntimesWithWeaponDataArray();
            SyncWeaponAmmoDictionary();
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
                    continue;

                weaponRuntimes[i] = new WeaponRuntime(weaponData);
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
            SyncWeaponAmmoDictionary();
            return weaponAmmoByClass.TryGetValue(weaponClass, out ammo);
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
