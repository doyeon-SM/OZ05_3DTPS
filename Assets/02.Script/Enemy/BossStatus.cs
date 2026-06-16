using System;
using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 전용 Status — EnemyStatus를 상속하여 TakeDamage/Die override.
    /// BossData SO에서 maxHealth/attackPower를 초기화합니다.
    /// HP 변경 시 OnHPChanged 이벤트를 발행하여 BossHUDManager가 구독합니다.
    ///
    /// [무적]
    ///  - IsInvincible이 true인 동안 TakeDamage()는 즉시 무시됩니다.
    ///  - 분기패턴(2페이즈 전환 패턴) 진행 중 BossController가 이 플래그를 제어합니다.
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

        /// <summary>true인 동안 TakeDamage가 무시됩니다 (분기패턴 등 무적 구간용).</summary>
        public bool IsInvincible { get; set; }

        private bool _isDead;

        protected override void OnEnable()
        {
            _isDead = false;
            IsInvincible = false;
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
        /// IsInvincible이 true이면 데미지를 무시합니다.
        /// </summary>
        public override void TakeDamage(int value)
        {
            if (_isDead) return;
            if (IsInvincible) return;

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
