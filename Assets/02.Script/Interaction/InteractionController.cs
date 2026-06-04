using StarterAssets;
using UnityEngine;

/// <summary>
/// 플레이어의 상호작용 시스템 컨트롤러
/// - StarterAssetsInputs.Interact (Player.Interaction / [E]키) 입력을 감지
/// - 카메라 중앙 Raycast로 Interactable 레이어 오브젝트를 탐지
/// - IInteraction 인터페이스를 구현한 오브젝트와 상호작용
/// </summary>
public class InteractionController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StarterAssetsInputs _input;
    [SerializeField] private Camera _mainCamera;

    [Header("Raycast 설정")]
    [SerializeField] private float _interactDistance = 5f;
    [SerializeField] private LayerMask _interactableLayer;

    // 현재 Raycast로 감지 중인 대상
    private IInteraction _currentTarget;

    private void Awake()
    {
        if (_input == null)
            _input = GetComponent<StarterAssetsInputs>();

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_input == null)
            Debug.LogError("[InteractionController] StarterAssetsInputs가 없습니다. 플레이어 오브젝트에 StarterAssetsInputs 컴포넌트를 추가하거나 Inspector에서 연결해주세요.", this);

        if (_mainCamera == null)
            Debug.LogError("[InteractionController] Main Camera가 없습니다. MainCamera 태그를 확인해주세요.", this);
    }

    private void Update()
    {
        DetectInteractable();
        HandleInteractionInput();
    }

    /// <summary>
    /// 매 프레임 카메라 중앙에서 Raycast를 발사해 Interactable 레이어 오브젝트를 탐지합니다.
    /// </summary>
    private void DetectInteractable()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance, _interactableLayer))
        {
            // Raycast에 맞은 오브젝트에서 IInteraction 탐색 (자신 또는 부모)
            IInteraction interactable = hit.collider.GetComponent<IInteraction>()
                ?? hit.collider.GetComponentInParent<IInteraction>();

            _currentTarget = interactable;

            //if (_currentTarget != null)
                //Debug.Log($"[InteractionController] 상호작용 가능 오브젝트 감지: {hit.collider.gameObject.name} | [E]키를 눌러 상호작용");
        }
        else
        {
            _currentTarget = null;
        }
    }

    /// <summary>
    /// StarterAssetsInputs.Interact (Player.Interaction / [E]키) 입력을 처리합니다.
    /// Interactable 레이어 오브젝트를 바라보고 있을 때만 IInteraction.Interaction()을 호출합니다.
    /// </summary>
    private void HandleInteractionInput()
    {
        if (_input == null) return;

        // StarterAssetsInputs.Interact: OnInteraction() → InteractInput() → Interact 필드
        if (_input.Interact)
        {
            // 입력 소비 (한 프레임만 처리되도록)
            _input.Interact = false;

            if (_currentTarget != null)
            {
                Debug.Log($"[InteractionController] 상호작용 실행: {(_currentTarget as MonoBehaviour)?.gameObject.name}");
                _currentTarget.Interaction();
            }
            else
            {
                Debug.Log("[InteractionController] [E]키 입력 - 주변에 상호작용 가능한 오브젝트가 없습니다.");
            }
        }
    }
}
