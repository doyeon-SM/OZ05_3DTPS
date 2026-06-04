using System;
using UnityEngine;

[Serializable]
public struct ItemCatalogEntry
{
    [Tooltip("Inventory Dictionary key. WorldItem.itemId-DoorRequirement.requiredItemId")]
    public string id;
    [Tooltip("획득 메시지 등에 표시될 이름")]
    public string displayName;

    public int maxStack;

    [Tooltip("Icon Image color")]
    public Color iconTint;

    [Tooltip("이 아이템 ID에 대응하는 WorldItem 3D 프리팩. null이면 기본 프리팩 사용")]
    public WorldItem worldItemPrefab;
}