using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("ItemCatalog")]
    [Tooltip("아이템 카탈로그 단일 소스입니다.")]
    [SerializeField] private ItemCatalogManager itemCatalogManager;

    [Header("Slot Grid")]
    [Tooltip("인벤 그리드 칸 수. UI(InventoryGridUI)와 맞출 것.")]
    [SerializeField] private int slotCapacity = 20;

    [Header("Runtime")]
    public List<string> itemIDs = new List<string>();
    //public Queue<string> pickUpMessages = new Queue<string>();
    //public Stack<string> undoStack = new Stack<string>();
    public Dictionary<string, int> itemCountById = new Dictionary<string, int>();

    /// <summary>인벤 데이터가 바뀌었을 때 UI 등이 구독합니다.</summary>
    public event Action InventoryChanged;

    private readonly List<InventorySlotData> inventorySlots = new List<InventorySlotData>();

    /// <summary>그리드 칸 수(UI 프리팹 개수와 동일하게 설정).</summary>
    public int SlotCapacity => slotCapacity;

    /// <summary>슬롯 스냅샷(UI 전체 redraw용). 내부 리스트와 동일 참조이므로 외부에서 수정하지 말 것.</summary>
    public IReadOnlyList<InventorySlotData> InventorySlots => inventorySlots;

    private void Awake()
    {
        EnsureCatalogReference();
        InitializeSlots();
    }

    /// <summary>
    /// UI에서 아이콘·이름 등을 가져올 때 사용합니다. 미등록 id이면 false.
    /// </summary>
    public bool TryGetCatalogEntry(string itemId, out ItemCatalogEntry entry)
    {
        entry = default;
        return itemCatalogManager != null && itemCatalogManager.TryGetEntry(itemId, out entry);
    }
    /// <summary>슬롯 리스트를 비우고 slotCapacity만큼 빈 칸을 만듭니다.</summary>
    private void InitializeSlots()
    {
        inventorySlots.Clear();
        int safeCapacity = Mathf.Max(0, slotCapacity);
        for (int i = 0; i < safeCapacity; i++)
        {
            inventorySlots.Add(new InventorySlotData { itemId = string.Empty, amount = 0 });
        }
    }
    /// <summary>모든 슬롯을 비웁니다.</summary>
    private void ClearAllSlots()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i] = new InventorySlotData { itemId = string.Empty, amount = 0 };
        }
    }
    /// <summary>
    /// 외부에서 아이템을 넣을 때(상점·보상 등). 슬롯·스택 규칙을 반영합니다.
    /// </summary>
    public bool TryAddItems(string itemId, int amount)
    {
        return TryAddItemsInternal(itemId, amount, out _);
    }
    /// <summary>
    /// 월드 습득 전용 추가 API입니다. 성공 시 Undo 기록을 남기고 실제 추가 수량을 반환합니다.
    /// </summary>
    public bool TryAddItemsFromPickup(string itemId, int amount, out int addedAmount)
    {
        return TryAddItemsInternal(itemId, amount, out addedAmount);
    }
    public void EnqueuePickupMessage(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return;
        }

        string normalizedId = itemId.Trim();
        string displayName = ResolveDisplayName(normalizedId);
        //pickUpMessages.Enqueue($"{displayName} 획득 x{amount}");
        Debug.Log($"[Inventory] {displayName} 획득 x{amount}");
    }
    /// <summary>
    /// recordPerUnitForUndo가 true일 때만 Undo 스택에 한 개씩 쌓습니다.
    /// </summary>
    private bool TryAddItemsInternal(string itemId, int amount, out int addedAmount)
    {
        addedAmount = 0;
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return false;
        }

        itemId = itemId.Trim();
        if (!IsRegisteredItemId(itemId))
        {
            Debug.LogWarning("[Inventory] 미등록 id TryAdd: " + itemId);
            return false;
        }

        int roomInSlots = GetTotalRoomForItemInSlots(itemId);
        if (roomInSlots <= 0)
        {
            return false;
        }

        int toAdd = Mathf.Min(amount, roomInSlots);
        if (toAdd <= 0)
        {
            return false;
        }

        int placed = PlaceAmountIntoSlots(itemId, toAdd);
        if (placed <= 0)
        {
            return false;
        }

        if (placed != toAdd)
        {
            Debug.LogWarning($"[Inventory] 슬롯 배치 수가 예상과 다릅니다. 예상={toAdd} 실제={placed}. 이후 로직을 점검하세요.");
        }

        for (int i = 0; i < placed; i++)
        {
            itemIDs.Add(itemId);
        }

        IncreaseItemCount(itemId, placed);
        addedAmount = placed;
        RaiseInventoryChanged();
        return true;
    }
    /// <summary>
    /// 문·퀘스트 등에서 개수를 줄일 때 사용. 슬롯·List·Dictionary를 함께 맞춥니다.
    /// </summary>
    public bool TryRemoveItems(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return false;
        }

        itemId = itemId.Trim();
        if (GetItemCount(itemId) < amount)
        {
            return false;
        }

        RemoveAmountFromSlots(itemId, amount);
        RemoveFromItemIdList(itemId, amount);
        DecreaseItemCount(itemId, amount);
        RaiseInventoryChanged();
        return true;
    }
    public bool HasAtLeast(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return amount == 0;
        }

        return GetItemCount(itemId.Trim()) >= amount;
    }

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        if (itemCountById.TryGetValue(itemId.Trim(), out int count))
        {
            return count;
        }
        return 0;
    }

    /*private void PrintInventory()
    {
        Debug.Log("====[Inventory: List]====");
        for (int i = 0; i < itemIDs.Count; i++)
        {
            Debug.Log(itemIDs[i]);
        }
        Debug.Log($"[Inventory Count] : {itemIDs.Count}");

        Debug.Log("====[Inventory: Dictionary]====");
        foreach (KeyValuePair<string, int> pair in itemCountById)
        {
            Debug.Log($"{pair.Key} : {pair.Value}");
        }

        Debug.Log("====[Inventory: Slots]====");
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlotData slot = inventorySlots[i];
            Debug.Log($"[{i}] {(slot.IsEmpty ? "(empty)" : slot.itemId + " x" + slot.amount)}");
        }
    }*/

    private void RaiseInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }

    /// <summary>해당 id를 넣을 수 있는 전체 여유(모든 슬롯 합산, int 범위로 캡).</summary>
    private int GetTotalRoomForItemInSlots(string itemId)
    {
        int maxStack = GetMaxStackForItem(itemId);
        long room = 0;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlotData slot = inventorySlots[i];
            if (slot.IsEmpty)
            {
                room += maxStack == int.MaxValue ? int.MaxValue : maxStack;
            }
            else if (slot.itemId == itemId)
            {
                long cap = maxStack == int.MaxValue ? int.MaxValue : maxStack;
                room += cap - slot.amount;
            }
        }

        if (room >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)room;
    }
    /// <summary>같은 종류 스택에 남는 칸이 있으면 그 인덱스, 없으면 -1.</summary>
    private int FindFirstStackableSlotIndex(string itemId, int maxStack)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlotData slot = inventorySlots[i];
            if (slot.IsEmpty || slot.itemId != itemId)
            {
                continue;
            }

            if (maxStack == int.MaxValue)
            {
                // 슬롯당 한도가 사실상 무제한일 때도 int 오버플로를 막기 위해 가득 찬 슬롯은 건너뜁니다.
                if (slot.amount < int.MaxValue)
                {
                    return i;
                }

                continue;
            }

            if (slot.amount < maxStack)
            {
                return i;
            }
        }

        return -1;
    }
    private int FindFirstEmptySlotIndex()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>슬롯에 itemId를 최대 amount만큼 채우고, 실제 들어간 개수를 반환합니다.</summary>
    private int PlaceAmountIntoSlots(string itemId, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int maxStack = GetMaxStackForItem(itemId);
        int remaining = amount;
        int totalPlaced = 0;

        while (remaining > 0)
        {
            int slotIndex = FindFirstStackableSlotIndex(itemId, maxStack);
            if (slotIndex < 0)
            {
                slotIndex = FindFirstEmptySlotIndex();
            }

            if (slotIndex < 0)
            {
                break;
            }

            InventorySlotData slot = inventorySlots[slotIndex];
            int currentInSlot = slot.IsEmpty ? 0 : slot.amount;
            int cap = maxStack == int.MaxValue ? int.MaxValue : maxStack;
            long canFitLong = (long)cap - currentInSlot;
            int canFit = canFitLong > int.MaxValue ? int.MaxValue : (int)canFitLong;
            if (canFit <= 0)
            {
                Debug.LogWarning("[Inventory] PlaceAmountIntoSlots: canFit<=0 불일치");
                break;
            }

            int put = Mathf.Min(remaining, canFit);
            slot.itemId = itemId;
            slot.amount = currentInSlot + put;
            inventorySlots[slotIndex] = slot;
            remaining -= put;
            totalPlaced += put;
        }

        return totalPlaced;
    }
    /// <summary>뒤쪽 슬롯부터 itemId를 amount만큼 제거합니다.</summary>
    private void RemoveAmountFromSlots(string itemId, int amount)
    {
        int remaining = amount;
        for (int i = inventorySlots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlotData slot = inventorySlots[i];
            if (slot.IsEmpty || slot.itemId != itemId)
            {
                continue;
            }

            int take = Mathf.Min(slot.amount, remaining);
            slot.amount -= take;
            remaining -= take;
            if (slot.amount <= 0)
            {
                slot.itemId = string.Empty;
                slot.amount = 0;
            }

            inventorySlots[i] = slot;
        }
    }
    private bool IsRegisteredItemId(string targetId)
    {
        return itemCatalogManager != null && itemCatalogManager.IsRegistered(targetId);
    }

    private int GetMaxStackForItem(string itemId)
    {
        return itemCatalogManager != null ? itemCatalogManager.GetMaxStack(itemId) : 0;
    }

    private string ResolveDisplayName(string itemId)
    {
        return itemCatalogManager != null ? itemCatalogManager.ResolveDisplayName(itemId) : itemId;
    }
    private void IncreaseItemCount(string itemId, int amount)
    {
        if (itemCountById.TryGetValue(itemId, out int currentCount))
        {
            itemCountById[itemId] = currentCount + amount;
        }
        else
        {
            itemCountById[itemId] = amount;
        }
    }

    private void DecreaseItemCount(string itemId, int amount)
    {
        if (!itemCountById.TryGetValue(itemId, out int currentCount))
        {
            return;
        }

        int nextCount = currentCount - amount;
        if (nextCount <= 0)
        {
            itemCountById.Remove(itemId);
        }
        else
        {
            itemCountById[itemId] = nextCount;
        }
    }

    /// <summary>
    /// List에서 해당 id를 뒤에서부터 amount개 제거합니다.
    /// </summary>
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
    private void EnsureCatalogReference()
    {
        if (itemCatalogManager == null)
        {
            itemCatalogManager = FindFirstObjectByType<ItemCatalogManager>();
        }

        if (itemCatalogManager == null)
        {
            Debug.LogWarning("[PlayerInventory] ItemCatalogManager 참조가 없습니다. 카탈로그 기반 검증이 실패할 수 있습니다.");
        }
    }
}
