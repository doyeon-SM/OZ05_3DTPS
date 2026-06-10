using UnityEngine;

/// <summary>
/// Interaction() 호출 시 StageSelectUI를 열어 씬 이동 흐름을 시작한다.
/// 실제 씬 이동은 StageSelectUI → [이동] 버튼에서 처리한다.
/// </summary>
public class SceneMoveInteractionObject : MonoBehaviour, IInteraction
{
    [SerializeField] private StageSelectUI stageSelectUI;

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
