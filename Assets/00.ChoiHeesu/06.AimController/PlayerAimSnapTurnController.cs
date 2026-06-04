using StarterAssets;
using UnityEngine;

namespace _00.ChoiHeesu._04.StateChangeScript
{
    [DefaultExecutionOrder(120)]
    public class PlayerAimSnapTurnController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private PlayerAimController playerAimController;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private StarterAssetsInputs input;
        [SerializeField] private AnimationController animationController;

        [Header("State")]
        [SerializeField] private bool onlyNormalState = true;
        [SerializeField] private bool requireNoMoveInput = true;
        [SerializeField] private float moveInputThreshold = 0.01f;

        [Header("Snap Turn")]
        [SerializeField] private float thresholdAngle = 45f;
        [SerializeField] private float turnAngle = 90f;
        [SerializeField] private float turnSmoothingDuration = 0.1f;
        [SerializeField] private float cooldown = 0.15f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay;

        public float CurrentSignedAimAngle { get; private set; }

        private bool isTurning;
        private float cooldownTimer;
        private float turnElapsed;
        private Quaternion turnStartRotation;
        private Quaternion turnTargetRotation;
        private bool missingThirdPersonControllerLogged;
        private bool missingAimControllerLogged;
        private bool missingBodyRootLogged;
        private bool missingAnimationControllerLogged;

        private void Awake()
        {
            CacheReferences();
        }

        private void LateUpdate()
        {
            CacheReferences();

            if (!HasRequiredReferences())
                return;

            CurrentSignedAimAngle = CalculateSignedAimAngle();

            if (drawDebugRay)
                DrawDebugDirections();

            if (isTurning)
            {
                UpdateSmoothTurn();
                return;
            }

            if (!CanRunInCurrentState())
                return;

            UpdateCooldown();

            if (cooldownTimer > 0f)
                return;

            if (Mathf.Abs(CurrentSignedAimAngle) < thresholdAngle)
                return;

            SnapTurn(CurrentSignedAimAngle > 0f ? 1f : -1f);
        }

        private void CacheReferences()
        {
            if (thirdPersonController == null)
                thirdPersonController = FindInPlayerHierarchy<ThirdPersonController>();

            if (playerAimController == null)
                playerAimController = FindInPlayerHierarchy<PlayerAimController>();

            if (input == null)
                input = FindInPlayerHierarchy<StarterAssetsInputs>();

            if (animationController == null)
                animationController = FindInPlayerHierarchy<AnimationController>();

            if (bodyRoot == null)
                bodyRoot = ResolveBodyRoot();
        }

        private T FindInPlayerHierarchy<T>() where T : Component
        {
            if (TryGetComponent(out T component))
                return component;

            component = GetComponentInParent<T>();
            if (component != null)
                return component;

            Transform playerRoot = ResolvePlayerRoot();
            if (playerRoot == null)
                return GetComponentInChildren<T>(true);

            component = playerRoot.GetComponent<T>();
            return component != null ? component : playerRoot.GetComponentInChildren<T>(true);
        }

        private Transform ResolvePlayerRoot()
        {
            if (thirdPersonController != null)
                return thirdPersonController.transform;

            if (playerAimController != null)
                return playerAimController.transform;

            if (input != null)
                return input.transform;

            ThirdPersonController parentController = GetComponentInParent<ThirdPersonController>();
            if (parentController != null)
                return parentController.transform;

            ThirdPersonController childController = GetComponentInChildren<ThirdPersonController>(true);
            return childController != null ? childController.transform : null;
        }

        private Transform ResolveBodyRoot()
        {
            Transform playerRoot = ResolvePlayerRoot();
            return playerRoot != null ? playerRoot : transform;
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = true;

            if (thirdPersonController == null)
            {
                LogOnce(ref missingThirdPersonControllerLogged,
                    "[PlayerAimSnapTurnController] Player_Soldier 계층에서 ThirdPersonController를 찾을 수 없습니다.");
                hasReferences = false;
            }

            if (playerAimController == null)
            {
                LogOnce(ref missingAimControllerLogged,
                    "[PlayerAimSnapTurnController] Player_Soldier 계층에서 PlayerAimController를 찾을 수 없습니다.");
                hasReferences = false;
            }

            if (bodyRoot == null)
            {
                LogOnce(ref missingBodyRootLogged,
                    "[PlayerAimSnapTurnController] 회전 기준이 될 bodyRoot Transform을 찾을 수 없습니다.");
                hasReferences = false;
            }

            return hasReferences;
        }

