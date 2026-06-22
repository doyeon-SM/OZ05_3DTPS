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

        [Header("보상")]
        [Tooltip("보스 처치 시 보스 사망 위치에 고정으로 소환할 보상 오브젝트. 보스는 더 이상 StageManager의 확률 기반 랜덤 드랍을 사용하지 않고 이 오브젝트 하나만 소환한다.")]
        public GameObject rewardPrefab;

        [Header("BGM")]
        [Tooltip("보스 등장(소환) 시점에 BGMManager를 통해 재생할 BGM 클립. 비워두면 BGM이 바뀌지 않습니다.")]
        public AudioClip bossBgmClip;
    }
}
