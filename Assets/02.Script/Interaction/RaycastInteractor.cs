using StarterAssets;
using _00.ChoiHeesu._02.RayCastInteract;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00.ChoiHeesu._01.MoveTest.Interact
{
    public class RaycastInteractor : MonoBehaviour
    {
        private const string InteractUICanvasName = "Canvas";
        private const string InteractUIObjectName = "InteractUI";

        [Header("필요 Scripts")]
        [SerializeField] private AnimationController animationController;
        [SerializeField] private InteractPrintUI interactPrintUI;
        [Header("내부 배열 , EventChannel 설정")]
        [SerializeField] private WeaponData[] SelectWeaponData;
        [SerializeField] private SingleStringEventChannel itemIDEventChannel;
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Raycast 설정")]
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask interactableLayerMask = ~0;
        [SerializeField] private LayerMask blockInteractionLayerMask;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [Header("Debug 설정")]
        [SerializeField] private bool drawDebugRay = true;
        [SerializeField] private Color debugRayColor = Color.cyan;

        private InteractableWeaponItem currentTarget;
        private bool missingCameraLogged;
        private bool missingInteractPrintUILogged;
        private bool missingItemIDEventChannelLogged;
        private bool missingPlayerInventoryLogged;

        private void Awake()
        {
            CacheSceneStartReferences();

            if (mainCamera == null)
                ReportMissingReference(nameof(mainCamera), "Raycast를 생성할 Camera가 없습니다. Main Camera 태그 또는 Inspector 연결을 확인해주세요.");

            if (interactPrintUI == null)
                ReportMissingReference(nameof(interactPrintUI), "아이템 정보 UI를 출력하려면 InteractPrintUI를 Inspector에 연결해야 합니다.");

            if (playerInventory == null && itemIDEventChannel == null)
                ReportMissingReference(nameof(playerInventory), "무기 언락을 처리할 PlayerInventory 또는 fallback ItemIDEventChannel이 필요합니다.");
        }

        private void CacheSceneStartReferences()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (interactPrintUI == null)
                interactPrintUI = FindInteractPrintUIInLoadedScenes();

            CachePlayerInventory(false);
        }

        private InteractPrintUI FindInteractPrintUIInLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                for (int j = 0; j < rootObjects.Length; j++)
                {
                    GameObject rootObject = rootObjects[j];
                    if (rootObject == null || rootObject.name != InteractUICanvasName)
                        continue;

                    Transform interactUITransform = rootObject.transform.Find(InteractUIObjectName);
                    if (interactUITransform == null)
                        continue;

                    if (interactUITransform.TryGetComponent(out InteractPrintUI foundUI))
                        return foundUI;

                    foundUI = interactUITransform.GetComponentInChildren<InteractPrintUI>(true);
                    if (foundUI != null)
                        return foundUI;
                }
            }

            return FindFirstObjectByType<InteractPrintUI>(FindObjectsInactive.Include);
        }

        private void ValidateRequiredReferences()
        {
            if (interactPrintUI == null)
                ReportMissingReference(nameof(interactPrintUI), "아이템 정보 UI를 출력하려면 InteractPrintUI를 Inspector에 연결해야 합니다.");

            if (playerInventory == null)
                CachePlayerInventory(false);

            if (playerInventory == null && itemIDEventChannel == null)
                ReportMissingReference(nameof(playerInventory), "무기 언락을 처리할 PlayerInventory 또는 fallback ItemIDEventChannel이 필요합니다.");
        }

        private void Update()
        {
            ValidateRequiredReferences();
            DrawDebugRay();
            CheckWeaponItem();
        }

        public void RefreshCurrentTarget()
        {
            ValidateRequiredReferences();
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
            if (!TryGetCurrentWeaponData(out _))
                return false;

            InteractableWeaponItem pickupTarget = currentTarget;
            if (pickupTarget == null)
                return false;

            CachePlayerInventory(false);
            if (!pickupTarget.TryInteract(playerInventory, itemIDEventChannel))
                return false;

            ClearCurrentTarget();
            return true;
        }

        public bool CanPickupCurrentItem()
        {
            if (!TryGetCurrentWeaponData(out _))
                return false;

            CachePlayerInventory(false);
            return playerInventory != null || itemIDEventChannel != null;
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
            else if (fieldName == nameof(playerInventory))
            {
                if (missingPlayerInventoryLogged)
                    return;

                missingPlayerInventoryLogged = true;
            }

            Debug.LogError($"[RaycastInteractor] {fieldName}이 null입니다. {message}", this);
        }


        private bool CachePlayerInventory(bool logIfMissing)
        {
            if (playerInventory != null)
                return true;

            if (TryGetComponent(out playerInventory))
                return true;

            playerInventory = GetComponentInParent<PlayerInventory>();
            if (playerInventory != null)
                return true;

            playerInventory = GetComponentInChildren<PlayerInventory>(true);
            if (playerInventory != null)
                return true;

            Transform rootTransform = transform.root;
            if (rootTransform != null)
            {
                playerInventory = rootTransform.GetComponentInChildren<PlayerInventory>(true);
                if (playerInventory != null)
                    return true;
            }

            playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
            if (playerInventory != null)
            {
                missingPlayerInventoryLogged = false;
                return true;
            }

            if (logIfMissing)
                ReportMissingReference(nameof(playerInventory), "무기 언락을 처리할 PlayerInventory를 찾을 수 없습니다.");

            return false;
        }
    }
}
