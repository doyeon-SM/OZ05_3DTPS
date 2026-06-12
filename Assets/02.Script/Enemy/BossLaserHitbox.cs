using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 원거리 레이저 공격 예고/판정용 자식 오브젝트.
    ///
    /// [배치]
    ///  - 보스 중심(원점)에서 정면(forward) 방향으로 길게 뻗는 Capsule Collider(Trigger).
    ///  - 길이는 BossData.laserRange, 반경은 0.5m 기준.
    ///
    /// [동작]
    ///  - 예고 단계(추적 2초): Telegraph 표시만, 판정 없음. BossController가 매 프레임 회전시킴.
    ///  - 공격 단계(1초, 고정): 판정 활성화 + 0.2초 간격 5회 틱 데미지.
    /// </summary>
    public class BossLaserHitbox : MonoBehaviour
    {
        [Header("표시")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.35f);
        [SerializeField] private Color attackColor = new Color(1f, 0f, 0f, 0.8f);

        [Header("틱 데미지")]
        [SerializeField] private float tickInterval = 0.2f;
        [SerializeField] private int tickCount = 5;

        private int _tickDamage;
        private Coroutine _tickRoutine;
        private readonly HashSet<PlayerStatus> _targetsInRange = new HashSet<PlayerStatus>();

        /// <summary>예고 표시 시작 (판정 없음).</summary>
        public void ShowTelegraph()
        {
            gameObject.SetActive(true);
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = telegraphColor;
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
            _targetsInRange.Clear();
        }
    }
}
