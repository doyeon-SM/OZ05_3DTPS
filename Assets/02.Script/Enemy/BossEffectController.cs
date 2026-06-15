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

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(clip);
        }
    }
}
