using UnityEngine;

public class Door : MonoBehaviour, IInteraction
{
    // isDoorOpen: true = open / false = close
    private bool _isDoorOpen;

    // SetActive(false)를 쓰면 Collider까지 꺼져 Raycast가 통과되어 닫기가 불가능해집니다.
    // 해결책: MeshRenderer만 끄고 Collider는 그대로 유지합니다.
    // → 문이 열려도 Collider가 살아있어 Raycast가 감지 → 다시 [E] 눌러 닫기 가능
    [SerializeField] private GameObject doorObject;
    [SerializeField] private GameObject leftDoorObject;
    [SerializeField] private GameObject rightDoorObject;

    public bool IsDoorOpen => _isDoorOpen;


    // IInteraction 구현
    // InteractionController가 [E]키 입력 시 호출
    public void Interaction()
    {
        Debug.Log("[Door] 상호작용 실행 - 현재 상태: " + (_isDoorOpen ? "열림" : "닫힘"));
        if (!_isDoorOpen) Open();
        else              Close();
    }

    // 문 열기: MeshRenderer만 끔, Collider는 유지
    private void Open()
    {
        doorObject.SetActive(false);
        _isDoorOpen = true;
        Debug.Log("[Door] 열림 (MeshRenderer OFF / Collider ON)");
    }

    // 문 닫기: MeshRenderer 다시 켬
    private void Close()
    {
        doorObject.SetActive(true);
        _isDoorOpen = false;
        Debug.Log("[Door] 닫힘 (MeshRenderer ON)");
    }

    private void OnDrawGizmosSelected()
    {
        // Scene 뷰에서 문 상태 시각화 (열림=초록, 닫힘=빨강)
        Gizmos.color = _isDoorOpen
            ? new Color(0f, 1f, 0f, 0.25f)
            : new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
