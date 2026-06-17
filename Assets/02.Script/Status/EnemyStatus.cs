using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;

namespace _01.Scenes.PhaseValidation
{
    public class EnemyStatus : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData enemyData;

        [SerializeField] private int currentHealth;
        private bool isDead;

        // 섹터 매니저에서 구독 — 풀 반환 처리 위임
        public event Action<EnemyStatus> OnDied;

        public int AttackPower  => enemyData != null ? enemyData.attackPower  : 0;
        public float RespawnDelay => enemyData != null ? enemyData.respawnDelay : 5f;
        public EnemyData Data   => enemyData;

        private NavMeshAgent _navAgent;
        private BehaviorGraphAgent _behaviorAgent;
        private bool _aiComponentsCached;

        private void CacheAIComponents()
        {
            if (_aiComponentsCached) return;
            _navAgent = GetComponent<NavMeshAgent>();
            _behaviorAgent = GetComponent<BehaviorGraphAgent>();
            _aiComponentsCached = true;
        }

        /// <summary>
        /// 적의 AI(추적/행동)를 켜고 끈다. false면 NavMeshAgent와 BehaviorGraphAgent를 모두 정지시켜
        /// 화면에는 보이지만 가만히 서 있는(미리 소환된 대기) 상태가 된다.
        /// </summary>
        public void SetAIActive(bool active)
        {
            CacheAIComponents();

            if (_behaviorAgent != null)
                _behaviorAgent.enabled = active;

            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = !active;
                if (!active)
                    _navAgent.ResetPath();
            }
        }

        protected virtual void OnEnable()
        {
            isDead = false;
            ResetHealth();
        }

        public void Initialize(EnemyData data)
        {
            enemyData = data;
            isDead = false;
            ResetHealth();
        }

        private void ResetHealth()
        {
            currentHealth = enemyData != null ? enemyData.maxHealth : 100;
        }

        public virtual void TakeDamage(int value)
        {
            if (isDead) return;

            currentHealth -= value;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log($"{gameObject.name}이 죽었습니다.");

            StageManager.Instance?.OnEnemyDied(transform.position);
            OnDied?.Invoke(this);
        }
    }
}
