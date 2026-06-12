using Unity.Cinemachine;
using UnityEngine;

namespace _00.ChoiHeesu._04.StateChangeScript
{
    public class CameraSet : MonoBehaviour
    {
        public enum PlayerCameraMode
        {
            ThirdPerson,
            FollowAiming,
            ADS
        }

        public static CameraSet ActiveInstance { get; private set; }

        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private CinemachineCamera thirdPersonCamera;
        [SerializeField] private CinemachineCamera followAimingCamera;
        [SerializeField] private CinemachineCamera adsCamera;

        [Header("Priority")]
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        [Header("Blend")]
        [SerializeField] private CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;
        [SerializeField] private float thirdPersonToFollowAimingBlendTime = 0.18f;
        [SerializeField] private float followAimingToThirdPersonBlendTime = 0.32f;
        [SerializeField] private float thirdPersonToADSBlendTime = 0.18f;
        [SerializeField] private float adsToThirdPersonBlendTime = 0.32f;
        [SerializeField] private float aimModeSwitchBlendTime = 0.12f;

        [Header("Camera Angle Set ( Min , Max )")]
        [SerializeField] private Vector2 thirdPersonCameraAngleLimit = new Vector2(-30f, 70f);
        [SerializeField] private Vector2 followAimingCameraAngleLimit = new Vector2(-30f, 70f);
        [SerializeField] private Vector2 adsCameraAngleLimit = new Vector2(-30f, 70f);

        [Header("Options")]
        [SerializeField] private bool dontDestroyOnLoad;
        [SerializeField] private bool autoCacheChildReferences = true;
        [SerializeField] private bool configureADSAsFirstPerson = true;
        [SerializeField] private bool useFollowTargetAsLookAt;

        private bool hasCameraState;
        private PlayerCameraMode currentCameraMode;
        private bool isDuplicateInstance;

        private bool missingBrainLogged;
        private bool missingThirdPersonCameraLogged;
        private bool missingFollowAimingCameraLogged;
        private bool missingADSCameraLogged;

        public Vector2 CurrentCameraAngleLimit => GetCameraAngleLimit(currentCameraMode);

        private void Awake()
        {
            if (dontDestroyOnLoad && ActiveInstance != null && ActiveInstance != this)
            {
                isDuplicateInstance = true;
                Destroy(gameObject);
                return;
            }

            ActiveInstance = this;
            CacheReferences();

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (isDuplicateInstance)
                return;

            ActiveInstance = this;
            CacheReferences();
        }

        private void OnDisable()
        {
            if (isDuplicateInstance)
                return;

            if (ActiveInstance == this)
                ActiveInstance = null;
        }

        public void BindTargets(Transform cameraFollowTarget, Transform adsFollowTarget)
        {
            CacheReferences();

            if (thirdPersonCamera != null && cameraFollowTarget != null)
            {
                thirdPersonCamera.Follow = cameraFollowTarget;

                if (useFollowTargetAsLookAt)
                    thirdPersonCamera.LookAt = cameraFollowTarget;
            }

            if (followAimingCamera != null && cameraFollowTarget != null)
            {
                followAimingCamera.Follow = cameraFollowTarget;

                if (useFollowTargetAsLookAt)
                    followAimingCamera.LookAt = cameraFollowTarget;
            }

            if (adsCamera != null && adsFollowTarget != null)
            {
                EnsureADSFirstPersonPipeline();
                adsCamera.Follow = adsFollowTarget;

                if (useFollowTargetAsLookAt)
                    adsCamera.LookAt = adsFollowTarget;
            }
        }

        public void SetAiming(bool useADSCamera, bool instant = false)
        {
            SetCameraMode(useADSCamera ? PlayerCameraMode.ADS : PlayerCameraMode.ThirdPerson, instant);
        }

        public void SetCameraMode(PlayerCameraMode nextCameraMode, bool instant = false)
        {
            if (!HasRequiredReferences())
                return;

            PlayerCameraMode resolvedCameraMode = ResolveCameraMode(nextCameraMode);
            float blendTime = GetBlendTime(resolvedCameraMode, instant);
            SetBrainBlend(blendTime);

            SetCameraPriority(thirdPersonCamera, resolvedCameraMode == PlayerCameraMode.ThirdPerson ? activePriority : inactivePriority);
            SetCameraPriority(followAimingCamera, resolvedCameraMode == PlayerCameraMode.FollowAiming ? activePriority : inactivePriority);
            SetCameraPriority(adsCamera, resolvedCameraMode == PlayerCameraMode.ADS ? activePriority : inactivePriority);

            currentCameraMode = resolvedCameraMode;
            hasCameraState = true;
        }

        public Vector2 GetCameraAngleLimit(PlayerCameraMode cameraMode)
        {
            switch (cameraMode)
            {
                case PlayerCameraMode.FollowAiming:
                    return ToPitchClampLimit(followAimingCameraAngleLimit);
                case PlayerCameraMode.ADS:
                    return ToPitchClampLimit(adsCameraAngleLimit);
                default:
                    return ToPitchClampLimit(thirdPersonCameraAngleLimit);
            }
        }

