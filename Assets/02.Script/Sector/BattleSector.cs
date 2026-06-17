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

            // 적을 씬 시작 시점에 미리 소환해두고(화면에 보이는 상태), AI는 꺼둔다(추적 off).
            // 입장문을 열고 영역에 들어와 StartBattle()이 호출되어야 AI가 켜진다.
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
                enemy.SetAIActive(false);
            }

            Debug.Log($"[{sectorData.sectorName}] 적 {remainingEnemyCount}마리 미리 소환 완료 (대기 상태)");
        }

        protected override void StartBattle()
        {
            if (sectorData == null) return;

            // 이미 미리 소환되어 대기 중인 적들의 AI를 켠다(추적 시작).
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                    enemy.SetAIActive(true);
            }

            Debug.Log($"[{sectorData.sectorName}] 전투 시작 — 적 {remainingEnemyCount}마리 활성화");
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
