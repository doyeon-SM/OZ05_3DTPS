using System.Collections.Generic;
using UnityEngine;
using TurretDemo;

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

        // 스폰된 오브젝트 중 터렛인 것만 별도 추적
        private readonly List<NearestEnemyTurretController> sectorTurrets = new();

        protected override void OnSectorStart()
        {
            if (sectorData == null)
            {
                Debug.LogWarning($"[{gameObject.name}] BattleSectorData가 설정되지 않았습니다.");
                return;
            }

            sectorTurrets.Clear();
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

                // 스폰된 오브젝트가 터렛이면 별도 목록에 등록
                var turret = enemy.GetComponent<NearestEnemyTurretController>();
                if (turret != null)
                    sectorTurrets.Add(turret);
            }

            Debug.Log($"[{sectorData.sectorName}] 소환 완료 — 적 {remainingEnemyCount}마리 (터렛 {sectorTurrets.Count}개 포함, 대기 상태)");
        }

        protected override void StartBattle()
        {
            if (sectorData == null) return;

            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                    enemy.SetAIActive(true);
            }

            // 터렛은 enemy.SetAIActive()와 별개로 자체 AI 활성화 필요
            foreach (var turret in sectorTurrets)
                if (turret != null) turret.SetAIActive(true);

            Debug.Log($"[{sectorData.sectorName}] 전투 시작 — 적 {remainingEnemyCount}마리 + 터렛 {sectorTurrets.Count}개 활성화");
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
