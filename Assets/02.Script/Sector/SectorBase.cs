using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 전투/점령 섹터의 공통 기반 클래스.
    /// - 플레이어 진입 감지 (OnTriggerEnter)
    /// - 문(입장문/퇴장문) 관리
    /// - 섹터 클리어 처리
    ///
    /// [문 동작 설계]
    /// 시작    : entryDoor 비활성화(통과 가능), exitDoor 활성화(막혀있음)
    /// 진입 시 : entryDoor 활성화(퇴로 차단)
    /// 클리어  : exitDoor 비활성화(다음 구역 개방), entryDoor는 그대로(열린 채 유지)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class SectorBase : MonoBehaviour
    {
        [Header("섹터 문")]
        [Tooltip("섹터 입장 시 활성화될 문 — 시작 시 비활성, 진입 시 활성화하여 퇴로 차단")]
        [SerializeField] protected GameObject entryDoor;

        [Tooltip("섹터 클리어 시 비활성화될 문 — 시작 시 활성, 클리어 시 비활성화하여 다음 구역 개방")]
        [SerializeField] protected GameObject exitDoor;

        [Header("상태")]
        [SerializeField] private bool isCleared = false;

        protected bool IsCleared => isCleared;
        protected bool isBattleActive = false;

        protected List<EnemyStatus> activeEnemies = new();

        private void Start()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            // 시작 시: 입장문 비활성화(통과 가능), 퇴장문 활성화(막혀있음)
            if (entryDoor != null) entryDoor.SetActive(false);
            if (exitDoor != null) exitDoor.SetActive(true);

            OnSectorStart();
        }

        /// <summary>씬 시작 시 풀 준비 등 초기화 (서브클래스 구현)</summary>
        protected abstract void OnSectorStart();

        /// <summary>플레이어가 섹터에 진입했을 때 전투 시작 (서브클래스 구현)</summary>
        protected abstract void StartBattle();

        /// <summary>섹터 클리어 조건 달성 시 호출 (서브클래스에서 호출)</summary>
        protected virtual void ClearSector()
        {
            if (isCleared) return;
            isCleared = true;
            isBattleActive = false;

            // 남아있는 적 모두 풀 반환
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && enemy.gameObject.activeSelf)
                    EnemyPoolManager.Instance.ReturnToPool(enemy);
            }
            activeEnemies.Clear();

            // 클리어 시: 퇴장문만 비활성화(다음 구역 개방), 입장문은 열린 상태 유지
            if (exitDoor != null) exitDoor.SetActive(false);

            Debug.Log($"[{gameObject.name}] 섹터 클리어!");
            OnSectorCleared();
        }

        /// <summary>클리어 후 추가 처리가 필요할 때 서브클래스에서 오버라이드</summary>
        protected virtual void OnSectorCleared() { }

        private void OnTriggerEnter(Collider other)
        {
            if (isCleared || isBattleActive) return;
            if (!other.CompareTag("Player")) return;

            isBattleActive = true;

            // 진입 시: 입장문 활성화(퇴로 차단)
            if (entryDoor != null) entryDoor.SetActive(true);

            Debug.Log($"[{gameObject.name}] 플레이어 진입 — 전투 시작");
            StartBattle();
        }
    }
}
