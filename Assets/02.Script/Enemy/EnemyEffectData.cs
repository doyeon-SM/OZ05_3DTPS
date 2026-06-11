using UnityEngine;

/// <summary>
/// 적 공통 SFX/VFX 데이터를 보관하는 ScriptableObject.
/// Project 창에서 우클릭 → Create → Enemy → EnemyEffectData 로 생성.
/// 각 적 프리팹에 EnemyEffectController를 붙이고 이 에셋을 할당한다.
/// </summary>
[CreateAssetMenu(fileName = "EnemyEffectData", menuName = "Enemy/EnemyEffectData")]
public class EnemyEffectData : ScriptableObject
{
    [Header("=== 공격 이펙트 ===")]
    [Tooltip("공격 시 재생할 사운드 클립 (없으면 무음)")]
    public AudioClip attackSFX;

    [Tooltip("공격 시 재생할 VFX 프리팹 (ParticleSystem 등, 없으면 생략)")]
    public GameObject attackVFX;

    [Tooltip("공격 VFX가 생성될 기준 위치 오프셋 (로컬 좌표)")]
    public Vector3 attackVFXOffset = Vector3.zero;

    [Header("=== 피격 이펙트 ===")]
    [Tooltip("피격 시 재생할 사운드 클립 (없으면 무음)")]
    public AudioClip hitSFX;

    [Tooltip("피격 시 재생할 VFX 프리팹 (ParticleSystem 등, 없으면 생략)")]
    public GameObject hitVFX;

    [Tooltip("피격 VFX가 생성될 기준 위치 오프셋 (로컬 좌표)")]
    public Vector3 hitVFXOffset = Vector3.zero;

    [Header("=== 오디오 설정 ===")]
    [Range(0f, 1f)]
    [Tooltip("공격 사운드 볼륨 (0~1)")]
    public float attackVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("피격 사운드 볼륨 (0~1)")]
    public float hitVolume = 1f;

    [Header("=== VFX 수명 설정 ===")]
    [Tooltip("생성된 VFX 오브젝트를 자동 삭제할 시간(초). 0이면 자동 삭제 안 함.")]
    public float vfxLifetime = 2f;
}
