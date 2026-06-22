using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using _01.Scenes.PhaseValidation;

/// <summary>
/// StageUI 전체를 관리하는 컴포넌트.
/// - GoalUI/Panel/Grid 아래에 P_Goal 프리팹 슬롯 생성
/// - 최대 maxVisibleGoals개만 표시, 초과분은 큐에 보관
/// - 슬롯 완료 시 큐에서 다음 목표를 꺼내 표시, moreIndicator로 잔여 여부 안내
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

    [Header("목표 표시 설정")]
    [Tooltip("동시에 표시할 최대 슬롯 수")]
    [SerializeField] private int maxVisibleGoals = 4;

    [Tooltip("대기 중인 목표가 있을 때 표시할 '...' 오브젝트 (RectMask2D 패널 외부에 배치 권장)")]
    [SerializeField] private GameObject moreIndicator;

    [Header("보스 안내 (목표 100% 달성 시 표시)")]
    [Tooltip("100% 달성 시 안내 문구를 표시할 텍스트")]
    [SerializeField] private TextMeshProUGUI bossAnnounceText;

    [Tooltip("Inspector에서 자유롭게 설정하는 안내 문구 (예: 보스가 등장합니다!)")]
    [SerializeField] private string bossAnnounceMessage = "보스가 등장합니다!";

    [Tooltip("안내 문구를 감싸는 패널(선택). 비워두면 텍스트만 갱신한다.")]
    [SerializeField] private GameObject bossAnnouncePanel;

    private bool _hasShownBossAnnounce;
    private int  _visibleCount;

    // 화면에 아직 표시되지 않은 목표들의 초기화 액션 큐
    private readonly Queue<Action<GoalSlot>> _pendingGoals = new();

    private void Start()
    {
        StageManager stage = StageManager.Instance;
        if (stage == null)
        {
            Debug.LogWarning("[StageUIManager] StageManager.Instance가 null입니다.");
            return;
        }

        stage.OnGoalUpdated += UpdatePercentText;
        UpdatePercentText(stage.GoalPercent * 100f);
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

    private void ShowBossAnnounce()
    {
        _hasShownBossAnnounce = true;
        if (bossAnnouncePanel != null) bossAnnouncePanel.SetActive(true);
        if (bossAnnounceText  != null) bossAnnounceText.text = bossAnnounceMessage;
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

        _pendingGoals.Clear();
        _visibleCount = 0;

        // 전체 목표를 큐에 적재
        foreach (var sector in stage.GetSectorList())
        {
            if (sector == null || sector.IsCleared) continue;
            var captured = sector;
            _pendingGoals.Enqueue(slot => slot.Initialize(captured));
        }

        foreach (var repair in stage.GetRepairList())
        {
            if (repair == null || repair.IsRepaired) continue;
            var captured = repair;
            _pendingGoals.Enqueue(slot => slot.Initialize(captured));
        }

        // 첫 maxVisibleGoals개만 표시
        int toSpawn = Mathf.Min(maxVisibleGoals, _pendingGoals.Count);
        for (int i = 0; i < toSpawn; i++)
            SpawnNextSlot();

        RefreshMoreIndicator();

        if (bossAnnounceText != null)
            bossAnnounceText.text = "";
    }

    /// <summary>큐에서 목표를 하나 꺼내 슬롯을 생성한다.</summary>
    private void SpawnNextSlot()
    {
        if (_pendingGoals.Count == 0) return;

        Action<GoalSlot> initAction = _pendingGoals.Dequeue();
        GameObject obj  = Instantiate(goalSlotPrefab, goalGrid);
        GoalSlot   slot = obj.GetComponent<GoalSlot>();
        if (slot == null) return;

        initAction(slot);
        slot.SetOnCompleted(OnSlotCompleted);
        _visibleCount++;
    }

    /// <summary>GoalSlot이 완료됐을 때 호출된다.</summary>
    private void OnSlotCompleted()
    {
        _visibleCount--;

        // 대기 중인 목표가 있으면 다음 것을 표시
        if (_pendingGoals.Count > 0)
            SpawnNextSlot();

        RefreshMoreIndicator();
    }

    /// <summary>대기 목표 유무에 따라 '...' 오브젝트를 켜고 끈다.</summary>
    private void RefreshMoreIndicator()
    {
        if (moreIndicator != null)
            moreIndicator.SetActive(_pendingGoals.Count > 0);
    }
}
