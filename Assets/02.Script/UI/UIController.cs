using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            // StarterAssetsInputs를 지연 탐색 (씬 전환 후 Player가 새로 생성되므로)
            if (_input == null)
                _input = FindObjectOfType<StarterAssetsInputs>();

            if (_input == null) return;

            if (_input.UIClose)
            {
                _input.UIClose = false;
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
            _activeUIStack.Push((ui, onClose));
            Debug.Log($"[UIController] Push: {ui.name} (스택 깊이: {_activeUIStack.Count})");
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
        /// 현재 열려 있는 UI가 있는지 여부
        /// </summary>
        public bool HasActiveUI => _activeUIStack.Count > 0;
    }
}
