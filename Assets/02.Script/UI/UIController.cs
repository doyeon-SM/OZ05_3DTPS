using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using StarterAssets;
using UnityEngine.EventSystems;

namespace _01.Scenes.PhaseValidation.UI
{
    /// <summary>
    /// UI 스택 매니저 — ESC(UIClose) 입력 시 열린 UI를 차례로 닫습니다.
    /// 
    /// [사용법]
    ///  UI를 열 때 : UIController.Instance.Push(gameObject, onClose콜백)
    ///  UI를 닫을 때 : UIController.Instance.Pop()  또는 ESC 입력
    /// </summary>
    public class UIController : MonoBehaviour
    {
        public static UIController Instance { get; private set; }

        // 열린 UI 스택 — (UI 오브젝트, 닫힐 때 호출할 콜백) 쌍으로 저장
        private readonly Stack<(GameObject ui, System.Action onClose)> _activeUIStack = new();

        private StarterAssetsInputs _input;

        [Header("옵션 창")]
        [Tooltip("ESC 입력 시(스택 0 상태) 열릴 옵션 창 프리팹. 씬에 미리 배치하지 않고 런타임에 Instantiate합니다.")]
        [SerializeField] private GameObject optionsMenuPrefab;
        [Tooltip("옵션 창을 생성할 부모 오브젝트의 이름. 각 씬의 Canvas 아래에 이 이름으로 미리 배치되어 있어야 합니다.\n" +
                 "UIController는 DontDestroyOnLoad로 씬을 넘어 유지되지만, 이 부모 오브젝트는 씬마다 새로 생성되므로\n" +
                 "Transform을 직접 들고 있지 않고 씬 전환마다 이름으로 다시 찾는다.")]
        [SerializeField] private string optionsMenuParentName = "OptionUIRoot";

        private GameObject _optionsMenuInstance;
        private bool _isCursorOverridden;
        private Coroutine _cursorRestoreCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 씬 전환 후에도 UI 스택/옵션 창 기능이 계속 동작하도록 파괴되지 않게 한다.
            // (LobyScene에서 다른 씬으로 넘어가도 이 오브젝트는 유지된다.)
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // StarterAssetsInputs를 지연 탐색 (씬 전환 후 Player가 새로 생성되므로)
            if (_input == null)
                _input = FindFirstObjectByType<StarterAssetsInputs>();

            if (_input == null) return;

            if (_input.UIClose)
            {
                _input.UIClose = false;

                if (_activeUIStack.Count == 0)
                    OpenOptionsMenu();
                else
                    Pop();
            }
        }

        /// <summary>
        /// UI를 열 때 스택에 등록합니다.
        /// onClose : ESC 또는 Pop() 호출 시 실행할 닫기 로직
        /// </summary>
        public void Push(GameObject ui, System.Action onClose = null)
        {
            if (ui == null) return;

            bool wasEmpty = _activeUIStack.Count == 0;
            _activeUIStack.Push((ui, onClose));
            Debug.Log($"[UIController] Push: {ui.name} (스택 깊이: {_activeUIStack.Count})");

            // 스택이 비어 있다가 첫 UI가 열리는 순간 커서를 보여준다.
            // (각 UI 스크립트의 EnableUIMode와 별개로, 스택 기준의 안전망 역할)
            if (wasEmpty)
                ShowCursor();
        }

        /// <summary>
        /// 스택 최상단 UI를 닫습니다. ESC 입력 시 자동 호출됩니다.
        /// </summary>
        public void Pop()
        {
            if (_activeUIStack.Count == 0)
            {
                Debug.Log("[UIController] 닫을 UI가 없습니다.");
                return;
            }

            var (ui, onClose) = _activeUIStack.Pop();
            Debug.Log($"[UIController] Pop: {ui?.name} (남은 스택: {_activeUIStack.Count})");

            // 콜백이 있으면 콜백으로 닫기 (StageSelectUI.CloseUI 등 커스텀 닫기 로직)
            // 콜백이 없으면 단순 SetActive(false)
            if (onClose != null)
                onClose.Invoke();
            else if (ui != null)
                ui.SetActive(false);

            // 스택이 완전히 비었다면(stack: 0) 커서를 강제로 비활성화한다.
            // onClose 콜백이 커서를 직접 처리하지 않거나 처리를 깜빡해도
            // 여기서 항상 최종 상태를 보정해준다.
            if (_activeUIStack.Count == 0)
                HideCursor();
        }

