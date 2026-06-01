using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 점령 섹터 타이머 UI.
    /// OccupySector의 timerUI 슬롯에 연결하면 자동으로 업데이트된다.
    /// </summary>
    public class OccupyTimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Slider timerSlider;

        private void Awake()
        {
            // 시작 시 비활성화
            gameObject.SetActive(false);
        }

        /// <summary>점령 시작 시 UI 표시</summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>점령 종료 시 UI 숨김</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateTimer(float remaining, float total)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }

            if (timerSlider != null)
            {
                timerSlider.value = remaining / total;
            }
        }

        public void OnOccupySuccess()
        {
            if (timerText != null) timerText.text = "점령 성공!";
            if (timerSlider != null) timerSlider.value = 0f;
        }
    }
}
