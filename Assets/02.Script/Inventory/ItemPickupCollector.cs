using UnityEngine;

public class ItemPickupCollector : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;

    // ItemCatalogManager는 DontDestroyOnLoad 싱글톤이므로 Instance로 직접 참조
    private ItemCatalogManager Catalog => ItemCatalogManager.Instance;

    private void Awake()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
    }

    private void OnTriggerEnter(Collider other)
    {
        WorldItem worldItem = other.GetComponent<WorldItem>();
        if (worldItem == null) return;

        if (playerInventory == null)
        {
            Debug.LogWarning("[ItemPickupCollector] PlayerInventory null");
            return;
        }

        if (Catalog == null)
        {
            Debug.LogWarning("[ItemPickupCollector] ItemCatalogManager.Instance null");
            return;
        }

        string normalizedId = NormalizeItemId(worldItem.itemID);
        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogWarning($"[ItemPickupCollector] itemID가 비어있음. object={worldItem.gameObject.name}");
            return;
        }

        if (!Catalog.IsRegistered(normalizedId))
        {
            Debug.LogWarning($"[ItemPickupCollector] 미등록 아이템 id={normalizedId}, object={worldItem.gameObject.name}");
            return;
        }

        int requestedAmount = Mathf.Max(1, worldItem.amount);
        if (!playerInventory.TryAddItemsFromPickup(normalizedId, requestedAmount, out int addedAmount))
        {
            Debug.LogWarning($"[ItemPickupCollector] TryAddItemsFromPickup 실패. id={normalizedId}, requested={requestedAmount}, added={addedAmount}");
            return;
        }

        playerInventory.EnqueuePickupMessage(normalizedId, addedAmount);
        worldItem.ReturnOrDestroy();
    }

    private static string NormalizeItemId(string rawItemId)
    {
        return string.IsNullOrWhiteSpace(rawItemId) ? string.Empty : rawItemId.Trim();
    }
}
