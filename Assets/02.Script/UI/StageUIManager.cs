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

    [Header("보스 안내 (목표 100% 달성 시 표시)")]
    [Tooltip("100% 달성 시 안내 문구를 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI bossAnnounceText;

    [Tooltip("Inspector에서 자유롭게 설정하는 안내 문구 (예: 보스가 등장합니다!)")]
    [SerializeField] private string bossAnnounceMessage = "보스가 등장합니다!";

    [Tooltip("안내 문구를 감싸는 패널(선택). 비워두면 텍스트만 갱신한다.")]
    [SerializeField] private GameObject bossAnnouncePanel;

    private bool _hasShownBossAnnounce;

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

        if (percent >= 100f && !_hasShownBossAnnounce)
            ShowBossAnnounce();
    }

    /// <summary>목표 100% 달성 시 1회 호출 — Inspector에 설정된 안내 문구를 표시한다.</summary>
    private void ShowBossAnnounce()
    {
        _hasShownBossAnnounce = true;

        if (bossAnnouncePanel != null)
            bossAnnouncePanel.SetActive(true);

        if (bossAnnounceText != null)
            bossAnnounceText.text = bossAnnounceMessage;
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
