using _00.ChoiHeesu._03.WeaponChangeSystem;
using _02.Script.Combat;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        [Header("Input")]
        [SerializeField] private string grenadeInputActionName = "Grenade";
#if !ENABLE_INPUT_SYSTEM
        [SerializeField] private KeyCode legacyGrenadeKey = KeyCode.G;
#endif

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
        private bool wasGrenadeInputHeld;
        private bool queuedGrenadeInputPressed;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput playerInput;
        private InputAction grenadeInputAction;
        private bool grenadeInputActionSubscribed;
#endif

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
#if ENABLE_INPUT_SYSTEM
            SubscribeGrenadeInputAction();
#endif
            BindExternalRuntimeReferences(true);
            SubscribeWeaponChanged();
        }

        private void Start()
        {
            CacheReferences();
#if ENABLE_INPUT_SYSTEM
            SubscribeGrenadeInputAction();
#endif
            BindExternalRuntimeReferences(true);
            SubscribeWeaponChanged();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
#if ENABLE_INPUT_SYSTEM
            UnsubscribeGrenadeInputAction();
#endif
            UnsubscribeWeaponChanged();
            queuedGrenadeInputPressed = false;
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
                ExitGrenadeMode(true, "NoGrenadesInUpdate");
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

            ExitGrenadeMode(true, "ThrowEndNoGrenades");
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

            ExitGrenadeMode(false, "WeaponChangeRequest");
            return true;
        }

        public void ExitGrenadeMode()
        {
            ExitGrenadeMode(true, "External");
        }

        private void ExitGrenadeMode(bool playWeaponChangeAnimation, string reason = "")
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

            if (wasGrenadeMode && logMissingReferences)
            {
                Debug.Log(
                    $"[GrenadeThrowController] Grenade mode exited. reason:{reason}, playWeaponChange:{playWeaponChangeAnimation}",
                    this);
            }
        }

        private void HandleGrenadeEquipInput()
        {
            if (!ConsumeGrenadeEquipPressed())
                return;

            TryEnterGrenadeMode(true);
        }

        private bool ConsumeGrenadeEquipPressed()
        {
            bool queuedInputPressed = queuedGrenadeInputPressed;
            queuedGrenadeInputPressed = false;

            bool isGrenadeInputHeld = ReadGrenadeInputHeld();
            bool isGrenadeInputPressed = isGrenadeInputHeld && !wasGrenadeInputHeld;
            wasGrenadeInputHeld = isGrenadeInputHeld;

            bool messageInputPressed = input != null && input.GrenadePressed;

            if (input != null)
            {
                if (!isGrenadeInputHeld && input.Grenade)
                    input.GrenadeInput(false);

                input.ConsumeGrenadeInput();
            }

            bool detected = queuedInputPressed || messageInputPressed || isGrenadeInputPressed;

            if (detected && logMissingReferences)
            {
                Debug.Log(
                    $"[GrenadeThrowController] Grenade input detected. queued:{queuedInputPressed}, message:{messageInputPressed}, polled:{isGrenadeInputPressed}, held:{isGrenadeInputHeld}",
                    this);
            }

            return detected;
        }

        private bool ReadGrenadeInputHeld()
        {
#if ENABLE_INPUT_SYSTEM
            CacheGrenadeInputAction();

            if (grenadeInputAction != null && grenadeInputAction.ReadValue<float>() > 0.5f)
                return true;

            if (Keyboard.current != null && Keyboard.current.gKey.isPressed)
                return true;
#else
            return Input.GetKey(legacyGrenadeKey);
#endif

            return input != null && input.Grenade;
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

            if (logMissingReferences)
            {
                Debug.Log(
                    $"[GrenadeThrowController] Grenade mode entered. GrenadeCount:{(weaponRuntimeManager != null ? weaponRuntimeManager.GrenadeCount : -1)}",
                    this);
            }

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
            {
                ClearAimCancelInput();
                return;
            }

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
                ExitGrenadeMode(true, "BeginHoldNoGrenades");
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

            ClearAimCancelInput();
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
                ExitGrenadeMode(true, "AbortThrowNoGrenades");
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
            ClearAimCancelInput();

            if (!HasGrenadesAvailable())
            {
                ExitGrenadeMode(true, "CancelEndNoGrenades");
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

        private void ClearAimCancelInput()
        {
            if (input == null)
                return;

            input.ClearAimInputState();
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
                $"[GrenadeThrowController] Grenade entry blocked. reason:{blockReason}, " +
                $"isGrenadeMode:{isGrenadeMode}, isHoldingGrenade:{isHoldingGrenade}, isThrowingGrenade:{isThrowingGrenade}, " +
                $"isGrenadeThrowLocked:{isGrenadeThrowLocked}, isCancelingGrenade:{isCancelingGrenade}, pendingThrow:{pendingThrow}, " +
                $"inputGrenadeHeld:{(input != null && input.Grenade)}, polledGrenadeHeld:{wasGrenadeInputHeld}, " +
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

#if ENABLE_INPUT_SYSTEM
            CacheGrenadeInputAction();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void CacheGrenadeInputAction()
        {
            if (playerInput == null)
                TryGetComponent(out playerInput);

            if (playerInput == null)
                playerInput = GetComponentInParent<PlayerInput>();

            if (playerInput == null || playerInput.actions == null || string.IsNullOrWhiteSpace(grenadeInputActionName))
            {
                SetGrenadeInputAction(null);
                return;
            }

            SetGrenadeInputAction(playerInput.actions.FindAction(grenadeInputActionName, false));
        }

        private void SetGrenadeInputAction(InputAction nextAction)
        {
            if (grenadeInputAction == nextAction)
                return;

            UnsubscribeGrenadeInputAction();
            grenadeInputAction = nextAction;

            if (isActiveAndEnabled)
                SubscribeGrenadeInputAction();
        }

        private void SubscribeGrenadeInputAction()
        {
            if (grenadeInputActionSubscribed || grenadeInputAction == null)
                return;

            grenadeInputAction.performed += HandleGrenadeInputActionPerformed;
            grenadeInputActionSubscribed = true;
        }

        private void UnsubscribeGrenadeInputAction()
        {
            if (!grenadeInputActionSubscribed)
                return;

            if (grenadeInputAction != null)
                grenadeInputAction.performed -= HandleGrenadeInputActionPerformed;

            grenadeInputActionSubscribed = false;
        }

        private void HandleGrenadeInputActionPerformed(InputAction.CallbackContext context)
        {
            queuedGrenadeInputPressed = true;

            if (logMissingReferences)
                Debug.Log("[GrenadeThrowController] Grenade InputAction performed queued.", this);
        }
#endif

        private void BindExternalRuntimeReferences(bool logIfMissing)
        {
            WeaponRuntimeManager foundManager = WeaponRuntimeManager.Instance;

            if (foundManager == null)
                foundManager = FindFirstObjectByType<WeaponRuntimeManager>(FindObjectsInactive.Include);

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
                ExitGrenadeMode(false, "CurrentWeaponChanged");
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
