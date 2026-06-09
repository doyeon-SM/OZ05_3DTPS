using UnityEngine;
using System;
using System.Collections.Generic;

public class ItemCatalogManager : MonoBehaviour, IItemCatalogReader
{
    [Header("Item Catalog (단일 등록표)")]
    [Tooltip("비어 있으면 샘플용 기본 행이 런타임에만 채워집니다. 씬 저장 시 Inspector에 직접 넣는 것을 권장합니다.")]
    [SerializeField] private ItemCatalogEntry[] itemCatalogEntries;

    private static ItemCatalogManager instance = null;

    private readonly Dictionary<string, ItemCatalogEntry> catalogById = new Dictionary<string, ItemCatalogEntry>();

    private void Awake()
    {
        EnsureCatalogNotEmptyForRuntime();
        BuildCatalogDictionary();

        if(null == instance)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static ItemCatalogManager Instance
    {
        get { if (null == instance) return null; return instance; }
    }
    public bool TryGetEntry(string itemId, out ItemCatalogEntry entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        return catalogById.TryGetValue(itemId.Trim(), out entry);
    }
    public bool IsRegistered(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && catalogById.ContainsKey(itemId);
    }
    public int GetMaxStack(string itemId)
    {
        if (catalogById.TryGetValue(itemId, out ItemCatalogEntry entry))
        {
            if (entry.maxStack <= 0)
                return int.MaxValue;

            return entry.maxStack;
        }
        return 0;
    }
    public string ResolveDisplayName(string itemId)
    {
        if (TryGetEntry(itemId, out ItemCatalogEntry entry) && !string.IsNullOrEmpty(entry.displayName))
            return entry.displayName;

        return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
    }
    private void EnsureCatalogNotEmptyForRuntime()
    {
        if (itemCatalogEntries != null && itemCatalogEntries.Length > 0)
        {
            return;
        }

        /*itemCatalogEntries = new[]
        {
            new ItemCatalogEntry { id = "potion", displayName = "포션", maxStack = 99, isUse = false, icon = null, iconTint = Color.white },
            new ItemCatalogEntry { id = "key_red", displayName = "빨간 열쇠", maxStack = 99, isUse = false, icon = null, iconTint = Color.white },
            new ItemCatalogEntry { id = "key_blue", displayName = "파란 열쇠", maxStack = 99, isUse = false, icon = null, iconTint = Color.white },
            new ItemCatalogEntry { id = "key_green", displayName = "초록 열쇠", maxStack = 99, isUse = false, icon = null, iconTint = Color.white },
            new ItemCatalogEntry { id = "gold", displayName = "골드", maxStack = 9999, isUse = false, icon = null, iconTint = Color.white },
        };*/
        Debug.LogWarning("[PlayerInventory] itemCatalogEntries가 비어 있습니다. ");
    }
    private void BuildCatalogDictionary()
    {
        catalogById.Clear();
        if (itemCatalogEntries == null)
            return;

        for (int i = 0; i < itemCatalogEntries.Length; i++)
        {
            ItemCatalogEntry entry = itemCatalogEntries[i];
            if (string.IsNullOrWhiteSpace(entry.id))
            {
                Debug.LogWarning($"[ItemCatalogManager] Null ID Entry. index={i}");
                continue;
            }

            string normalizedId = entry.id.Trim();
            if (catalogById.ContainsKey(normalizedId))
            {
                Debug.LogWarning($"[ItemCatalogManager] ContainsKey ignore: {normalizedId}");
                continue;
            }

            ItemCatalogEntry stored = entry;
            stored.id = normalizedId;
            catalogById.Add(normalizedId, stored);
        }
    }
    //random entry bool try
    public bool TryGetRandomEntry(out ItemCatalogEntry entry)
    {
        entry = default;

        if (itemCatalogEntries == null || itemCatalogEntries.Length == 0)
            return false;

        int randomIndex = UnityEngine.Random.Range(0, itemCatalogEntries.Length);
        entry = itemCatalogEntries[randomIndex];

        if (string.IsNullOrWhiteSpace(entry.id))
            return false;

        return catalogById.TryGetValue(entry.id.Trim(), out entry);
    }

    public ItemCatalogEntry[] GetAllEntries()
    {
        return itemCatalogEntries;
    }
}
