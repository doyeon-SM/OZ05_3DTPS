using StarterAssets;
using UnityEngine;

namespace _00.ChoiHeesu._04.StateChangeScript
{
    [DefaultExecutionOrder(100)]
    public class PlayerAimController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform aimTarget;

        [Header("Aim Ray")]
        [SerializeField] private float aimRange = 100f;
        [SerializeField] private LayerMask aimMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Body Rotation")]
        [SerializeField] private bool rotateBody = true;
        [SerializeField] private float bodyRotationSpeed = 720f;
        [SerializeField] private float minAimDirectionDistance = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay;

        public Vector3 CurrentAimPoint { get; private set; }
        public Vector3 CurrentAimDirection { get; private set; }

        private bool missingThirdPersonControllerLogged;
        private bool missingAimCameraLogged;
        private bool missingBodyRootLogged;

        private void Awake()
        {
            CacheReferences();
        }

        private void LateUpdate()
        {
            if (!HasRequiredReferences())
                return;

            UpdateAimPoint();

            if (IsAimState(thirdPersonController.CurrentActionState))
                RotateBodyToAimPoint();
        }

        private void CacheReferences()
        {
            if (thirdPersonController == null)
                TryGetComponent(out thirdPersonController);

            if (aimCamera == null)
                aimCamera = Camera.main;

            if (bodyRoot == null)
                bodyRoot = transform;
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

            if (aimCamera == null)
            {
                LogMissingReference(nameof(aimCamera), ref missingAimCameraLogged,
                    "화면 중앙 Ray를 계산할 Camera를 연결하거나 MainCamera 태그를 설정해주세요.");
                hasReferences = false;
            }

            if (bodyRoot == null)
            {
                LogMissingReference(nameof(bodyRoot), ref missingBodyRootLogged,
                    "회전시킬 플레이어 몸체 Transform을 연결해주세요.");
                hasReferences = false;
            }

            return hasReferences;
        }

        private void UpdateAimPoint()
        {
            Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(aimRay, out RaycastHit hit, aimRange, aimMask, triggerInteraction))
                CurrentAimPoint = hit.point;
            else
                CurrentAimPoint = aimRay.origin + aimRay.direction * aimRange;

            CurrentAimDirection = (CurrentAimPoint - bodyRoot.position).normalized;

            if (aimTarget != null)
                aimTarget.position = CurrentAimPoint;

            if (drawDebugRay)
                Debug.DrawLine(aimRay.origin, CurrentAimPoint, Color.cyan);
        }

        private void RotateBodyToAimPoint()
        {
            if (!rotateBody)
                return;

            Vector3 lookDirection = CurrentAimPoint - bodyRoot.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude < minAimDirectionDistance * minAimDirectionDistance)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            bodyRoot.rotation = Quaternion.RotateTowards(
                bodyRoot.rotation,
                targetRotation,
                bodyRotationSpeed * Time.deltaTime);
        }

        private static bool IsAimState(PlayerActionState actionState)
        {
            return actionState == PlayerActionState.AimHold || actionState == PlayerActionState.Aiming;
        }

        private void LogMissingReference(string fieldName, ref bool alreadyLogged, string message)
        {
            if (alreadyLogged)
                return;

            Debug.LogError($"[PlayerAimController] {fieldName}이 null입니다. {message}", this);
            alreadyLogged = true;
        }

        private void OnValidate()
        {
            aimRange = Mathf.Max(aimRange, 0.01f);
            bodyRotationSpeed = Mathf.Max(bodyRotationSpeed, 0f);
            minAimDirectionDistance = Mathf.Max(minAimDirectionDistance, 0f);
        }
    }
}
