using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VolumetricLines;
using _01.Scenes.PhaseValidation._26._05._14;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 원거리 레이저 공격 예고/판정용 자식 오브젝트.
    ///
    /// [배치]
    ///  - 보스 중심(원점)에서 정면(forward) 방향으로 길게 뻗는 Capsule Collider(Trigger).
    ///  - 길이는 BossData.laserRange, 반경은 0.5m 기준.
    ///  - VolumetricLineBehavior(레이저 빔 비주얼)를 함께 배치.
    ///
    /// [동작]
    ///  - 예고 단계(추적 2초): VolumetricLine의 LightSaberFactor = 1 (얇은/예고 형태). 판정 없음.
    ///    BossController가 매 프레임 회전시킴.
    ///  - 공격 단계(1초, 고정): LightSaberFactor = 0.9 (발사 형태) + 판정 활성화 + 0.2초 간격 5회 틱 데미지.
    ///  - LineColor는 telegraph/attack 구분 없이 attackColor로 통일.
    /// </summary>
    public class BossLaserHitbox : MonoBehaviour
    {
        [Header("표시")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color attackColor = new Color(1f, 0f, 0f, 0.8f);

        [Header("볼류메트릭 라인")]
        [Tooltip("레이저 빔 비주얼을 담당하는 VolumetricLineBehavior. 예고/발사 시 LightSaberFactor 및 LineColor를 갱신합니다.")]
        [SerializeField] private VolumetricLineBehavior volumetricLine;

        [Tooltip("예고 단계의 LightSaberFactor 값")]
        [SerializeField] private float telegraphLightSaberFactor = 1f;

        [Tooltip("발사 직후 도달하는 LightSaberFactor 값")]
        [SerializeField] private float attackLightSaberFactor = 0.7f;

        [Tooltip("발사 시작 시 1 -> attackLightSaberFactor로 변하는 시간(초)")]
        [SerializeField] private float attackFactorRampInDuration = 0.7f;

        [Tooltip("발사 종료 시 attackLightSaberFactor -> 1로 돌아오는 시간(초)")]
        [SerializeField] private float attackFactorRampOutDuration = 0.3f;

        [Header("틱 데미지")]
        [SerializeField] private float tickInterval = 0.2f;
        [SerializeField] private int tickCount = 5;

        private int _tickDamage;
        private Coroutine _tickRoutine;
        private Coroutine _lightSaberFactorRoutine;
        private readonly HashSet<PlayerStatus> _targetsInRange = new HashSet<PlayerStatus>();

        /// <summary>예고 표시 시작 (판정 없음).</summary>
        public void ShowTelegraph()
        {
            gameObject.SetActive(true);
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = attackColor;

            if (_lightSaberFactorRoutine != null)
            {
                StopCoroutine(_lightSaberFactorRoutine);
                _lightSaberFactorRoutine = null;
            }

            if (volumetricLine != null)
            {
                volumetricLine.LineColor = attackColor;
                volumetricLine.LightSaberFactor = telegraphLightSaberFactor;
            }
        }

        /// <summary>
        /// 공격 시작 — 틱 데미지 판정 활성화.
        /// </summary>
        /// <param name="tickDamage">틱 1회당 데미지</param>
        public void StartAttack(int tickDamage)
        {
            _tickDamage = tickDamage;
            gameObject.SetActive(true);
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = attackColor;

            if (volumetricLine != null)
            {
                volumetricLine.LineColor = attackColor;
            }

            if (_lightSaberFactorRoutine != null) StopCoroutine(_lightSaberFactorRoutine);
            _lightSaberFactorRoutine = StartCoroutine(LightSaberFactorRoutine());

            if (_tickRoutine != null) StopCoroutine(_tickRoutine);
            _tickRoutine = StartCoroutine(TickRoutine());
        }

        /// <summary>표시 및 판정 종료.</summary>
        public void Hide()
        {
            if (_tickRoutine != null)
            {
                StopCoroutine(_tickRoutine);
                _tickRoutine = null;
            }
            if (_lightSaberFactorRoutine != null)
            {
                StopCoroutine(_lightSaberFactorRoutine);
                _lightSaberFactorRoutine = null;
            }
            // ramp-out이 끝나기 전에 Hide()가 호출되어도 LightSaberFactor가 항상 1로 복귀하도록 보장
            if (volumetricLine != null)
                volumetricLine.LightSaberFactor = 1f;

            _targetsInRange.Clear();
            gameObject.SetActive(false);
        }

        private IEnumerator TickRoutine()
        {
            for (int i = 0; i < tickCount; i++)
            {
                foreach (var target in _targetsInRange)
                {
                    if (target != null)
                    {
                        target.TakeDamage(_tickDamage);
                        Debug.Log($"[BossLaserHitbox] Player 피격 | tick={i + 1}/{tickCount} damage={_tickDamage}");
                    }
                }
                yield return new WaitForSeconds(tickInterval);
            }
        }

        /// <summary>
        /// 발사 단계 동안 LightSaberFactor를 1 -> attackLightSaberFactor (attackFactorRampInDuration)
        /// -> 1 (attackFactorRampOutDuration) 순서로 변화시킵니다.
        /// </summary>
        private IEnumerator LightSaberFactorRoutine()
        {
            if (volumetricLine == null) yield break;

            const float fullFactor = 1f;

            // 1 -> attackLightSaberFactor
            float elapsed = 0f;
            while (elapsed < attackFactorRampInDuration)
            {
                float tNorm = attackFactorRampInDuration > 0f ? elapsed / attackFactorRampInDuration : 1f;
                volumetricLine.LightSaberFactor = Mathf.Lerp(fullFactor, attackLightSaberFactor, tNorm);
                elapsed += Time.deltaTime;
                yield return null;
            }
            volumetricLine.LightSaberFactor = attackLightSaberFactor;

            // attackLightSaberFactor -> 1
            elapsed = 0f;
            while (elapsed < attackFactorRampOutDuration)
            {
                float tNorm = attackFactorRampOutDuration > 0f ? elapsed / attackFactorRampOutDuration : 1f;
                volumetricLine.LightSaberFactor = Mathf.Lerp(attackLightSaberFactor, fullFactor, tNorm);
                elapsed += Time.deltaTime;
                yield return null;
            }
            volumetricLine.LightSaberFactor = fullFactor;

            _lightSaberFactorRoutine = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerStatus status = other.GetComponentInParent<PlayerStatus>();
            if (status != null) _targetsInRange.Add(status);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerStatus status = other.GetComponentInParent<PlayerStatus>();
            if (status != null) _targetsInRange.Remove(status);
        }

        private void OnDisable()
        {
            if (_tickRoutine != null)
            {
                StopCoroutine(_tickRoutine);
                _tickRoutine = null;
            }
            if (_lightSaberFactorRoutine != null)
            {
                StopCoroutine(_lightSaberFactorRoutine);
                _lightSaberFactorRoutine = null;
            }
            _targetsInRange.Clear();
        }
    }
}
