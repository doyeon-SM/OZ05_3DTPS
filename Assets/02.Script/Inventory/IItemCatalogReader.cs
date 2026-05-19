using UnityEngine;

public interface IItemCatalogReader
{
    bool TryGetEntry(string itemId, out ItemCatalogEntry entry);
    bool IsRegistered(string itemId);
    int GetMaxStack(string itemId);
    string ResolveDisplayName(string itemId);
}
