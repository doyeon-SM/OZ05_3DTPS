using _00.ChoiHeesu._03.WeaponChangeSystem;
using _02.Script.Combat;
using StarterAssets;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProjectSpedex
{
    public class RadialMenuRouter : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private WeaponRuntimeManager weaponRuntimeManager;
        [SerializeField] private RadialMenu radialMenu;
        [SerializeField] private StarterAssetsInputs starterAssetsInputs;
        [SerializeField] private GameObject radialMenuRoot;

        [Header("Open Close")]
        [SerializeField] private bool startClosed = true;
        [SerializeField] private bool closeOnButtonClicked = true;

        [Header("Button Event")]
        [SerializeField] private SingleStringEventChannel selectedWeaponIDEventChannel;
        [SerializeField] private UnityEvent<string> onWeaponSelected = new UnityEvent<string>();

        [Header("Locked Color")]
        [SerializeField] private Color unLockedDisabledColor = Color.gray;
        [SerializeField] private Color unLockedElementColor = Color.gray;

        [Header("Unlocked Button Color")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = Color.white;
        [SerializeField] private Color selectedColor = Color.white;

        [Header("Unlocked Element Color")]
        [SerializeField] private Color interactableElementColor = Color.white;

        private readonly Dictionary<Button, UnityAction> registeredButtonActions = new Dictionary<Button, UnityAction>();
        private bool missingStarterAssetsInputsLogged;

        private void Awake()
        {
            CacheReferences();

            if (startClosed)
                SetRadialMenuActive(false);
        }

        private void OnEnable()
        {
            CacheReferences();
            RefreshRadialMenu();
        }

        private void Update()
        {
            HandleWeaponSelectInput();
        }

        private void OnDisable()
        {
            ClearButtonListeners();
        }

        private void CacheReferences()
        {
            if (radialMenu == null)
                TryGetComponent(out radialMenu);

            if (radialMenuRoot == null && radialMenu != null)
                radialMenuRoot = radialMenu.gameObject;
        }

        private void HandleWeaponSelectInput()
        {
            if (starterAssetsInputs == null)
            {
                ReportMissingStarterAssetsInputs();
                return;
            }

            if (!starterAssetsInputs.WeaponSelect)
                return;

            ToggleRadialMenu();
            starterAssetsInputs.WeaponSelect = false;
        }

        public void ToggleRadialMenu()
        {
            if (radialMenuRoot == null)
            {
                Debug.LogError("[RadialMenuRouter] radialMenuRoot가 null입니다. 켜고 끌 Weapon Radial Menu UI Root를 Inspector에 연결해주세요.", this);
                return;
            }

            SetRadialMenuActive(!radialMenuRoot.activeSelf);
        }

        public void OpenRadialMenu()
        {
            SetRadialMenuActive(true);
        }

        public void CloseRadialMenu()
        {
            SetRadialMenuActive(false);
        }

        private void SetRadialMenuActive(bool isActive)
        {
            if (radialMenuRoot == null)
                return;

            if (!isActive && radialMenuRoot == gameObject)
            {
                Debug.LogError("[RadialMenuRouter] radialMenuRoot가 RadialMenuRouter와 같은 GameObject입니다. 이 오브젝트를 끄면 Q 입력으로 다시 열 수 없습니다. Router는 항상 켜진 부모/관리 오브젝트에 두고, radialMenuRoot에는 실제 메뉴 UI 자식을 연결해주세요.", this);
                return;
            }

            radialMenuRoot.SetActive(isActive);

            if (isActive)
                RefreshRadialMenu();
        }

        public void RefreshRadialMenu()
        {
            if (!CanRefresh())
                return;

            WeaponRuntime[] weaponRuntimes = weaponRuntimeManager.WeaponRuntimes;

            for (int i = 0; i < radialMenu.elements.Count; i++)
            {
                RadialMenuElement element = radialMenu.elements[i];
                WeaponRuntime runtime = i < weaponRuntimes.Length ? weaponRuntimes[i] : null;
                RefreshElement(i, element, runtime);
            }
        }

        private bool CanRefresh()
        {
            if (weaponRuntimeManager == null)
            {
                Debug.LogError("[RadialMenuRouter] weaponRuntimeManager가 null입니다. Inspector에 WeaponRuntimeManager를 연결해주세요.", this);
                return false;
            }

            if (radialMenu == null)
            {
                Debug.LogError("[RadialMenuRouter] radialMenu가 null입니다. 같은 오브젝트에 RadialMenu가 없다면 Inspector에 연결해주세요.", this);
                return false;
            }

            if (radialMenu.elements == null)
            {
                Debug.LogError("[RadialMenuRouter] radialMenu.elements가 null입니다. RadialMenu의 Elements 목록을 확인해주세요.", this);
                return false;
            }

            if (weaponRuntimeManager.WeaponRuntimes == null)
            {
                Debug.LogError("[RadialMenuRouter] WeaponRuntimeManager.WeaponRuntimes가 null입니다. WeaponRuntimeManager 설정을 확인해주세요.", this);
                return false;
            }

            return true;
        }

        private void RefreshElement(int index, RadialMenuElement element, WeaponRuntime runtime)
        {
            if (element == null)
            {
                Debug.LogError($"[RadialMenuRouter] RadialMenu Elements[{index}]가 null입니다. RadialMenu의 Elements 목록을 확인해주세요.", this);
                return;
            }

            if (element.button == null)
            {
                Debug.LogError($"[RadialMenuRouter] {element.gameObject.name}의 Button이 null입니다. RadialMenuElement.button을 연결해주세요.", element);
                return;
            }

            if (runtime == null || runtime.data == null)
            {
                element.label = string.Empty;
                element.ItemID = string.Empty;
                SetElementIcon(element, null);
                SetElementInteractable(element, false);
                SetElementText(element, string.Empty, unLockedElementColor);
                SetElementVisualColor(element, unLockedElementColor);
                RegisterButtonListener(element);
                return;
            }

            WeaponData weaponData = runtime.data;

            element.label = weaponData.WeaponName;
            element.ItemID = weaponData.WeaponId;
            SetElementIcon(element, weaponData.UnLockIcon);
            SetElementText(element, weaponData.WeaponName, runtime.UnLocked ? interactableElementColor : unLockedElementColor);
            SetElementVisualColor(element, runtime.UnLocked ? interactableElementColor : unLockedElementColor);
            SetElementInteractable(element, runtime.UnLocked);
            RegisterButtonListener(element);
        }

        private void SetElementIcon(RadialMenuElement element, Sprite icon)
        {
            if (element.Icon == null)
            {
                Debug.LogError($"[RadialMenuRouter] {element.gameObject.name}의 Icon Image가 null입니다. RadialMenuElement.Icon을 연결해주세요.", element);
                return;
            }

            element.Icon.sprite = icon;
            element.Icon.enabled = icon != null;
        }

        private void SetElementText(RadialMenuElement element, string text, Color color)
        {
            Text uiText = GetElementText(element);
            if (uiText != null)
            {
                uiText.text = text;
                uiText.color = color;
                return;
            }

            TMP_Text tmpText = GetElementTMPText(element);
            if (tmpText != null)
            {
                tmpText.text = text;
                tmpText.color = color;
            }
        }

        private void SetElementVisualColor(RadialMenuElement element, Color color)
        {
            if (element.Icon != null)
                element.Icon.color = color;
        }

        private void SetElementInteractable(RadialMenuElement element, bool isUnlocked)
        {
            element.button.interactable = isUnlocked;

            ColorBlock colorBlock = element.button.colors;
            colorBlock.disabledColor = unLockedDisabledColor;

            if (isUnlocked)
            {
                colorBlock.normalColor = normalColor;
                colorBlock.pressedColor = pressedColor;
                colorBlock.selectedColor = selectedColor;
            }

            element.button.colors = colorBlock;
        }

        private Text GetElementText(RadialMenuElement element)
        {
            if (element.button == null)
                return null;

            return element.button.GetComponentInChildren<Text>(true);
        }

        private TMP_Text GetElementTMPText(RadialMenuElement element)
        {
            if (element.button == null)
                return null;

            return element.button.GetComponentInChildren<TMP_Text>(true);
        }

        private void RegisterButtonListener(RadialMenuElement element)
        {
            if (element == null || element.button == null)
                return;

            if (registeredButtonActions.TryGetValue(element.button, out UnityAction previousAction))
                element.button.onClick.RemoveListener(previousAction);

            UnityAction action = () => HandleElementButtonClicked(element);
            registeredButtonActions[element.button] = action;
            element.button.onClick.AddListener(action);
        }

        private void ClearButtonListeners()
        {
            foreach (KeyValuePair<Button, UnityAction> buttonAction in registeredButtonActions)
            {
                if (buttonAction.Key != null)
                    buttonAction.Key.onClick.RemoveListener(buttonAction.Value);
            }

            registeredButtonActions.Clear();
        }

        private void HandleElementButtonClicked(RadialMenuElement element)
        {
            if (element == null)
                return;

            if (string.IsNullOrWhiteSpace(element.ItemID))
            {
                Debug.LogError($"[RadialMenuRouter] {element.gameObject.name}의 ItemID가 비어 있어 무기 선택 이벤트를 보낼 수 없습니다.", element);
                return;
            }

            if (selectedWeaponIDEventChannel != null)
                selectedWeaponIDEventChannel.Raise(element.ItemID);

            Debug.Log($"[RadialMenuRouter] 선택된 WeaponID: {element.ItemID}", element);
            onWeaponSelected.Invoke(element.ItemID);

            if (closeOnButtonClicked)
                CloseRadialMenu();
        }

        private void ReportMissingStarterAssetsInputs()
        {
            if (missingStarterAssetsInputsLogged)
                return;

            Debug.LogError("[RadialMenuRouter] starterAssetsInputs가 null입니다. Q키 WeaponSelect 입력을 받으려면 Player의 StarterAssetsInputs를 Inspector에 연결해주세요.", this);
            missingStarterAssetsInputsLogged = true;
        }
    }
}
