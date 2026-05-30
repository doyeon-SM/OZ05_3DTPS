using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 점령 섹터: 정해진 시간 동안 점령 구역에서 버티면 클리어.
    /// 처치된 적은 일정 시간 후 섹터 내 랜덤 위치에 재소환된다.
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
        private bool isOccupying = false;

        // 재소환 대기 중인 코루틴을 적별로 관리
        private Dictionary<EnemyStatus, Coroutine> respawnCoroutines = new();

        protected override void OnSectorStart()
        {
            if (sectorData == null)
            {
                Debug.LogWarning($"[{gameObject.name}] OccupySectorData가 설정되지 않았습니다.");
                return;
            }

            // 씬 시작 시 필요한 적 풀 미리 생성
            foreach (var entry in sectorData.enemyEntries)
            {
                if (entry.enemyData != null)
                    EnemyPoolManager.Instance.PrewarmPool(entry.enemyData, entry.count);
            }
        }

        protected override void StartBattle()
        {
            if (sectorData == null) return;

            remainingTime = sectorData.occupyDuration;
            isOccupying = true;

            // 초기 소환
            foreach (var entry in sectorData.enemyEntries)
            {
                for (int i = 0; i < entry.count; i++)
                    SpawnEnemy(entry.enemyData);
            }

            if (timerUI != null) timerUI.Show();
            StartCoroutine(OccupyTimerCoroutine());
            Debug.Log($"[{sectorData.sectorName}] 점령 시작 — {sectorData.occupyDuration}초 버티기");
        }

        private void SpawnEnemy(EnemyData data)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            EnemyStatus enemy = EnemyPoolManager.Instance.Spawn(data, spawnPos, Quaternion.identity);

            if (enemy == null) return;

            activeEnemies.Add(enemy);
            enemy.OnDied += OnEnemyDied;
        }

        private void OnEnemyDied(EnemyStatus enemy)
        {
            enemy.OnDied -= OnEnemyDied;
            activeEnemies.Remove(enemy);

            if (!isOccupying) return;

            // 점령 섹터: 처치 후 개별 딜레이를 가지고 재소환
            EnemyData data = enemy.Data;
            EnemyPoolManager.Instance.ReturnToPool(enemy);

            if (data != null)
            {
                Coroutine co = StartCoroutine(RespawnAfterDelay(data, data.respawnDelay));
                // 같은 enemy 인스턴스가 재사용될 수 있으므로 data 기준으로 추적 불가,
                // 코루틴은 섹터 클리어 시 StopAllCoroutines로 정리
            }
        }

        private IEnumerator RespawnAfterDelay(EnemyData data, float delay)
        {
            yield return new WaitForSeconds(delay);

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

            // 시간 만료 = 점령 성공
            isOccupying = false;
            StopAllCoroutines();
            ClearSector();
        }

        protected override void OnSectorCleared()
        {
            isOccupying = false;
            if (timerUI != null) { timerUI.OnOccupySuccess(); timerUI.Hide(); }
            Debug.Log($"[{sectorData.sectorName}] 점령 성공!");
        }

        /// <summary>
        /// 점령 구역 내 랜덤 위치를 반환한다. (excludeRadius 범위 제외)
        /// </summary>
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
