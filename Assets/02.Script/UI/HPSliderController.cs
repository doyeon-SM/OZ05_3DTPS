using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _01.Scenes.PhaseValidation.UI
{
    public class HPSliderController : MonoBehaviour
    {
        [SerializeField] private DoubleIntEventChannel hpChanged;

        [Header("HP Sliders")]
        [FormerlySerializedAs("slider")]
        [SerializeField] private Slider afterSlider;
        [SerializeField] private Slider beforeSlider;

        [Header("Damage Animation")]
        [SerializeField, Min(0f)] private float delayTime = 0.25f;
        [SerializeField, Min(0f)] private float valueChangeDuration = 0.25f;

        private Coroutine hpChangeCoroutine;
        private float currentTargetValue = 1f;
        private bool hasTargetValue;

        private void OnEnable()
        {
            if (hpChanged != null)
                hpChanged.Register(OnHpChanged);
        }

        private void OnDisable()
        {
            if (hpChanged != null)
                hpChanged.Unregister(OnHpChanged);

            if (hpChangeCoroutine != null)
            {
                StopCoroutine(hpChangeCoroutine);
                hpChangeCoroutine = null;
            }
        }

        private void OnHpChanged(int currentHp, int maxHp)
        {   
            float hpRatio = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            float previousTargetValue = hasTargetValue ? currentTargetValue : GetCurrentSliderValue(hpRatio);
            bool isDamage = hpRatio < previousTargetValue;

            currentTargetValue = hpRatio;
            hasTargetValue = true;

            if (hpChangeCoroutine != null)
                StopCoroutine(hpChangeCoroutine);

            hpChangeCoroutine = StartCoroutine(ApplyHpChange(hpRatio, isDamage));
        }

        private IEnumerator ApplyHpChange(float targetValue, bool useDelay)
        {
            float afterStartValue = GetSliderValue(afterSlider, targetValue);
            float beforeStartValue = GetSliderValue(beforeSlider, afterStartValue);
            float beforeDelay = useDelay ? Mathf.Max(0f, delayTime) : 0f;
            float duration = Mathf.Max(0f, valueChangeDuration);

            if (duration <= 0f)
            {
                SetSliderValue(afterSlider, targetValue);

                if (beforeDelay > 0f)
                    yield return new WaitForSeconds(beforeDelay);

                SetSliderValue(beforeSlider, targetValue);
                hpChangeCoroutine = null;
                yield break;
            }

            float elapsedTime = 0f;
            float totalTime = Mathf.Max(duration, beforeDelay + duration);

            while (elapsedTime < totalTime)
            {
                elapsedTime += Time.deltaTime;

                float afterProgress = Mathf.Clamp01(elapsedTime / duration);
                SetSliderValue(afterSlider, Mathf.Lerp(afterStartValue, targetValue, afterProgress));

                if (elapsedTime >= beforeDelay)
                {
                    float beforeProgress = Mathf.Clamp01((elapsedTime - beforeDelay) / duration);
                    SetSliderValue(beforeSlider, Mathf.Lerp(beforeStartValue, targetValue, beforeProgress));
                }

                yield return null;
            }

            SetSliderValue(afterSlider, targetValue);
            SetSliderValue(beforeSlider, targetValue);
            hpChangeCoroutine = null;
        }

        private float GetCurrentSliderValue(float fallbackValue)
        {
            if (afterSlider != null)
                return afterSlider.value;

            if (beforeSlider != null)
                return beforeSlider.value;

            return fallbackValue;
        }

        private float GetSliderValue(Slider targetSlider, float fallbackValue)
        {
            return targetSlider != null ? targetSlider.value : fallbackValue;
        }

        private void SetSliderValue(Slider targetSlider, float value)
        {
            if (targetSlider != null)
                targetSlider.value = value;
        }
    }
}
