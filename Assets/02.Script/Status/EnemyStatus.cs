using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    public class EnemyStatus : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData enemyData;

        private int currentHealth;
        private bool isDead;

        // 매니저에서 구독 — 적이 죽었을 때 풀 반환 처리를 위임
        public event Action<EnemyStatus> OnDied;

        public int AttackPower => enemyData != null ? enemyData.attackPower : 0;
        public float RespawnDelay => enemyData != null ? enemyData.respawnDelay : 5f;
        public EnemyData Data => enemyData;

        /// <summary>
        /// 풀에서 꺼낼 때(SetActive true → OnEnable) HP와 사망 플래그를 초기화한다.
        /// </summary>
        private void OnEnable()
        {
            isDead = false;
            ResetHealth();
        }

        /// <summary>
        /// 섹터에서 소환 직후 호출 — EnemyData를 주입하고 HP를 초기화한다.
        /// OnEnable 이후에 호출되므로 data가 바뀐 경우 재설정이 필요하다.
        /// </summary>
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

        public void TakeDamage(int value)
        {
            if (isDead) return;

            currentHealth -= value;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log($"{gameObject.name}이 죽었습니다.");
            OnDied?.Invoke(this);
        }
    }
}
