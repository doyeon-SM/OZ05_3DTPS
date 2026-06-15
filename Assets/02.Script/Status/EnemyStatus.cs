using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    public class EnemyStatus : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData enemyData;

        [SerializeField] private int currentHealth;
        private bool isDead;

        // 섹터 매니저에서 구독 — 풀 반환 처리 위임
        public event Action<EnemyStatus> OnDied;

        public int AttackPower => enemyData != null ? enemyData.attackPower : 0;
        public float RespawnDelay => enemyData != null ? enemyData.respawnDelay : 5f;
        public EnemyData Data => enemyData;

        private void OnEnable()
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

            // 드랍 처리 — StageManager에 사망 위치 전달
            StageManager.Instance?.OnEnemyDied(transform.position);

            // 풀 반환 처리 — 섹터에 위임
            OnDied?.Invoke(this);
        }
    }
}
