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

            if (entryDoor == null) Debug.Log("EntryDoor Null");
            if (exitDoor == null) Debug.Log("ExitDoor Null");

            if (entryDoor != null)
            {
                entryDoorObject = entryDoor.GetComponent<Door>();
                entryDoorObject.Interaction();
                entryDoorObject.SetDoorActive(true);
            }
            if (exitDoor != null)
            {
                exitDoorObject = exitDoor.GetComponent<Door>();
                exitDoorObject.SetDoorActive(false);
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

            if (exitDoor != null && entryDoor != null)
            {
                entryDoorObject.SetDoorActive(true);
                exitDoorObject.SetDoorActive(true);
                exitDoorObject.Interaction();
            }
            else
            {
                entryDoorObject.SetDoorActive(true);
                entryDoorObject.Interaction();
            }

            Debug.Log($"[{gameObject.name}] 섹터 클리어!");
            OnSectorCleared();

            // 클리어 이벤트 발행
            OnCleared?.Invoke();
        }

        protected virtual void OnSectorCleared() { }

        private void OnTriggerEnter(Collider other)
        {
            if (isCleared || isBattleActive) return;
            if (!other.CompareTag("Player")) return;

            isBattleActive = true;

            if (entryDoor != null)
            {
                entryDoorObject.Interaction();
                entryDoorObject.SetDoorActive(false);
            }

            Debug.Log($"[{gameObject.name}] 플레이어 진입 — 전투 시작");
            StartBattle();
        }
    }
}
