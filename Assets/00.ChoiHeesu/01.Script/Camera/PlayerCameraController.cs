using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00.ChoiHeesu._04.StateChangeScript
{
    [DisallowMultipleComponent]
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private CameraSet cameraSet;
        [SerializeField] private Transform cameraFollowTarget;
        [SerializeField] private Transform adsFollowTarget;

        [Header("Auto Find")]
        [SerializeField] private string cameraFollowTargetName = "CameraFollowTarget";
        [SerializeField] private string adsFollowTargetName = "ADSFollowTarget";

        private PlayerActionState lastActionState;
        private bool hasLastActionState;

        private bool missingThirdPersonControllerLogged;
        private bool missingCameraSetLogged;
        private bool missingCameraFollowTargetLogged;
        private bool missingADSFollowTargetLogged;
        private bool isDuplicateController;

        private void Awake()
        {
            if (!TryKeepPrimaryController())
                return;

            CacheReferences();
        }

        private void OnEnable()
        {
            if (isDuplicateController)
                return;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (isDuplicateController)
                return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            if (isDuplicateController)
                return;

            RegisterToCameraSet(true);
        }

        private void LateUpdate()
        {
            if (isDuplicateController)
                return;

            if (!HasRequiredReferences())
                return;

            PlayerActionState currentActionState = thirdPersonController.CurrentActionState;

            if (hasLastActionState && currentActionState == lastActionState)
                return;

            ApplyCameraState(currentActionState, false);
        }

        private void CacheReferences()
        {
            if (thirdPersonController == null)
                TryGetComponent(out thirdPersonController);

            if (cameraFollowTarget == null)
                cameraFollowTarget = FindChildByName(transform.root, cameraFollowTargetName);

            if (cameraFollowTarget == null && thirdPersonController != null && thirdPersonController.CinemachineCameraTarget != null)
                cameraFollowTarget = thirdPersonController.CinemachineCameraTarget.transform;

            if (adsFollowTarget == null)
                adsFollowTarget = FindChildByName(transform.root, adsFollowTargetName);

            if (cameraSet == null)
                cameraSet = FindCameraSet();
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

            if (cameraSet == null)
            {
                LogMissingReference(nameof(cameraSet), ref missingCameraSetLogged,
                    "씬에 CameraSet 오브젝트를 배치하거나 Inspector에 연결해주세요.");
                hasReferences = false;
            }

            if (cameraFollowTarget == null)
            {
                LogMissingReference(nameof(cameraFollowTarget), ref missingCameraFollowTargetLogged,
                    "CameraRootSet 아래에 CameraFollowTarget을 만들거나 Inspector에 연결해주세요.");
                hasReferences = false;
            }

            if (adsFollowTarget == null)
            {
                LogMissingReference(nameof(adsFollowTarget), ref missingADSFollowTargetLogged,
                    "CameraRootSet 아래에 ADSFollowTarget을 만들거나 Inspector에 연결해주세요.");
                hasReferences = false;
            }

            return hasReferences;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            cameraSet = null;
            missingCameraSetLogged = false;
            RegisterToCameraSet(true);
        }

        private bool TryKeepPrimaryController()
        {
            PlayerCameraController[] controllers = GetComponents<PlayerCameraController>();
            if (controllers.Length <= 1)
                return true;

            PlayerCameraController primaryController = controllers[0];
            int bestScore = primaryController.GetReferenceScore();

            for (int i = 1; i < controllers.Length; i++)
            {
                int score = controllers[i].GetReferenceScore();
                if (score <= bestScore)
                    continue;

                primaryController = controllers[i];
                bestScore = score;
            }

            if (primaryController == this)
                return true;

            isDuplicateController = true;
            enabled = false;
            Debug.LogWarning("[PlayerCameraController] 같은 GameObject에 중복 컴포넌트가 있어 이 컴포넌트를 비활성화했습니다.", this);
            return false;
        }

        private int GetReferenceScore()
        {
            int score = 0;

            if (thirdPersonController != null)
                score++;

            if (cameraSet != null)
                score++;

            if (cameraFollowTarget != null)
                score++;

            if (adsFollowTarget != null)
                score++;

            return score;
        }

        private void RegisterToCameraSet(bool instant)
        {
            if (!HasRequiredReferences())
                return;

            cameraSet.BindTargets(cameraFollowTarget, adsFollowTarget);
            ApplyCameraState(thirdPersonController.CurrentActionState, instant);
        }

        private void ApplyCameraState(PlayerActionState actionState, bool instant)
        {
            if (cameraSet == null)
                return;

            CameraSet.PlayerCameraMode cameraMode = GetCameraMode(actionState);
            cameraSet.SetCameraMode(cameraMode, instant || !hasLastActionState);

            lastActionState = actionState;
            hasLastActionState = true;
        }

        private static CameraSet.PlayerCameraMode GetCameraMode(PlayerActionState actionState)
        {
            if (actionState == PlayerActionState.Aiming)
                return CameraSet.PlayerCameraMode.ADS;

            if (actionState == PlayerActionState.AimHold)
                return CameraSet.PlayerCameraMode.FollowAiming;

            return CameraSet.PlayerCameraMode.ThirdPerson;
        }

        private static CameraSet FindCameraSet()
        {
            if (CameraSet.ActiveInstance != null)
                return CameraSet.ActiveInstance;

#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<CameraSet>();
#else
            return FindObjectOfType<CameraSet>();
#endif
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), childName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void LogMissingReference(string fieldName, ref bool alreadyLogged, string message)
        {
            if (alreadyLogged)
                return;

            Debug.LogError($"[PlayerCameraController] {fieldName}이 null입니다. {message}", this);
            alreadyLogged = true;
        }
    }
}
