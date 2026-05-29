using System.Collections;
using StarterAssets;
using UnityEngine;

namespace _02.Script.Combat
{
    public class WeaponController : MonoBehaviour
    {
        private const int LogNoWeaponRuntime = 0;
        private const int LogNoWeaponData = 1;
        private const int LogReloading = 2;
        private const int LogFireDelay = 3;
        private const int LogNoAmmo = 4;
        private const int LogReloadFullAmmo = 5;
        private const int LogReloadNotUseAmmo = 6;
        private const int LogReloadStart = 7;
        private const int LogReloadComplete = 8;

        [Header("Weapon")]
        [SerializeField] private WeaponRuntime[] weaponRuntime;
        [SerializeField] private int currentWeaponIndex;
        [SerializeField] private GameObject gunSocket;

        [Header("Current Weapon State")]
        [SerializeField] private bool isfire;
        [SerializeField] private bool isReloading;
        [SerializeField] private bool autoReloadOnEmpty = true;
        [SerializeField] private float shotDelayTime;
        [SerializeField] private float saveTime = -999f;
        
        [Header("Event Channels")]
        [SerializeField] private DoubleIntEventChannel AmmoChangeChannel;

        public float RPM => CurrentWeapon != null && CurrentWeapon.data != null ? CurrentWeapon.data.RPM : 0f;
        public bool AutoFire => CurrentWeapon != null && CurrentWeapon.data != null && CurrentWeapon.data.AutoFire;
        public int Damage => CurrentWeapon != null && CurrentWeapon.data != null ? CurrentWeapon.data.Damage : 0;
        public bool IsFiring => isfire;
        public bool IsReloading => isReloading;

        public bool VariableChange;

        private Coroutine _reloadCoroutine;
        [SerializeField]private AnimationController _animationController;

        private WeaponRuntime CurrentWeapon
        {
            get
            {
                if (weaponRuntime == null || weaponRuntime.Length == 0) return null;
                if (currentWeaponIndex < 0 || currentWeaponIndex >= weaponRuntime.Length) return null;

                return weaponRuntime[currentWeaponIndex];
            }
        }

        #region Unity Life Cycle

        private void Awake()
        {
            AwakeSetting();
        }

        private void Start()
        {
            WeaponRuntime currentWeapon = CurrentWeapon;
            if (!ValidateCurrentWeapon(currentWeapon)) return;

            SendAmmoUI(currentWeapon.currentAmmo, currentWeapon.data.MagazineSize);
        }

        #endregion

        #region Script Methods

        private void AwakeSetting()
        {
            ClampCurrentWeaponIndex();
            InitializeWeaponRuntimes();
            if(_animationController == null)_animationController =  GetComponentInParent<AnimationController>();
            SetReloadingAnimation(false);
            RPMCalculate(); // RPM으로 딜레이 시간 계산
        }

        private void ClampCurrentWeaponIndex()
        {
            if (weaponRuntime == null || weaponRuntime.Length == 0)
            {
                currentWeaponIndex = 0;
                return;
            }

            currentWeaponIndex = Mathf.Clamp(currentWeaponIndex, 0, weaponRuntime.Length - 1);
        }

        private void InitializeWeaponRuntimes()
        {
            if (weaponRuntime == null) return;

            for (int i = 0; i < weaponRuntime.Length; i++)
            {
                if (weaponRuntime[i] == null || weaponRuntime[i].data == null) continue;

                weaponRuntime[i].InitializeAmmo();
            }
        }

        public bool TryFire()
        {
            isfire = false;

            WeaponRuntime currentWeapon = CurrentWeapon;
            if (!ValidateCurrentWeapon(currentWeapon)) return false;

            if (isReloading)
            {
                LogWeaponState(LogReloading);
                return false;
            }

            if (!CanFireByDelay())
            {
                LogWeaponState(LogFireDelay);
                return false;
            }

            if (!currentWeapon.HasAmmo() || !currentWeapon.hasEnoughAmmo())
            {
                LogWeaponState(LogNoAmmo);
                return false;
            }

            int beforeAmmo = currentWeapon.currentAmmo;
            currentWeapon.ConsumeAmmo();
            saveTime = Time.time;
            isfire = true;

            if (currentWeapon.data.UseAmmo)
            {
                Debug.Log($"현재 남은 총알 {beforeAmmo} -> {currentWeapon.currentAmmo} / {currentWeapon.data.MagazineSize}");
                SendAmmoUI(currentWeapon.currentAmmo, currentWeapon.data.MagazineSize);
                TryAutoReload(currentWeapon);
            }
            else
            {
                Debug.Log("발사 성공: 탄약을 사용하지 않는 무기입니다.");
            }

            return true;
        }

