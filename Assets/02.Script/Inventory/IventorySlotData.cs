using System;
using UnityEngine;

/// <summary>
/// 슬롯 인벤 UI와 동기화되는 한칸의 데이터
/// 빈 슬롯은 itemID가 비었거나 amount 가 0 이하인 경우로 판정 ~ 
/// </summary>
/// 

[Serializable]
public struct InventorySlotData
{
    public string itemId;
    public int amount;

    // 비어있으면 
    public bool IsEmpty =>
        string.IsNullOrEmpty(itemId) || amount <= 0;
    // 빈 슬롯은 itemID가 비었거나 amount 가 0 이하인 경우로 판정
}
