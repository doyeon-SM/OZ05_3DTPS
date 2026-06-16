using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 부채꼴 공격 예고/판정 표시용 자식 오브젝트.
    /// 실제 데미지 판정은 BossController에서 OverlapSphere + 각도 체크로 수행하며,
    /// 이 컴포넌트는 시각적 표시(예고 → 공격 색상 전환)만 담당한다.
    ///
    /// [배치]
    ///  - 보스 자식, 보스 정면(forward) 기준 위치/회전으로 미리 배치 (Plane).
    ///  - 후방 공격 시 BossController가 이 오브젝트를 180도 회전시켜 재사용한다.
    /// </summary>
    public class BossFanAttackHitbox : MonoBehaviour
    {
        [Header("표시")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.35f);
        [SerializeField] private Color attackColor = new Color(1f, 0f, 0f, 0.8f);

        /// <summary>예고 표시 시작 (반투명 빨간색).</summary>
        public void ShowTelegraph()
        {
            gameObject.SetActive(true);
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = telegraphColor;
        }

        /// <summary>공격 시점 표시 (진한 빨간색). 짧게 표시 후 Hide 호출 권장.</summary>
        public void ShowAttack()
        {
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = attackColor;
        }

        /// <summary>표시 종료.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
