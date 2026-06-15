using _00.ChoiHeesu._03.WeaponChangeSystem;
using _02.Script.Combat;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00.ChoiHeesu._01.Script.Explosion
{
    [DisallowMultipleComponent]
    public class GrenadeThrowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StarterAssetsInputs input;
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private AnimationController animationController;
        [SerializeField] private Animator animator;
        [SerializeField] private WeaponRuntimeManager weaponRuntimeManager;
        [SerializeField] private WeaponSwitcher weaponSwitcher;
        [SerializeField] private GrenadeTrajectoryPreview trajectoryPreview;
        [SerializeField] private Rigidbody grenadePrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private GameObject handGrenadeVisual;

        [Header("Throw")]
        [SerializeField] private float throwForce = 12f;
        [SerializeField] private float upwardModifier = 0.35f;
        [SerializeField] private float torqueForce = 8f;

        [Header("Animator Parameters")]
        [SerializeField] private string grenadeAnimatorParameter = "Grenade";
        [SerializeField] private string enterGrenadeTriggerParameter = "EnterGrenade";
        [SerializeField] private string throwTriggerParameter;
        [SerializeField] private string cancelTriggerParameter;

        [Header("Debug")]
        [SerializeField] private bool logMissingReferences;

        private bool isGrenadeMode;
        private bool isHoldingGrenade;
        private bool isThrowingGrenade;
        private bool isGrenadeThrowLocked;
        private bool isCancelingGrenade;
        private bool pendingThrow;
        private bool wasAttackPressed;
        private GrenadeThrowData cachedThrowData;
        private bool hasCachedThrowData;
        private WeaponController subscribedWeaponController;
        private bool missingWeaponRuntimeManagerLogged;

        public bool IsGrenadeMode => isGrenadeMode;
        public bool IsGrenadeThrowLocked => isGrenadeThrowLocked;
        public bool IsWeaponChangeBlocked => isGrenadeMode && (isGrenadeThrowLocked || isHoldingGrenade || pendingThrow || isThrowingGrenade || isCancelingGrenade);

        private void Awake()
        {
            CacheReferences();
            SetHandGrenadeVisible(false);
            SyncTrajectorySettings();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            CacheReferences();
            BindExternalRuntimeReferences(true);
            SubscribeWeaponChanged();
        }

        private void Start()
        {
            CacheReferences();
            BindExternalRuntimeReferences(true);
            SubscribeWeaponChanged();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribeWeaponChanged();
            HideTrajectory();
        }

        private void Update()
        {
            CacheReferences();
            BindExternalRuntimeReferences(false);
            SubscribeWeaponChanged();
            SyncTrajectorySettings();

            if (input == null)
                return;

            HandleGrenadeEquipInput();

            if (!isGrenadeMode)
            {
                wasAttackPressed = input.Attack;
                return;
            }

            if (!HasGrenadesAvailable() && !isThrowingGrenade)
            {
                ExitGrenadeMode();
                wasAttackPressed = input.Attack;
                return;
            }

            HandleGrenadeRoutineInput();
            wasAttackPressed = input.Attack;
        }

        public void OnGrenadeThrowAnimationEvent()
        {
            if (!isThrowingGrenade || !pendingThrow)
                return;

            if (!CanSpawnGrenade())
            {
                AbortThrow();
                return;
            }

            if (!TryConsumeGrenade())
            {
                AbortThrow();
                return;
            }

            if (!TrySpawnGrenade())
            {
                AbortThrow();
                return;
            }

            pendingThrow = false;
            SetHandGrenadeVisible(false);
        }

        public void OnGrenadeThrowAnimationEnd()
        {
            isThrowingGrenade = false;
            isGrenadeThrowLocked = false;
            pendingThrow = false;
            isHoldingGrenade = false;
            isCancelingGrenade = false;
            hasCachedThrowData = false;
            HideTrajectory();

            if (HasGrenadesAvailable())
            {
                SetGrenadeAnimator(true);
                PlayEnterGrenadeTrigger();
                EnterGrenadeNormalState();
                SetHandGrenadeVisible(true);
                return;
            }

            ExitGrenadeMode();
        }

        public void OnGrenadeCancelAnimationEnd()
        {
            RestoreGrenadeNormalAfterCancel();
        }

        public void OnGrenadeCancleAnimationEnd()
        {
            OnGrenadeCancelAnimationEnd();
        }

        public bool TryExitGrenadeModeForWeaponChange()
        {
            if (!isGrenadeMode)
                return true;

            if (IsWeaponChangeBlocked)
                return false;

            ExitGrenadeMode(false);
            return true;
        }

        public void ExitGrenadeMode()
        {
            ExitGrenadeMode(true);
        }

        private void ExitGrenadeMode(bool playWeaponChangeAnimation)
        {
            bool wasGrenadeMode = isGrenadeMode;

            isGrenadeMode = false;
            isHoldingGrenade = false;
            isThrowingGrenade = false;
            isGrenadeThrowLocked = false;
            isCancelingGrenade = false;
            pendingThrow = false;
            hasCachedThrowData = false;

            HideTrajectory();
            SetGrenadeAnimator(false);
            SetHandGrenadeVisible(false);

            if (weaponSwitcher != null)
                weaponSwitcher.SetWeaponsVisible(true);

            if (thirdPersonController != null)
                thirdPersonController.ClearGrenadeActionState();

            if (wasGrenadeMode && playWeaponChangeAnimation)
                PlayWeaponChangeTrigger();
        }

        private void HandleGrenadeEquipInput()
        {
            if (!input.GrenadePressed)
                return;

            input.ConsumeGrenadeInput();

            TryEnterGrenadeMode(true);
        }

        private bool TryEnterGrenadeMode(bool logBlocked = false)
        {
            if (!CanEnterGrenadeMode(out string blockReason))
            {
                if (logBlocked)
                    LogGrenadeEntryBlocked(blockReason);

                return false;
            }

            isGrenadeMode = true;
            isHoldingGrenade = false;
            isThrowingGrenade = false;
            isGrenadeThrowLocked = false;
            isCancelingGrenade = false;
            pendingThrow = false;
            hasCachedThrowData = false;
            wasAttackPressed = input != null && input.Attack;

            if (weaponSwitcher != null)
                weaponSwitcher.SetWeaponsVisible(false);

            SetHandGrenadeVisible(true);
            SetGrenadeAnimator(true);
            PlayEnterGrenadeTrigger();
            EnterGrenadeNormalState();
            return true;
        }

        private bool CanEnterGrenadeMode(out string blockReason)
        {
            if (isGrenadeMode)
            {
                blockReason = "이미 수류탄 모드입니다.";
                return false;
            }

            if (isGrenadeThrowLocked || isCancelingGrenade || isThrowingGrenade || pendingThrow)
            {
                blockReason = $"수류탄 상태 플래그가 잠겨 있습니다. locked:{isGrenadeThrowLocked}, canceling:{isCancelingGrenade}, throwing:{isThrowingGrenade}, pending:{pendingThrow}";
                return false;
            }

            if (!HasGrenadesAvailable())
            {
                blockReason = weaponRuntimeManager == null
                    ? "WeaponRuntimeManager를 찾지 못했습니다."
                    : $"남은 수류탄이 없습니다. GrenadeCount:{weaponRuntimeManager.GrenadeCount}";
                return false;
            }

            if (animator == null && animationController == null)
            {
                blockReason = "Animator 또는 AnimationController 참조가 없습니다.";
                return false;
            }

            blockReason = string.Empty;
            return true;
        }

        private void HandleGrenadeRoutineInput()
        {
            if (isCancelingGrenade)
                return;

            if (CanCancelThrowInput())
            {
                CancelGrenadeHold();
                return;
            }

            if (input.Attack && !isHoldingGrenade && !isGrenadeThrowLocked)
                BeginGrenadeHold();

            if (isHoldingGrenade && !pendingThrow && !isThrowingGrenade)
                UpdateGrenadeHold();

            if (wasAttackPressed && !input.Attack && isHoldingGrenade && !pendingThrow && !isThrowingGrenade)
                ConfirmGrenadeThrow();
        }

        private void BeginGrenadeHold()
        {
            if (!HasGrenadesAvailable())
            {
                ExitGrenadeMode();
                return;
            }

            isHoldingGrenade = true;
            EnterGrenadeRoutineState();
            SetGrenadeAnimator(true);
        }

        private void UpdateGrenadeHold()
        {
            if (trajectoryPreview == null)
                return;

            if (trajectoryPreview.TryUpdatePreview(out GrenadeThrowData throwData))
            {
                cachedThrowData = throwData;
                hasCachedThrowData = true;
            }
        }

        private bool CanCancelThrowInput()
        {
            if (!isHoldingGrenade || pendingThrow || isThrowingGrenade)
                return false;

            return input.AimHoldPressed || input.ADSClickPressed || input.AimHold;
        }

        private void CancelGrenadeHold()
        {
            isHoldingGrenade = false;
            pendingThrow = false;
            isThrowingGrenade = false;
            isGrenadeThrowLocked = true;
            isCancelingGrenade = true;
            hasCachedThrowData = false;
            wasAttackPressed = input != null && input.Attack;

            HideTrajectory();
            PlayOptionalTrigger(cancelTriggerParameter);
            EnterGrenadeRoutineState();
        }

        private void ConfirmGrenadeThrow()
        {
            HideTrajectory();

            if (!TryCacheThrowData())
            {
                CancelGrenadeHold();
                return;
            }

            isHoldingGrenade = false;
            isThrowingGrenade = true;
            isGrenadeThrowLocked = true;
            pendingThrow = true;

            PlayOptionalTrigger(throwTriggerParameter);
            EnterGrenadeRoutineState();
        }

        private bool TryCacheThrowData()
        {
            if (hasCachedThrowData)
                return true;

            if (trajectoryPreview == null)
                return false;

            if (trajectoryPreview.TryGetCurrentThrowData(out cachedThrowData))
            {
                hasCachedThrowData = true;
                return true;
            }

            if (trajectoryPreview.TryCalculateThrowData(out cachedThrowData))
            {
                hasCachedThrowData = true;
                return true;
            }

            return false;
        }

        private bool TrySpawnGrenade()
        {
            if (!CanSpawnGrenade())
                return false;

            Quaternion spawnRotation = cachedThrowData.ThrowDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(cachedThrowData.ThrowDirection)
                : throwPoint.rotation;

            Rigidbody grenade = Instantiate(grenadePrefab, throwPoint.position, spawnRotation);
            SetLinearVelocity(grenade, Vector3.zero);
            grenade.angularVelocity = Vector3.zero;
            grenade.AddForce(cachedThrowData.InitialVelocity * grenade.mass, ForceMode.Impulse);
            grenade.AddTorque(Random.insideUnitSphere * Mathf.Max(torqueForce, 0f), ForceMode.Impulse);
            return true;
        }

        private bool CanSpawnGrenade()
        {
            if (grenadePrefab != null && throwPoint != null && TryCacheThrowData())
                return true;

            if (logMissingReferences)
                Debug.LogWarning("[GrenadeThrowController] grenadePrefab, throwPoint 또는 투척 계산값이 없어 수류탄을 생성할 수 없습니다.", this);

            return false;
        }

        private bool TryConsumeGrenade()
        {
            BindExternalRuntimeReferences(true);

            return weaponRuntimeManager != null && weaponRuntimeManager.TryConsumeGrenade();
        }

        private bool HasGrenadesAvailable()
        {
            BindExternalRuntimeReferences(true);

            return weaponRuntimeManager != null && weaponRuntimeManager.HasGrenades;
        }

        private void AbortThrow()
        {
            isThrowingGrenade = false;
            isGrenadeThrowLocked = false;
            pendingThrow = false;
            isHoldingGrenade = false;
            isCancelingGrenade = false;
            hasCachedThrowData = false;
            HideTrajectory();

            if (HasGrenadesAvailable())
                EnterGrenadeNormalState();
            else
                ExitGrenadeMode();
        }

        private void RestoreGrenadeNormalAfterCancel()
        {
            isThrowingGrenade = false;
            isGrenadeThrowLocked = false;
            pendingThrow = false;
            isHoldingGrenade = false;
            isCancelingGrenade = false;
            hasCachedThrowData = false;
            HideTrajectory();

            if (!HasGrenadesAvailable())
            {
                ExitGrenadeMode();
                return;
            }

            SetGrenadeAnimator(true);
            SetHandGrenadeVisible(true);
            PlayEnterGrenadeTrigger();
            EnterGrenadeNormalState();
        }

        private void EnterGrenadeNormalState()
        {
            if (thirdPersonController != null)
                thirdPersonController.SetGrenadeActionState(PlayerActionState.GrenadeNormal);
        }

        private void EnterGrenadeRoutineState()
        {
            if (thirdPersonController != null)
                thirdPersonController.SetGrenadeActionState(PlayerActionState.GrenadeRoutine);
        }

        private void HideTrajectory()
        {
            if (trajectoryPreview != null)
                trajectoryPreview.Hide();
        }

        private void SetHandGrenadeVisible(bool isVisible)
        {
            if (handGrenadeVisual != null)
                handGrenadeVisual.SetActive(isVisible);
        }

        private void SetGrenadeAnimator(bool isGrenade)
        {
            if (animationController != null && grenadeAnimatorParameter == "Grenade")
            {
                animationController.SetGrenade(isGrenade);
                return;
            }

            if (animator == null || string.IsNullOrWhiteSpace(grenadeAnimatorParameter))
                return;

            animator.SetBool(grenadeAnimatorParameter, isGrenade);
        }

        private void PlayEnterGrenadeTrigger()
        {
            if (animationController != null && enterGrenadeTriggerParameter == "EnterGrenade")
            {
                animationController.PlayEnterGrenade();
                return;
            }

            PlayOptionalTrigger(enterGrenadeTriggerParameter);
        }

        private void PlayWeaponChangeTrigger()
        {
            if (animationController != null)
                animationController.SetWeaponChange();
        }

        private void LogGrenadeEntryBlocked(string blockReason)
        {
            Debug.LogError(
                $"[GrenadeThrowController] G키 수류탄 모드 진입이 차단되었습니다. reason:{blockReason}, " +
                $"isGrenadeMode:{isGrenadeMode}, isHoldingGrenade:{isHoldingGrenade}, isThrowingGrenade:{isThrowingGrenade}, " +
                $"isGrenadeThrowLocked:{isGrenadeThrowLocked}, isCancelingGrenade:{isCancelingGrenade}, pendingThrow:{pendingThrow}, " +
                $"weaponRuntimeManager:{(weaponRuntimeManager != null ? weaponRuntimeManager.name : "null")}",
                this);
        }

        private void PlayOptionalTrigger(string triggerParameter)
        {
            if (animator == null || string.IsNullOrWhiteSpace(triggerParameter))
                return;

            if (!HasAnimatorParameter(triggerParameter, AnimatorControllerParameterType.Trigger))
                return;

            animator.SetTrigger(triggerParameter);
        }

        private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == parameterType && parameter.name == parameterName)
                    return true;
            }

            if (logMissingReferences)
                Debug.LogWarning($"[GrenadeThrowController] Animator에 {parameterType} Parameter '{parameterName}'가 없습니다.", this);

            return false;
        }

        private void CacheReferences()
        {
            if (input == null)
                TryGetComponent(out input);

            if (thirdPersonController == null)
                TryGetComponent(out thirdPersonController);

            if (animationController == null)
                TryGetComponent(out animationController);

            if (animator == null)
                TryGetComponent(out animator);

            if (weaponSwitcher == null)
                weaponSwitcher = GetComponentInChildren<WeaponSwitcher>(true);

            if (weaponSwitcher == null)
                weaponSwitcher = GetComponentInParent<WeaponSwitcher>();
        }

        private void BindExternalRuntimeReferences(bool logIfMissing)
        {
            WeaponRuntimeManager foundManager = WeaponRuntimeManager.Instance;

            if (foundManager == null)
            {
                // WeaponRuntimeManager는 싱글톤 초기화가 끝난 뒤에만 안전하게 사용할 수 있다.
                // 플레이어 Awake가 RuntimeManager Awake보다 먼저 실행되면 Find로 객체는 보이지만 Instance는 아직 null일 수 있다.
                WeaponRuntimeManager pendingManager = FindFirstObjectByType<WeaponRuntimeManager>(FindObjectsInactive.Include);
                if (pendingManager != null)
                    return;
            }

            if (foundManager == null)
            {
                if (logIfMissing && logMissingReferences && !missingWeaponRuntimeManagerLogged)
                {
                    Debug.LogWarning("[GrenadeThrowController] WeaponRuntimeManager를 찾지 못했습니다. 씬에 RuntimeManager가 있는지 확인해주세요.", this);
                    missingWeaponRuntimeManagerLogged = true;
                }

                return;
            }

            if (weaponRuntimeManager != foundManager)
            {
                UnsubscribeWeaponChanged();
                weaponRuntimeManager = foundManager;
            }

            missingWeaponRuntimeManagerLogged = false;

            // 플레이어는 씬마다 새로 생성될 수 있으므로, 외부 RuntimeManager가 현재 씬의 WeaponController를 다시 잡도록 갱신한다.
            if (weaponRuntimeManager.WeaponController == null)
                weaponRuntimeManager.FindAndBindWeaponController();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UnsubscribeWeaponChanged();
            weaponRuntimeManager = null;
            CacheReferences();
            BindExternalRuntimeReferences(true);
            SubscribeWeaponChanged();
        }

        private void SubscribeWeaponChanged()
        {
            BindExternalRuntimeReferences(false);

            WeaponController currentWeaponController = weaponRuntimeManager != null ? weaponRuntimeManager.WeaponController : null;
            if (currentWeaponController == subscribedWeaponController)
                return;

            UnsubscribeWeaponChanged();
            subscribedWeaponController = currentWeaponController;

            if (subscribedWeaponController != null)
                subscribedWeaponController.CurrentWeaponChanged += HandleCurrentWeaponChanged;
        }

        private void UnsubscribeWeaponChanged()
        {
            if (subscribedWeaponController == null)
                return;

            subscribedWeaponController.CurrentWeaponChanged -= HandleCurrentWeaponChanged;
            subscribedWeaponController = null;
        }

        private void HandleCurrentWeaponChanged(WeaponRuntime weaponRuntime)
        {
            if (isGrenadeMode)
                ExitGrenadeMode(false);
        }

        private void SyncTrajectorySettings()
        {
            if (trajectoryPreview != null)
                trajectoryPreview.SetThrowSettings(throwForce, upwardModifier);
        }

        private static void SetLinearVelocity(Rigidbody targetRigidbody, Vector3 velocity)
        {
            if (targetRigidbody == null)
                return;

            // Unity 6에서는 linearVelocity, 이전 버전에서는 velocity를 사용합니다.
#if UNITY_6000_0_OR_NEWER
            targetRigidbody.linearVelocity = velocity;
#else
            targetRigidbody.velocity = velocity;
#endif
        }

        private void OnValidate()
        {
            throwForce = Mathf.Max(throwForce, 0f);
            upwardModifier = Mathf.Max(upwardModifier, 0f);
            torqueForce = Mathf.Max(torqueForce, 0f);
        }
    }
}
