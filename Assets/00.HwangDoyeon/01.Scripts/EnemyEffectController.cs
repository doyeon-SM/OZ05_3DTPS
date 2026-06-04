using UnityEngine;

/// <summary>
/// 모든 적 프리팹에 공통으로 붙이는 SFX/VFX 컨트롤러.
/// ────────────────────────────────────────────────
/// ▶ 사용법
///   1. 적 프리팹에 이 컴포넌트를 추가한다.
///   2. Inspector 에서 EnemyEffectData ScriptableObject 에셋을 할당한다.
///   3. 공격 시점 : PlayAttackEffects() 호출  (예: EnemyAnimationEventReceiver.Hit())
///      피격 시점 : PlayHitEffects()    호출  (예: EnemyStatus.TakeDamage())
///      피격 위치를 알 때 : PlayHitEffects(hitPoint) 오버로드 사용
/// ────────────────────────────────────────────────
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EnemyEffectController : MonoBehaviour
{
    [Tooltip("적 공통 SFX/VFX 데이터 에셋 (EnemyEffectData ScriptableObject)")]
    [SerializeField] private EnemyEffectData effectData;

    private AudioSource audioSource;

    // ────────── 초기화 ──────────
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D 사운드

        if (effectData == null)
            Debug.LogWarning($"[EnemyEffectController] {gameObject.name}: effectData가 할당되지 않았습니다.");
    }

    // ════════════════════════════════════════════════
    //  공개 API
    // ════════════════════════════════════════════════

    /// <summary>
    /// 적이 공격할 때 호출 — 공격 SFX + VFX 재생
    /// </summary>
    public void PlayAttackEffects()
    {
        if (effectData == null) return;

        PlaySFX(effectData.attackSFX, effectData.attackVolume);
        SpawnVFX(effectData.attackVFX, transform.position + transform.TransformDirection(effectData.attackVFXOffset), transform.rotation);
    }

    /// <summary>
    /// 피격 위치를 모를 때 — 적 자신의 위치에서 피격 이펙트 재생
    /// </summary>
    public void PlayHitEffects()
    {
        PlayHitEffects(transform.position + transform.TransformDirection(effectData != null ? effectData.hitVFXOffset : Vector3.zero));
    }

    /// <summary>
    /// 피격 위치를 알 때 — 해당 월드 좌표에서 피격 이펙트 재생
    /// </summary>
    /// <param name="hitPoint">충돌이 발생한 월드 좌표</param>
    public void PlayHitEffects(Vector3 hitPoint)
    {
        if (effectData == null) return;

        PlaySFX(effectData.hitSFX, effectData.hitVolume);
        SpawnVFX(effectData.hitVFX, hitPoint, Quaternion.identity);
    }

    // ════════════════════════════════════════════════
    //  내부 헬퍼
    // ════════════════════════════════════════════════

    /// <summary>AudioSource 를 통해 클립을 1회 재생</summary>
    private void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    /// <summary>VFX 프리팹을 지정 위치에 Instantiate, 수명 후 자동 삭제</summary>
    private void SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation)
    {
        if (vfxPrefab == null) return;

        GameObject vfxInstance = Instantiate(vfxPrefab, position, rotation);

        // 파티클 시스템이 있으면 즉시 재생
        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play();

        // 수명이 설정되어 있으면 자동 삭제
        if (effectData.vfxLifetime > 0f)
            Destroy(vfxInstance, effectData.vfxLifetime);
    }

    // ════════════════════════════════════════════════
    //  외부 에셋 런타임 교체 (옵셔널)
    // ════════════════════════════════════════════════

    /// <summary>
    /// 런타임에서 이펙트 데이터를 교체할 때 사용 (예: 보스 페이즈 전환)
    /// </summary>
    public void SetEffectData(EnemyEffectData newData)
    {
        effectData = newData;
    }

    /// <summary>현재 할당된 EnemyEffectData 반환</summary>
    public EnemyEffectData GetEffectData() => effectData;
}
