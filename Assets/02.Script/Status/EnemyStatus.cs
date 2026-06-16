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

        public int AttackPower  => enemyData != null ? enemyData.attackPower  : 0;
        public float RespawnDelay => enemyData != null ? enemyData.respawnDelay : 5f;
        public EnemyData Data   => enemyData;

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
