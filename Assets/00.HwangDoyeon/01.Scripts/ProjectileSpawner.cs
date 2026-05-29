using System;
using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 레거시: 풀링 방식으로 전환되어 더 이상 사용하지 않습니다.
    /// Projectile 생성은 BaseTurretController 내부 풀에서 처리합니다.
    /// </summary>
    [Obsolete("풀링 방식으로 전환됨. BaseTurretController 내부 풀을 사용하세요.")]
    public static class ProjectileSpawner
    {
        public static GameObject Spawn(
            GameObject projectilePrefab,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            float speedUnitsPerSecond,
            float lifeTimeSeconds,
            float damageAmount,
            int shooterTeamId,
            Transform parent)
        {
            if (projectilePrefab == null) return null;
            GameObject instance = UnityEngine.Object.Instantiate(
                projectilePrefab, spawnPosition, spawnRotation, parent);
            return instance;
        }
    }
}
