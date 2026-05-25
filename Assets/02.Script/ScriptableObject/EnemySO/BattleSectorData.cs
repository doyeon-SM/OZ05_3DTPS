using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    [Serializable]
    public class EnemyPlacementData
    {
        [Tooltip("배치할 적의 데이터")]
        public EnemyData enemyData;

        [Tooltip("씬에서 미리 배치된 스폰 위치")]
        public Vector3 spawnPosition;

        [Tooltip("스폰 시 회전값")]
        public Vector3 spawnRotation;
    }

    [CreateAssetMenu(fileName = "BattleSectorData", menuName = "Stage/Battle Sector Data")]
    public class BattleSectorData : ScriptableObject
    {
        [Header("섹터 정보")]
        public string sectorName = "전투 섹터";

        [Header("배치 적 목록")]
        [Tooltip("이 섹터에서 소환될 적들의 배치 정보")]
        public EnemyPlacementData[] enemyPlacements;
    }
}
