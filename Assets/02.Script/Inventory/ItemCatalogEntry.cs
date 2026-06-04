using System;
using UnityEngine;

[Serializable]
public struct ItemCatalogEntry
{
    [Tooltip("Inventory Dictionary key. WorldItem.itemId-DoorRequirement.requiredItemId")]
    public string id;
    [Tooltip("획득 메시지 등에 쓰는 표시 이름 ")]
    public string displayName;

    public int maxStack;
    //public ItemType category;

    //public bool isUse;

    //[Tooltip("Inventory Slot UI Icon")]
    //public Sprite icon;
    [Tooltip("Icon Image color")]
    public Color iconTint;
}