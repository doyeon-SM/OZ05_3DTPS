using StarterAssets;
using _00.ChoiHeesu._01.MoveTest.Interact;
using UnityEngine;

/// <summary>
/// 플레이어의 상호작용 시스템 컨트롤러
/// - StarterAssetsInputs.Interact (Player.Interaction / [E]키) 입력을 감지
/// - 카메라 중앙 Raycast로 Interactable 레이어 오브젝트를 탐지
/// - IInteraction 인터페이스를 구현한 오브젝트와 상호작용
/// - 감지된 오브젝트의 InteractionLabelUI를 Show/Hide 합니다.
/// </summary>
public class InteractionController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StarterAssetsInputs _input;
    [SerializeField] private UnityEngine.Camera _mainCamera;
    [SerializeField] private RaycastInteractor _raycastInteractor;
    [SerializeField] private AnimationController _animationController;


    [Header("Raycast 설정")]
    [SerializeField] private float _interactDistance = 5f;
    [SerializeField] private LayerMask _interactableLayer;

    // 현재 Raycast로 감지 중인 대상
    private IInteraction _currentTarget;

    // 현재 표시 중인 UI (이전 타겟의 UI를 닫기 위해 보관)
    private InteractionLabelUI _currentLabelUI;

    private void Awake()
    {
        if (_input == null)
            _input = GetComponent<StarterAssetsInputs>();

        if (_mainCamera == null)
            _mainCamera = UnityEngine.Camera.main;

        CacheRaycastInteractor();
        CacheAnimationController();

        if (_input == null)
            UnityEngine.Debug.LogError("[InteractionController] StarterAssetsInputs가 없습니다.", this);

        if (_mainCamera == null)
            UnityEngine.Debug.LogError("[InteractionController] Main Camera가 없습니다. MainCamera 태그를 확인해주세요.", this);
    }

    private void Update()
    {
        DetectInteractable();
        HandleInteractionInput();
    }

    /// <summary>
    /// 매 프레임 카메라 중앙에서 Raycast를 발사해 Interactable 레이어 오브젝트를 탐지합니다.
    /// 타겟이 바뀔 때만 UI를 갱신합니다.
    /// </summary>
    private void DetectInteractable()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ViewportPointToRay(new UnityEngine.Vector3(0.5f, 0.5f, 0f));

        IInteraction detected = null;

        if (UnityEngine.Physics.Raycast(ray, out UnityEngine.RaycastHit hit, _interactDistance, _interactableLayer))
        {
            detected = hit.collider.GetComponent<IInteraction>()
                    ?? hit.collider.GetComponentInParent<IInteraction>();
        }

        // 타겟이 바뀐 경우에만 UI 갱신
        if (detected != _currentTarget)
        {
            // 이전 UI 닫기
            if (_currentLabelUI != null)
            {
                _currentLabelUI.Hide();
                _currentLabelUI = null;
            }

            _currentTarget = detected;

            // 새 타겟의 UI 열기
            if (_currentTarget != null)
            {
                var mb = _currentTarget as UnityEngine.MonoBehaviour;
                if (mb != null)
                {
                    var labelUI = mb.GetComponentInChildren<InteractionLabelUI>(true);
                    if (labelUI != null)
                    {
                        labelUI.SetCamera(_mainCamera.transform);
                        labelUI.Show(_currentTarget.InteractionLabel);
                        _currentLabelUI = labelUI;
                    }
                }
            }
        }
    }

    /// <summary>
    /// StarterAssetsInputs.Interact (Player.Interaction / [E]키) 입력을 처리합니다.
    /// </summary>
    private void HandleInteractionInput()
    {
        if (_input == null) return;

        if (_input.Interact)
        {
            _input.Interact = false;

            if (_currentTarget != null)
            {
                UnityEngine.Debug.Log($"[InteractionController] 상호작용 실행: {(_currentTarget as UnityEngine.MonoBehaviour)?.gameObject.name}");
                bool shouldPlayInteractionAnimation = ShouldPlayPlayerInteractionAnimation(_currentTarget);
                _currentTarget.Interaction();

                if (shouldPlayInteractionAnimation)
                    _animationController?.SetInteractive();
            }
            else if (TryInteractWeaponItem())
            {
                _animationController?.SetPickup();
                UnityEngine.Debug.Log("[InteractionController] 무기 오브젝트 상호작용 실행");
            }
            else
            {
                UnityEngine.Debug.Log("[InteractionController] [E]키 입력 - 주변에 상호작용 가능한 오브젝트가 없습니다.");
            }
        }
    }


    private static bool ShouldPlayPlayerInteractionAnimation(IInteraction target)
    {
        return target is IPlayerInteractionAnimationTarget animationTarget &&
               animationTarget.CanPlayPlayerInteractionAnimation;
    }

    private void CacheRaycastInteractor()
    {
        if (_raycastInteractor != null)
            return;

        _raycastInteractor = GetComponentInChildren<RaycastInteractor>(true);
        if (_raycastInteractor != null)
            return;

        _raycastInteractor = GetComponentInParent<RaycastInteractor>();
        if (_raycastInteractor != null)
            return;

        if (transform.root != null)
            _raycastInteractor = transform.root.GetComponentInChildren<RaycastInteractor>(true);
    }

    private void CacheAnimationController()
    {
        if (_animationController != null)
            return;

        _animationController = GetComponent<AnimationController>();
        if (_animationController != null)
            return;

        _animationController = GetComponentInParent<AnimationController>();
        if (_animationController != null)
            return;

        if (transform.root != null)
            _animationController = transform.root.GetComponentInChildren<AnimationController>(true);
    }

    private bool TryInteractWeaponItem()
    {
        CacheRaycastInteractor();
        if (_raycastInteractor == null)
            return false;

        _raycastInteractor.RefreshCurrentTarget();
        return _raycastInteractor.CanPickupCurrentItem() &&
               _raycastInteractor.TryPickupCurrentItem();
    }
}
