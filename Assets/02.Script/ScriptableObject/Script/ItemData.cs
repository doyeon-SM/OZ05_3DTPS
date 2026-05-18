using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    [CreateAssetMenu(fileName = "Item_New", menuName = "ItemData")]
    public class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        public string itemId;
        public string itemName;
        public ItemType itemType;
        public Sprite icon;
        
        [TextArea]
        public string description;
        
        [Header("경제 정보")]
        public int buyPrice;
        public int sellPrice;
        
        [Header("사용 여부")]
        public bool canUse;
        public bool canStack;
        public int maxStackCount = 99;
        
        [Header("사용 효과")]
        public List<ItemEffect> effects = new List<ItemEffect>();
    }    
    
}