        private void SendAmmoUI(int currentAmmo, int magazineSize)
        {
            if (AmmoChangeChannel == null)
            {
                Debug.LogWarning("AmmoChangeChannel이 연결되어 있지 않아 탄약 UI를 갱신할 수 없습니다.");
                return;
            }

            AmmoChangeChannel.Raise(currentAmmo, magazineSize);
        }
        public void TryReload()
        {
            WeaponRuntime currentWeapon = CurrentWeapon;
            if (!ValidateCurrentWeapon(currentWeapon)) return;

            if (!currentWeapon.data.UseAmmo)
            {
                LogWeaponState(LogReloadNotUseAmmo);
                return;
            }

            if (isReloading)
            {
                LogWeaponState(LogReloading);
                return;
            }

            if (currentWeapon.currentAmmo >= currentWeapon.data.MagazineSize)
            {
                LogWeaponState(LogReloadFullAmmo);
                return;
            }

            _reloadCoroutine = StartCoroutine(ReloadRoutine(currentWeapon));
        }

        private void TryAutoReload(WeaponRuntime currentWeapon)
        {
            if (!autoReloadOnEmpty) return;
            if (currentWeapon == null || currentWeapon.data == null) return;
            if (!currentWeapon.data.UseAmmo) return;
            if (currentWeapon.currentAmmo > 0) return;

            TryReload();
        }

        private IEnumerator ReloadRoutine(WeaponRuntime reloadWeapon)
        {
            isReloading = true;
            SetReloadingAnimation(isReloading);
            LogWeaponState(LogReloadStart);

            float reloadTime = Mathf.Max(reloadWeapon.data.ReloadTime, 0f);
            yield return new WaitForSeconds(reloadTime);

            reloadWeapon.Reload();
            isReloading = false;
            SetReloadingAnimation(isReloading);
            _reloadCoroutine = null;
            SendAmmoUI(reloadWeapon.currentAmmo, reloadWeapon.data.MagazineSize);

            LogWeaponState(LogReloadComplete);
        }

        private void SetReloadingAnimation(bool reloading)
        {
            if (_animationController == null) return;

            _animationController.SetReloading(reloading);
        }
        
        private void RPMCalculate() // RPM을 받아서 초당 딜레이 타임으로 변환 계산합니다. (60sec / RPM)
        {
            WeaponRuntime currentWeapon = CurrentWeapon;
            if (currentWeapon == null || currentWeapon.data == null || currentWeapon.data.RPM <= 0f)
            {
                shotDelayTime = 0f;
                return;
            }

            shotDelayTime = 60.0f / currentWeapon.data.RPM;
        }
        private bool CanFireByDelay()
        {
            return Time.time > saveTime + shotDelayTime;
        }

        private bool ValidateCurrentWeapon(WeaponRuntime currentWeapon)
        {
            // null 방어코드 메서드.
            if (currentWeapon == null)
            {
                LogWeaponState(LogNoWeaponRuntime);
                return false;
            }

            if (currentWeapon.data == null)
            {
                LogWeaponState(LogNoWeaponData);
                return false;
            }

            return true;
        }

        private void LogWeaponState(int logType)
        {
            // 추후 In Game Log UI로 전환할 수 있도록 무기 관련 로그는 이 메서드에서만 처리합니다.
            switch (logType)
            {
                case LogNoWeaponRuntime:
                    Debug.LogWarning("발사 불가: 현재 무기 Runtime이 없습니다.");
                    break;
                case LogNoWeaponData:
                    Debug.LogWarning("발사 불가: 현재 무기 데이터가 없습니다.");
                    break;
                case LogReloading:
                    Debug.Log("발사 불가: 재장전 중입니다.");
                    break;
                case LogFireDelay:
                    Debug.Log("발사 불가: 아직 발사 딜레이 중입니다.");
                    break;
                case LogNoAmmo:
                    Debug.Log("발사 불가: 탄약이 부족합니다.");
                    break;
                case LogReloadFullAmmo:
                    Debug.Log("재장전 불가: 탄창이 이미 가득 차 있습니다.");
                    break;
                case LogReloadNotUseAmmo:
                    Debug.Log("재장전 불가: 탄약을 사용하지 않는 무기입니다.");
                    break;
                case LogReloadStart:
                    Debug.Log("재장전을 시작합니다.");
                    break;
                case LogReloadComplete:
                    Debug.Log("재장전이 완료되었습니다.");
                    break;
                default:
                    Debug.LogWarning("알 수 없는 무기 로그 타입입니다.");
                    break;
            }
        }

        #endregion
    }
}
