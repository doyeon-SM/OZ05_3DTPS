using System.Collections;
using UnityEngine;

namespace _00.ChoiHeesu._01.Script
{
    [DisallowMultipleComponent]
    public class HitCrossHairController : MonoBehaviour
    {
        private const string DefaultHitCrossHairObjectName = "HitCrossHair_UI";

        [Header("References")]
        [SerializeField] private GameObject hitCrossHairObject;
        [SerializeField] private CanvasGroup hitCrossHairCanvasGroup;

        [Header("Hit Filter")]
        [SerializeField] private LayerMask hitLayerMask;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.05f;
        [SerializeField, Min(0f)] private float holdDuration = 0.08f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.15f;
        [SerializeField] private bool useUnscaledTime;

        private Coroutine hitRoutine;
        private bool missingHitCrossHairLogged;

        private void Reset()
        {
            hitLayerMask = LayerMask.GetMask("Enemy");
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            CacheReferences();
            HitFeedbackEvents.Hit += OnHit;
            SetAlpha(0f);
        }

        private void OnDisable()
        {
            HitFeedbackEvents.Hit -= OnHit;

            if (hitRoutine != null)
            {
                StopCoroutine(hitRoutine);
                hitRoutine = null;
            }
        }

        private void OnValidate()
        {
            fadeInDuration = Mathf.Max(fadeInDuration, 0f);
            holdDuration = Mathf.Max(holdDuration, 0f);
            fadeOutDuration = Mathf.Max(fadeOutDuration, 0f);
        }

        private void OnHit(HitFeedbackEventData hitData)
        {
            if (!hitData.IsInLayerMask(hitLayerMask))
                return;

            CacheReferences();
            if (hitCrossHairCanvasGroup == null)
            {
                ReportMissingHitCrossHair();
                return;
            }

            if (hitRoutine != null)
                StopCoroutine(hitRoutine);

            hitRoutine = StartCoroutine(PlayHitCrossHairRoutine());
        }

        private IEnumerator PlayHitCrossHairRoutine()
        {
            if (hitCrossHairObject != null && !hitCrossHairObject.activeSelf)
                hitCrossHairObject.SetActive(true);

            yield return FadeAlpha(hitCrossHairCanvasGroup.alpha, 1f, fadeInDuration);

            float holdTimer = 0f;
            while (holdTimer < holdDuration)
            {
                holdTimer += GetDeltaTime();
                yield return null;
            }

            yield return FadeAlpha(hitCrossHairCanvasGroup.alpha, 0f, fadeOutDuration);
            hitRoutine = null;
        }

        private IEnumerator FadeAlpha(float startAlpha, float targetAlpha, float duration)
        {
            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                yield break;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += GetDeltaTime();
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, smoothProgress));
                yield return null;
            }

            SetAlpha(targetAlpha);
        }

        private float GetDeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private void CacheReferences()
        {
            if (hitCrossHairObject == null)
            {
                Transform foundTransform = FindChildRecursive(transform, DefaultHitCrossHairObjectName);
                if (foundTransform == null && transform.root != transform)
                    foundTransform = FindChildRecursive(transform.root, DefaultHitCrossHairObjectName);

                if (foundTransform != null)
                    hitCrossHairObject = foundTransform.gameObject;
            }

            if (hitCrossHairObject == null && gameObject.name == DefaultHitCrossHairObjectName)
                hitCrossHairObject = gameObject;

            if (hitCrossHairCanvasGroup == null && hitCrossHairObject != null)
            {
                if (!hitCrossHairObject.TryGetComponent(out hitCrossHairCanvasGroup))
                    hitCrossHairCanvasGroup = hitCrossHairObject.AddComponent<CanvasGroup>();
            }

            if (hitCrossHairCanvasGroup != null)
            {
                hitCrossHairCanvasGroup.interactable = false;
                hitCrossHairCanvasGroup.blocksRaycasts = false;
            }
        }

        private Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null)
                return null;

            if (root.name == targetName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), targetName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void SetAlpha(float alpha)
        {
            if (hitCrossHairCanvasGroup != null)
                hitCrossHairCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private void ReportMissingHitCrossHair()
        {
            if (missingHitCrossHairLogged)
                return;

            Debug.LogError("[HitCrossHairController] HitCrossHair_UI 또는 CanvasGroup을 찾을 수 없습니다. Inspector 연결을 확인해주세요.", this);
            missingHitCrossHairLogged = true;
        }
    }
}
