using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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
    // 외부 진입점
    // -------------------------------------------------------

    /// <summary>SceneMoveInteractionObject.Interaction()에서 호출</summary>
    public void OpenStageList()
    {
        stageListPanel.SetActive(true);
        stageInfoPopup.SetActive(false);
    }

    public void CloseStageList()
    {
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
        stageNameText.text  = info.stageName;
        //sceneNameText.text  = info.sceneName;
        stageInfoPopup.SetActive(true);
    }
    private void CloseUI()
    {
        ClosePopup();
        stageListPanel.SetActive(false);
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

        // UI 닫고 씬 이동
        CloseStageList();
        Debug.Log($"[StageSelectUI] '{_selectedStage.sceneName}' 씬으로 이동합니다.");
        SceneManager.LoadScene(_selectedStage.sceneName);
    }
}
