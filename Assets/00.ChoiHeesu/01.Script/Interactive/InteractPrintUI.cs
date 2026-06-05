using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00.ChoiHeesu._02.RayCastInteract
{
    public class InteractPrintUI : MonoBehaviour
    {
        [Header("UI Text")]
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text itemDescript;

        [Header("UI Image")]
        [SerializeField] private Image itemIcon;

        [Header("자식 오브젝트 이름 , 만약 null이라면 해당 string으로 찾아서 부착합니다.")]
        public string itemNameObjectName = "ItemName";
        public string itemDescriptObjectName = "ItemDescript";
        public string itemIconObjectName = "ItemIcon";

        private bool missingItemNameLogged;
        private bool missingItemDescriptLogged;
        private bool missingItemIconLogged;

        private void Awake()
        {
            CacheReferences();
            ValidateReferences();
            Hide();
        }

        public void Show(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogError("[InteractPrintUI] Show에 전달된 WeaponData가 null입니다. RaycastInteractor 또는 InteractableWeaponItem의 데이터 연결을 확인하세요.", this);
                Hide();
                return;
            }

            CacheReferences();
            ValidateReferences();

            if (itemName != null)
                itemName.text = weaponData.WeaponName;

            if (itemDescript != null)
                itemDescript.text = weaponData.WeaponDescription;

            if (itemIcon != null)
            {
                itemIcon.sprite = weaponData.UnLockIcon;
                itemIcon.enabled = weaponData.UnLockIcon != null;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void CacheReferences()
        {
            if (itemName == null)
                itemName = FindChildComponentByName<TMP_Text>(itemNameObjectName);

            if (itemDescript == null)
                itemDescript = FindChildComponentByName<TMP_Text>(itemDescriptObjectName);

            if (itemIcon == null)
                itemIcon = FindChildComponentByName<Image>(itemIconObjectName);
        }

        private void ValidateReferences()
        {
            if (itemName == null)
                ReportMissingReference(nameof(itemName), itemNameObjectName, "TMP_Text");

            if (itemDescript == null)
                ReportMissingReference(nameof(itemDescript), itemDescriptObjectName, "TMP_Text");

            if (itemIcon == null)
                ReportMissingReference(nameof(itemIcon), itemIconObjectName, "Image");
        }

        private T FindChildComponentByName<T>(string childName) where T : Component
        {
            if (string.IsNullOrWhiteSpace(childName))
                return null;

            Transform child = FindChildByName(transform, childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private Transform FindChildByName(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform foundChild = FindChildByName(child, childName);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }

        private void ReportMissingReference(string fieldName, string childName, string componentName)
        {
            if (fieldName == nameof(itemName))
            {
                if (missingItemNameLogged)
                    return;

                missingItemNameLogged = true;
            }
            else if (fieldName == nameof(itemDescript))
            {
                if (missingItemDescriptLogged)
                    return;

                missingItemDescriptLogged = true;
            }
            else if (fieldName == nameof(itemIcon))
            {
                if (missingItemIconLogged)
                    return;

                missingItemIconLogged = true;
            }

            Debug.LogError($"[InteractPrintUI] {fieldName}이 null입니다. Inspector에 직접 연결하거나, 자식 오브젝트 이름 '{childName}'에 {componentName} 컴포넌트가 있는지 확인하세요.", this);
        }
    }
}
