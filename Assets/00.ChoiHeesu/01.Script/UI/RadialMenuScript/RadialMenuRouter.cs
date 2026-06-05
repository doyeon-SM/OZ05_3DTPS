using _00.ChoiHeesu._03.WeaponChangeSystem;
using _02.Script.Combat;
using StarterAssets;
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
        [SerializeField] private float selectionDeadZone = 0f;

        [Header("Button Event")]
        [SerializeField] private SingleStringEventChannel selectedWeaponIDEventChannel;
        [SerializeField] private UnityEvent<string> onWeaponSelected = new UnityEvent<string>();

        [Header("Locked Color")]
        [SerializeField] private Color unLockedDisabledColor = Color.gray;
        [SerializeField] private Color unLockedElementColor = Color.gray;

        [Header("Unlocked Button Color")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = Color.white;
        [Header("Selected Button Color by Bullet Condition")]
        [SerializeField] private Color selectHasAmmoColor = Color.white;
        [SerializeField] private Color notAmmoSelectedColor = Color.red;

        [Header("Unlocked Element Color")]
        [SerializeField] private Color interactableElementColor = Color.white;

        private bool missingStarterAssetsInputsLogged;
        private bool wasWeaponSelectHeld;

        private void Awake()
        {
            CacheReferences();
            SyncWeaponSelectState();

            if (startClosed)
                SetRadialMenuActive(false);
        }

        private void OnEnable()
        {
            CacheReferences();
            SyncWeaponSelectState();
            RefreshRadialMenu();
            SetLookInputBlocked(radialMenuRoot != null && radialMenuRoot.activeSelf);
        }

        private void Update()
        {
            HandleWeaponSelectInput();
        }

        private void OnDisable()
        {
            SetLookInputBlocked(false);
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

            bool isWeaponSelectHeld = starterAssetsInputs.WeaponSelect;
            bool weaponSelectPressed = starterAssetsInputs.WeaponSelectPressed || (isWeaponSelectHeld && !wasWeaponSelectHeld);
            bool weaponSelectReleased = starterAssetsInputs.WeaponSelectReleased || (!isWeaponSelectHeld && wasWeaponSelectHeld);

            if (weaponSelectPressed)
            {
                OpenRadialMenu();
            }

            if (weaponSelectReleased)
            {
                SubmitCurrentSelection();
            }

            wasWeaponSelectHeld = isWeaponSelectHeld;
            starterAssetsInputs.ConsumeWeaponSelectInput();
        }

        private void SyncWeaponSelectState()
        {
            wasWeaponSelectHeld = starterAssetsInputs != null && starterAssetsInputs.WeaponSelect;
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
            {
                if (!isActive)
                    SetLookInputBlocked(false);

                return;
            }

            if (!isActive && radialMenuRoot == gameObject)
            {
                SetLookInputBlocked(false);
                Debug.LogError("[RadialMenuRouter] radialMenuRoot가 RadialMenuRouter와 같은 GameObject입니다. 이 오브젝트를 끄면 Q 입력으로 다시 열 수 없습니다. Router는 항상 켜진 부모/관리 오브젝트에 두고, radialMenuRoot에는 실제 메뉴 UI 자식을 연결해주세요.", this);
                return;
            }

            radialMenuRoot.SetActive(isActive);
            SetLookInputBlocked(isActive);

            if (isActive)
                RefreshRadialMenu();
        }

        private void SetLookInputBlocked(bool isBlocked)
        {
            if (starterAssetsInputs == null)
                return;

            starterAssetsInputs.SetLookInputBlocked(isBlocked);
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
                SetElementInteractable(element, false, false);
                SetElementText(element, string.Empty, unLockedElementColor);
                SetElementAmmoText(element, string.Empty, unLockedElementColor);
                SetElementVisualColor(element, unLockedElementColor);
                return;
            }

            WeaponData weaponData = runtime.data;
            bool hasAmmo = HasSelectableAmmo(runtime);

            element.label = weaponData.WeaponName;
            element.ItemID = weaponData.WeaponId;
            SetElementIcon(element, weaponData.UnLockIcon);
            SetElementText(element, weaponData.WeaponName, runtime.UnLocked ? interactableElementColor : unLockedElementColor);
            SetElementAmmoText(element, GetAmmoPrint(weaponData), runtime.UnLocked ? interactableElementColor : unLockedElementColor);
            SetElementVisualColor(element, runtime.UnLocked ? interactableElementColor : unLockedElementColor);
            SetElementInteractable(element, runtime.UnLocked, hasAmmo);
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

        private void SetElementAmmoText(RadialMenuElement element, string ammoPrint, Color color)
        {
            if (element.AmmoText == null)
                return;

            element.AmmoText.text = ammoPrint;
            element.AmmoText.color = color;
        }

        private string GetAmmoPrint(WeaponData weaponData)
        {
            if (weaponData == null)
                return string.Empty;

            int ammo = 0;
            if (weaponRuntimeManager != null)
                weaponRuntimeManager.TryGetWeaponAmmo(weaponData.WeaponType, out ammo);

            string ammoPrint = "Ammo";
            ammoPrint += "\n";
            ammoPrint += ammo.ToString();
            return ammoPrint;
        }

        private void SetElementVisualColor(RadialMenuElement element, Color color)
        {
            if (element.Icon != null)
                element.Icon.color = color;
        }

        private void SetElementInteractable(RadialMenuElement element, bool isUnlocked, bool hasAmmo)
        {
            element.button.interactable = isUnlocked;

            ColorBlock colorBlock = element.button.colors;
            colorBlock.disabledColor = unLockedDisabledColor;

            if (isUnlocked)
            {
                colorBlock.normalColor = normalColor;
                colorBlock.pressedColor = pressedColor;
                colorBlock.selectedColor = hasAmmo ? selectHasAmmoColor : notAmmoSelectedColor;
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

        private void SubmitCurrentSelection()
        {
            if (radialMenuRoot == null || !radialMenuRoot.activeSelf)
                return;

            if (IsPointerInDeadZone())
            {
                CloseRadialMenu();
                return;
            }

            if (!TryGetCurrentSelectedElement(out RadialMenuElement selectedElement))
            {
                CloseRadialMenu();
                return;
            }

            HandleElementButtonClicked(selectedElement);
        }

        private bool IsPointerInDeadZone()
        {
            return selectionDeadZone > 0f && radialMenu != null && radialMenu.CurrentPointerDistance <= selectionDeadZone;
        }

        private bool TryGetCurrentSelectedElement(out RadialMenuElement selectedElement)
        {
            selectedElement = null;

            if (radialMenu == null || radialMenu.elements == null)
                return false;

            int selectedIndex = radialMenu.index;
            if (selectedIndex < 0 || selectedIndex >= radialMenu.elements.Count)
                return false;

            selectedElement = radialMenu.elements[selectedIndex];
            return selectedElement != null;
        }

        private void HandleElementButtonClicked(RadialMenuElement element)
        {
            if (element == null)
                return;

            if (string.IsNullOrWhiteSpace(element.ItemID))
            {
                Debug.LogError($"[RadialMenuRouter] {element.gameObject.name}의 ItemID가 비어 있어 무기 선택 이벤트를 보낼 수 없습니다.", element);
                CloseRadialMenuAfterSelection();
                return;
            }

            if (!CanSendWeaponSelectEvent(element, out string blockMessage))
            {
                // TODO : 추후 삭제할것
                Debug.LogError(blockMessage, element);
                CloseRadialMenuAfterSelection();
                return;
            }

            if (selectedWeaponIDEventChannel != null)
                selectedWeaponIDEventChannel.Raise(element.ItemID);

            Debug.Log($"[RadialMenuRouter] 선택된 WeaponID: {element.ItemID}", element);
            onWeaponSelected.Invoke(element.ItemID);

            CloseRadialMenuAfterSelection();
        }

        private void CloseRadialMenuAfterSelection()
        {
            if (closeOnButtonClicked)
                CloseRadialMenu();
        }

        private bool CanSendWeaponSelectEvent(RadialMenuElement element, out string blockMessage)
        {
            blockMessage = string.Empty;

            if (!TryGetWeaponRuntime(element.ItemID, out WeaponRuntime runtime))
            {
                blockMessage = $"[RadialMenuRouter] WeaponID '{element.ItemID}'와 일치하는 WeaponRuntime을 찾을 수 없어 이벤트 발신을 막았습니다.";
                return false;
            }

            if (!runtime.UnLocked)
            {
                blockMessage = $"[RadialMenuRouter] WeaponID '{element.ItemID}'는 아직 언락되지 않아 이벤트 발신을 막았습니다.";
                return false;
            }

            if (!HasSelectableAmmo(runtime))
            {
                blockMessage = $"[RadialMenuRouter] WeaponID '{element.ItemID}'는 사용할 Ammo가 없어 이벤트 발신을 막았습니다.";
                return false;
            }

            return true;
        }

        private bool HasSelectableAmmo(WeaponRuntime runtime)
        {
            if (runtime == null || runtime.data == null)
                return false;

            if (!runtime.data.UseAmmo)
                return true;

            if (weaponRuntimeManager == null)
                return false;

            return weaponRuntimeManager.TryGetWeaponAmmo(runtime.data.WeaponType, out int ammo) && ammo > 0;
        }

        private bool TryGetWeaponRuntime(string itemId, out WeaponRuntime foundRuntime)
        {
            foundRuntime = null;

            if (weaponRuntimeManager == null || weaponRuntimeManager.WeaponRuntimes == null || string.IsNullOrWhiteSpace(itemId))
                return false;

            string normalizedItemId = itemId.Trim();
            WeaponRuntime[] weaponRuntimes = weaponRuntimeManager.WeaponRuntimes;

            for (int i = 0; i < weaponRuntimes.Length; i++)
            {
                WeaponRuntime runtime = weaponRuntimes[i];

                if (runtime == null || runtime.data == null || string.IsNullOrWhiteSpace(runtime.data.WeaponId))
                    continue;

                if (runtime.data.WeaponId.Trim() != normalizedItemId)
                    continue;

                foundRuntime = runtime;
                return true;
            }

            return false;
        }

        private void ReportMissingStarterAssetsInputs()
        {
            if (missingStarterAssetsInputsLogged)
                return;

            Debug.LogError("[RadialMenuRouter] starterAssetsInputs가 null입니다. WeaponSelect 입력을 받으려면 Player의 StarterAssetsInputs를 Inspector에 연결해주세요.", this);
            missingStarterAssetsInputsLogged = true;
        }
    }
}
