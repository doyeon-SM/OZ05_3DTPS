using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSpedex
{
    [RequireComponent(typeof(Button))]
    public class LobbyMoveButton : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string lobbySceneName = "LobyScene";
        [SerializeField] private string spawnPointName = "SpawnPoint";

        [Header("Input")]
        [SerializeField] private bool unblockGameplayInputBeforeLoad = true;
        [SerializeField] private bool lockCursorBeforeLoad = true;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(MoveToLobby);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(MoveToLobby);
        }

        public void MoveToLobby()
        {
            if (string.IsNullOrWhiteSpace(lobbySceneName))
            {
                Debug.LogError("[LobbyMoveButton] 이동할 로비 씬 이름이 비어 있습니다.", this);
                return;
            }

            if (ScenePositionManager.Instance != null && !string.IsNullOrWhiteSpace(spawnPointName))
                ScenePositionManager.Instance.SetNextSpawnPoint(spawnPointName);
            else if (ScenePositionManager.Instance == null)
                Debug.LogWarning("[LobbyMoveButton] ScenePositionManager 인스턴스가 없어 로비 스폰 위치를 예약하지 못했습니다.", this);

            StarterAssetsInputs inputs = FindFirstObjectByType<StarterAssetsInputs>(FindObjectsInactive.Include);
            if (inputs != null)
            {
                if (unblockGameplayInputBeforeLoad)
                    inputs.SetGameplayInputBlocked(false);

                if (lockCursorBeforeLoad)
                    inputs.cursorLocked = true;
            }

            if (lockCursorBeforeLoad)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            SceneManager.LoadScene(lobbySceneName);
        }
    }
}
