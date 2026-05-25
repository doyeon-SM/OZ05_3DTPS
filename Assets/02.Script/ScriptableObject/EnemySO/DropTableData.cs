using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    [Serializable]
    public class DropEntry
    {
        [Tooltip("ItemCatalogManager의 id와 일치해야 합니다.")]
        public string itemId;

        [Range(0f, 100f)]
        [Tooltip("드랍 확률 (0~100%)")]
        public float dropChance = 50f;

        [Tooltip("드랍 수량 최솟값")]
        public int minAmount = 1;

        [Tooltip("드랍 수량 최댓값")]
        public int maxAmount = 1;
    }

    [CreateAssetMenu(fileName = "DropTableData", menuName = "Stage/Drop Table Data")]
    public class DropTableData : ScriptableObject
    {
        [Header("드랍 목록")]
        [Tooltip("각 항목은 독립적으로 확률 판정됩니다. 여러 아이템이 동시에 드랍될 수 있습니다.")]
        public DropEntry[] entries;

        /// <summary>
        /// 확률 판정을 수행하고 드랍할 항목 목록을 반환한다.
        /// </summary>
        public System.Collections.Generic.List<(string itemId, int amount)> RollDrops()
        {
            var result = new System.Collections.Generic.List<(string, int)>();

            if (entries == null) return result;

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.itemId)) continue;

                float roll = UnityEngine.Random.Range(0f, 100f);
                if (roll <= entry.dropChance)
                {
                    int amount = UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1);
                    result.Add((entry.itemId.Trim(), amount));
                }
            }

            return result;
        }
    }
}