        /// <summary>
        /// 스택의 모든 UI를 한 번에 닫습니다.
        /// </summary>
        public void PopAll()
        {
            while (_activeUIStack.Count > 0)
                Pop();
        }

        /// <summary>
        /// 열린 UI가 하나도 없는 상태(stack: 0)에서 ESC 입력 시 호출됩니다.
        /// 옵션 창은 미리 배치하지 않고 prefab을 런타임에 Instantiate하여 사용합니다.
        /// </summary>
        private void OpenOptionsMenu()
        {
            if (optionsMenuPrefab == null)
            {
                Debug.LogWarning("[UIController] optionsMenuPrefab이 연결되지 않았습니다. Inspector에서 프리팹을 할당해주세요.");
                return;
            }

            EnableUIMode();
            Transform parent = FindOptionsMenuParent();
            _optionsMenuInstance = Instantiate(optionsMenuPrefab, parent);
            _optionsMenuInstance.SetActive(true);
            Push(_optionsMenuInstance, CloseOptionsMenu);
        }

        /// <summary>
        /// 현재 활성화된 씬에서 "OptionUIRoot"(또는 지정한 이름) 오브젝트를 찾아 부모로 사용한다.
        /// UIController는 씬을 넘어 유지되지만, 각 씬의 Canvas 하위 오브젝트는 씬마다 새로 생성되므로
        /// Transform을 캐시하지 않고 옵션 창을 열 때마다 다시 찾는다.
        /// </summary>
        private Transform FindOptionsMenuParent()
        {
            if (string.IsNullOrEmpty(optionsMenuParentName))
                return null;

            var found = GameObject.Find(optionsMenuParentName);
            if (found == null)
            {
                Debug.LogWarning($"[UIController] '{optionsMenuParentName}' 오브젝트를 현재 씬에서 찾을 수 없습니다. 옵션 창이 부모 없이 생성됩니다.");
                return null;
            }

            return found.transform;
        }

        /// <summary>옵션 창을 닫고, Instantiate된 인스턴스를 파괴합니다. (Pop()의 onClose 콜백으로 등록됨)</summary>
        private void CloseOptionsMenu()
        {
            DisableUIMode();

            if (_optionsMenuInstance != null)
            {
                Destroy(_optionsMenuInstance);
                _optionsMenuInstance = null;
            }
        }

        // ── 마우스 커서 표시/숨김 (스택 기준 — Push/Pop에서 호출) ──────

        private void ShowCursor()
        {
            StopCursorRestoreCoroutine();

            if (_input != null)
                _input.cursorLocked = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HideCursor()
        {
            RestoreGameplayCursorState();
            RestartCursorRestoreCoroutine();
        }

        private void RestoreGameplayCursorState()
        {
            if (_input == null)
                _input = FindFirstObjectByType<StarterAssetsInputs>();

            if (_input != null)
                _input.cursorLocked = true;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RestartCursorRestoreCoroutine()
        {
            StopCursorRestoreCoroutine();
            _cursorRestoreCoroutine = StartCoroutine(RestoreCursorAfterUIClose());
        }

        private void StopCursorRestoreCoroutine()
        {
            if (_cursorRestoreCoroutine == null)
                return;

            StopCoroutine(_cursorRestoreCoroutine);
            _cursorRestoreCoroutine = null;
        }

        private IEnumerator RestoreCursorAfterUIClose()
        {
            yield return new WaitForEndOfFrame();

            if (_activeUIStack.Count == 0)
                RestoreGameplayCursorState();

            yield return null;

            if (_activeUIStack.Count == 0)
                RestoreGameplayCursorState();

            _cursorRestoreCoroutine = null;
        }

        // ── UI 모드 전환 (마우스 표시 / 카메라·공격 입력 차단) ──────────
        // StageSelectUI.EnableUIMode/DisableUIMode와 동일한 패턴.

        private void EnableUIMode()
        {
            if (_isCursorOverridden) return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_input != null)
            {
                _input.SetLookInputBlocked(true);
                _input.SetAttackInputBlocked(true);
            }

            _isCursorOverridden = true;
        }

        private void DisableUIMode()
        {
            if (!_isCursorOverridden) return;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_input != null)
            {
                _input.SetLookInputBlocked(false);
                _input.SetAttackInputBlocked(false);
            }

            _isCursorOverridden = false;
        }

        /// <summary>
        /// 현재 열려 있는 UI가 있는지 여부
        /// </summary>
        public bool HasActiveUI => _activeUIStack.Count > 0;
    }
}
