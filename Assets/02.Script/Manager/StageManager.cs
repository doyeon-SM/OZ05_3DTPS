using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 스테이지 전반을 관리하는 매니저.
    /// - 공용 드랍 테이블 보유
    /// - EnemyStatus.OnDied 이벤트를 받아 아이템 드랍 처리
    /// - 이후 보스 도전 해금 등 스테이지 흐름 관리 예정
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("스테이지 공용 드랍 테이블")]
        [SerializeField] private DropTableData dropTable;

        //[Header("참조")]
        //[SerializeField] private ItemCatalogManager itemCatalogManager;

        [Header("드랍 연출")]
        [Tooltip("드랍 아이템이 적 위치에서 위로 튀어오를 오프셋")]
        [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.5f, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            //if (itemCatalogManager == null)
            //   itemCatalogManager = FindFirstObjectByType<ItemCatalogManager>();
        }

        /// <summary>
        /// EnemyPoolManager 또는 SectorBase에서 적 사망 시 호출.
        /// 드랍 테이블을 굴려 아이템을 월드에 스폰한다.
        /// </summary>
        public void OnEnemyDied(Vector3 deathPosition)
        {
            if (dropTable == null)
            {
                Debug.LogWarning("[StageManager] DropTableData가 설정되지 않았습니다.");
                return;
            }

            if (ItemDropPoolManager.Instance == null)
            {
                Debug.LogWarning("[StageManager] ItemDropPoolManager.Instance가 없습니다.");
                return;
            }

            var drops = dropTable.RollDrops();

            foreach (var (itemId, amount) in drops)
            {
                // ItemCatalogManager에서 displayName 조회
                string displayName = itemId;
                if (ItemCatalogManager.Instance != null)
                    displayName = ItemCatalogManager.Instance.ResolveDisplayName(itemId);

                // 여러 아이템이 겹치지 않도록 살짝 랜덤 오프셋
                Vector3 scatter = new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    0f,
                    Random.Range(-0.4f, 0.4f)
                );

                Vector3 spawnPos = deathPosition + dropOffset + scatter;
                ItemDropPoolManager.Instance.Spawn(itemId, displayName, amount, spawnPos);

                Debug.Log($"[StageManager] 드랍: {displayName} x{amount} @ {spawnPos}");
            }
        }
    }
}
