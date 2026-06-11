using UnityEngine;

/// <summary>
/// IInteraction 구현 예시 — Interactable 레이어 오브젝트에 부착해서 사용
/// </summary>
public class SampleInteractableObject : MonoBehaviour, IInteraction
{
    [Header("상호작용 UI")]
    [SerializeField] private string _interactionLabel = "[E] 상호작용";

    [SerializeField] private string _interactMessage = "오브젝트와 상호작용했습니다!";

    // IInteraction
    public string InteractionLabel => _interactionLabel;

    public void Interaction()
    {
        Debug.Log($"[{gameObject.name}] {_interactMessage}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
