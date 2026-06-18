using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using StarterAssets;

/// <summary>
/// 옵션 창(P_Option) 내부 버튼 동작을 관리한다.
///
/// [기능]
///  - B_GameQuit : 게임 종료
///  - B_Audio    : audioPanelPrefab(P_Audio)을 출력
///  - B_Game     : gamePanelPrefab(P_Game)을 출력
///  - 기본 상태  : audioPanelPrefab이 출력된 상태로 시작
///
/// 오디오/게임 패널은 미리 배치하지 않고, 버튼 클릭 시 prefab을 Instantiate하여 교체한다.
/// (UIController가 옵션 창 자체를 prefab으로 관리하는 것과 동일한 패턴)
/// </summary>
public class OptionController : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button quitButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Button gameButton;

    [Header("포기(GiveUp) — LobyScene이 아닐 때 GameQuit 대신 표시")]
    [SerializeField] private Button giveUpButton;

    [Tooltip("로비 씬 이름 (Build Settings에 등록된 이름)")]
    [SerializeField] private string lobbySceneName = "LobyScene";

    [Tooltip("로비 씬 도착 시 위치할 스폰 포인트 이름")]
    [SerializeField] private string spawnPointName = "SpawnPoint";

    [Header("설정 패널 프리팹 (미리 배치하지 않고 런타임에 Instantiate)")]
    [Tooltip("Audio 버튼 클릭 시 출력할 프리팹 (P_Audio)")]
    [SerializeField] private GameObject audioPanelPrefab;
    [Tooltip("Game 버튼 클릭 시 출력할 프리팹 (P_Game)")]
    [SerializeField] private GameObject gamePanelPrefab;

    [Tooltip("패널이 생성될 부모 Transform. 비워두면 이 오브젝트(P_Option) 바로 아래에 생성된다.")]
    [SerializeField] private Transform panelContainer;

    private GameObject _currentPanelInstance;

    private void Awake()
    {
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        else
            Debug.LogWarning("[OptionController] quitButton이 연결되지 않았습니다.");

        if (audioButton != null)
            audioButton.onClick.AddListener(ShowAudioPanel);
        else
            Debug.LogWarning("[OptionController] audioButton이 연결되지 않았습니다.");

        if (gameButton != null)
            gameButton.onClick.AddListener(ShowGamePanel);
        else
            Debug.LogWarning("[OptionController] gameButton이 연결되지 않았습니다.");

        if (giveUpButton != null)
            giveUpButton.onClick.AddListener(OnGiveUpButtonClicked);
        else
            Debug.LogWarning("[OptionController] giveUpButton이 연결되지 않았습니다.");

        ApplyQuitOrGiveUpVisibility();

        // 옵션 창이 열릴 때 기본값(Audio 패널)으로 시작한다.
        ShowAudioPanel();
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────

    private void OnQuitButtonClicked()
    {
        Debug.Log("[OptionController] 게임 종료 버튼 클릭");

#if UNITY_EDITOR
        // 에디터에서는 Application.Quit()이 동작하지 않으므로 Play 모드를 종료한다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 현재 씬이 LobyScene이면 GameQuit 버튼만, 아니면 GiveUp 버튼만 표시한다.
    /// </summary>
    private void ApplyQuitOrGiveUpVisibility()
    {
        bool isLobby = SceneManager.GetActiveScene().name == "LobyScene";

        if (quitButton != null)
            quitButton.gameObject.SetActive(isLobby);

        if (giveUpButton != null)
            giveUpButton.gameObject.SetActive(!isLobby);
    }

    /// <summary>
    /// GiveUp 버튼 클릭 — 게임을 종료하지 않고 LobyScene으로 이동한다.
    /// </summary>
    private void OnGiveUpButtonClicked()
    {
        Debug.Log("[OptionController] 포기(GiveUp) 버튼 클릭 — 로비로 이동합니다.");

        if (ScenePositionManager.Instance != null)
            ScenePositionManager.Instance.SetNextSpawnPoint(spawnPointName);
        else
            Debug.LogWarning("[OptionController] ScenePositionManager 인스턴스가 없어 로비 스폰 위치를 예약하지 못했습니다.");

        StarterAssetsInputs inputs = FindFirstObjectByType<StarterAssetsInputs>(FindObjectsInactive.Include);
        if (inputs != null)
        {
            inputs.SetGameplayInputBlocked(false);
            inputs.cursorLocked = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(lobbySceneName);
    }

    private void ShowAudioPanel()
    {
        ShowPanel(audioPanelPrefab);
    }

    private void ShowGamePanel()
    {
        ShowPanel(gamePanelPrefab);
    }

    // ── 패널 교체 ─────────────────────────────────────────────

    /// <summary>현재 표시 중인 패널을 파괴하고 새 패널 프리팹을 Instantiate한다.</summary>
    private void ShowPanel(GameObject panelPrefab)
    {
        if (panelPrefab == null)
        {
            Debug.LogWarning("[OptionController] 출력할 패널 프리팹이 연결되지 않았습니다.");
            return;
        }

        if (_currentPanelInstance != null)
            Destroy(_currentPanelInstance);

        Transform parent = panelContainer != null ? panelContainer : transform;
        _currentPanelInstance = Instantiate(panelPrefab, parent);
    }
}
