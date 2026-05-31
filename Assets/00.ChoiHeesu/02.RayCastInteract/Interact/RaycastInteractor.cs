using StarterAssets;
using _00.ChoiHeesu._02.RayCastInteract;
using UnityEngine;

namespace _00.ChoiHeesu._01.MoveTest.Interact
{
    public class RaycastInteractor : MonoBehaviour
    {
        [Header("필요 Scripts")]
        [SerializeField] private AnimationController animationController;

        // UI
        [SerializeField] private WeaponData[] SelectWeaponData;
        [SerializeField] private InteractPrintUI interactPrintUI;
        [SerializeField] private SingleStringEventChannel itemIDEventChannel;

        [Header("Raycast 설정")]
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask interactableLayerMask = ~0;
        [SerializeField] private LayerMask blockInteractionLayerMask;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private bool drawDebugRay = true;
        [SerializeField] private Color debugRayColor = Color.cyan;

        private InteractableWeaponItem currentTarget;
        private bool missingCameraLogged;
        private bool missingInteractPrintUILogged;
        private bool missingItemIDEventChannelLogged;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                ReportMissingReference(nameof(mainCamera), "Raycast를 생성할 Camera가 없습니다. Main Camera 태그 또는 Inspector 연결을 확인하세요.");

            if (interactPrintUI == null)
                ReportMissingReference(nameof(interactPrintUI), "아이템 정보 UI를 출력하려면 InteractPrintUI를 Inspector에 연결해야 합니다.");

            if (itemIDEventChannel == null)
                ReportMissingReference(nameof(itemIDEventChannel), "픽업한 아이템의 WeaponId를 전달하려면 ItemIDEventChannel을 Inspector에 연결해야 합니다.");
        }

        private void ValidateRequiredReferences()
        {
            if (interactPrintUI == null)
                ReportMissingReference(nameof(interactPrintUI), "아이템 정보 UI를 출력하려면 InteractPrintUI를 Inspector에 연결해야 합니다.");

            if (itemIDEventChannel == null)
                ReportMissingReference(nameof(itemIDEventChannel), "픽업한 아이템의 WeaponId를 전달하려면 ItemIDEventChannel을 Inspector에 연결해야 합니다.");
        }

        private void Update()
        {
            ValidateRequiredReferences();
            DrawDebugRay();
            CheckWeaponItem();
        }

        private void CheckWeaponItem()
        {
            if (mainCamera == null)
            {
                ReportMissingReference(nameof(mainCamera), "Raycast를 생성할 Camera가 없습니다. Main Camera 태그 또는 Inspector 연결을 확인하세요.");
                return;
            }

            Ray ray = GetCameraCenterRay();
            int raycastLayerMask = interactableLayerMask | blockInteractionLayerMask;

            if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, raycastLayerMask, triggerInteraction))
            {
                ClearCurrentTarget();
                return;
            }

            if (IsInLayerMask(hit.collider.gameObject.layer, blockInteractionLayerMask))
            {
                ClearCurrentTarget();
                return;
            }

            if (!hit.collider.TryGetComponent(out InteractableWeaponItem weaponItem))
            {
                weaponItem = hit.collider.GetComponentInParent<InteractableWeaponItem>();
            }

            if (weaponItem == null)
            {
                ClearCurrentTarget();
                return;
            }

            if (currentTarget == weaponItem)
                return;

            currentTarget = weaponItem;
            ShowWeaponData(weaponItem);
        }

        public bool TryPickupCurrentItem()
        {
            if (!TryGetCurrentWeaponData(out WeaponData weaponData))
                return false;

            if (itemIDEventChannel == null)
            {
                ReportMissingReference(nameof(itemIDEventChannel), "아이템 픽업 ID 이벤트를 발행할 수 없습니다.");
                return false;
            }

            InteractableWeaponItem pickupTarget = currentTarget;

            itemIDEventChannel.Raise(weaponData.WeaponId);
            pickupTarget.Pickup();
            ClearCurrentTarget();

            return true;
        }

        public bool CanPickupCurrentItem()
        {
            return itemIDEventChannel != null && TryGetCurrentWeaponData(out _);
        }

        private bool TryGetCurrentWeaponData(out WeaponData weaponData)
        {
            weaponData = null;

            if (currentTarget == null)
                return false;

            weaponData = currentTarget.GetWeaponData();

            if (weaponData == null)
            {
                Debug.LogError($"[RaycastInteractor] {currentTarget.gameObject.name}의 WeaponData가 null입니다. InteractableWeaponItem의 Current Weapon Data를 연결해주세요.", currentTarget);
                return false;
            }

            if (string.IsNullOrWhiteSpace(weaponData.WeaponId))
            {
                Debug.LogError($"[RaycastInteractor] {weaponData.name}의 WeaponId가 비어 있습니다. WeaponData S.O의 WeaponId를 입력해주세요.", weaponData);
                return false;
            }

            return true;
        }

        private void DrawDebugRay()
        {
            if (!drawDebugRay || mainCamera == null)
                return;

            Ray ray = GetCameraCenterRay();
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, debugRayColor);
        }

        private Ray GetCameraCenterRay()
        {
            return mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        private void ShowWeaponData(InteractableWeaponItem weaponItem)
        {
            WeaponData weaponData = weaponItem.GetWeaponData();

            if (weaponData == null)
            {
                Debug.LogError($"[RaycastInteractor] {weaponItem.gameObject.name}의 WeaponData가 null입니다. InteractableWeaponItem의 Current Weapon Data를 연결해주세요.", weaponItem);
                ClearCurrentTarget();
                return;
            }

            if (interactPrintUI != null)
                interactPrintUI.Show(weaponData);
            else
                ReportMissingReference(nameof(interactPrintUI), "감지한 아이템 정보를 UI로 출력할 수 없습니다.");
        }

        private void ClearCurrentTarget()
        {
            currentTarget = null;

            if (interactPrintUI != null)
                interactPrintUI.Hide();
        }

        private void ReportMissingReference(string fieldName, string message)
        {
            if (fieldName == nameof(mainCamera))
            {
                if (missingCameraLogged)
                    return;

                missingCameraLogged = true;
            }
            else if (fieldName == nameof(interactPrintUI))
            {
                if (missingInteractPrintUILogged)
                    return;

                missingInteractPrintUILogged = true;
            }
            else if (fieldName == nameof(itemIDEventChannel))
            {
                if (missingItemIDEventChannelLogged)
                    return;

                missingItemIDEventChannelLogged = true;
            }

            Debug.LogError($"[RaycastInteractor] {fieldName}이 null입니다. {message}", this);
        }
    }
}
