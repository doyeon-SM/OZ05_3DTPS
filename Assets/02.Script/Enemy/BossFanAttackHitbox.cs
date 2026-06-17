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
    ///
    /// [Shader]
    ///  - BossFanIndicator Shader Graph 사용. Plane 메시는 사각형 그대로,
    ///    Material의 _FanAngle/_Direction/_EdgeSoftness로 부채꼴 모양만 alpha 처리.
    /// </summary>
    public class BossFanAttackHitbox : MonoBehaviour
    {
        private static readonly int BaseColorId    = Shader.PropertyToID("_BaseColor");
        private static readonly int FanAngleId     = Shader.PropertyToID("_FanAngle");
        private static readonly int DirectionId    = Shader.PropertyToID("_Direction");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");

        [Header("표시")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.35f);
        [SerializeField] private Color attackColor = new Color(1f, 0f, 0f, 0.8f);

        [Header("부채꼴 Shader 설정")]
        [Tooltip("부채꼴 전체 각도(도). BossController의 fanAngle과 동기화 권장.")]
        [SerializeField] private float fanAngle = 90f;

        [Tooltip("부채꼴이 향하는 기준 방향(도). Plane의 +Z 기준 0으로 두면 보통 정면과 일치.")]
        [SerializeField] private float direction = 0f;

        [Tooltip("경계 안티앨리어싱 정도.")]
        [SerializeField] private float edgeSoftness = 0.02f;

        private MaterialPropertyBlock _propBlock;

        private MaterialPropertyBlock PropBlock
        {
            get
            {
                if (_propBlock == null)
                    _propBlock = new MaterialPropertyBlock();
                return _propBlock;
            }
        }

        private void Awake()
        {
            ApplyShapeProperties();
        }

        /// <summary>외부(BossController)에서 부채꼴 각도를 동기화할 때 호출.</summary>
        public void SetFanAngle(float angle)
        {
            fanAngle = angle;
            ApplyShapeProperties();
        }

        /// <summary>전방(0도)/후방(180도) 등 방향 전환 시 호출.</summary>
        public void SetDirection(float dir)
        {
            direction = dir;
            ApplyShapeProperties();
        }

        private void ApplyShapeProperties()
        {
            if (indicatorRenderer == null) return;

            indicatorRenderer.GetPropertyBlock(PropBlock);
            PropBlock.SetFloat(FanAngleId, fanAngle);
            PropBlock.SetFloat(DirectionId, direction);
            PropBlock.SetFloat(EdgeSoftnessId, edgeSoftness);
            indicatorRenderer.SetPropertyBlock(PropBlock);
        }

        /// <summary>예고 표시 시작 (반투명 빨간색).</summary>
        public void ShowTelegraph()
        {
            gameObject.SetActive(true);
            ApplyShapeProperties();
            SetColor(telegraphColor);
        }

        /// <summary>공격 시점 표시 (진한 빨간색). 짧게 표시 후 Hide 호출 권장.</summary>
        public void ShowAttack()
        {
            SetColor(attackColor);
        }

        /// <summary>표시 종료.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetColor(Color color)
        {
            if (indicatorRenderer == null) return;

            indicatorRenderer.GetPropertyBlock(PropBlock);
            PropBlock.SetColor(BaseColorId, color);
            indicatorRenderer.SetPropertyBlock(PropBlock);
        }
    }
}
