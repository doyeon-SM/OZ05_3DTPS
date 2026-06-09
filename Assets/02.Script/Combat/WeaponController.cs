using _00.ChoiHeesu._03.WeaponChangeSystem;
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
        private const int LogNoHitscanSystem = 9;
        private const int LogNoAimCamera = 10;
        private const int LogNoMuzzle = 11;
        private const int LogNoAimMask = 12;
        private const int LogNoShotMask = 13;
        private const int LogNoMuzzleBlockMask = 14;

        [Header("Weapon")]
        [SerializeField] private WeaponRuntime currentWeapon;
        [SerializeField] private GameObject gunSocket;

        [Header("Current Weapon State")]
        [SerializeField] private bool isfire;
        [SerializeField] private bool isReloading;
        [SerializeField] private bool autoReloadOnEmpty = true;
        [SerializeField] private float saveTime = -999f;

        [Header("Attack System")]
        [SerializeField] private TPS_TwoStepHitscanSystem hitscanSystem;
        [SerializeField] private PlayerSpreadProvider spreadProvider;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float aimRange;
        [SerializeField] private float shotRange;
        [SerializeField] private float muzzleBlockRadius;
        [SerializeField] private LayerMask aimMask;
        [SerializeField] private LayerMask shotMask;
        [SerializeField] private LayerMask muzzleBlockMask;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float hitEffectLifeTime = 0.2f;
        
        [Header("Event Channels")]
        [SerializeField] private DoubleIntEventChannel AmmoChangeChannel;

        public float RPM => CurrentWeapon != null && CurrentWeapon.data != null ? CurrentWeapon.data.RPM : 0f;
        public int Damage => CurrentWeapon != null && CurrentWeapon.data != null ? CurrentWeapon.data.Damage : 0;
        public bool IsFiring => isfire;
        public bool IsReloading => isReloading;
        public WeaponRuntime CurrentWeaponRuntime => CurrentWeapon;
        public Vector3 LastAimDirection { get; private set; }

        public bool SetMuzzle(Transform nextMuzzle)
        {
            if (nextMuzzle == null)
            {
                Debug.LogError("[WeaponController] 갱신할 Muzzle이 null입니다.", this);
                return false;
            }

            muzzle = nextMuzzle;
            return true;
        }

        public bool VariableChange;

        private Coroutine _reloadCoroutine;
        private Coroutine _burstCoroutine;
        [SerializeField]private AnimationController _animationController;
        private bool wasAttackInputPressed;
        private bool isAttackAnimationActive;

        private WeaponRuntime CurrentWeapon
        {
            get
            {
                return currentWeapon;
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
            if (currentWeapon != null)
                currentWeapon.RefreshCachedData();

            if(_animationController == null)_animationController =  GetComponentInParent<AnimationController>();
            CacheCombatReferences();
            SetReloadingAnimation(false);
            SetAttackAnimation(false);
        }

        private void CacheCombatReferences()
        {
            if (hitscanSystem == null)
                hitscanSystem = GetComponent<TPS_TwoStepHitscanSystem>();

            if (spreadProvider == null)
                spreadProvider = GetComponentInParent<PlayerSpreadProvider>();

            if (aimRange <= 0f)
                aimRange = 100f;

            if (shotRange <= 0f)
                shotRange = 100f;

            if (muzzleBlockRadius <= 0f)
                muzzleBlockRadius = 0.5f;
        }

        public bool SetCurrentWeaponRuntime(WeaponRuntime nextWeaponRuntime)
        {
            if (!ValidateCurrentWeapon(nextWeaponRuntime))
                return false;

            bool weaponChanged = currentWeapon != nextWeaponRuntime;
            if (currentWeapon != nextWeaponRuntime)
            {
                StopCurrentWeaponActions();
                currentWeapon = nextWeaponRuntime;
                saveTime = -999f;
                wasAttackInputPressed = false;
            }

            currentWeapon.RefreshCachedData();
            SendAmmoUI(currentWeapon.currentAmmo, currentWeapon.data.MagazineSize);

            if (weaponChanged && currentWeapon.data.UseAmmo && currentWeapon.currentAmmo <= 0)
                TryReload();

            return true;
        }

        private void StopCurrentWeaponActions()
        {
            if (_burstCoroutine != null)
            {
                StopCoroutine(_burstCoroutine);
                _burstCoroutine = null;
            }

            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                _reloadCoroutine = null;
            }

            isfire = false;
            isReloading = false;
            SetReloadingAnimation(false);
            SetAttackAnimation(false);
        }

        private bool TryBuildHitscanRequest(out HitscanFireRequest request)
        {
            request = default;

            WeaponRuntime currentWeapon = CurrentWeapon;
            if (!ValidateCurrentWeapon(currentWeapon))
                return false;

            CacheCombatReferences();

            if (hitscanSystem == null)
            {
                LogWeaponState(LogNoHitscanSystem);
                return false;
            }

            Camera fireCamera = Camera.main;
            if (fireCamera == null)
            {
                LogWeaponState(LogNoAimCamera);
                return false;
            }

            if (muzzle == null)
            {
                LogWeaponState(LogNoMuzzle);
                return false;
            }

            if (aimMask.value == 0)
            {
                LogWeaponState(LogNoAimMask);
                return false;
            }

            if (shotMask.value == 0)
            {
                LogWeaponState(LogNoShotMask);
                return false;
            }

            if (muzzleBlockMask.value == 0)
            {
                LogWeaponState(LogNoMuzzleBlockMask);
                return false;
            }

            request = new HitscanFireRequest
            {
                AimCamera = fireCamera,
                Muzzle = muzzle,
                AimRange = aimRange,
                ShotRange = shotRange,
                MuzzleBlockRadius = muzzleBlockRadius,
                AimMask = aimMask,
                ShotMask = shotMask,
                MuzzleBlockMask = muzzleBlockMask,
                HitEffectPrefab = hitEffectPrefab,
                HitEffectLifeTime = Mathf.Max(hitEffectLifeTime, 0.01f),
                Damage = Damage,
                SpreadAngle = GetCurrentSpreadAngle(currentWeapon.data)
            };

            return true;
        }

        public bool HandleAttackInput(bool isAttackPressed)
        {
            bool isAttackStarted = isAttackPressed && !wasAttackInputPressed;
            bool fired = false;

            WeaponRuntime currentWeapon = CurrentWeapon;
            if (isAttackPressed && !ValidateCurrentWeapon(currentWeapon))
            {
                wasAttackInputPressed = isAttackPressed;
                SetAttackAnimation(false);
                return false;
            }

            if (currentWeapon != null && currentWeapon.data != null)
            {
                switch (currentWeapon.data.fireMode)
                {
                    case FireMode.Single:
                        fired = isAttackStarted && TryAttack();
                        break;
                    case FireMode.Burst:
                        fired = isAttackStarted && TryStartBurstAttack(currentWeapon);
                        break;
                    case FireMode.Auto:
                        fired = isAttackPressed && TryAttack();
                        break;
                    default:
                        Debug.LogWarning($"지원하지 않는 FireMode입니다: {currentWeapon.data.fireMode}", this);
                        break;
                }
            }

            wasAttackInputPressed = isAttackPressed;

            if (fired)
                SetAttackAnimation(true);
            else
                SetAttackAnimation(ShouldKeepAttackAnimation(currentWeapon, isAttackPressed));

            return fired;
        }

        private bool ShouldKeepAttackAnimation(WeaponRuntime currentWeapon, bool isAttackPressed)
        {
            if (currentWeapon == null || currentWeapon.data == null)
                return false;

            switch (currentWeapon.data.fireMode)
            {
                case FireMode.Auto:
                    return isAttackAnimationActive && isAttackPressed && CanHoldAttackAnimation(currentWeapon);
                case FireMode.Burst:
                    return isAttackAnimationActive && _burstCoroutine != null && CanHoldAttackAnimation(currentWeapon);
                default:
                    return false;
            }
        }

        private bool CanHoldAttackAnimation(WeaponRuntime currentWeapon)
        {
            if (isReloading)
                return false;

            if (currentWeapon == null || currentWeapon.data == null)
                return false;

            return currentWeapon.HasAmmo() && currentWeapon.hasEnoughAmmo();
        }

        private bool TryStartBurstAttack(WeaponRuntime burstWeapon)
        {
            if (_burstCoroutine != null)
                return false;

            if (!ValidateCurrentWeapon(burstWeapon))
                return false;

            if (!CanFireByDelay())
            {
                LogWeaponState(LogFireDelay);
                return false;
            }

            int burstCount = Mathf.Max(burstWeapon.data.burstCount, 1);
            bool fired = TryAttack();

            if (!fired)
                return false;

            if (burstCount > 1)
                _burstCoroutine = StartCoroutine(BurstRoutine(burstWeapon, burstCount - 1));

            return true;
        }

        private IEnumerator BurstRoutine(WeaponRuntime burstWeapon, int remainingShotCount)
        {
            for (int i = 0; i < remainingShotCount; i++)
            {
                yield return new WaitForSeconds(burstWeapon.ShotDelayTime);

                if (CurrentWeapon != burstWeapon)
                    break;

                bool fired = TryAttack();

                if (!fired)
                    break;

                SetAttackAnimation(true);
            }

            _burstCoroutine = null;
        }

        public bool TryAttack()
        {
            if (!TryBuildHitscanRequest(out HitscanFireRequest request))
                return false;

            if (!hitscanSystem.CanFire(request))
                return false;

            if (!TryFire())
                return false;

            WeaponData weaponData = CurrentWeapon.data;
            bool fired = weaponData.WeaponType == WeaponClass.SG
                ? hitscanSystem.FireShotgun(request, weaponData.pelletCount)
                : hitscanSystem.Fire(request);

            if (fired)
                LastAimDirection = hitscanSystem.AimDirection;

            return fired;
        }

        private float GetCurrentSpreadAngle(WeaponData weaponData)
        {
            if (weaponData == null)
                return 0f;

            float weaponBaseSpreadAngle = Mathf.Max(weaponData.basicSpreadAngle, 0f);
            if (spreadProvider == null)
                return weaponBaseSpreadAngle;

            return spreadProvider.GetTotalSpreadAngle(weaponBaseSpreadAngle);
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

            if (WeaponRuntimeManager.Instance == null)
            {
                Debug.LogWarning("[WeaponController] WeaponRuntimeManager가 없어 보유 탄약을 확인할 수 없습니다.", this);
                return;
            }

            if (!WeaponRuntimeManager.Instance.TryGetReloadPreview(currentWeapon, out int reloadAmount, out int consumedAmmo, out string blockMessage))
            {
                Debug.Log(blockMessage, this);
                return;
            }

            Debug.Log($"[WeaponController] 재장전 예정: 탄창 +{reloadAmount}, 보유 탄약 -{consumedAmmo}");
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

            if (CurrentWeapon != reloadWeapon)
            {
                isReloading = false;
                SetReloadingAnimation(isReloading);
                _reloadCoroutine = null;
                yield break;
            }

            WeaponRuntimeManager weaponRuntimeManager = WeaponRuntimeManager.Instance;
            if (weaponRuntimeManager == null)
            {
                Debug.Log("[WeaponController] WeaponRuntimeManager가 없어 재장전을 적용할 수 없습니다.", this);
                isReloading = false;
                SetReloadingAnimation(isReloading);
                _reloadCoroutine = null;
                yield break;
            }

            if (!weaponRuntimeManager.TryReloadWeapon(reloadWeapon, out int reloadAmount, out int consumedAmmo, out string blockMessage))
            {
                Debug.Log(blockMessage, this);
                isReloading = false;
                SetReloadingAnimation(isReloading);
                _reloadCoroutine = null;
                yield break;
            }

            isReloading = false;
            SetReloadingAnimation(isReloading);
            _reloadCoroutine = null;
            SendAmmoUI(reloadWeapon.currentAmmo, reloadWeapon.data.MagazineSize);
            Debug.Log($"[WeaponController] 재장전 적용: 탄창 +{reloadAmount}, 보유 탄약 -{consumedAmmo}");

            LogWeaponState(LogReloadComplete);
        }

        private void SetReloadingAnimation(bool reloading)
        {
            if (_animationController == null) return;

            _animationController.SetReloading(reloading);
        }

        private void SetAttackAnimation(bool attacking)
        {
            isAttackAnimationActive = attacking;

            if (_animationController == null) return;

            _animationController.SetAttack(attacking);
        }
        
        private bool CanFireByDelay()
        {
            WeaponRuntime currentWeapon = CurrentWeapon;
            float currentShotDelayTime = currentWeapon != null ? currentWeapon.ShotDelayTime : 0f;

            return Time.time > saveTime + currentShotDelayTime;
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
                case LogNoHitscanSystem:
                    Debug.LogWarning("발사 불가: TPS_TwoStepHitscanSystem이 없습니다.");
                    break;
                case LogNoAimCamera:
                    Debug.LogWarning("발사 불가: AimCamera가 없습니다. MainCamera 태그가 붙은 카메라가 있는지 확인해주세요.");
                    break;
                case LogNoMuzzle:
                    Debug.LogWarning("발사 불가: WeaponController의 muzzle이 null입니다. Inspector에서 실제 장착 무기의 Muzzle Transform을 연결해주세요.");
                    break;
                case LogNoAimMask:
                    Debug.LogWarning("발사 불가: aimMask가 비어 있습니다. WeaponController의 Attack System 설정을 확인해주세요.");
                    break;
                case LogNoShotMask:
                    Debug.LogWarning("발사 불가: shotMask가 비어 있습니다. WeaponController의 Attack System 설정을 확인해주세요.");
                    break;
                case LogNoMuzzleBlockMask:
                    Debug.LogWarning("발사 불가: muzzleBlockMask가 비어 있습니다. WeaponController의 Attack System 설정을 확인해주세요.");
                    break;
                default:
                    Debug.LogWarning("알 수 없는 무기 로그 타입입니다.");
                    break;
            }
        }

        #endregion
    }
}
