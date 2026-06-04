using UnityEngine;

namespace _00.ChoiHeesu._04.StateChangeScript
{
    [DefaultExecutionOrder(150)]
    public class RigAimTargetController : MonoBehaviour
    {
        private const string HeadAimTargetPath = "AimTargets/HeadAimTarget";
        private const string ChestAimTargetPath = "AimTargets/ChestAimTarget";
        private const string ChestReferencePath = "Bip001/Bip001 Pelvis/Bip001 Spine";
        private const string LegacyChestReferencePath = "VisualSet/Skeleton/Bip001/Bip001 Pelvis/Bip001 Spine";

        [Header("References")]
        [SerializeField] private PlayerAimController playerAimController;
        [SerializeField] private Transform headAimTarget;
        [SerializeField] private Transform chestAimTarget;
        [SerializeField] private Transform chestReference;

        [Header("Head Target")]
        [SerializeField] private bool updateHeadTarget = true;

        [Header("Chest Target")]
        [SerializeField] private bool updateChestTarget = true;
        [SerializeField] private bool chestUsesHorizontalDirection = true;
        [SerializeField] private float chestTargetDistance = 20f;
        [SerializeField] private float chestHeightOffset;

        [Header("Smoothing")]
        [SerializeField] private float targetMoveSpeed;

        private bool missingAimControllerLogged;
        private bool missingTargetLogged;
        private bool sharedTargetLogged;

        private void Awake()
        {
            CacheReferences();
        }

        private void LateUpdate()
        {
            CacheReferences();

            if (!HasRequiredReferences())
                return;

            Vector3 aimPoint = playerAimController.CurrentAimPoint;

            if (updateChestTarget && chestAimTarget != null)
                MoveTarget(chestAimTarget, CalculateChestTargetPosition(aimPoint));

            if (updateHeadTarget && headAimTarget != null)
                MoveTarget(headAimTarget, aimPoint);
        }

        private void CacheReferences()
        {
            Transform playerRoot = ResolvePlayerRoot();

            if (playerAimController == null)
                playerAimController = FindPlayerAimController(playerRoot);

            if (playerRoot == null)
                playerRoot = ResolvePlayerRoot();

            if (headAimTarget == null)
                headAimTarget = FindChildByPath(playerRoot, HeadAimTargetPath);

            if (chestAimTarget == null)
                chestAimTarget = FindChildByPath(playerRoot, ChestAimTargetPath);

            if (chestReference == null)
                chestReference = FindChildByPath(playerRoot, ChestReferencePath);

            if (chestReference == null)
                chestReference = FindChildByPath(playerRoot, LegacyChestReferencePath);

            if (chestReference == null)
                chestReference = playerRoot != null ? playerRoot : transform;
        }

        private PlayerAimController FindPlayerAimController(Transform playerRoot)
        {
            if (TryGetComponent(out PlayerAimController controller))
                return controller;

            controller = GetComponentInParent<PlayerAimController>();
            if (controller != null)
                return controller;

            if (playerRoot == null)
                return GetComponentInChildren<PlayerAimController>(true);

            controller = playerRoot.GetComponent<PlayerAimController>();
            return controller != null ? controller : playerRoot.GetComponentInChildren<PlayerAimController>(true);
        }

        private Transform ResolvePlayerRoot()
        {
            if (playerAimController != null)
                return playerAimController.transform;

            PlayerAimController parentAimController = GetComponentInParent<PlayerAimController>();
            if (parentAimController != null)
                return parentAimController.transform;

            PlayerAimController childAimController = GetComponentInChildren<PlayerAimController>(true);
            return childAimController != null ? childAimController.transform : null;
        }

        private Transform FindChildByPath(Transform root, string path)
        {
            return root != null ? root.Find(path) : null;
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = true;

            if (playerAimController == null)
            {
                LogOnce(ref missingAimControllerLogged,
                    "[RigAimTargetController] Player_Soldier 계층에서 PlayerAimController를 찾을 수 없습니다.");
                hasReferences = false;
            }

            if ((updateHeadTarget && headAimTarget == null) &&
                (updateChestTarget && chestAimTarget == null))
            {
                LogOnce(ref missingTargetLogged,
                    "[RigAimTargetController] AimTargets/HeadAimTarget 또는 AimTargets/ChestAimTarget을 찾을 수 없습니다.");
                hasReferences = false;
            }

            if (headAimTarget != null && chestAimTarget != null && headAimTarget == chestAimTarget)
            {
                LogOnce(ref sharedTargetLogged,
                    "[RigAimTargetController] Head와 Chest가 같은 Target을 사용 중입니다. 머리 보정까지 필요하면 Target을 분리하세요.");
            }

            return hasReferences;
        }

        private Vector3 CalculateChestTargetPosition(Vector3 aimPoint)
        {
            if (!chestUsesHorizontalDirection)
                return aimPoint;

            Vector3 origin = chestReference != null ? chestReference.position : transform.position;
            Vector3 direction = aimPoint - origin;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.forward;

            return origin
                + Vector3.up * chestHeightOffset
                + direction.normalized * chestTargetDistance;
        }

        private void MoveTarget(Transform target, Vector3 targetPosition)
        {
            if (targetMoveSpeed <= 0f)
            {
                target.position = targetPosition;
                return;
            }

            target.position = Vector3.MoveTowards(
                target.position,
                targetPosition,
                targetMoveSpeed * Time.deltaTime);
        }

        private void LogOnce(ref bool alreadyLogged, string message)
        {
            if (alreadyLogged)
                return;

            Debug.LogError(message, this);
            alreadyLogged = true;
        }

        private void OnValidate()
        {
            chestTargetDistance = Mathf.Max(chestTargetDistance, 0.01f);
            targetMoveSpeed = Mathf.Max(targetMoveSpeed, 0f);
        }
    }
}
