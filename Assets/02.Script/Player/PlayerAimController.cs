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
        [SerializeField] private bool ignoreBodyRootColliders = true;
        [SerializeField] private int aimHitBufferSize = 16;

        [Header("Body Rotation")]
        [SerializeField] private bool rotateBody = true;
        [SerializeField] private bool rotateBodyByCameraForward = true;
        [SerializeField] private bool keepCameraTargetRotationStable = true;
        [SerializeField] private float bodyRotationSpeed = 720f;
        [SerializeField] private float minAimDirectionDistance = 0.1f;
        [SerializeField] private float bodyRotationDeadZone = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay;

        public Vector3 CurrentAimPoint { get; private set; }
        public Vector3 CurrentAimDirection { get; private set; }

        private bool missingThirdPersonControllerLogged;
        private bool missingAimCameraLogged;
        private bool missingBodyRootLogged;
        private RaycastHit[] aimHits = new RaycastHit[16];

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

            if (TryGetAimHit(aimRay, out RaycastHit hit))
                CurrentAimPoint = hit.point;
            else
                CurrentAimPoint = aimRay.origin + aimRay.direction * aimRange;

            CurrentAimDirection = (CurrentAimPoint - bodyRoot.position).normalized;

            if (aimTarget != null)
                aimTarget.position = CurrentAimPoint;

            if (drawDebugRay)
                Debug.DrawLine(aimRay.origin, CurrentAimPoint, Color.cyan);
        }

        private bool TryGetAimHit(Ray aimRay, out RaycastHit hit)
        {
            if (!ignoreBodyRootColliders || bodyRoot == null)
                return Physics.Raycast(aimRay, out hit, aimRange, aimMask, triggerInteraction);

            EnsureAimHitBuffer();

            int hitCount = Physics.RaycastNonAlloc(aimRay, aimHits, aimRange, aimMask, triggerInteraction);
            hit = new RaycastHit();

            bool hasHit = false;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit currentHit = aimHits[i];
                if (currentHit.collider == null || currentHit.collider.transform.IsChildOf(bodyRoot))
                    continue;

                if (currentHit.distance >= closestDistance)
                    continue;

                closestDistance = currentHit.distance;
                hit = currentHit;
                hasHit = true;
            }

            return hasHit;
        }

        private void EnsureAimHitBuffer()
        {
            int safeBufferSize = Mathf.Max(aimHitBufferSize, 1);
            if (aimHits == null || aimHits.Length != safeBufferSize)
                aimHits = new RaycastHit[safeBufferSize];
        }

        private void RotateBodyToAimPoint()
        {
            if (!rotateBody)
                return;

            if (!TryGetBodyLookDirection(out Vector3 lookDirection))
                return;

            if (IsWithinBodyRotationDeadZone(lookDirection))
                return;

            Transform cameraRotationTarget = GetCameraRotationTarget();
            Quaternion cameraTargetRotation = cameraRotationTarget != null
                ? cameraRotationTarget.rotation
                : Quaternion.identity;

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            bodyRoot.rotation = Quaternion.RotateTowards(
                bodyRoot.rotation,
                targetRotation,
                bodyRotationSpeed * Time.deltaTime);

            if (cameraRotationTarget != null)
                cameraRotationTarget.rotation = cameraTargetRotation;
        }

        private bool TryGetBodyLookDirection(out Vector3 lookDirection)
        {
            if (rotateBodyByCameraForward && aimCamera != null)
            {
                lookDirection = aimCamera.transform.forward;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude >= minAimDirectionDistance * minAimDirectionDistance)
                    return true;
            }

            lookDirection = CurrentAimPoint - bodyRoot.position;
            lookDirection.y = 0f;
            return lookDirection.sqrMagnitude >= minAimDirectionDistance * minAimDirectionDistance;
        }

        private bool IsWithinBodyRotationDeadZone(Vector3 lookDirection)
        {
            if (bodyRotationDeadZone <= 0f)
                return false;

            Vector3 currentForward = bodyRoot.forward;
            currentForward.y = 0f;

            if (currentForward.sqrMagnitude <= 0.0001f || lookDirection.sqrMagnitude <= 0.0001f)
                return false;

            float angle = Vector3.Angle(currentForward.normalized, lookDirection.normalized);
            return angle <= bodyRotationDeadZone;
        }

        private Transform GetCameraRotationTarget()
        {
            if (!keepCameraTargetRotationStable ||
                thirdPersonController == null ||
                thirdPersonController.CinemachineCameraTarget == null)
            {
                return null;
            }

            Transform cameraTarget = thirdPersonController.CinemachineCameraTarget.transform;
            if (cameraTarget == bodyRoot || !cameraTarget.IsChildOf(bodyRoot))
                return null;

            return cameraTarget;
        }

        private static bool IsAimState(PlayerActionState actionState)
        {
            return actionState == PlayerActionState.AimHold ||
                   actionState == PlayerActionState.Aiming ||
                   actionState == PlayerActionState.Normal_Fire ||
                   actionState == PlayerActionState.GrenadeRoutine;
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
            aimHitBufferSize = Mathf.Max(aimHitBufferSize, 1);
            bodyRotationSpeed = Mathf.Max(bodyRotationSpeed, 0f);
            minAimDirectionDistance = Mathf.Max(minAimDirectionDistance, 0f);
            bodyRotationDeadZone = Mathf.Max(bodyRotationDeadZone, 0f);
        }
    }
}
