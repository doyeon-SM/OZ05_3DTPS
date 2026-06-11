using UnityEngine;
using TMPro;

/// <summary>
/// IInteraction 오브젝트의 머리 위에 붙는 World Space 상호작용 레이블 UI.
/// - Y축 빌보드: 카메라 방향으로 좌우 회전만 추적
/// - InteractionController가 Show/Hide를 호출합니다.
/// - 상속 확장 가능: Show/Hide/LateUpdate를 virtual로 제공합니다.
/// </summary>
public class InteractionLabelUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] protected TextMeshProUGUI _labelText;

    [Header("빌보드 설정")]
    [Tooltip("카메라가 없으면 MainCamera 태그로 자동 탐색합니다.")]
    [SerializeField] protected Transform _cameraTransform;

    protected virtual void Awake()
    {
        if (_cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) _cameraTransform = cam.transform;
        }
        gameObject.SetActive(false);
    }

    protected virtual void LateUpdate()
    {
        if (_cameraTransform == null) return;

        // Y축 빌보드: 카메라 수평 방향만 추적
        Vector3 dir = _cameraTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(-dir);
    }

    /// <summary>
    /// 레이블 텍스트를 설정하고 UI를 표시합니다.
    /// </summary>
    public virtual void Show(string label)
    {
        if (_labelText != null)
            _labelText.text = label;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// UI를 숨깁니다.
    /// </summary>
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 카메라 참조를 갱신합니다. 씬 전환 후 InteractionController에서 호출합니다.
    /// </summary>
    public void SetCamera(Transform cameraTransform)
    {
        _cameraTransform = cameraTransform;
    }
}