        private void CacheReferences()
        {
            if (!autoCacheChildReferences)
                return;

            if (mainCamera == null)
                mainCamera = GetComponentInChildren<Camera>(true);

            if (cinemachineBrain == null)
            {
                if (mainCamera != null)
                    mainCamera.TryGetComponent(out cinemachineBrain);

                if (cinemachineBrain == null)
                    cinemachineBrain = GetComponentInChildren<CinemachineBrain>(true);
            }

            if (thirdPersonCamera != null && followAimingCamera != null && adsCamera != null)
            {
                EnsureADSFirstPersonPipeline();
                return;
            }

            CinemachineCamera[] cameras = GetComponentsInChildren<CinemachineCamera>(true);
            foreach (CinemachineCamera targetCamera in cameras)
            {
                if (targetCamera == null)
                    continue;

                string cameraName = targetCamera.name.ToLowerInvariant();
                if (adsCamera == null && cameraName.Contains("ads"))
                {
                    adsCamera = targetCamera;
                    continue;
                }

                if (followAimingCamera == null && IsFollowAimingCameraName(cameraName))
                {
                    followAimingCamera = targetCamera;
                    continue;
                }

                if (thirdPersonCamera == null && !IsFollowAimingCameraName(cameraName))
                    thirdPersonCamera = targetCamera;
            }

            EnsureADSFirstPersonPipeline();
        }

        private static bool IsFollowAimingCameraName(string cameraName)
        {
            return cameraName.Contains("followaiming") ||
                   cameraName.Contains("follow aiming") ||
                   cameraName.Contains("aimhold") ||
                   cameraName.Contains("aim hold");
        }

        private void EnsureADSFirstPersonPipeline()
        {
            if (!configureADSAsFirstPerson || adsCamera == null)
                return;

            if (!HasPipelineStage(adsCamera, CinemachineCore.Stage.Body))
                adsCamera.gameObject.AddComponent<CinemachineHardLockToTarget>();

            if (!HasPipelineStage(adsCamera, CinemachineCore.Stage.Aim))
                adsCamera.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();
        }

        private static bool HasPipelineStage(CinemachineCamera targetCamera, CinemachineCore.Stage stage)
        {
            CinemachineComponentBase[] components = targetCamera.GetComponents<CinemachineComponentBase>();
            foreach (CinemachineComponentBase component in components)
            {
                if (component != null && component.Stage == stage)
                    return true;
            }

            return false;
        }

        private PlayerCameraMode ResolveCameraMode(PlayerCameraMode nextCameraMode)
        {
            if (nextCameraMode == PlayerCameraMode.FollowAiming && followAimingCamera == null)
            {
                LogMissingReference(nameof(followAimingCamera), ref missingFollowAimingCameraLogged,
                    "CameraSet 자식의 FollowAimingCamera CinemachineCamera를 연결해주세요.");
                return PlayerCameraMode.ThirdPerson;
            }

            return nextCameraMode;
        }

        private bool HasRequiredReferences()
        {
            CacheReferences();

            bool hasReferences = true;

            if (cinemachineBrain == null)
            {
                LogMissingReference(nameof(cinemachineBrain), ref missingBrainLogged,
                    "CameraSet 자식의 Main Camera에 CinemachineBrain을 연결해주세요.");
                hasReferences = false;
            }

            if (thirdPersonCamera == null)
            {
                LogMissingReference(nameof(thirdPersonCamera), ref missingThirdPersonCameraLogged,
                    "CameraSet 자식의 일반 3인칭 CinemachineCamera를 연결해주세요.");
                hasReferences = false;
            }

            if (adsCamera == null)
            {
                LogMissingReference(nameof(adsCamera), ref missingADSCameraLogged,
                    "CameraSet 자식의 ADS CinemachineCamera를 연결해주세요.");
                hasReferences = false;
            }

            return hasReferences;
        }

        private float GetBlendTime(PlayerCameraMode nextCameraMode, bool instant)
        {
            if (instant || !hasCameraState)
                return 0f;

            if (currentCameraMode == nextCameraMode)
                return 0f;

            if (currentCameraMode == PlayerCameraMode.ThirdPerson && nextCameraMode == PlayerCameraMode.FollowAiming)
                return thirdPersonToFollowAimingBlendTime;

            if (currentCameraMode == PlayerCameraMode.FollowAiming && nextCameraMode == PlayerCameraMode.ThirdPerson)
                return followAimingToThirdPersonBlendTime;

            if (currentCameraMode == PlayerCameraMode.ThirdPerson && nextCameraMode == PlayerCameraMode.ADS)
                return thirdPersonToADSBlendTime;

            if (currentCameraMode == PlayerCameraMode.ADS && nextCameraMode == PlayerCameraMode.ThirdPerson)
                return adsToThirdPersonBlendTime;

            return aimModeSwitchBlendTime;
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

        private static Vector2 ToPitchClampLimit(Vector2 angleLimit)
        {
            float downAngle = Mathf.Abs(angleLimit.x);
            float upAngle = Mathf.Abs(angleLimit.y);
            return new Vector2(-upAngle, downAngle);
        }

        private void LogMissingReference(string fieldName, ref bool alreadyLogged, string message)
        {
            if (alreadyLogged)
                return;

            Debug.LogError($"[CameraSet] {fieldName}이 null입니다. {message}", this);
            alreadyLogged = true;
        }

        private void OnValidate()
        {
            activePriority = Mathf.Max(activePriority, inactivePriority + 1);
            thirdPersonToFollowAimingBlendTime = Mathf.Max(thirdPersonToFollowAimingBlendTime, 0f);
            followAimingToThirdPersonBlendTime = Mathf.Max(followAimingToThirdPersonBlendTime, 0f);
            thirdPersonToADSBlendTime = Mathf.Max(thirdPersonToADSBlendTime, 0f);
            adsToThirdPersonBlendTime = Mathf.Max(adsToThirdPersonBlendTime, 0f);
            aimModeSwitchBlendTime = Mathf.Max(aimModeSwitchBlendTime, 0f);
        }
    }
}
