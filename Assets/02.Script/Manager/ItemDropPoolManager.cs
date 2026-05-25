using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// WorldItem 오브젝트 풀링 매니저.
    /// 아이템 드랍 시 Instantiate 대신 풀에서 꺼내고,
    /// 플레이어가 줍거나 일정 시간 후 풀에 반환한다.
    /// </summary>
    public class ItemDropPoolManager : MonoBehaviour
    {
        public static ItemDropPoolManager Instance { get; private set; }

        [Header("풀 설정")]
        [Tooltip("WorldItem 프리팹 (WorldItem 컴포넌트 필수)")]
        [SerializeField] private WorldItem worldItemPrefab;

        [Tooltip("미리 생성해둘 WorldItem 수")]
        [SerializeField] private int prewarmCount = 20;

        [Tooltip("풀링 오브젝트 부모 Transform (씬 정리용)")]
        [SerializeField] private Transform poolRoot;

        private Queue<WorldItem> pool = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (poolRoot == null)
                poolRoot = new GameObject("ItemDropPool").transform;

            Prewarm();
        }

        private void Prewarm()
        {
            if (worldItemPrefab == null)
            {
                Debug.LogWarning("[ItemDropPoolManager] WorldItem 프리팹이 설정되지 않았습니다.");
                return;
            }

            for (int i = 0; i < prewarmCount; i++)
                ReturnToPool(CreateItem());
        }

        /// <summary>
        /// 지정 위치에 아이템을 스폰한다.
        /// </summary>
        public WorldItem Spawn(string itemId, string displayName, int amount, Vector3 position)
        {
            WorldItem item = pool.Count > 0 ? pool.Dequeue() : CreateItem();

            item.itemID = itemId;
            item.itemDisplayName = displayName;
            item.amount = amount;

            item.transform.SetParent(null);
            item.transform.position = position;
            item.gameObject.SetActive(true);

            return item;
        }

        /// <summary>
        /// WorldItem을 풀에 반환한다. (줍기 완료 또는 시간 만료 시 호출)
        /// </summary>
        public void ReturnToPool(WorldItem item)
        {
            if (item == null) return;

            item.gameObject.SetActive(false);
            item.transform.SetParent(poolRoot);
            pool.Enqueue(item);
        }

        private WorldItem CreateItem()
        {
            WorldItem item = Instantiate(worldItemPrefab, poolRoot);
            item.gameObject.SetActive(false);
            return item;
        }
    }
}
