using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 전용 Status — EnemyStatus를 상속하여 TakeDamage/Die override.
    /// BossData SO에서 maxHealth/attackPower를 초기화합니다.
    /// HP 변경 시 OnHPChanged 이벤트를 발행하여 BossHUDManager가 구독합니다.
    /// </summary>
    public class BossStatus : EnemyStatus
    {
        [Header("보스 데이터")]
        [SerializeField] private BossData bossData;

        // HP 변경 이벤트 (currentHP, maxHP)
        public event Action<int, int> OnHPChanged;

        // 보스 사망 이벤트
        public event Action OnBossDied;

        public int MaxHP     { get; private set; }
        public int CurrentHP { get; private set; }
        public BossData BossData => bossData;

        private bool _isDead;

        protected override void OnEnable()
        {
            _isDead = false;
            if (bossData != null) InitFromBossData();
        }

        /// <summary>
        /// BossSector에서 호출 — BossData로 초기화합니다.
        /// </summary>
        public void InitializeBoss(BossData data)
        {
            bossData = data;
            _isDead  = false;
            InitFromBossData();
        }

        private void InitFromBossData()
        {
            MaxHP     = bossData.maxHealth;
            CurrentHP = MaxHP;
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }

        /// <summary>
        /// override — 데미지 수신, OnHPChanged 발행, 사망 처리.
        /// </summary>
        public override void TakeDamage(int value)
        {
            if (_isDead) return;

            CurrentHP = Mathf.Max(CurrentHP - value, 0);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            Debug.Log($"[BossStatus] 피해 -{value} | HP {CurrentHP}/{MaxHP}");

            if (CurrentHP <= 0) Die();
        }

        protected override void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("================================================================");
            Debug.Log("[BossStatus] ★ 보스 처치 완료! ★");
            Debug.Log("================================================================");

            BossHUDManager.Instance?.Hide();
            OnBossDied?.Invoke();
            StageManager.Instance?.OnEnemyDied(transform.position);

            Destroy(gameObject);
        }
    }
}
