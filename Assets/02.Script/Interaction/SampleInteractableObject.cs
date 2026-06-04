using UnityEngine;

/// <summary>
/// IInteraction 구현 예시 — Interactable 레이어 오브젝트에 부착해서 사용
/// 플레이어가 바라보고 [E]키를 누르면 Debug.Log가 출력됩니다.
/// </summary>
public class SampleInteractableObject : MonoBehaviour, IInteraction
{
    [SerializeField] private string _interactMessage = "오브젝트와 상호작용했습니다!";

    /// <summary>
    /// IInteraction.Interaction() 구현
    /// InteractionController가 [E]키 입력 시 이 메서드를 호출합니다.
    /// </summary>
    public void Interaction()
    {
        Debug.Log($"[{gameObject.name}] {_interactMessage}");
    }

    private void OnDrawGizmosSelected()
    {
        // Scene 뷰에서 상호작용 오브젝트 시각화
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
