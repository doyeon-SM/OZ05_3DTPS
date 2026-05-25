using System.Collections.Generic;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 씬 시작 시 모든 적을 풀링하여 관리하는 매니저.
    /// SectorBase에서 적 소환/반환 요청을 보내면 이 매니저가 처리한다.
    /// </summary>
    public class EnemyPoolManager : MonoBehaviour
    {
        public static EnemyPoolManager Instance { get; private set; }

        [Tooltip("풀링된 오브젝트의 부모 Transform (씬 정리용)")]
        [SerializeField] private Transform poolRoot;

        // key: EnemyData, value: 해당 타입의 오브젝트 풀
        private Dictionary<EnemyData, Queue<EnemyStatus>> pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (poolRoot == null)
                poolRoot = new GameObject("EnemyPool").transform;
        }

        /// <summary>
        /// 씬 시작 시 SectorBase에서 호출 — 필요한 적을 미리 풀에 생성해둔다.
        /// </summary>
        public void PrewarmPool(EnemyData data, int count)
        {
            if (data == null || data.prefab == null) return;

            if (!pools.ContainsKey(data))
                pools[data] = new Queue<EnemyStatus>();

            for (int i = 0; i < count; i++)
            {
                EnemyStatus enemy = CreateEnemy(data);
                ReturnToPool(enemy);
            }
        }

        /// <summary>
        /// 풀에서 적을 꺼내 지정 위치/회전으로 소환한다.
        /// </summary>
        public EnemyStatus Spawn(EnemyData data, Vector3 position, Quaternion rotation)
        {
            if (data == null || data.prefab == null) return null;

            if (!pools.ContainsKey(data) || pools[data].Count == 0)
            {
                EnemyStatus newEnemy = CreateEnemy(data);
                ActivateEnemy(newEnemy, data, position, rotation);
                return newEnemy;
            }

            EnemyStatus enemy = pools[data].Dequeue();
            ActivateEnemy(enemy, data, position, rotation);
            return enemy;
        }

        /// <summary>
        /// 적을 비활성화하여 풀에 반환한다.
        /// </summary>
        public void ReturnToPool(EnemyStatus enemy)
        {
            if (enemy == null) return;

            // Data가 없으면 풀에 넣지 못하므로 단순 비활성화만 처리
            if (enemy.Data == null)
            {
                Debug.LogWarning($"[EnemyPoolManager] {enemy.name}의 EnemyData가 null입니다. 풀에 반환하지 않고 비활성화만 처리합니다.");
                enemy.gameObject.SetActive(false);
                return;
            }

            enemy.gameObject.SetActive(false);
            enemy.transform.SetParent(poolRoot);

            if (!pools.ContainsKey(enemy.Data))
                pools[enemy.Data] = new Queue<EnemyStatus>();

            pools[enemy.Data].Enqueue(enemy);
        }

        private EnemyStatus CreateEnemy(EnemyData data)
        {
            GameObject obj = Instantiate(data.prefab, poolRoot);
            obj.SetActive(false); // 생성 직후 비활성화 상태로 풀에 대기

            EnemyStatus status = obj.GetComponent<EnemyStatus>();
            if (status == null)
                status = obj.AddComponent<EnemyStatus>();

            return status;
        }

        private void ActivateEnemy(EnemyStatus enemy, EnemyData data, Vector3 position, Quaternion rotation)
        {
            enemy.transform.SetParent(null);
            enemy.transform.SetPositionAndRotation(position, rotation);
            enemy.gameObject.SetActive(true);  // OnEnable → ResetHealth 호출됨
            enemy.Initialize(data);             // data 확정 주입 (OnEnable 이후)
        }
    }
}
