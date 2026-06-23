using _01.Scenes.PhaseValidation.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 스테이지 정보 팝업 UI를 담당하는 컴포넌트.
/// StageSelectUI 아래의 자식 패널(StageInfoPopup)에 부착한다.
/// Inspector에서 각 UI 요소를 직접 연결한다.
///
/// [UI 연결 대상 — Inspector에서 자식 패널에 직접 설정]
///  - stageNameText      : 스테이지 이름 텍스트
///  - stageLevelText     : 스테이지 레벨 텍스트
///  - stageInfoTitleText : 스테이지 정보 제목 텍스트
///  - stageInfoText      : 스테이지 설명 텍스트
///  - warningText        : 씬 미할당 등 경고 메시지 텍스트 (평소 비활성)
///  - confirmButton      : 이동(확인) 버튼
///  - closePopupButton   : 팝업 닫기 버튼
/// </summary>
public class StageInfoUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI stageLevelText;
    [SerializeField] private TextMeshProUGUI stageInfoTitleText;
    [SerializeField] private TextMeshProUGUI stageInfoText;

    [Header("경고 텍스트 (씬 미할당 등)")]
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("버튼")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closePopupButton;

    /// <summary>씬 검증을 통과한 경우에만 StageSelectUI에서 구독한 콜백이 호출된다.</summary>
    public System.Action<StageInfoSO> OnConfirmClicked;

    private StageInfoSO _currentInfo;

    private void Awake()
    {
        confirmButton.onClick.AddListener(HandleConfirm);
        closePopupButton.onClick.AddListener(() => UIController.Instance?.Pop());
    }

    // ── 외부 인터페이스 ───────────────────────────────────────

    /// <summary>
    /// 팝업에 스테이지 정보를 채운다.
    /// GameObject 활성화(SetActive)는 StageSelectUI 측에서 처리한다.
    /// </summary>
    public void Populate(StageInfoSO info)
    {
        _currentInfo            = info;
        stageNameText.text      = info.stageName;
        stageLevelText.text     = info.stageLevel;
        stageInfoTitleText.text = info.stageInfoTitle;
        stageInfoText.text      = info.stageInfo;

        // 새 스테이지를 열 때 이전 경고 초기화
        HideWarning();
    }

    /// <summary>현재 선택 정보를 초기화한다.</summary>
    public void Clear()
    {
        _currentInfo = null;
        HideWarning();
    }

    // ── 확인 버튼 처리 ────────────────────────────────────────

    private void HandleConfirm()
    {
        // 1) StageInfoSO 미할당 검사
        if (_currentInfo == null)
        {
            ShowWarning("스테이지 정보가 연결되어 있지 않습니다.");
            return;
        }

        // 2) sceneName 공백 검사
        if (string.IsNullOrEmpty(_currentInfo.sceneName))
        {
            ShowWarning($"[{_currentInfo.stageName}] 씬 이름이 비어 있습니다.\n(StageInfoSO.sceneName 미설정)");
            return;
        }

        // 3) Build Settings 등록 여부 검사
        if (!IsSceneInBuildSettings(_currentInfo.sceneName))
        {
            ShowWarning($"[{_currentInfo.stageName}] 씬을 찾을 수 없습니다.\n(Build Settings 미등록: '{_currentInfo.sceneName}')");
            return;
        }

        // 검증 통과 → 씬 이동 위임
        OnConfirmClicked?.Invoke(_currentInfo);
    }

    // ── 씬 존재 검증 ──────────────────────────────────────────

    /// <summary>
    /// Build Settings에 등록된 씬 중 <paramref name="sceneName"/>과 일치하는 항목이 있는지 반환한다.
    /// SceneUtility.GetScenePathByIndex로 전체 경로를 얻은 뒤 파일 이름만 비교한다.
    /// </summary>
private static bool IsSceneInBuildSettings(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            // path 예시: "Assets/Scenes/Stage01.unity" → 파일명만 추출
            string nameOnly = System.IO.Path.GetFileNameWithoutExtension(path);
            if (nameOnly == sceneName)
                return true;
        }
        return false;
    }

    // ── 경고 텍스트 헬퍼 ─────────────────────────────────────

    private void ShowWarning(string message)
    {
        Debug.LogWarning($"[StageInfoUI] {message}");

        if (warningText == null) return;
        warningText.text = message;
        warningText.gameObject.SetActive(true);
    }

    private void HideWarning()
    {
        if (warningText == null) return;
        warningText.text = string.Empty;
        warningText.gameObject.SetActive(false);
    }
}
