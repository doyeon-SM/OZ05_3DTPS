using UnityEngine;

namespace _00.ChoiHeesu._01.Script
{
    [DisallowMultipleComponent]
    public class CrossHairData : MonoBehaviour
    {
        [Header("UI Parts")]
        [SerializeField] private RectTransform point;
        [SerializeField] private RectTransform top;
        [SerializeField] private RectTransform bottom;
        [SerializeField] private RectTransform left;
        [SerializeField] private RectTransform right;

        [Header("Gap Direction")]
        [SerializeField] private Vector2 topDirection = Vector2.up;
        [SerializeField] private Vector2 bottomDirection = Vector2.down;
        [SerializeField] private Vector2 leftDirection = Vector2.right;
        [SerializeField] private Vector2 rightDirection = Vector2.left;

        public GameObject SourcePrefab { get; private set; }

        private Vector2 pointBasePosition;
        private Vector2 topBasePosition;
        private Vector2 bottomBasePosition;
        private Vector2 leftBasePosition;
        private Vector2 rightBasePosition;
        private bool hasCachedBasePositions;

        private void Reset()
        {
            AutoAssignParts();
        }

        private void Awake()
        {
            AutoAssignParts();
            CacheBasePositions();
        }

        public void Initialize(GameObject sourcePrefab)
        {
            SourcePrefab = sourcePrefab;
            AutoAssignParts();
            CacheBasePositions();
            SetGap(0f);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetGap(float gap)
        {
            if (!hasCachedBasePositions)
                CacheBasePositions();

            float safeGap = Mathf.Max(gap, 0f);
            ApplyAnchoredPosition(point, pointBasePosition, Vector2.zero, 0f);
            ApplyAnchoredPosition(top, topBasePosition, topDirection, safeGap);
            ApplyAnchoredPosition(bottom, bottomBasePosition, bottomDirection, safeGap);
            ApplyAnchoredPosition(left, leftBasePosition, leftDirection, safeGap);
            ApplyAnchoredPosition(right, rightBasePosition, rightDirection, safeGap);
        }

        private void CacheBasePositions()
        {
            pointBasePosition = GetAnchoredPosition(point);
            topBasePosition = GetAnchoredPosition(top);
            bottomBasePosition = GetAnchoredPosition(bottom);
            leftBasePosition = GetAnchoredPosition(left);
            rightBasePosition = GetAnchoredPosition(right);
            hasCachedBasePositions = true;
        }

        private void AutoAssignParts()
        {
            if (point == null)
                point = FindChildRect("point");

            if (top == null)
                top = FindChildRect("top");

            if (bottom == null)
                bottom = FindChildRect("bottom");

            if (left == null)
                left = FindChildRect("left");

            if (right == null)
                right = FindChildRect("right");
        }

        private RectTransform FindChildRect(string keyword)
        {
            RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                RectTransform rectTransform = rectTransforms[i];
                if (rectTransform == null || rectTransform == transform)
                    continue;

                if (rectTransform.name.ToLowerInvariant().Contains(keyword))
                    return rectTransform;
            }

            return null;
        }

        private static Vector2 GetAnchoredPosition(RectTransform rectTransform)
        {
            return rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        }

        private static void ApplyAnchoredPosition(RectTransform rectTransform, Vector2 basePosition, Vector2 direction, float gap)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchoredPosition = basePosition + direction * gap;
        }
    }
}
