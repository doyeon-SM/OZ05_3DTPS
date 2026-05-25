using UnityEngine;

public class ItemPickupCollector : MonoBehaviour
{
    [Tooltip("����ǰ ���¸� �ٲ� �κ�. ��� ������ ���� ������Ʈ���� PlayerInventory�� ã���ϴ�.")]
    [SerializeField] private PlayerInventory playerInventory;

    [Tooltip("id��� ����/���� ��Ģ�� ����. ��� ������ ������ ItemCatalogManager�� Find�մϴ�.")]
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
            Debug.LogWarning($"[ItemPickupCollector] ��� �ִ� itemID�� ���� WorldItem�� �����մϴ�. object = {worldItem.gameObject.name}");
            return;
        }
        if (!itemCatalogManager.IsRegistered(normalizedId))
        {
            Debug.LogWarning($"[ItemPickupCollector] īŻ�α׿� ���� itemId�Դϴ�. id={normalizedId}, object={worldItem.gameObject.name}");
            return;
        }

        int requestedAmount = Mathf.Max(1, worldItem.amount);
        if (!playerInventory.TryAddItemsFromPickup(normalizedId, requestedAmount, out int addedAmount))
        {
            Debug.LogWarning($"[ItemPickupCollector] ���� ����(����/���� �ѵ�). id={normalizedId}, requested={requestedAmount},added={addedAmount}");
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
