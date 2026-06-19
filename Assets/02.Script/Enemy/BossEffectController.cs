using System.Collections;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 공격 시 VFX/SFX 재생을 담당하는 컴포넌트.
    ///
    /// [VFX]
    ///  - 공격 판정이 발생하는 순간(BossController.ApplyFanDamage,
    ///    BossFloorPatternController.ApplyFloorDamage 호출 시점)에 코드에서 직접 Instantiate.
    ///  - 멜리/바닥패턴 프리팹을 Inspector에서 할당.
    ///  - 레이저 VFX는 LaserHitbox에서 직접 처리(이 컨트롤러에서 제거됨).
    ///  - 사망 폭발 VFX는 사망 애니메이션의 Animation Event에서 OnDeathExplosionVfx()를 호출하여 재생.
    ///
    /// [SFX]
    ///  - 예고(Telegraph) / 판정(Hit) 단계를 구분하여 각각 재생.
    ///  - 애니메이션에 맞춰 재생되도록 Animation Event에서 호출할 public 트리거 메서드를 제공.
    ///  - AudioSource는 자동으로 GetComponent/AddComponent 처리.
    ///  - 현재는 애니메이션이 없는 프로토타입이므로, 추후 애니메이션 적용 시
    ///    해당 클립의 Animation Event에 OnXXXSFX_YYY() 메서드를 연결하면 됨.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BossEffectController : MonoBehaviour
    {
        [Header("멜리(부채꼴) 공격 - VFX")]
        [Tooltip("멜리 공격 판정 시점에 생성할 VFX 프리팹")]
        [SerializeField] private GameObject meleeVfxPrefab;

        [Header("멜리(부채꼴) 공격 - SFX")]
        [Tooltip("멜리 공격 예고(Telegraph) SFX")]
        [SerializeField] private AudioClip meleeTelegraphSfxClip;

        [Tooltip("멜리 공격 판정(Hit) SFX")]
        [SerializeField] private AudioClip meleeHitSfxClip;

        [Header("레이저 공격 - SFX")]
        [Tooltip("레이저 공격 예고(Telegraph) SFX")]
        [SerializeField] private AudioClip laserTelegraphSfxClip;

        [Tooltip("레이저 공격 판정(Hit) SFX")]
        [SerializeField] private AudioClip laserHitSfxClip;

        [Header("바닥 패턴 공격 - VFX")]
        [Tooltip("바닥 패턴 공격 판정 시점에 생성할 VFX 프리팹")]
        [SerializeField] private GameObject floorPatternVfxPrefab;

        [Header("바닥 패턴 공격 - SFX")]
        [Tooltip("바닥 패턴 공격 예고(Telegraph) SFX")]
        [SerializeField] private AudioClip floorPatternTelegraphSfxClip;

        [Tooltip("바닥 패턴 공격 판정(Hit) SFX")]
        [SerializeField] private AudioClip floorPatternHitSfxClip;

        [Header("피격(데미지 받음) - SFX")]
        [Tooltip("보스가 데미지를 받을 때마다 재생할 사운드 클립들 (여러 개면 랜덤 재생)")]
        [SerializeField] private AudioClip[] hitTakenSfxClips;

        [Range(0f, 1f)]
        [Tooltip("피격 SFX 볼륨 (0~1)")]
        [SerializeField] private float hitTakenSfxVolume = 1f;

        [Header("사망 연출 - 폭발 VFX")]
        [Tooltip("보스 사망 시 보스 주변에 생성할 폭발 VFX 프리팹")]
        [SerializeField] private GameObject deathExplosionVfxPrefab;

        [Tooltip("사망 시 생성할 폭발 VFX 개수")]
        [SerializeField] private int deathExplosionCount = 3;

        [Tooltip("보스 위치를 기준으로 폭발이 생성될 수평 반경")]
        [SerializeField] private float deathExplosionRadius = 1.5f;

        [Tooltip("폭발과 폭발 사이의 생성 간격(초). 0이면 한 번에 모두 생성한다.")]
        [SerializeField] private float deathExplosionInterval = 0.08f;

        [Header("VFX 설정")]
        [Tooltip("생성된 VFX 오브젝트 자동 파괴 시간(초). ParticleSystem의 duration+lifetime보다 길게 설정.")]
        [SerializeField] private float vfxLifetime = 3f;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
        }

        // ── VFX: 공격 판정 시점에 코드에서 직접 호출 ──────────────

        /// <summary>멜리(부채꼴) 공격 판정 위치에 VFX 생성.</summary>
        /// <param name="position">생성 위치(월드)</param>
        /// <param name="rotation">생성 회전(월드). 미지정 시 identity.</param>
        public void PlayMeleeVfx(Vector3 position, Quaternion rotation = default)
        {
            SpawnVfx(meleeVfxPrefab, position, rotation);
        }

        /// <summary>바닥 패턴 공격 판정 위치에 VFX 생성.</summary>
        public void PlayFloorPatternVfx(Vector3 position, Quaternion rotation = default)
        {
            SpawnVfx(floorPatternVfxPrefab, position, rotation);
        }

        private void SpawnVfx(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return;

            if (rotation.Equals(default(Quaternion)))
                rotation = Quaternion.identity;

            GameObject instance = Instantiate(prefab, position, rotation);
            Destroy(instance, vfxLifetime);
        }

        // ── VFX: 사망 폭발 ────────────────────────────────────
        // [Animation Event 전용] 사망(Die) 애니메이션 클립에서 호출하세요.

        /// <summary>
        /// [Animation Event 전용] 보스 주변(deathExplosionRadius 반경 내) 랜덤 위치에
        /// deathExplosionCount개의 폭발 VFX를 deathExplosionInterval 간격으로 순차 생성한다.
        /// 사망 애니메이션의 Animation Event에서 호출하도록 연결하세요.
        /// </summary>
        public void OnDeathExplosionVfx()
        {
            StartCoroutine(SpawnDeathExplosionsRoutine());
        }

        private IEnumerator SpawnDeathExplosionsRoutine()
        {
            if (deathExplosionVfxPrefab == null)
            {
                Debug.LogWarning("[BossEffectController] deathExplosionVfxPrefab이 설정되지 않았습니다.");
                yield break;
            }

            for (int i = 0; i < deathExplosionCount; i++)
            {
                Vector2 offset2D = Random.insideUnitCircle * deathExplosionRadius;
                Vector3 spawnPos = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);
                SpawnVfx(deathExplosionVfxPrefab, spawnPos, Quaternion.identity);

                if (deathExplosionInterval > 0f)
                    yield return new WaitForSeconds(deathExplosionInterval);
            }
        }

        // ── SFX: 예고(Telegraph) ─────────────────────────────
        // Animation Event에서 호출하거나, 예고 시작 시점에 코드에서 직접 호출.

        /// <summary>멜리(부채꼴) 공격 예고 SFX 재생.</summary>
        public void OnTelegraphSFX_Melee()
        {
            PlaySfx(meleeTelegraphSfxClip);
        }

        /// <summary>레이저 공격 예고 SFX 재생.</summary>
        public void OnTelegraphSFX_Laser()
        {
            PlaySfx(laserTelegraphSfxClip);
        }

        /// <summary>바닥 패턴 공격 예고 SFX 재생.</summary>
        public void OnTelegraphSFX_FloorPattern()
        {
            PlaySfx(floorPatternTelegraphSfxClip);
        }

        // ── SFX: 판정(Hit) ───────────────────────────────────
        // Animation Event에서 호출하거나, 데미지 판정 시점에 코드에서 직접 호출.

        /// <summary>멜리(부채꼴) 공격 판정 SFX 재생.</summary>
        public void OnHitSFX_Melee()
        {
            PlaySfx(meleeHitSfxClip);
        }

        /// <summary>레이저 공격 판정 SFX 재생.</summary>
        public void OnHitSFX_Laser()
        {
            PlaySfx(laserHitSfxClip);
        }

        /// <summary>바닥 패턴 공격 판정 SFX 재생.</summary>
        public void OnHitSFX_FloorPattern()
        {
            PlaySfx(floorPatternHitSfxClip);
        }

        // ── SFX: 피격(데미지 받음) ─────────────────────────────
        // BossStatus.TakeDamage() 등 데미지 처리 시점에서 직접 호출.
        // 기존 Telegraph/Hit SFX(Stop()+Play() 방식)와 겹쳐도 끊기지 않도록 PlayOneShot으로 재생.

        /// <summary>보스가 데미지를 받을 때 호출 — 피격 SFX 재생(랜덤 베리에이션).</summary>
        public void OnDamageTakenSFX()
        {
            PlayRandomSfx(hitTakenSfxClips, hitTakenSfxVolume);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;

            // 이전에 재생 중인 SFX를 끊고 새 SFX로 즉시 전환 (중첩 재생 방지)
            _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.Play();
        }

        /// <summary>배열에서 클립을 무작위로 골라 PlayOneShot 재생 (기존 재생 중인 SFX를 끊지 않음).</summary>
        private void PlayRandomSfx(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0 || _audioSource == null) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}
