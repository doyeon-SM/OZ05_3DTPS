using TMPro;
using UnityEngine;
using _01.Scenes.PhaseValidation;

/// <summary>
/// StageUI 전체를 관리하는 컴포넌트.
/// - GoalUI/Panel/Grid 아래에 P_Goal 프리팹 슬롯 생성
/// - StageManager.OnGoalUpdated 구독 → 달성도 % 텍스트 갱신
/// </summary>
public class StageUIManager : MonoBehaviour
{
    [Header("달성도 텍스트 (예: 60%)")]
    [SerializeField] private TextMeshProUGUI goalPercentText;

    [Header("목표 슬롯이 쌓일 Grid Transform")]
    [SerializeField] private Transform goalGrid;

    [Header("P_Goal 프리팹")]
    [SerializeField] private GameObject goalSlotPrefab;

    private void Start()
    {
        StageManager stage = StageManager.Instance;
        if (stage == null)
        {
            Debug.LogWarning("[StageUIManager] StageManager.Instance가 null입니다.");
            return;
        }

        // 달성도 이벤트 구독
        stage.OnGoalUpdated += UpdatePercentText;

        // 초기 달성도 표시
        UpdatePercentText(stage.GoalPercent * 100f);

        // 목표 슬롯 생성
        SpawnGoalSlots(stage);
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnGoalUpdated -= UpdatePercentText;
    }

    private void UpdatePercentText(float percent)
    {
        if (goalPercentText != null)
            goalPercentText.text = $"{Mathf.RoundToInt(percent)}%";
    }

    private void SpawnGoalSlots(StageManager stage)
    {
        if (goalSlotPrefab == null)
        {
            Debug.LogWarning("[StageUIManager] goalSlotPrefab이 없습니다.");
            return;
        }
        if (goalGrid == null)
        {
            Debug.LogWarning("[StageUIManager] goalGrid가 없습니다.");
            return;
        }

        // SectorBase 슬롯
        foreach (var sector in stage.GetSectorList())
        {
            if (sector == null) continue;
            if (sector.IsCleared) continue; // 이미 클리어된 목표는 생성 안 함

            GameObject obj = Instantiate(goalSlotPrefab, goalGrid);
            GoalSlot slot = obj.GetComponent<GoalSlot>();
            if (slot != null) slot.Initialize(sector);
        }

        // RepairObject 슬롯
        foreach (var repair in stage.GetRepairList())
        {
            if (repair == null) continue;
            if (repair.IsRepaired) continue;

            GameObject obj = Instantiate(goalSlotPrefab, goalGrid);
            GoalSlot slot = obj.GetComponent<GoalSlot>();
            if (slot != null) slot.Initialize(repair);
        }
    }
}
