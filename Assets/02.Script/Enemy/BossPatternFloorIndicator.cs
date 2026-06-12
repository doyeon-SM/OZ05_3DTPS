using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 바닥패턴(격자) 공격 예고/판정 표시용 풀링 오브젝트.
    ///
    /// [사용법]
    ///  - BossFloorPatternController가 보스 자식으로 미리 9개를 풀링해두고 꺼내 사용한다.
    ///  - SetRect(): 격자 칸(또는 줄) 범위에 맞춰 위치/크기를 동적으로 조정 (Plane 기준).
    ///  - ShowTelegraph() → 데미지 판정 → ShowAttack() → Hide() 흐름으로 사용.
    /// </summary>
    public class BossPatternFloorIndicator : MonoBehaviour
    {
        [Header("표시")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.35f);
        [SerializeField] private Color attackColor = new Color(1f, 0f, 0f, 0.8f);

        [Tooltip("Plane의 로컬 Y 오프셋 (바닥 높이 보정용)")]
        [SerializeField] private float yOffset = -3.9f;

        /// <summary>
        /// 월드 좌표 기준 사각형 범위(centerX, centerZ, sizeX, sizeZ)에 맞춰
        /// 이 오브젝트(부모: 보스)의 localPosition/localScale을 설정합니다.
        /// Plane 기본 크기는 10m × 10m이므로 scale = size / 10.
        /// </summary>
        public void SetRect(Vector3 worldCenter, float sizeX, float sizeZ)
        {
            Transform parent = transform.parent;
            Vector3 localCenter = parent != null ? parent.InverseTransformPoint(worldCenter) : worldCenter;

            transform.localPosition = new Vector3(localCenter.x, yOffset, localCenter.z);
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);
        }

        public void ShowTelegraph()
        {
            gameObject.SetActive(true);
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = telegraphColor;
        }

        public void ShowAttack()
        {
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = attackColor;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
