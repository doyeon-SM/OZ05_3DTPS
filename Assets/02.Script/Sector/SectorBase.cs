using System;
using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    [RequireComponent(typeof(Collider))]
    public abstract class SectorBase : MonoBehaviour
    {
        [Header("섹터 이름")]
        [SerializeField] protected string sectorName;

        [Header("섹터 문")]
        [SerializeField] protected GameObject entryDoor;
        [SerializeField] protected GameObject exitDoor;

        [Header("상태")]
        [SerializeField] private bool isCleared = false;

        public bool IsCleared => isCleared;
        public string SectorName => sectorName;

        // 섹터 클리어 시 발행 (StageManager, StageUIManager가 구독)
        public event Action OnCleared;

        protected bool isBattleActive = false;
        protected List<EnemyStatus> activeEnemies = new();

        private Door entryDoorObject;
        private Door exitDoorObject;

        private void Start()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            if (entryDoor != null)
            {
                entryDoorObject = entryDoor.GetComponent<Door>();
                // 입장문은 닫힌 상태로 시작한다(Door 기본값 _isDoorOpen=false).
                // 플레이어가 직접 상호작용해서 열어야 한다 — 강제로 열어두지 않는다.
                entryDoorObject?.SetDoorActive(true);
            }
            if (exitDoor != null)
            {
                exitDoorObject = exitDoor.GetComponent<Door>();
                exitDoorObject?.SetDoorActive(false);
            }

            OnSectorStart();
        }

        protected abstract void OnSectorStart();
        protected abstract void StartBattle();

        protected virtual void ClearSector()
        {
            if (isCleared) return;
            isCleared = true;
            isBattleActive = false;

            foreach (var enemy in activeEnemies)
                if (enemy != null && enemy.gameObject.activeSelf)
                    EnemyPoolManager.Instance.ReturnToPool(enemy);
            activeEnemies.Clear();

            // 문이 있는 경우에만 처리
            if (entryDoorObject != null && exitDoorObject != null)
            {
                entryDoorObject.SetDoorActive(true);
                exitDoorObject.SetDoorActive(true);
                exitDoorObject.Interaction();
            }
            else if (entryDoorObject != null)
            {
                entryDoorObject.SetDoorActive(true);
                entryDoorObject.Interaction();
            }
            // 문이 없는 섹터(보스룸 등)는 문 처리 없이 클리어

            Debug.Log($"[{gameObject.name}] 섹터 클리어!");
            OnSectorCleared();
            OnCleared?.Invoke();
        }

        protected virtual void OnSectorCleared() { }

        private void OnTriggerEnter(Collider other)
        {
            if (isCleared || isBattleActive) return;
            if (!other.CompareTag("Player")) return;

            isBattleActive = true;

            if (entryDoorObject != null)
            {
                entryDoorObject.Interaction();
                entryDoorObject.SetDoorActive(false);
            }

            Debug.Log($"[{gameObject.name}] 플레이어 진입 — 전투 시작");
            StartBattle();
        }
    }
}
