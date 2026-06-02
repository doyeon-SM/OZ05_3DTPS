using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;

namespace _00.ChoiHeesu._04.StateChangeScript
{
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private CinemachineCamera thirdPersonCamera;
        [SerializeField] private CinemachineCamera adsCamera;

        [Header("Priority")]
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        [Header("Blend")]
        [SerializeField] private CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;
        [SerializeField] private float normalToAimingBlendTime = 0.18f;
        [SerializeField] private float aimingToNormalBlendTime = 0.32f;

        private PlayerActionState lastActionState;
        private bool hasLastActionState;

        private bool missingThirdPersonControllerLogged;
        private bool missingCinemachineBrainLogged;
        private bool missingThirdPersonCameraLogged;
        private bool missingAdsCameraLogged;

        private void Awake()
        {
            CacheReferences();
        }

        private void Start()
        {
            if (!HasRequiredReferences())
                return;

            ApplyCameraState(thirdPersonController.CurrentActionState, 0f);
        }

        private void LateUpdate()
        {
            if (!HasRequiredReferences())
                return;

            PlayerActionState currentActionState = thirdPersonController.CurrentActionState;

            if (hasLastActionState && currentActionState == lastActionState)
                return;

            float blendTime = GetBlendTime(currentActionState);
            ApplyCameraState(currentActionState, blendTime);
        }

        private void CacheReferences()
        {
            if (thirdPersonController == null)
                TryGetComponent(out thirdPersonController);

            if (cinemachineBrain == null && Camera.main != null)
                Camera.main.TryGetComponent(out cinemachineBrain);
        }

        private bool HasRequiredReferences()
        {
            CacheReferences();

            bool hasReferences = true;

            if (thirdPersonController == null)
            {
                LogMissingReference(nameof(thirdPersonController), ref missingThirdPersonControllerLogged,
                    "플레이어의 ThirdPersonController를 연결해주세요.");
                hasReferences = false;
            }

            if (cinemachineBrain == null)
            {
                LogMissingReference(nameof(cinemachineBrain), ref missingCinemachineBrainLogged,
                    "Main Camera에 있는 CinemachineBrain을 연결해주세요.");
                hasReferences = false;
            }

            if (thirdPersonCamera == null)
            {
                LogMissingReference(nameof(thirdPersonCamera), ref missingThirdPersonCameraLogged,
                    "일반 3인칭 CinemachineCamera를 연결해주세요.");
                hasReferences = false;
            }

            if (adsCamera == null)
            {
                LogMissingReference(nameof(adsCamera), ref missingAdsCameraLogged,
                    "ADS용 CinemachineCamera를 연결해주세요.");
                hasReferences = false;
            }

            return hasReferences;
        }

        private float GetBlendTime(PlayerActionState currentActionState)
        {
            if (!hasLastActionState)
                return 0f;

            if (currentActionState == PlayerActionState.Aiming)
                return normalToAimingBlendTime;

            return aimingToNormalBlendTime;
        }

        private void ApplyCameraState(PlayerActionState actionState, float blendTime)
        {
            SetBrainBlend(blendTime);

            bool isAiming = actionState == PlayerActionState.Aiming;
            SetCameraPriority(thirdPersonCamera, isAiming ? inactivePriority : activePriority);
            SetCameraPriority(adsCamera, isAiming ? activePriority : inactivePriority);

            lastActionState = actionState;
            hasLastActionState = true;
        }

        private void SetBrainBlend(float blendTime)
        {
            if (cinemachineBrain == null)
                return;

            cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, Mathf.Max(blendTime, 0f));
        }

        private static void SetCameraPriority(CinemachineCamera targetCamera, int priority)
        {
            if (targetCamera == null)
                return;

            targetCamera.Priority.Value = priority;
        }

        private void LogMissingReference(string fieldName, ref bool alreadyLogged, string message)
        {
            if (alreadyLogged)
                return;

            Debug.LogError($"[PlayerCameraController] {fieldName}이 null입니다. {message}", this);
            alreadyLogged = true;
        }

        private void OnValidate()
        {
            activePriority = Mathf.Max(activePriority, inactivePriority + 1);
            normalToAimingBlendTime = Mathf.Max(normalToAimingBlendTime, 0f);
            aimingToNormalBlendTime = Mathf.Max(aimingToNormalBlendTime, 0f);
        }
    }
}
