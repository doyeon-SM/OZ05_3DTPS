using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// StageButton에 부착 — 마우스 호버 시 흰색 테두리 Outline을 표시합니다.
/// 
/// [세팅 방법]
///  1. 각 StageButton 오브젝트에 이 컴포넌트를 추가합니다.
///  2. Outline 컴포넌트도 같은 오브젝트에 추가합니다.
///  3. Inspector에서 Outline Color, Thickness를 원하는 값으로 설정합니다.
/// </summary>
[RequireComponent(typeof(Outline))]
public class StageButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("테두리 설정")]
    [SerializeField] private Color hoverColor     = Color.white;
    [SerializeField] private Vector2 hoverThickness = new Vector2(3f, -3f);

    private Outline _outline;

    private void Awake()
    {
        _outline = GetComponent<Outline>();

        // 시작 시 테두리 비활성화
        _outline.enabled = false;
        _outline.effectColor     = hoverColor;
        _outline.effectDistance  = hoverThickness;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _outline.enabled = false;
    }
}
