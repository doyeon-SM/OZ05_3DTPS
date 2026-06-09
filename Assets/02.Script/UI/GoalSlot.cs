using System;
using TMPro;
using UnityEngine;

/// <summary>
/// P_Goal 프리팹에 붙는 컴포넌트.
/// 목표 이름을 표시하고, 완료 이벤트를 수신하면 Grid에서 자신을 제거한다.
/// </summary>
public class GoalSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goalNameText;

    private Action _unsubscribe;

    /// <summary>
    /// SectorBase 목표 슬롯 초기화
    /// </summary>
    public void Initialize(_01.Scenes.PhaseValidation.SectorBase sector)
    {
        if (goalNameText != null)
            goalNameText.text = sector.SectorName;

        sector.OnCleared += OnGoalCompleted;
        _unsubscribe = () => sector.OnCleared -= OnGoalCompleted;
    }

    /// <summary>
    /// RepairObject 목표 슬롯 초기화
    /// </summary>
    public void Initialize(RepairObject repair)
    {
        if (goalNameText != null)
            goalNameText.text = repair.RepairName;

        repair.OnRepaired += OnGoalCompleted;
        _unsubscribe = () => repair.OnRepaired -= OnGoalCompleted;
    }

    private void OnGoalCompleted()
    {
        _unsubscribe?.Invoke();
        _unsubscribe = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _unsubscribe?.Invoke();
    }
}
