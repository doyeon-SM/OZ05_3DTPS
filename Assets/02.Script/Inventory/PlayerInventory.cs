using System.Collections.Generic;
using _00.ChoiHeesu._03.WeaponChangeSystem;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    // ItemCatalogManager는 DontDestroyOnLoad 싱글톤이므로 Instance로 직접 참조
    private ItemCatalogManager Catalog => ItemCatalogManager.Instance;

    [Header("Runtime")]
    [SerializeField] private WeaponRuntimeManager weaponRuntimeManager;

    /*
     * 이전 방식: PlayerInventory 내부에서 아이템 ID 리스트와 수량 Dictionary를 직접 관리했습니다.
     * 현재는 픽업한 ItemID/Amount를 WeaponRuntimeManager의 ItemID 기반 Dictionary에 누적합니다.
     *
     * public List<string> itemIDs = new List<string>();
     * public Dictionary<string, int> itemCountById = new Dictionary<string, int>();
     */

    private bool missingWeaponRuntimeManagerLogged;

    private void Awake()
    {
        if (Catalog == null)
            Debug.LogWarning("[PlayerInventory] ItemCatalogManager.Instance가 null입니다.");

        CacheWeaponRuntimeManager(false);
    }

    private void Start()
    {
        if (!CacheWeaponRuntimeManager(false))
            ReportMissingWeaponRuntimeManager();
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

        return CacheWeaponRuntimeManager(true) &&
               weaponRuntimeManager.TryConsumeWeaponAmmo(itemId, amount, out _);
    }

    public bool HasAtLeast(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return amount == 0;
        return GetItemCount(itemId.Trim()) >= amount;
    }

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return 0;
        if (!CacheWeaponRuntimeManager(false)) return 0;

        weaponRuntimeManager.TryGetWeaponAmmo(itemId.Trim(), out int count);
        return count;
    }

    public bool TryGetCatalogEntry(string itemId, out ItemCatalogEntry entry)
    {
        entry = default;
        return Catalog != null && Catalog.TryGetEntry(itemId, out entry);
    }

    // ──────────────────────────────────────────────
    // Internal
    // ──────────────────────────────────────────────

    private bool TryAddItemsInternal(string itemId, int amount, out int addedAmount)
    {
        addedAmount = 0;
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return false;

        itemId = itemId.Trim();

        if (Catalog == null)
        {
            Debug.LogWarning("[PlayerInventory] ItemCatalogManager.Instance가 null — 아이템 추가 불가: " + itemId);
            return false;
        }

        if (!Catalog.IsRegistered(itemId))
        {
            Debug.LogWarning("[PlayerInventory] 미등록 아이템 id: " + itemId);
            return false;
        }

        if (!CacheWeaponRuntimeManager(true))
            return false;

        if (!weaponRuntimeManager.TryIncreaseWeaponAmmo(itemId, amount, out _))
        {
            Debug.LogWarning("[PlayerInventory] WeaponRuntimeManager 아이템 수량 추가 실패: " + itemId);
            return false;
        }

        addedAmount = amount;
        return true;
    }

    /*
     * 이전 방식: PlayerInventory 내부 Dictionary/List를 직접 증가/감소했습니다.
     * 현재는 WeaponRuntimeManager.TryIncreaseWeaponAmmo / TryConsumeWeaponAmmo를 사용합니다.
     *
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
    */

    private string ResolveDisplayName(string itemId)
    {
        return Catalog != null ? Catalog.ResolveDisplayName(itemId) : itemId;
    }

    private bool CacheWeaponRuntimeManager(bool logIfMissing)
    {
        if (weaponRuntimeManager != null)
            return true;

        weaponRuntimeManager = WeaponRuntimeManager.Instance;

        if (weaponRuntimeManager == null)
            weaponRuntimeManager = FindFirstObjectByType<WeaponRuntimeManager>(FindObjectsInactive.Include);

        if (weaponRuntimeManager != null)
        {
            missingWeaponRuntimeManagerLogged = false;
            return true;
        }

        if (logIfMissing)
            ReportMissingWeaponRuntimeManager();

        return false;
    }

    private void ReportMissingWeaponRuntimeManager()
    {
        if (missingWeaponRuntimeManagerLogged)
            return;

        Debug.LogError("[PlayerInventory] WeaponRuntimeManager를 찾을 수 없습니다. 씬에 DontDestroyOnLoad 상태의 WeaponRuntimeManager가 있는지 확인해주세요.", this);
        missingWeaponRuntimeManagerLogged = true;
    }
}
