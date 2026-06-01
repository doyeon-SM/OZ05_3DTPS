using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

            EnemyStatus enemy;
            if (!pools.ContainsKey(data) || pools[data].Count == 0)
                enemy = CreateEnemy(data);
            else
                enemy = pools[data].Dequeue();

            ActivateEnemy(enemy, data, position, rotation);
            return enemy;
        }

        /// <summary>
        /// 적을 비활성화하여 풀에 반환한다.
        /// NavMeshAgent 경로와 Rigidbody 속도를 초기화한 뒤 풀에 넣는다.
        /// </summary>
        public void ReturnToPool(EnemyStatus enemy)
        {
            if (enemy == null) return;

            if (enemy.Data == null)
            {
                Debug.LogWarning($"[EnemyPoolManager] {enemy.name}의 EnemyData가 null입니다. 풀에 반환하지 않고 비활성화만 처리합니다.");
                enemy.gameObject.SetActive(false);
                return;
            }

            // [Fix] 반환 시 NavMeshAgent 경로 초기화
            // 경로가 남아있으면 재소환 직후 이전 목적지로 이동을 시작하는 문제 방지
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            // [Fix] 반환 시 Rigidbody 속도 초기화
            // Enemy_Ch35 자식에 Rigidbody가 있으므로 자식 포함 검색
            Rigidbody rb = enemy.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
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
            obj.SetActive(false);

            EnemyStatus status = obj.GetComponent<EnemyStatus>();
            if (status == null)
                status = obj.AddComponent<EnemyStatus>();

            return status;
        }

        private void ActivateEnemy(EnemyStatus enemy, EnemyData data, Vector3 position, Quaternion rotation)
        {
            // 루트 transform 설정: SetActive 전에 위치·회전을 확정해야
            // OnEnable/Physics가 올바른 기준으로 초기화됨
            enemy.transform.SetParent(null);
            enemy.transform.SetPositionAndRotation(position, rotation);
            enemy.transform.localScale = data.prefab.transform.localScale;

            // [Fix] Enemy_Ch35(스켈레톤 루트) localRotation 초기화
            // Root Motion OFF 이전에 풀에 반환된 오브젝트의 누적 회전값을 제거
            // 이 값이 남아있으면 CapsuleCollider가 기울어진 채로 활성화됨
            Transform skeletonRoot = enemy.transform.Find("Enemy_Ch35");
            if (skeletonRoot != null)
                skeletonRoot.localRotation = Quaternion.identity;

            enemy.gameObject.SetActive(true);   // OnEnable → ResetHealth
            enemy.Initialize(data);             // data 확정 주입 (OnEnable 이후)

            // [Fix] 재소환 시 NavMeshAgent 상태 초기화
            // SetActive 이후에 호출해야 agent가 활성화된 상태에서 초기화됨
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }
        }
    }
}
