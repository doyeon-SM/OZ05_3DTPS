using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 전투 섹터: ScriptableObject에 정의된 위치에 적을 소환하고,
    /// 모두 처치하면 섹터 클리어.
    /// </summary>
    public class BattleSector : SectorBase
    {
        [Header("전투 섹터 데이터")]
        [SerializeField] private BattleSectorData sectorData;

        private int remainingEnemyCount = 0;

        protected override void OnSectorStart()
        {
            if (sectorData == null)
            {
                Debug.LogWarning($"[{gameObject.name}] BattleSectorData가 설정되지 않았습니다.");
                return;
            }

            // 씬 시작 시 필요한 적 풀 미리 생성
            foreach (var placement in sectorData.enemyPlacements)
            {
                if (placement.enemyData != null)
                    EnemyPoolManager.Instance.PrewarmPool(placement.enemyData, 1);
            }
        }

        protected override void StartBattle()
        {
            if (sectorData == null) return;

            remainingEnemyCount = sectorData.enemyPlacements.Length;

            foreach (var placement in sectorData.enemyPlacements)
            {
                if (placement.enemyData == null) continue;

                Quaternion rotation = Quaternion.Euler(placement.spawnRotation);
                EnemyStatus enemy = EnemyPoolManager.Instance.Spawn(
                    placement.enemyData,
                    placement.spawnPosition,
                    rotation
                );

                if (enemy == null) continue;

                activeEnemies.Add(enemy);
                enemy.OnDied += OnEnemyDied;
            }

            Debug.Log($"[{sectorData.sectorName}] 전투 시작 — 적 {remainingEnemyCount}마리 소환");
        }

        private void OnEnemyDied(EnemyStatus enemy)
        {
            enemy.OnDied -= OnEnemyDied;
            activeEnemies.Remove(enemy);

            // 전투 섹터: 처치 시 풀에 반환 (숨기기)
            EnemyPoolManager.Instance.ReturnToPool(enemy);

            remainingEnemyCount--;
            Debug.Log($"[{sectorData.sectorName}] 남은 적: {remainingEnemyCount}");

            if (remainingEnemyCount <= 0)
            {
                ClearSector();
            }
        }
    }
}
