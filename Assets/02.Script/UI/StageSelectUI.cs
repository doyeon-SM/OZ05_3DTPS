using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using StarterAssets;
using _01.Scenes.PhaseValidation.UI;

/// <summary>
/// 스테이지 선택 UI 전체를 관리한다.
///
/// [UI 계층 구조 예시]
/// StageSelectUI (이 컴포넌트)
///  ├─ StageListPanel          ← stageListPanel
///  │   └─ StageButton (x N)  ← stageButtons[ ] (각 버튼에 StageInfoSO 연결)
///  └─ StageInfoPopup          ← stageInfoPopup
///      ├─ TMP_Text (이름)     ← stageNameText
///      ├─ Button [이동]       ← confirmButton
///      └─ Button [닫기]       ← closePopupButton
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    [Header("스테이지 목록 패널")]
    [SerializeField] private GameObject stageListPanel;
    [SerializeField] private Button[] stageButtons;
    [SerializeField] private StageInfoSO[] stageInfos;
    [SerializeField] private Button closeStageButton;

    [Header("스테이지 정보 팝업")]
    [SerializeField] private GameObject stageInfoPopup;
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closePopupButton;

    private StageInfoSO _selectedStage;
    private StarterAssetsInputs _inputs;
    private bool _isCursorOverridden = false;

    private void Awake()
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int index = i;
            stageButtons[index].onClick.AddListener(() => OnStageButtonClicked(index));
        }

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        // 닫기 버튼은 CloseUI/ClosePopup을 직접 호출하지 않고 UIController.Pop()을 통해 닫는다.
        // → Pop()이 등록된 onClose(CloseUI/ClosePopup)를 그대로 실행해주면서, 스택도 함께 정리되고
        //    스택 기준 커서 처리(HideCursor)도 일관되게 트리거된다.
        closeStageButton.onClick.AddListener(() => UIController.Instance?.Pop());
        closePopupButton.onClick.AddListener(() => UIController.Instance?.Pop());

        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
    }

    // ── 커서 / 카메라 / 공격 제어 ─────────────────────────────

    private void EnableUIMode()
    {
        if (_isCursorOverridden) return;
        _inputs = FindObjectOfType<StarterAssetsInputs>();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        if (_inputs != null)
        {
            _inputs.SetLookInputBlocked(true);
            _inputs.SetAttackInputBlocked(true);   // 입력 발생 즉시 차단 (Update 순서 무관)
        }
        _isCursorOverridden = true;
    }

    private void DisableUIMode()
    {
        if (!_isCursorOverridden) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        if (_inputs != null)
        {
            _inputs.SetLookInputBlocked(false);
            _inputs.SetAttackInputBlocked(false);
        }
        _isCursorOverridden = false;
    }

    // ── 외부 진입점 ───────────────────────────────────────────

    /// <summary>SceneMoveInteractionObject.Interaction()에서 호출</summary>
    public void OpenStageList()
    {
        EnableUIMode();
        stageListPanel.SetActive(true);
        stageInfoPopup.SetActive(false);
        UIController.Instance?.Push(stageListPanel, CloseUI);
    }

    public void CloseStageList()
    {
        DisableUIMode();
        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
    }

    // ── 내부 흐름 ─────────────────────────────────────────────

    private void OnStageButtonClicked(int index)
    {
        if (index < 0 || index >= stageInfos.Length || stageInfos[index] == null)
        {
            Debug.LogWarning($"[StageSelectUI] stageInfos[{index}]가 비어 있습니다.");
            return;
        }
        _selectedStage = stageInfos[index];
        OpenPopup(_selectedStage);
    }

    private void OpenPopup(StageInfoSO info)
    {
        stageNameText.text = info.stageName;
        stageInfoPopup.SetActive(true);
        UIController.Instance?.Push(stageInfoPopup, ClosePopup);
    }

    public void CloseUI()
    {
        DisableUIMode();
        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
        _selectedStage = null;
    }

    private void ClosePopup()
    {
        stageInfoPopup.SetActive(false);
        _selectedStage = null;
    }

    private void OnConfirmButtonClicked()
    {
        if (_selectedStage == null)
        {
            Debug.LogWarning("[StageSelectUI] 선택된 스테이지 정보가 없습니다.");
            return;
        }
        if (ScenePositionManager.Instance == null)
        {
            Debug.LogError("[StageSelectUI] ScenePositionManager 인스턴스가 없습니다.");
            return;
        }
        ScenePositionManager.Instance.SetNextSpawnPoint(_selectedStage.spawnPointName);
        DisableUIMode();
        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
        Debug.Log($"[StageSelectUI] '{_selectedStage.sceneName}' 씬으로 이동합니다.");
        SceneManager.LoadScene(_selectedStage.sceneName);
    }
}
