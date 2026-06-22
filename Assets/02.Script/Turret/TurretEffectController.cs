using System;
using UnityEngine;
using _01.Scenes.PhaseValidation;

namespace TurretDemo
{
    /// <summary>
    /// 터렷 전용 SFX/VFX 컨트롤러.
    /// ────────────────────────────────────────────────
    /// ▶ 사용법
    ///   1. Enemy_Turret(루트)에 이 컴포넌트를 추가한다.
    ///   2. Inspector에서 머즐 VFX 프리팹 / 공격·피격·사망 SFX 클립을 직접 할당한다.
    ///   3. 공격 시점 : NearestEnemyTurretController.OnProjectileFired()에서 PlayAttackEffects() 호출
    ///      피격 시점 : EnemyStatus.OnDamaged 이벤트 구독 → PlayHitEffects()
    ///      사망 시점 : EnemyStatus.OnDied   이벤트 구독 → PlayDeathEffects()
    /// ────────────────────────────────────────────────
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TurretEffectController : MonoBehaviour
    {
        [Header("=== 공격(머즐) VFX ===")]
        [Tooltip("발사 시 MuzzlePoint에서 재생할 VFX 프리팹 (ParticleSystem 등, 없으면 생략)")]
        [SerializeField] private GameObject muzzleFlashPrefab;

        [Tooltip("muzzleFlashPrefab을 자동 삭제할 시간(초). 0 이하면 자동 삭제하지 않음.")]
        [SerializeField] private float muzzleFlashLifetimeSeconds = 1.5f;

        [Header("=== 공격 SFX ===")]
        [Tooltip("발사 시 재생할 사운드 클립들 (여러 개면 매 발사마다 랜덤 재생)")]
        [SerializeField] private AudioClip[] attackSfxClips;

        [Range(0f, 1f)]
        [SerializeField] private float attackSfxVolume = 1f;

        [Header("=== 피격 SFX ===")]
        [Tooltip("데미지를 받을 때마다 재생할 사운드 클립들 (여러 개면 랜덤 재생)")]
        [SerializeField] private AudioClip[] hitSfxClips;

        [Range(0f, 1f)]
        [SerializeField] private float hitSfxVolume = 1f;

        [Header("=== 사망 SFX ===")]
        [Tooltip("사망 시 1회 재생할 사운드 클립")]
        [SerializeField] private AudioClip deathSfxClip;

        [Range(0f, 1f)]
        [SerializeField] private float deathSfxVolume = 1f;

        private AudioSource audioSource;
        private EnemyStatus enemyStatus;

        // ── 초기화 ─────────────────────────────────
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            enemyStatus = GetComponent<EnemyStatus>();
            if (enemyStatus == null)
                Debug.LogWarning($"[TurretEffectController] {gameObject.name}: EnemyStatus를 찾을 수 없습니다 — 피격/사망 SFX 비활성화");
        }

        private void OnEnable()
        {
            if (enemyStatus == null) return;
            enemyStatus.OnDamaged += HandleDamaged;
            enemyStatus.OnDied    += HandleDied;
        }

        private void OnDisable()
        {
            if (enemyStatus == null) return;
            enemyStatus.OnDamaged -= HandleDamaged;
            enemyStatus.OnDied    -= HandleDied;
        }

        // ════════════════════════════════════════════════
        //  공개 API
        // ════════════════════════════════════════════════

        /// <summary>
        /// 발사(공격) 시 호출 — 머즐 VFX + 공격 SFX 재생.
        /// </summary>
        /// <param name="muzzleTransform">VFX를 생성할 기준 위치. null이면 이 오브젝트 위치 사용.</param>
        public void PlayAttackEffects(Transform muzzleTransform = null)
        {
            Transform spawn = muzzleTransform != null ? muzzleTransform : transform;
            SpawnVfx(muzzleFlashPrefab, spawn.position, spawn.rotation, muzzleFlashLifetimeSeconds);
            PlayRandomClip(attackSfxClips, attackSfxVolume);
        }

        // ── 내부 이벤트 핸들러 (EnemyStatus 구독) ───
        private void HandleDamaged(int damageAmount)
        {
            PlayRandomClip(hitSfxClips, hitSfxVolume);
        }

        private void HandleDied(EnemyStatus status)
        {
            if (deathSfxClip == null || audioSource == null) return;
            audioSource.PlayOneShot(deathSfxClip, deathSfxVolume);
        }

        // ════════════════════════════════════════════════
        //  내부 헬퍼
        // ════════════════════════════════════════════════
        private void PlayRandomClip(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;
            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip == null) return;
            audioSource.PlayOneShot(clip, volume);
        }

        private void SpawnVfx(GameObject prefab, Vector3 position, Quaternion rotation, float lifetimeSeconds)
        {
            if (prefab == null) return;

            GameObject instance = Instantiate(prefab, position, rotation);

            ParticleSystem ps = instance.GetComponent<ParticleSystem>();
            if (ps != null)
                ps.Play();

            if (lifetimeSeconds > 0f)
                Destroy(instance, lifetimeSeconds);
        }
    }
}
