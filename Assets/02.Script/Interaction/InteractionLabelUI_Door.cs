using UnityEngine;
using TMPro;

/// <summary>
/// 문 전용 상호작용 레이블 UI.
/// - 앞면(_labelText)과 뒷면(_labelText2) 두 개의 텍스트를 동시에 표시합니다.
/// - 각 텍스트는 독립적으로 Y축 빌보드 처리됩니다.
///   (앞면은 부모의 transform이 처리, 뒷면은 _labelText2의 부모 transform이 처리)
/// </summary>
public class InteractionLabelUI_Door : InteractionLabelUI
{
    [Header("뒷면 레이블 (Door)")]
    [Tooltip("문 반대편에 배치할 두 번째 텍스트 오브젝트입니다.")]
    [SerializeField] private TextMeshProUGUI _labelText2;

    [Tooltip("_labelText2가 속한 빌보드 Transform. 없으면 _labelText2의 부모를 사용합니다.")]
    [SerializeField] private Transform _backFacePivot;

    protected override void Awake()
    {
        base.Awake();

        // _backFacePivot 미지정 시 _labelText2의 부모로 자동 설정
        if (_backFacePivot == null && _labelText2 != null)
            _backFacePivot = _labelText2.transform.parent;
    }

    protected override void LateUpdate()
    {
        // 앞면 빌보드는 부모가 처리
        base.LateUpdate();

        // 뒷면 빌보드: 카메라 수평 방향으로 독립 회전
        if (_cameraTransform == null || _backFacePivot == null) return;

        Vector3 dir = _cameraTransform.position - _backFacePivot.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _backFacePivot.rotation = Quaternion.LookRotation(-dir);
    }

    /// <summary>
    /// 앞면과 뒷면 텍스트를 동시에 설정하고 UI를 표시합니다.
    /// </summary>
    public override void Show(string label)
    {
        base.Show(label);

        if (_labelText2 != null)
            _labelText2.text = label;
    }

    public override void Hide()
    {
        base.Hide();
    }
}
