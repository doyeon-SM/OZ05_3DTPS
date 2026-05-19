using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemInfoView : MonoBehaviour
{
    [Header("표시")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject emptyVisual;
    [SerializeField] private Button useButton;

    [Header("Ref")]
    [SerializeField] private PlayerInventory inventory;
    //[SerializeField] private PlayerStat playerStat;

    private InventorySlotData nowSlot;
    private IItemCatalogReader nowCatalog;

    private void Awake()
    {
        if (useButton != null)
            useButton.onClick.AddListener(UseItem);
        SetEmptyVisual();
    }
    public void Bind(InventorySlotData slot, IItemCatalogReader catalogReader)
    {
        if(slot.IsEmpty)
        {
            SetEmptyVisual();
            return;
        }
        if(emptyVisual != null)
        {
            emptyVisual.SetActive(false);
        }
        nowSlot = slot;
        nowCatalog = catalogReader;
        //Debug.Log($"[InventoryItemInfoView] Bind 성공 {nowSlot} | {nowCatalog}");
        ItemCatalogEntry entry = default;
        bool hasCatalogEntry = nowCatalog != null && nowCatalog.TryGetEntry(nowSlot.itemId, out entry);

        if(iconImage != null)
        {
            Sprite iconSprite = hasCatalogEntry ? entry.icon : null;
            iconImage.enabled = iconSprite != null;
            iconImage.sprite = iconSprite;
            if(hasCatalogEntry)
            {
                Color tint = entry.iconTint.a < 0.01f ? Color.white : entry.iconTint;
                iconImage.color = tint;
            }
            else
            {
                iconImage.color = Color.white;
            }
        }
        if(itemNameText != null)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = hasCatalogEntry ? entry.displayName : string.Empty;
        }
        if(amountText != null)
        {
            amountText.gameObject.SetActive(true);

            amountText.text = nowSlot.amount > 0 ? nowSlot.amount.ToString() : string.Empty;
            amountText.text += " / ";
            amountText.text += hasCatalogEntry ? entry.maxStack.ToString() : string.Empty;
        }
        if(useButton != null)
        {
            useButton.gameObject.SetActive(hasCatalogEntry && entry.isUse && inventory != null);
        }
    }

    private void UseItem()
    {
        if(inventory.TryRemoveItems(nowSlot.itemId, 1))
        {
            Debug.Log($"{nowSlot.itemId}을(를) 1개 사용합니다.");
            switch(nowSlot.itemId)
            {
                case "potion":
                    //playerStat.ChangeHP(10);
                    break;
                case "big_potion":
                    //playerStat.ChangeHP(30);
                    break;
                default:
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"아이템 사용에 실패했습니다. {nowSlot.itemId} | {nowSlot.amount} | ");
        }
    }


    private void SetEmptyVisual()
    {
        if(iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
        if(itemNameText != null)
        {
            itemNameText.text = string.Empty;
            itemNameText.gameObject.SetActive(false);
        }
        if(amountText != null)
        {
            amountText.text = string.Empty;
            amountText.gameObject.SetActive(false);
        }
        if(useButton != null)
        {
            useButton.gameObject.SetActive(false);
        }
        if(emptyVisual != null)
        {
            emptyVisual.SetActive(true);
        }
    }
}
