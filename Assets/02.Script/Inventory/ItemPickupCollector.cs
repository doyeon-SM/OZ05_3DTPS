using UnityEngine;

public class ItemPickupCollector : MonoBehaviour
{
    [Tooltip("소지품 상태를 바꿀 인벤. 비어 있으면 같은 오브젝트에서 PlayerInventory를 찾습니다.")]
    [SerializeField] private PlayerInventory playerInventory;

    [Tooltip("id등록 여부/스택 규칙의 기준. 비어 있으면 씬에서 ItemCatalogManager를 Find합니다.")]
    [SerializeField] private ItemCatalogManager itemCatalogManager;

    private void Awake()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
        if (itemCatalogManager == null)
            itemCatalogManager = GetComponent<ItemCatalogManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        WorldItem worldItem = other.GetComponent<WorldItem>();

        if (worldItem == null) return;
        if(playerInventory == null)
        {
            Debug.LogWarning("[ItemPickupCollector] PlayerInventory null");
            return;
        }
        if (itemCatalogManager == null)
        {
            Debug.LogWarning("[ItemPickupCollector] ItemCatalogManager null");
            return;
        }
        string normalizedId = NormalizeItemId(worldItem.itemID);
        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogWarning($"[ItemPickupCollector] 비어 있는 itemID를 가진 WorldItem을 무시합니다. object = {worldItem.gameObject.name}");
            return;
        }
        if (!itemCatalogManager.IsRegistered(normalizedId))
        {
            Debug.LogWarning($"[ItemPickupCollector] 카탈로그에 없는 itemId입니다. id={normalizedId}, object={worldItem.gameObject.name}");
            return;
        }

        int requestedAmount = Mathf.Max(1, worldItem.amount);
        if (!playerInventory.TryAddItemsFromPickup(normalizedId, requestedAmount, out int addedAmount))
        {
            Debug.LogWarning($"[ItemPickupCollector] 습득 실패(슬롯/스택 한도). id={normalizedId}, requested={requestedAmount},added={addedAmount}");
            return;
        }

        playerInventory.EnqueuePickupMessage(normalizedId, addedAmount);
        Destroy(worldItem.gameObject);
    }

    private static string NormalizeItemId(string rawItemId)
    {
        return string.IsNullOrWhiteSpace(rawItemId) ? string.Empty : rawItemId.Trim();
    }
}
