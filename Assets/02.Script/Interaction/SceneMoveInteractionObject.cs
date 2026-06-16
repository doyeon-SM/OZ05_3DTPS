using UnityEngine;

/// <summary>
/// Interaction() 호출 시 StageSelectUI를 열어 씬 이동 흐름을 시작한다.
/// </summary>
public class SceneMoveInteractionObject : MonoBehaviour, IInteraction
{
    [Header("상호작용 UI")]
    [SerializeField] private string _interactionLabel = "[E] 스테이지 선택";

    [SerializeField] private StageSelectUI stageSelectUI;

    // IInteraction
    public string InteractionLabel => _interactionLabel;

    public void Interaction()
    {
        if (stageSelectUI == null)
        {
            Debug.LogError("[SceneMoveInteractionObject] stageSelectUI가 연결되지 않았습니다.");
            return;
        }
        stageSelectUI.OpenStageList();
    }
}