        private bool CanRunInCurrentState()
        {
            if (onlyNormalState && thirdPersonController.CurrentActionState != PlayerActionState.Normal)
                return false;

            if (!requireNoMoveInput || input == null)
                return true;

            return input.move.sqrMagnitude <= moveInputThreshold * moveInputThreshold;
        }

        private float CalculateSignedAimAngle()
        {
            Vector3 forward = bodyRoot.forward;
            forward.y = 0f;

            Vector3 aimDirection = playerAimController.CurrentAimPoint - bodyRoot.position;
            aimDirection.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f || aimDirection.sqrMagnitude <= 0.0001f)
                return 0f;

            return Vector3.SignedAngle(forward.normalized, aimDirection.normalized, Vector3.up);
        }

        private void UpdateCooldown()
        {
            if (cooldownTimer <= 0f)
                return;

            cooldownTimer = Mathf.Max(cooldownTimer - Time.deltaTime, 0f);
        }

        private void SnapTurn(float direction)
        {
            PlaySnapTurnAnimation(direction);

            if (turnSmoothingDuration <= 0f)
            {
                bodyRoot.Rotate(Vector3.up, turnAngle * direction, Space.World);
                cooldownTimer = cooldown;
                return;
            }

            isTurning = true;
            turnElapsed = 0f;
            turnStartRotation = bodyRoot.rotation;
            turnTargetRotation = Quaternion.AngleAxis(turnAngle * direction, Vector3.up) * turnStartRotation;
        }

        private void UpdateSmoothTurn()
        {
            turnElapsed += Time.deltaTime;

            float progress = turnSmoothingDuration <= 0f
                ? 1f
                : Mathf.Clamp01(turnElapsed / turnSmoothingDuration);
            bodyRoot.rotation = Quaternion.Slerp(turnStartRotation, turnTargetRotation, progress);

            if (progress < 1f)
                return;

            bodyRoot.rotation = turnTargetRotation;
            isTurning = false;
            cooldownTimer = cooldown;
        }

        private void PlaySnapTurnAnimation(float direction)
        {
            if (animationController == null)
            {
                LogWarningOnce(ref missingAnimationControllerLogged,
                    "[PlayerAimSnapTurnController] AnimationController를 찾을 수 없어 SnapTurn 애니메이션은 재생하지 않습니다.");
                return;
            }

            animationController.PlaySnapTurn(direction);
        }


        private void DrawDebugDirections()
        {
            Vector3 origin = bodyRoot.position + Vector3.up * 1.2f;
            Debug.DrawRay(origin, bodyRoot.forward * 2f, Color.blue);

            Vector3 aimDirection = playerAimController.CurrentAimPoint - bodyRoot.position;
            aimDirection.y = 0f;

            if (aimDirection.sqrMagnitude > 0.0001f)
                Debug.DrawRay(origin, aimDirection.normalized * 2f, Color.magenta);
        }

        private void LogOnce(ref bool alreadyLogged, string message)
        {
            if (alreadyLogged)
                return;

            Debug.LogError(message, this);
            alreadyLogged = true;
        }

        private void LogWarningOnce(ref bool alreadyLogged, string message)
        {
            if (alreadyLogged)
                return;

            Debug.LogWarning(message, this);
            alreadyLogged = true;
        }


        private void OnValidate()
        {
            thresholdAngle = Mathf.Clamp(thresholdAngle, 0f, 180f);
            turnAngle = Mathf.Clamp(turnAngle, 0f, 180f);
            turnSmoothingDuration = Mathf.Max(turnSmoothingDuration, 0f);
            cooldown = Mathf.Max(cooldown, 0f);
            moveInputThreshold = Mathf.Max(moveInputThreshold, 0f);
        }
    }
}
