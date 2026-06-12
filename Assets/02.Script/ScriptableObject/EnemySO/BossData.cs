using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    [CreateAssetMenu(fileName ="BossData", menuName ="Stage/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("기본 설정")]
        public GameObject prefab;

        [Header("스탯")]
        public int maxHealth = 100;
        public int attackPower = 10;

        [Header("패턴 설정")]
        [Tooltip("근접 패턴 판정 반경(m). 이 범위 안이면 부채꼴 공격, 밖이면 원거리 레이저 공격.")]
        public float meleeRangeRadius = 3f;

        [Tooltip("보스 회전 속도(도/초)")]
        public float rotationSpeed = 90f;

        [Tooltip("레이저 사거리(m)")]
        public float laserRange = 20f;
    }
}
