using System;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00.ChoiHeesu._07.UIController
{
    /// <summary>
    /// 개요:
    /// - Player UI가 많아졌을 때 각 UI 패널의 활성화/비활성화를 한 곳에서 관리하기 위한 총괄 컨트롤러입니다.
    /// - 이 스크립트가 붙은 GameObject의 자식 UI 오브젝트를 검사해서 관리 목록에 등록합니다.
    /// - StarterAssetsInputs에 모아둔 입력 값을 읽어서 각 UI를 키거나 끕니다.
    /// - UI가 켜질 때는 열린 순서를 기록하고, ESC 입력 시 가장 마지막에 열린 UI부터 닫습니다.
    /// - 구상 단계에서는 Queue라고 표현했지만, ESC로 "최근에 연 UI부터 닫기" 위해 실제 동작은 Stack 방식(LIFO)에 가깝게 관리합니다.
    ///
    /// 코드 동작:
    /// 1. Awake에서 Inspector에 등록된 UI와 자식 UI를 uiEntries에 등록합니다.
    /// 2. 필요하다면 시작 시 등록된 UI를 모두 SetActive(false)로 닫습니다.
    /// 3. Update에서 UI별 Toggle 입력과 UIClose 입력을 검사합니다.
    /// 4. OpenUI는 UI를 SetActive(true)로 켜고 activeUiStack에 기록합니다.
    /// 5. CloseUI는 UI를 SetActive(false)로 끄고 activeUiStack에서 제거합니다.
    /// 6. CloseTopUI는 activeUiStack의 마지막 UI를 꺼내 닫습니다.
    ///
    /// 추후 수정 방향:
    /// - UI별 Cursor 처리, Time.timeScale 정지, Player 입력 잠금, UI 애니메이션 재생 등을 UIEntry에 옵션으로 추가할 수 있습니다.
    /// - 현재는 GameObject.SetActive 중심으로 동작하므로 CanvasGroup fade 연출이 필요하면 OpenUI/CloseUI 내부만 확장하면 됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIController : MonoBehaviour
    {
        private enum UIInputType
        {
            None,
            Inventory
        }

        [Serializable]
        private class UIEntry
        {
            [Header("UI")]
            [SerializeField] private string uiId;
            [SerializeField] private GameObject uiRoot;

            [Header("Input")]
            [SerializeField] private UIInputType toggleInput = UIInputType.None;

            public string UIId => uiId;
            public GameObject UIRoot => uiRoot;
            public UIInputType ToggleInput => toggleInput;

            public void SetRoot(GameObject root)
            {
                uiRoot = root;
                SetDefaultIdIfEmpty();
            }

            public void SetDefaultIdIfEmpty()
            {
                if (string.IsNullOrWhiteSpace(uiId) && uiRoot != null)
                    uiId = uiRoot.name;
            }
        }

        [Header("Input")]
        [SerializeField] private StarterAssetsInputs starterAssetsInputs;
        [SerializeField] private bool autoFindStarterAssetsInputs = true;
        [SerializeField] private bool allowSceneSearchForStarterAssetsInputs = true;

        [Header("Registry")]
        [SerializeField] private List<UIEntry> uiEntries = new List<UIEntry>();
        [SerializeField] private bool registerChildrenOnAwake = true;
        [SerializeField] private bool registerOnlyDirectChildren = true;
        [SerializeField] private bool startWithAllRegisteredUIClosed = true;

        [Header("Debug")]
        [SerializeField] private bool logMissingReference;

        private readonly List<GameObject> activeUiStack = new List<GameObject>();
        private readonly HashSet<GameObject> registeredUiSet = new HashSet<GameObject>();
        private bool missingStarterAssetsInputsErrorLogged;
        private bool hasSearchedSceneForStarterAssetsInputs;

        public int ActiveUICount => activeUiStack.Count;
        public bool HasActiveUI => activeUiStack.Count > 0;

        private void Awake()
        {
            CacheReferences(false);
            RegisterInspectorEntries();

            if (registerChildrenOnAwake)
                RegisterChildUIObjects();

            if (startWithAllRegisteredUIClosed)
                CloseAllUI();
            else
                SyncActiveStackFromCurrentState();
        }

        private void Start()
        {
            CacheReferences(true);
            ReportMissingStarterAssetsInputsIfNeeded();
        }

        private void Update()
        {
            RemoveExternallyClosedUIFromStack();
            HandleToggleInputs();
            HandleCloseTopInput();
        }

        public void RegisterUI(GameObject uiRoot)
        {
            if (uiRoot == null || registeredUiSet.Contains(uiRoot))
                return;

            UIEntry entry = new UIEntry();
            uiEntries.Add(entry);

            entry.SetRoot(uiRoot);
            RegisterEntry(entry);
        }

        public void OpenUI(string uiId)
        {
            UIEntry entry = FindEntry(uiId);
            if (entry != null)
                OpenUI(entry.UIRoot);
        }

        public void OpenUI(GameObject uiRoot)
        {
            if (!CanUseUI(uiRoot))
                return;

            uiRoot.SetActive(true);
            MoveToStackTop(uiRoot);
        }

        public void CloseUI(string uiId)
        {
            UIEntry entry = FindEntry(uiId);
            if (entry != null)
                CloseUI(entry.UIRoot);
        }

        public void CloseUI(GameObject uiRoot)
        {
            if (uiRoot == null)
                return;

            uiRoot.SetActive(false);
            activeUiStack.Remove(uiRoot);
        }

        public void ToggleUI(string uiId)
        {
            UIEntry entry = FindEntry(uiId);
            if (entry != null)
                ToggleUI(entry.UIRoot);
        }

        public void ToggleUI(GameObject uiRoot)
        {
            if (!CanUseUI(uiRoot))
                return;

            if (uiRoot.activeSelf)
                CloseUI(uiRoot);
            else
                OpenUI(uiRoot);
        }

        public void CloseTopUI()
        {
            RemoveExternallyClosedUIFromStack();

            if (activeUiStack.Count <= 0)
                return;

            GameObject topUI = activeUiStack[activeUiStack.Count - 1];
            CloseUI(topUI);
        }

        public void CloseAllUI()
        {
            for (int i = 0; i < uiEntries.Count; i++)
            {
                GameObject uiRoot = uiEntries[i] != null ? uiEntries[i].UIRoot : null;
                if (uiRoot != null)
                    uiRoot.SetActive(false);
            }

            activeUiStack.Clear();
        }

        public bool IsUIActive(string uiId)
        {
            UIEntry entry = FindEntry(uiId);
            return entry != null && entry.UIRoot != null && entry.UIRoot.activeSelf;
        }

        public bool IsUIActive(GameObject uiRoot)
        {
            return uiRoot != null && uiRoot.activeSelf;
        }

        private void RegisterInspectorEntries()
        {
            registeredUiSet.Clear();

            for (int i = uiEntries.Count - 1; i >= 0; i--)
            {
                UIEntry entry = uiEntries[i];
                if (entry == null || entry.UIRoot == null)
                {
                    if (logMissingReference)
                        Debug.LogWarning("[UIController] 비어있는 UIEntry가 있어 관리 목록에서 제외됩니다.", this);

                    uiEntries.RemoveAt(i);
                    continue;
                }

                RegisterEntry(entry);
            }
        }

        private void RegisterChildUIObjects()
        {
            if (registerOnlyDirectChildren)
            {
                for (int i = 0; i < transform.childCount; i++)
                    AddEntryIfMissing(transform.GetChild(i).gameObject);

                return;
            }

            Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < childTransforms.Length; i++)
            {
                Transform child = childTransforms[i];
                if (child == transform)
                    continue;

                AddEntryIfMissing(child.gameObject);
            }
        }

        private void AddEntryIfMissing(GameObject uiRoot)
        {
            if (uiRoot == null || registeredUiSet.Contains(uiRoot))
                return;

            UIEntry entry = new UIEntry();
            entry.SetRoot(uiRoot);
            uiEntries.Add(entry);
            RegisterEntry(entry);
        }

        private void RegisterEntry(UIEntry entry)
        {
            if (entry == null || entry.UIRoot == null)
                return;

            entry.SetDefaultIdIfEmpty();
            registeredUiSet.Add(entry.UIRoot);
        }

        private void SyncActiveStackFromCurrentState()
        {
            activeUiStack.Clear();

            for (int i = 0; i < uiEntries.Count; i++)
            {
                GameObject uiRoot = uiEntries[i] != null ? uiEntries[i].UIRoot : null;
                if (uiRoot != null && uiRoot.activeSelf)
                    activeUiStack.Add(uiRoot);
            }
        }

        private void HandleToggleInputs()
        {
            if (starterAssetsInputs == null)
            {
                ReportMissingStarterAssetsInputsIfNeeded();
                return;
            }

            bool consumedInventoryInput = false;

            for (int i = 0; i < uiEntries.Count; i++)
            {
                UIEntry entry = uiEntries[i];
                if (entry == null || entry.UIRoot == null || !IsToggleInputPressed(entry.ToggleInput))
                    continue;

                ToggleUI(entry.UIRoot);

                if (entry.ToggleInput == UIInputType.Inventory)
                    consumedInventoryInput = true;
            }

            if (consumedInventoryInput)
                starterAssetsInputs.InventoryInput(false);
        }

        private void HandleCloseTopInput()
        {
            if (starterAssetsInputs == null)
            {
                ReportMissingStarterAssetsInputsIfNeeded();
                return;
            }

            if (!starterAssetsInputs.UIClose)
                return;

            CloseTopUI();
            starterAssetsInputs.UICloseInput(false);
        }

        private void CacheReferences(bool includeSceneSearch)
        {
            if (starterAssetsInputs != null || !autoFindStarterAssetsInputs)
                return;

            if (TryGetComponent(out starterAssetsInputs))
                return;

            starterAssetsInputs = GetComponentInParent<StarterAssetsInputs>();
            if (starterAssetsInputs != null)
                return;

            starterAssetsInputs = GetComponentInChildren<StarterAssetsInputs>(true);
            if (starterAssetsInputs != null)
                return;

            Transform root = transform.root;
            if (root != null && root != transform)
                starterAssetsInputs = root.GetComponentInChildren<StarterAssetsInputs>(true);

            if (starterAssetsInputs != null ||
                !includeSceneSearch ||
                !allowSceneSearchForStarterAssetsInputs ||
                hasSearchedSceneForStarterAssetsInputs)
            {
                return;
            }

            hasSearchedSceneForStarterAssetsInputs = true;
            starterAssetsInputs = FindStarterAssetsInputsInLoadedScenes();
        }

        private StarterAssetsInputs FindStarterAssetsInputsInLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                for (int j = 0; j < rootObjects.Length; j++)
                {
                    StarterAssetsInputs foundInputs = rootObjects[j].GetComponentInChildren<StarterAssetsInputs>(true);
                    if (foundInputs != null)
                        return foundInputs;
                }
            }

            return null;
        }

        private void MoveToStackTop(GameObject uiRoot)
        {
            activeUiStack.Remove(uiRoot);
            activeUiStack.Add(uiRoot);
        }

        private void RemoveExternallyClosedUIFromStack()
        {
            for (int i = activeUiStack.Count - 1; i >= 0; i--)
            {
                GameObject uiRoot = activeUiStack[i];
                if (uiRoot == null || !uiRoot.activeSelf)
                    activeUiStack.RemoveAt(i);
            }
        }

        private bool CanUseUI(GameObject uiRoot)
        {
            if (uiRoot != null)
                return true;

            if (logMissingReference)
                Debug.LogWarning("[UIController] 관리 대상 UI가 null입니다.", this);

            return false;
        }

        private UIEntry FindEntry(string uiId)
        {
            if (string.IsNullOrWhiteSpace(uiId))
                return null;

            for (int i = 0; i < uiEntries.Count; i++)
            {
                UIEntry entry = uiEntries[i];
                if (entry != null && string.Equals(entry.UIId, uiId, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        private bool IsToggleInputPressed(UIInputType inputType)
        {
            if (starterAssetsInputs == null)
                return false;

            switch (inputType)
            {
                case UIInputType.Inventory:
                    return starterAssetsInputs.Inventory;
                default:
                    return false;
            }
        }

        private void ReportMissingStarterAssetsInputsIfNeeded()
        {
            if (starterAssetsInputs != null || missingStarterAssetsInputsErrorLogged)
                return;

            Debug.LogError("[UIController] StarterAssetsInputs를 찾을 수 없습니다. UIController가 UI 입력을 받으려면 씬에 Player의 StarterAssetsInputs가 1개 존재해야 합니다.", this);
            missingStarterAssetsInputsErrorLogged = true;
        }
    }
}
