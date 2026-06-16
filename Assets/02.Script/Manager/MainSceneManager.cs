using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// MainScene 전용 매니저.
    /// StartButton 클릭 시 지정된 씬(기본값: LobyScene)으로 이동한다.
    /// </summary>
    public class MainSceneManager : MonoBehaviour
    {
        [Header("버튼")]
        [SerializeField] private Button startButton;

        [Header("이동할 씬")]
        [Tooltip("StartButton 클릭 시 로드할 씬 이름 (Build Settings에 등록되어 있어야 합니다).")]
        [SerializeField] private string nextSceneName = "LobyScene";

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClicked);
            else
                Debug.LogWarning("[MainSceneManager] startButton이 연결되지 않았습니다.");
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        /// <summary>StartButton 클릭 시 nextSceneName으로 씬 전환.</summary>
        private void OnStartButtonClicked()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("[MainSceneManager] nextSceneName이 비어 있습니다.");
                return;
            }

            Debug.Log($"[MainSceneManager] '{nextSceneName}' 씬으로 이동합니다.");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
