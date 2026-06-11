using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using StarterAssets;

/// <summary>
/// 스테이지 선택 UI 전체를 관리한다.
///
/// [UI 계층 구조 예시]
/// StageSelectUI (이 컴포넌트)
///  ├─ StageListPanel          ← stageListPanel
///  │   └─ StageButton (x N)  ← stageButtons[ ] (각 버튼에 StageInfoSO 연결)
///  └─ StageInfoPopup          ← stageInfoPopup
///      ├─ TMP_Text (이름)     ← stageNameText
///      ├─ TMP_Text (씬이름)   ← sceneNameText
///      ├─ Button [이동]       ← confirmButton
///      └─ Button [닫기]       ← closePopupButton
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    // -------------------------------------------------------
    // Inspector 필드
    // -------------------------------------------------------
    [Header("스테이지 목록 패널")]
    [SerializeField] private GameObject stageListPanel;
    [SerializeField] private Button[] stageButtons;
    [SerializeField] private StageInfoSO[] stageInfos;   // stageButtons[i] ↔ stageInfos[i]
    [SerializeField] private Button closeStageButton;

    [Header("스테이지 정보 팝업")]
    [SerializeField] private GameObject stageInfoPopup;
    [SerializeField] private TextMeshProUGUI stageNameText;
    //[SerializeField] private TextMeshProUGUI sceneNameText;
    [SerializeField] private Button confirmButton;        // [이동] 버튼
    [SerializeField] private Button closePopupButton;     // [닫기] 버튼

    // -------------------------------------------------------
    // 런타임 상태
    // -------------------------------------------------------
    private StageInfoSO _selectedStage;

    // 커서/카메라 제어용
    private StarterAssetsInputs _inputs;
    private bool _isCursorOverridden = false;

    // -------------------------------------------------------
    // Unity 이벤트
    // -------------------------------------------------------
    private void Awake()
    {
        // 버튼 클릭 이벤트 등록
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int index = i; // 클로저 캡처용
            stageButtons[index].onClick.AddListener(() => OnStageButtonClicked(index));
        }

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        closeStageButton.onClick.AddListener(CloseUI);
        closePopupButton.onClick.AddListener(ClosePopup);

        // 시작 시 모두 닫힌 상태
        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
    }

    // -------------------------------------------------------
    // 커서 / 카메라 제어 (RadialMenu 참고)
    // -------------------------------------------------------

    /// <summary>
    /// UI 열릴 때 호출. 마우스 커서를 활성화하고 카메라 look 입력을 차단한다.
    /// </summary>
    private void EnableUIMode()
    {
        if (_isCursorOverridden) return;

        // StarterAssetsInputs를 지연 탐색 (씬 전환 후 Player가 새로 생성되므로)
        _inputs = FindObjectOfType<StarterAssetsInputs>();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (_inputs != null)
            _inputs.SetLookInputBlocked(true);

        _isCursorOverridden = true;
    }

    /// <summary>
    /// UI 닫힐 때 호출. 커서를 다시 잠그고 카메라 look 입력을 복원한다.
    /// </summary>
    private void DisableUIMode()
    {
        if (!_isCursorOverridden) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (_inputs != null)
            _inputs.SetLookInputBlocked(false);

        _isCursorOverridden = false;
    }

    // -------------------------------------------------------
    // 외부 진입점
    // -------------------------------------------------------

    /// <summary>SceneMoveInteractionObject.Interaction()에서 호출</summary>
    public void OpenStageList()
    {
        EnableUIMode();
        stageListPanel.SetActive(true);
        stageInfoPopup.SetActive(false);
    }

    public void CloseStageList()
    {
        DisableUIMode();
        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
    }

    // -------------------------------------------------------
    // 내부 흐름
    // -------------------------------------------------------

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
        //sceneNameText.text = info.sceneName;
        stageInfoPopup.SetActive(true);
    }

    private void CloseUI()
    {
        ClosePopup();
        stageListPanel.SetActive(false);
        DisableUIMode();
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

        // UI 닫고 씬 이동 (DisableUIMode는 씬 이동으로 인해 불필요하지만 명시적으로 호출)
        DisableUIMode();
        stageListPanel.SetActive(false);
        stageInfoPopup.SetActive(false);
        Debug.Log($"[StageSelectUI] '{_selectedStage.sceneName}' 씬으로 이동합니다.");
        SceneManager.LoadScene(_selectedStage.sceneName);
    }
}
