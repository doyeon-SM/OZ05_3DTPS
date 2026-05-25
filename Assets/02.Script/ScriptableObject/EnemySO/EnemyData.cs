using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    public enum EnemyType
    {
        MeleeRobot,     // 근접공격형 돌진로봇
        RangedRobot     // 원거리공격형 고정로봇
    }

    [CreateAssetMenu(fileName = "EnemyData", menuName = "Stage/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("기본 정보")]
        public EnemyType enemyType;
        public GameObject prefab;

        [Header("스탯")]
        public int maxHealth = 100;
        public int attackPower = 10;

        [Header("점령 섹터 재소환")]
        [Tooltip("점령 섹터에서 처치 후 재소환까지의 대기 시간(초)")]
        public float respawnDelay = 5f;
    }
}
