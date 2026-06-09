using UnityEngine;
using System;
using System.Collections.Generic;

namespace _01.Scenes.PhaseValidation
{
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("스테이지 목표 리스트")]
        [SerializeField] private List<SectorBase> sectorList;
        [SerializeField] private List<RepairObject> repairList;

        [Header("스테이지 공용 드랍 테이블")]
        [SerializeField] private DropTableData dropTable;

        [Header("드랍 연출")]
        [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.5f, 0f);

        // 달성도 (0.0 ~ 1.0)
        public float GoalPercent => goalPercent;
        private float goalPercent;

        // 달성도 변경 이벤트 (0~100 % float)
        public event Action<float> OnGoalUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            goalPercent = 0f;
        }

        private void Start()
        {
            // 각 목표에 완료 이벤트 구독
            foreach (var sector in sectorList)
            {
                if (sector != null)
                    sector.OnCleared += HandleGoalCompleted;
            }
            foreach (var repair in repairList)
            {
                if (repair != null)
                    repair.OnRepaired += HandleGoalCompleted;
            }

            SetGoalPercent();
        }

        private void OnDestroy()
        {
            foreach (var sector in sectorList)
                if (sector != null) sector.OnCleared -= HandleGoalCompleted;
            foreach (var repair in repairList)
                if (repair != null) repair.OnRepaired -= HandleGoalCompleted;
        }

        private void HandleGoalCompleted()
        {
            SetGoalPercent();
        }

        public void SetGoalPercent()
        {
            int total = sectorList.Count + repairList.Count;
            if (total == 0) { goalPercent = 0f; OnGoalUpdated?.Invoke(0f); return; }

            int complete = 0;
            foreach (var sector in sectorList)
                if (sector != null && sector.IsCleared) complete++;
            foreach (var repair in repairList)
                if (repair != null && repair.IsRepaired) complete++;

            goalPercent = (float)complete / total;
            OnGoalUpdated?.Invoke(goalPercent * 100f);

            Debug.Log($"[StageManager] 목표 달성도: {goalPercent * 100f:F0}% ({complete}/{total})");
        }

        public void OnEnemyDied(Vector3 deathPosition)
        {
            if (dropTable == null) { Debug.LogWarning("[StageManager] DropTableData가 설정되지 않았습니다."); return; }
            if (ItemDropPoolManager.Instance == null) { Debug.LogWarning("[StageManager] ItemDropPoolManager.Instance가 없습니다."); return; }

            var drops = dropTable.RollDrops();
            foreach (var (itemId, amount) in drops)
            {
                string displayName = itemId;
                if (ItemCatalogManager.Instance != null)
                    displayName = ItemCatalogManager.Instance.ResolveDisplayName(itemId);

                Vector3 scatter = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0f, UnityEngine.Random.Range(-0.4f, 0.4f));
                Vector3 spawnPos = deathPosition + dropOffset + scatter;
                ItemDropPoolManager.Instance.Spawn(itemId, displayName, amount, spawnPos);
            }
        }

        public List<SectorBase> GetSectorList() => sectorList;
        public List<RepairObject> GetRepairList() => repairList;
    }
}
