using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("ItemCatalog")]
    [SerializeField] private ItemCatalogManager itemCatalogManager;

    [Header("Runtime")]
    public List<string> itemIDs = new List<string>();
    public Dictionary<string, int> itemCountById = new Dictionary<string, int>();

    private void Awake()
    {
        EnsureCatalogReference();
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    public bool TryAddItems(string itemId, int amount)
    {
        return TryAddItemsInternal(itemId, amount, out _);
    }

    public bool TryAddItemsFromPickup(string itemId, int amount, out int addedAmount)
    {
        return TryAddItemsInternal(itemId, amount, out addedAmount);
    }

    public void EnqueuePickupMessage(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return;
        string displayName = ResolveDisplayName(itemId.Trim());
        Debug.Log($"[PlayerInventory] {displayName} 획득 x{amount}");
    }

    public bool TryRemoveItems(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return false;

        itemId = itemId.Trim();
        if (GetItemCount(itemId) < amount) return false;

        RemoveFromItemIdList(itemId, amount);
        DecreaseItemCount(itemId, amount);
        return true;
    }

    public bool HasAtLeast(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return amount == 0;
        return GetItemCount(itemId.Trim()) >= amount;
    }

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return 0;
        itemCountById.TryGetValue(itemId.Trim(), out int count);
        return count;
    }

    public bool TryGetCatalogEntry(string itemId, out ItemCatalogEntry entry)
    {
        entry = default;
        return itemCatalogManager != null && itemCatalogManager.TryGetEntry(itemId, out entry);
    }

    // ──────────────────────────────────────────────
    // Internal
    // ──────────────────────────────────────────────

    private bool TryAddItemsInternal(string itemId, int amount, out int addedAmount)
    {
        addedAmount = 0;
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return false;

        itemId = itemId.Trim();
        if (!IsRegisteredItemId(itemId))
        {
            Debug.LogWarning("[PlayerInventory] 미등록 아이템 id: " + itemId);
            return false;
        }

        for (int i = 0; i < amount; i++)
            itemIDs.Add(itemId);

        IncreaseItemCount(itemId, amount);
        addedAmount = amount;
        return true;
    }

    private void IncreaseItemCount(string itemId, int amount)
    {
        if (itemCountById.TryGetValue(itemId, out int current))
            itemCountById[itemId] = current + amount;
        else
            itemCountById[itemId] = amount;
    }

    private void DecreaseItemCount(string itemId, int amount)
    {
        if (!itemCountById.TryGetValue(itemId, out int current)) return;
        int next = current - amount;
        if (next <= 0) itemCountById.Remove(itemId);
        else itemCountById[itemId] = next;
    }

    private void RemoveFromItemIdList(string itemId, int amount)
    {
        int removed = 0;
        for (int i = itemIDs.Count - 1; i >= 0 && removed < amount; i--)
        {
            if (itemIDs[i] == itemId)
            {
                itemIDs.RemoveAt(i);
                removed++;
            }
        }
    }

    private bool IsRegisteredItemId(string itemId)
    {
        return itemCatalogManager != null && itemCatalogManager.IsRegistered(itemId);
    }

    private string ResolveDisplayName(string itemId)
    {
        return itemCatalogManager != null ? itemCatalogManager.ResolveDisplayName(itemId) : itemId;
    }

    private void EnsureCatalogReference()
    {
        if (itemCatalogManager == null)
            itemCatalogManager = FindFirstObjectByType<ItemCatalogManager>();

        if (itemCatalogManager == null)
            Debug.LogWarning("[PlayerInventory] ItemCatalogManager를 찾을 수 없습니다.");
    }
}
