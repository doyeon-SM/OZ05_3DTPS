using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    [Serializable]
    public class OccupyEnemyData
    {
        [Tooltip("소환할 적의 데이터")]
        public EnemyData enemyData;

        [Tooltip("소환 수량")]
        public int count = 3;
    }

    [CreateAssetMenu(fileName = "OccupySectorData", menuName = "Stage/Occupy Sector Data")]
    public class OccupySectorData : ScriptableObject
    {
        [Header("섹터 정보")]
        public string sectorName = "점령 섹터";

        [Header("점령 조건")]
        [Tooltip("점령 달성까지 버텨야 하는 시간(초)")]
        public float occupyDuration = 60f;

        [Header("소환 적 목록")]
        [Tooltip("이 섹터에서 소환될 적 종류 및 수량")]
        public OccupyEnemyData[] enemyEntries;
    }
}
