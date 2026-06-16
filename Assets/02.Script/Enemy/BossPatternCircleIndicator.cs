using System.Collections;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 패턴3(컨테이너) 낙하 지점에 표시되는 원형 예고.
    /// 반지름이 startRadius(0.5m)에서 endRadius(2m)까지 duration(3초)에 걸쳐 점점 커진다.
    /// Plane 기준 (기본 10m 지름 → scale = (지름)/10).
    /// </summary>
    public class BossPatternCircleIndicator : MonoBehaviour
    {
        [Header("표시")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("크기 변화")]
        [SerializeField] private float startRadius = 0.5f;
        [SerializeField] private float endRadius = 2f;
        [SerializeField] private float duration = 3f;

        [Tooltip("Plane의 로컬 Y 오프셋 (바닥 높이 보정용)")]
        [SerializeField] private float yOffset = -3.9f;

        private Coroutine _growRoutine;

        /// <summary>
        /// 지름한 웓드 좌�판 원형 예고를 표시하고, duration에 걸쳐 반지름을 키운다.
        /// </summary>
        public void Show(Vector3 worldCenter)
        {
            Transform parent = transform.parent;
            Vector3 localCenter = parent != null ? parent.InverseTransformPoint(worldCenter) : worldCenter;

            transform.localPosition = new Vector3(localCenter.x, yOffset, localCenter.z);
            transform.localRotation = Quaternion.identity;

            gameObject.SetActive(true);
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = telegraphColor;

            if (_growRoutine != null) StopCoroutine(_growRoutine);
            _growRoutine = StartCoroutine(GrowRoutine());
        }

        public void Hide()
        {
            if (_growRoutine != null)
            {
                StopCoroutine(_growRoutine);
                _growRoutine = null;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator GrowRoutine()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float radius = Mathf.Lerp(startRadius, endRadius, t);
                ApplyScale(radius);

                elapsed += Time.deltaTime;
                yield return null;
            }
            ApplyScale(endRadius);
        }

        private void ApplyScale(float radius)
        {
            float diameter = radius * 2f;
            transform.localScale = new Vector3(diameter / 10f, 1f, diameter / 10f);
        }

        private void OnDisable()
        {
            if (_growRoutine != null)
            {
                StopCoroutine(_growRoutine);
                _growRoutine = null;
            }
        }
    }
}
