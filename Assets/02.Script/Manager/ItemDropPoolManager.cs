using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// WorldItem 오브젝트 풀링 매니저.
    /// itemId별로 전용 프리팩을 사용해 3D 모델이 다른 WorldItem을 종류별 10개씩 예열한다.
    /// id를 통한 관리 방식은 유지하며, 프리팩이 없는 id는 defaultPrefab을 사용한다.
    /// </summary>
    public class ItemDropPoolManager : MonoBehaviour
    {
        public static ItemDropPoolManager Instance { get; private set; }

        [Header("기본 풀 설정")]
        [Tooltip("ItemCatalogManager에 전용 프리팩이 없는 id에 사용할 팔백 WorldItem 프리팩")]
        [SerializeField] private WorldItem defaultPrefab;

        [Tooltip("종류별 미리 생성해둔 WorldItem 수 (각 id마다)")]
        [SerializeField] private int prewarmCountPerType = 10;

        [Tooltip("풀링 오브젝트 부모 Transform (씨넜 정리용)")]
        [SerializeField] private Transform poolRoot;

        [Header("아이템 카탈로그 참조")]
        [Tooltip("id별 프리팩을 가져오기 위한 ItemCatalogManager")]
        [SerializeField] private ItemCatalogManager itemCatalogManager;

        // id -> 풀 (Queue)
        private readonly Dictionary<string, Queue<WorldItem>> pools = new();
        // 없는 id에 사용할 빈 풀 보호용
        private Queue<WorldItem> defaultPool = new();

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

            if (itemCatalogManager == null)
                itemCatalogManager = FindFirstObjectByType<ItemCatalogManager>();

            Prewarm();
        }

        /// <summary>
        /// ItemCatalogManager의 모든 entry를 순회하면서
        /// 전용 프리팩이 있는 id는 전용 풀, 없는 id는 defaultPool로 관리한다.
        /// </summary>
        private void Prewarm()
        {
            // defaultPrefab 풀 예열
            if (defaultPrefab == null)
            {
                Debug.LogWarning("[ItemDropPoolManager] defaultPrefab이 설정되지 않았습니다.");
            }
            else
            {
                for (int i = 0; i < prewarmCountPerType; i++)
                    ReturnToPool(null, CreateItem(defaultPrefab)); // id 없이 defaultPool에 넣음
            }

            if (itemCatalogManager == null) return;

            var entries = itemCatalogManager.GetAllEntries();
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.id)) continue;
                if (entry.worldItemPrefab == null) continue; // 프리팩 없으면 전용 풀 안 만듦

                string id = entry.id.Trim();
                if (!pools.ContainsKey(id))
                    pools[id] = new Queue<WorldItem>();

                for (int i = 0; i < prewarmCountPerType; i++)
                    pools[id].Enqueue(CreateItem(entry.worldItemPrefab));
            }
        }

        /// <summary>
        /// 지정 위치에 아이템을 스폰한다.
        /// itemId에 대응하는 전용 풀이 있으면 전용 풀에서, 없으면 defaultPool에서 꼼낸다.
        /// </summary>
        public WorldItem Spawn(string itemId, string displayName, int amount, Vector3 position)
        {
            WorldItem item = null;

            if (!string.IsNullOrWhiteSpace(itemId) && pools.TryGetValue(itemId.Trim(), out var typedPool))
            {
                // 전용 풀: 비어있으면 전용 프리팩으로 새로 생성
                if (typedPool.Count > 0)
                    item = typedPool.Dequeue();
                else if (itemCatalogManager != null &&
                         itemCatalogManager.TryGetEntry(itemId, out var entry) &&
                         entry.worldItemPrefab != null)
                    item = CreateItem(entry.worldItemPrefab);
            }

            // 전용 풀 없으면 defaultPool 사용
            if (item == null)
            {
                item = defaultPool.Count > 0
                    ? defaultPool.Dequeue()
                    : (defaultPrefab != null ? CreateItem(defaultPrefab) : null);
            }

            if (item == null)
            {
                Debug.LogError("[ItemDropPoolManager] 스폰 실패: 사용 가능한 프리팩이 없습니다.");
                return null;
            }

            item.itemID = itemId;
            item.itemDisplayName = displayName;
            item.amount = amount;

            item.transform.SetParent(null);
            item.transform.position = position;
            item.gameObject.SetActive(true);

            return item;
        }

        /// <summary>
        /// WorldItem을 풀에 반환한다. (줄이기 완료 또는 시간 만료 시 호출)
        /// </summary>
        public void ReturnToPool(WorldItem item)
        {
            if (item == null) return;

            item.gameObject.SetActive(false);
            item.transform.SetParent(poolRoot);

            // id에 맞는 전용 풀이 있으면 거기로, 없으면 defaultPool로
            string id = item.itemID;
            if (!string.IsNullOrWhiteSpace(id) && pools.TryGetValue(id.Trim(), out var typedPool))
                typedPool.Enqueue(item);
            else
                defaultPool.Enqueue(item);
        }

        // Prewarm 전용 오버로드: 비활성 상태로 바로 defaultPool에 넣음
        private void ReturnToPool(string _, WorldItem item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            item.transform.SetParent(poolRoot);
            defaultPool.Enqueue(item);
        }

        private WorldItem CreateItem(WorldItem prefab)
        {
            WorldItem item = Instantiate(prefab, poolRoot);
            item.gameObject.SetActive(false);
            return item;
        }
    }
}

