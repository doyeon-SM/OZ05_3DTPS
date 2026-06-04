using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 슬롯 UI 한 칸의 표시만 담당합니다. 데이터 변경은 PlayerInventory 쪽입니다.
/// </summary>
public class InventorySlotView : MonoBehaviour
{
    [Header("표시")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject emptyVisual;
    [SerializeField] private GameObject highlighVisual;
    [SerializeField] private Button button;

    public event Action<InventorySlotView> OnSlotClicked;

    private InventorySlotData nowSlot;
    private IItemCatalogReader nowCatalog;

    public InventorySlotData CachedSlot => nowSlot;
    public IItemCatalogReader CachedCatalog => nowCatalog;

    private void Awake()
    {
        if (button == null)
            Debug.Log("[InventorySlotView] button null");
        if (button != null)
            button.onClick.AddListener(HandleClick);
        SetSelected(false);
    }
    /// <summary>
    /// 슬롯 데이터와 카탈로그(아이콘 등)를 반영해 UI를 갱신합니다.
    /// </summary>
    /// <param name="slot">표시할 슬롯 상태</param>
    /// <param name="catalogReader">카탈로그 조회용(null이면 아이콘 없음)</param>
    public void Bind(InventorySlotData slot, IItemCatalogReader catalogReader)
    {
        if (slot.IsEmpty)
        {
            SetEmptyVisual();
            return;
        }

        if (emptyVisual != null)
        {
            emptyVisual.SetActive(false);
        }

        nowSlot = slot;
        nowCatalog = catalogReader;

        ItemCatalogEntry entry = default;
        bool hasCatalogEntry = catalogReader != null && catalogReader.TryGetEntry(slot.itemId, out entry);

        if (iconImage != null)
        {
            //Sprite iconSprite = hasCatalogEntry ? entry.icon : null;
            //iconImage.enabled = iconSprite != null;
            //iconImage.sprite = iconSprite;

            if (hasCatalogEntry)
            {
                Color tint = entry.iconTint.a < 0.01f ? Color.white : entry.iconTint;
                iconImage.color = tint;
            }
            else
            {
                iconImage.color = Color.white;
            }
        }

        if (amountText != null)
        {
            amountText.gameObject.SetActive(true);
            amountText.text = slot.amount > 1 ? slot.amount.ToString() : string.Empty;
        }
        if(button != null)
        {
            button.gameObject.SetActive(hasCatalogEntry);
        }
    }

    private void HandleClick()
    {
        //Debug.Log($"[InventorySlotView] Clicked slot {this}");
        OnSlotClicked?.Invoke(this);
    }
    public void SetSelected(bool isSelected)
    {
        if(highlighVisual != null)
        {
            highlighVisual.SetActive(isSelected);
        }
    }

    /// <summary>빈 슬롯일 때 아이콘·개수를 숨기고 빈 칸 표시만 켭니다.</summary>
    private void SetEmptyVisual()
    {
        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        if (amountText != null)
        {
            amountText.text = string.Empty;
            amountText.gameObject.SetActive(false);
        }

        if (emptyVisual != null)
        {
            emptyVisual.SetActive(true);
        }
        if(highlighVisual != null)
        {
            highlighVisual.SetActive(false);
        }
        if(button != null)
        {
            button.gameObject.SetActive(false);
        }
    }
}