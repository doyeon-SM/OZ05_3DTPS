using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 점령 섹터: 정해진 시간 동안 점령 구역에서 버티면 소환이 중단되고,
    /// 이후 남은 적을 모두 처치하면 클리어.
    ///
    /// [풀 사용 흐름]
    ///   PrewarmPool  → 비활성 상태로 풀에 대기
    ///   SpawnEnemy   → 풀에서 꺼내 활성화 (Spawn)
    ///   OnEnemyDied  → 풀에 반환 (ReturnToPool) 후 재소환 또는 전멸 확인
    ///   ClearSector  → SectorBase가 activeEnemies 잔여분 일괄 반환
    /// </summary>
    public class OccupySector : SectorBase
    {
        [Header("점령 섹터 데이터")]
        [SerializeField] private OccupySectorData sectorData;

        [Header("점령 구역")]
        [Tooltip("점령 구역 중심 Transform (이 오브젝트 범위 내에서 소환)")]
        [SerializeField] private Transform occupyZoneCenter;

        [Tooltip("점령 구역 반지름")]
        [SerializeField] private float occupyZoneRadius = 10f;

        [Tooltip("점령 구역에서 소환 제외할 반지름 (점령 지점 주변)")]
        [SerializeField] private float excludeRadius = 2f;

        [Header("UI 연동 (선택)")]
        [SerializeField] private OccupyTimerUI timerUI;

        private float remainingTime;

        /// <summary>타이머가 진행 중인 동안 true. false가 되면 재소환을 중단한다.</summary>
        private bool isOccupying = false;

        /// <summary>타이머가 만료된 뒤 적 전멸 대기 단계에 진입했는지 여부.</summary>
        private bool isWaitingForClear = false;

        protected override void OnSectorStart()
        {
            if (sectorData == null)
            {
                Debug.LogWarning($"[{gameObject.name}] OccupySectorData가 설정되지 않았습니다.");
                return;
            }

            // 적을 씬 시작 시점에 미리 소환해두고(화면에 보이는 상태), AI는 꺼둔다(추적 off).
            // 점령 구역에 입장해 StartBattle()이 호출되기 전까지는 isOccupying이 false이므로,
            // 이 상태에서 적이 죽어도 OnEnemyDied()의 재소환 로직이 동작하지 않는다.
            foreach (var entry in sectorData.enemyEntries)
            {
                for (int i = 0; i < entry.count; i++)
                    SpawnEnemy(entry.enemyData, activateAI: false);
            }
        }

        protected override void StartBattle()
        {
            if (sectorData == null) return;

            remainingTime = sectorData.occupyDuration;
            isOccupying = true;
            isWaitingForClear = false;

            // 이미 미리 소환되어 대기 중인 적들의 AI를 켠다(점령 시작과 동시에 추적 시작).
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                    enemy.SetAIActive(true);
            }

            if (timerUI != null) timerUI.Show();
            StartCoroutine(OccupyTimerCoroutine());
            Debug.Log($"[{sectorData.sectorName}] 점령 시작 — {sectorData.occupyDuration}초 버티기");
        }

        /// <summary>풀에서 꺼내 activeEnemies에 등록한다. activateAI가 false면 추적을 끈 채로 소환한다.</summary>
        private void SpawnEnemy(EnemyData data, bool activateAI = true)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            EnemyStatus enemy = EnemyPoolManager.Instance.Spawn(data, spawnPos, Quaternion.identity);

            if (enemy == null) return;

            activeEnemies.Add(enemy);
            enemy.OnDied += OnEnemyDied;
            enemy.SetAIActive(activateAI);
        }

        private void OnEnemyDied(EnemyStatus enemy)
        {
            // 이벤트 구독 해제 & activeEnemies 제거
            enemy.OnDied -= OnEnemyDied;
            activeEnemies.Remove(enemy);

            // [풀 반환] 사망한 적을 즉시 풀에 반환 (비활성화)
            EnemyData data = enemy.Data;
            EnemyPoolManager.Instance.ReturnToPool(enemy);

            if (isOccupying)
            {
                // ── 타이머 진행 중: 딜레이 후 재소환 ──
                if (data != null)
                    StartCoroutine(RespawnAfterDelay(data, data.respawnDelay));
            }
            else if (isWaitingForClear)
            {
                // ── 타이머 만료 후: 재소환 없이 전멸 확인 ──
                Debug.Log($"[{sectorData.sectorName}] 잔여 적 {activeEnemies.Count}마리");

                if (activeEnemies.Count == 0)
                    ClearSector();
            }
        }

        private IEnumerator RespawnAfterDelay(EnemyData data, float delay)
        {
            yield return new WaitForSeconds(delay);

            // 대기 중 타이머가 만료됐으면 소환하지 않고 종료
            // (이미 ReturnToPool 완료 상태이므로 추가 처리 불필요)
            if (!isOccupying) yield break;

            SpawnEnemy(data);
        }

        private IEnumerator OccupyTimerCoroutine()
        {
            while (remainingTime > 0f)
            {
                yield return null;
                remainingTime -= Time.deltaTime;

                if (timerUI != null)
                    timerUI.UpdateTimer(remainingTime, sectorData.occupyDuration);
            }

            // ── 타이머 만료: 소환 중단, 전멸 대기 단계로 전환 ──
            isOccupying = false;
            isWaitingForClear = true;

            if (timerUI != null) timerUI.OnOccupySuccess();
            Debug.Log($"[{sectorData.sectorName}] 점령 시간 종료! 남은 적 {activeEnemies.Count}마리를 처치하면 클리어.");

            // 타이머 만료 시점에 이미 적이 없는 경우 즉시 클리어
            if (activeEnemies.Count == 0)
                ClearSector();
        }

        protected override void OnSectorCleared()
        {
            isOccupying = false;
            isWaitingForClear = false;
            StopAllCoroutines();
            // 잔여 적 풀 반환은 SectorBase.ClearSector()가 처리
            if (timerUI != null) timerUI.Hide();
            Debug.Log($"[{sectorData.sectorName}] 클리어!");
        }

        /// <summary>점령 구역 내 랜덤 위치를 반환한다. (excludeRadius 범위 제외)</summary>
        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = occupyZoneCenter != null
                ? occupyZoneCenter.position
                : transform.position;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector2 rand = Random.insideUnitCircle * occupyZoneRadius;
                Vector3 candidate = center + new Vector3(rand.x, 0f, rand.y);

                if (Vector3.Distance(candidate, center) >= excludeRadius)
                    return candidate;
            }

            // fallback: 최대 반지름 방향
            Vector2 fallback = Random.onUnitSphere * occupyZoneRadius;
            return center + new Vector3(fallback.x, 0f, fallback.y);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = occupyZoneCenter != null ? occupyZoneCenter.position : transform.position;
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(center, occupyZoneRadius);
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(center, excludeRadius);
        }
    }
}
