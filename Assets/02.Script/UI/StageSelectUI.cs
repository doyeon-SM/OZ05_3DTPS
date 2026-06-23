using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using StarterAssets;
using _01.Scenes.PhaseValidation.UI;

/// <summary>
/// 스테이지 선택 UI 전체를 관리한다.
///
/// [UI 계층 구조]
/// StageSelectUI (이 컴포넌트)
///  ├─ StageListPanel          ← stageListPanel
///  │   └─ StageButton (x N)  ← stageButtons[ ]  (각 버튼에 StageInfoSO 연결)
///  └─ StageInfoPopup          ← stageInfoPopup   (StageInfoUI 컴포넌트 부착)
///
/// [다음 스테이지 잠금 규칙]
///  - stageButtons[0]  : 항상 활성화 (흰색)
///  - stageButtons[i]  : stageInfos[i-1] 씬의 보스를 클리어한 경우에만 흰색/클릭 가능
///                       그 외에는 검정색/클릭 불가
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
    /// <summary>자식 패널(StageInfoPopup)에 부착된 StageInfoUI 컴포넌트</summary>
    [SerializeField] private StageInfoUI stageInfoUI;

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

        stageInfoUI.OnConfirmClicked += OnConfirmButtonClicked;

        // Pop()이 등록된 onClose 콜백(CloseUI / ClosePopup)을 통해 스택과 커서를 일관되게 처리한다.
        closeStageButton.onClick.AddListener(() => UIController.Instance?.Pop());

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
            _inputs.SetAttackInputBlocked(true);
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
        RefreshStageButtonStates();
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

    // ── 스테이지 버튼 잠금 / 해제 ─────────────────────────────

    /// <summary>
    /// 보스 클리어 기록을 바탕으로 각 스테이지 버튼의 잠금 상태를 갱신한다.
    /// OpenStageList() 시점에 호출된다.
    /// </summary>
    private void RefreshStageButtonStates()
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            // 첫 번째 스테이지는 항상 해제, 이후는 이전 스테이지 보스 클리어 여부로 결정
            bool unlocked = (i == 0) || IsPreviousStageCleared(i);
            SetButtonLocked(stageButtons[i], !unlocked);
        }
    }

    /// <summary>index 번째 스테이지의 바로 이전 스테이지가 클리어되었는지 반환한다.</summary>
    private bool IsPreviousStageCleared(int index)
    {
        if (index <= 0) return true;
        if (SaveManager.Instance == null) return false;
        if (stageInfos == null || index - 1 >= stageInfos.Length) return false;
        StageInfoSO prev = stageInfos[index - 1];
        if (prev == null) return false;
        return SaveManager.Instance.IsStageCleared(prev.sceneName);
    }

    /// <summary>
    /// 버튼의 잠금 상태를 시각적으로 설정한다.
    /// locked = true  → 검정색 / 클릭 불가
    /// locked = false → 흰색   / 클릭 가능
    /// </summary>
    private void SetButtonLocked(Button btn, bool locked)
    {
        btn.interactable = !locked;
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = locked ? Color.black : Color.white;
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
        stageInfoUI.Populate(info);
        stageInfoPopup.SetActive(true);
        UIController.Instance?.Push(stageInfoPopup, ClosePopup);
    }

    public void CloseUI()
    {
        DisableUIMode();
        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
        stageInfoUI.Clear();
        _selectedStage = null;
    }

    private void ClosePopup()
    {
        stageInfoPopup.SetActive(false);
        stageInfoUI.Clear();
        _selectedStage = null;
    }

    private void OnConfirmButtonClicked(StageInfoSO info)
    {
        if (info == null)
        {
            Debug.LogWarning("[StageSelectUI] 선택된 스테이지 정보가 없습니다.");
            return;
        }
        if (ScenePositionManager.Instance == null)
        {
            Debug.LogError("[StageSelectUI] ScenePositionManager 인스턴스가 없습니다.");
            return;
        }
        ScenePositionManager.Instance.SetNextSpawnPoint(info.spawnPointName);
        DisableUIMode();
        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
        Debug.Log($"[StageSelectUI] '{info.sceneName}' 씬으로 이동합니다.");
        SceneManager.LoadScene(info.sceneName);
    }
}
