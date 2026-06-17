using UnityEngine;
using _01.Scenes.PhaseValidation;
using _01.Scenes.PhaseValidation.UI;

/// <summary>
/// 보스 처치 후 보스 위치 근처에 소환되는 상호작용 오브젝트.
///
/// [흐름]
///  1. BossSector.HandleBossDied()에서 보스 사망 위치(+오프셋)에 소환되며, Initialize()로 소속 섹터를 전달받음
///  2. 플레이어가 [E]키로 상호작용 → 섹터 클리어 여부 확인 후 BossClearUI.Show() 호출
///  3. 클리어 UI를 닫아도 다시 상호작용하면 재오픈 가능 (여러 번 상호작용 허용, 자가 파괴/비활성화 없음)
///
/// [방어코드]
///  정상 흐름에서는 섹터 클리어 이후에만 소환되므로 항상 IsCleared == true이지만,
///  혹시 다른 경로(예: 씬에 미리 배치)로 존재할 경우를 대비해 상호작용 시점에 한 번 더 확인한다.
///
/// [프리팹 구성 — Inspector에서 직접 준비]
///  - Layer: Interactable
///  - Collider 필요 (Raycast 감지용)
///  - 자식에 InteractionLabelUI (선택, 상호작용 라벨 표시용)
/// </summary>
public class BossExitObject : MonoBehaviour, IInteraction
{
    [Header("상호작용 UI")]
    [SerializeField] private string _interactionLabel = "[E] 클리어";

    private SectorBase _sector;

    // IInteraction
    public string InteractionLabel => _interactionLabel;

    /// <summary>BossSector에서 소환 직후 호출 — 클리어 여부 확인용 섹터 참조를 주입한다.</summary>
    public void Initialize(SectorBase sector)
    {
        _sector = sector;
    }

    public void Interaction()
    {
        // 방어코드: 섹터가 아직 클리어되지 않았다면 상호작용을 무시한다.
        if (_sector != null && !_sector.IsCleared)
        {
            Debug.LogWarning("[BossExitObject] 섹터가 아직 클리어되지 않아 상호작용이 무시됩니다.");
            return;
        }

        if (BossClearUI.Instance == null)
        {
            Debug.LogError("[BossExitObject] BossClearUI.Instance가 없습니다. 씬에 BossClearUI가 배치되어 있는지 확인해주세요.");
            return;
        }

        BossClearUI.Instance.Show();
    }
}
