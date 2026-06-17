using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using StarterAssets;

namespace _01.Scenes.PhaseValidation.UI
{
    /// <summary>
    /// 보스 클리어 시 표시되는 UI.
    ///
    /// [흐름]
    ///  1. BossSector.HandleBossDied() → 보스 사망 위치에 BossExitObject 소환
    ///  2. 플레이어가 BossExitObject와 상호작용([E]) → BossExitObject.Interaction() → BossClearUI.Instance.Show() 호출
    ///  3. 패널 표시 + 커서/입력 UI 모드 전환 (StageSelectUI와 동일 패턴)
    ///  4. [확인] 버튼 클릭 → ScenePositionManager로 스폰포인트 지정 후 LobyScene 이동
    ///
    /// BossExitObject는 여러 번 상호작용 가능하므로, 이 UI는 닫힌 뒤에도 다시 Show()로 재오픈될 수 있다.
    ///
    /// [UI 계층 구조 예시]
    /// BossClearUI (이 컴포넌트)
    ///  └ ClearPanel        ← clearPanel
    ///      └ Button [확인] ← confirmButton
    /// </summary>
    public class BossClearUI : MonoBehaviour
    {
        public static BossClearUI Instance { get; private set; }

        [Header("클리어 패널")]
        [SerializeField] private GameObject clearPanel;
        [SerializeField] private Button confirmButton;

        [Header("씬 이동 정보")]
        [Tooltip("이동할 로비 씬 이름 (Build Settings에 등록된 이름)")]
        [SerializeField] private string lobySceneName = "LobyScene";

        [Tooltip("로비 씬 도착 시 위치할 스폰 포인트 이름")]
        [SerializeField] private string spawnPointName = "SpawnPoint";

        private StarterAssetsInputs _inputs;
        private bool _isCursorOverridden;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (confirmButton != null)
                // CloseUI류와 동일하게, 직접 호출 대신 UIController.Pop()을 통해 onClose(OnConfirmButtonClicked)를 실행한다.
                confirmButton.onClick.AddListener(() => UIController.Instance?.Pop());

            if (clearPanel != null)
                clearPanel.SetActive(false);
        }

        // ── 커서 / 입력 제어 (StageSelectUI와 동일 패턴) ───────

        private void EnableUIMode()
        {
            if (_isCursorOverridden) return;
            _inputs = FindObjectOfType<StarterAssetsInputs>();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_inputs != null)
            {
                _inputs.SetLookInputBlocked(true);
                _inputs.SetAttackInputBlocked(true);
            }
            _isCursorOverridden = true;
        }

        private void DisableUIMode()
        {
            if (!_isCursorOverridden) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_inputs != null)
            {
                _inputs.SetLookInputBlocked(false);
                _inputs.SetAttackInputBlocked(false);
            }
            _isCursorOverridden = false;
        }

        // ── 외부 진입점 ────────────────────────────────────

        /// <summary>BossExitObject.Interaction()에서 호출 — 클리어 UI 표시.</summary>
        public void Show()
        {
            EnableUIMode();
            if (clearPanel != null)
                clearPanel.SetActive(true);
            UIController.Instance?.Push(clearPanel, OnConfirmButtonClicked);
        }

        // ── 확인 버튼 ───────────────────────────────────────

        private void OnConfirmButtonClicked()
        {
            DisableUIMode();
            if (clearPanel != null)
                clearPanel.SetActive(false);

            if (ScenePositionManager.Instance != null)
                ScenePositionManager.Instance.SetNextSpawnPoint(spawnPointName);
            else
                Debug.LogWarning("[BossClearUI] ScenePositionManager 인스턴스가 없습니다.");

            Debug.Log($"[BossClearUI] '{lobySceneName}' 씬으로 이동합니다.");
            SceneManager.LoadScene(lobySceneName);
        }
    }
}
