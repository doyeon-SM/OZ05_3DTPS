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
    ///
    /// [사망 연출]
    ///  - HP가 0이 되면 즉시 사라지지 않고 Animator의 Die 트리거를 실행해 사망 애니메이션을 재생한다.
    ///  - 보상 소환/사망 이벤트 발행/오브젝트 파괴는 코드에서 시간을 재서 처리하지 않고,
    ///    사망 애니메이션 클립의 Animation Event에서 AnimEvent_OnDeathComplete()를 호출하는 방식으로 동기화한다.
    ///    (BossEffectController.OnTelegraphSFX_*() 등 기존 Animation Event 연동 패턴과 동일)
    ///  - 폭발 VFX 역시 같은 방식으로, 사망 애니메이션의 Animation Event에서
    ///    BossEffectController.OnDeathExplosionVfx()를 호출하도록 연결한다.
    ///
    /// [보상]
    ///  - 보스 처치 시 StageManager의 확률 기반 랜덤 드랍(OnEnemyDied)을 사용하지 않고,
    ///    BossData.rewardPrefab을 보스 사망 위치에 고정으로 1개 소환합니다.
    /// </summary>
    public class BossStatus : EnemyStatus
    {
        [Header("보스 데이터")]
        [SerializeField] private BossData bossData;

        [Header("애니메이션")]
        [Tooltip("보스 Animator. 비워두면 자동으로 GetComponent 시도. 사망(Die) 트리거 재생에 사용됩니다.")]
        [SerializeField] private Animator animator;

        // ── Animator Trigger 상수 ────────────────────────────
        private const string TriggerDie = "Die";

        // HP 변경 이벤트 (currentHP, maxHP)
        public event Action<int, int> OnHPChanged;

        // 보스 사망 이벤트 — 사망 애니메이션이 끝난 시점(AnimEvent_OnDeathComplete)에 발행된다.
        public event Action OnBossDied;

        public int MaxHP     { get; private set; }
        public int CurrentHP { get; private set; }
        public BossData BossData => bossData;

        /// <summary>true인 동안 TakeDamage가 무시됩니다 (분기패턴 등 무적 구간용).</summary>
        public bool IsInvincible { get; set; }

        private bool _isDead;
        private BossEffectController _effectController;
        private BossController _bossController;
        private BossCinematicController _bossCinematic;

        private void Awake()
        {
            _effectController = GetComponent<BossEffectController>();
            _bossController    = GetComponent<BossController>();
            _bossCinematic     = GetComponent<BossCinematicController>();
            if (animator == null) animator = GetComponent<Animator>();
        }

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

            _effectController?.OnDamageTakenSFX();

            if (CurrentHP <= 0) Die();
        }

        /// <summary>
        /// HP가 0이 되면 호출. 이 시점에는 오브젝트를 바로 파괴하지 않고 사망 애니메이션만 재생한다.
        /// 실제 보상 소환/이벤트 발행/파괴는 AnimEvent_OnDeathComplete()(Animation Event)에서 처리된다.
        /// </summary>
        protected override void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("================================================================");
            Debug.Log("[BossStatus] ★ 보스 처치! 사망 애니메이션을 재생합니다. ★");
            Debug.Log("================================================================");

            BossHUDManager.Instance?.Hide();

            // 공격 패턴/판정을 즉시 중단해서 사망 애니메이션 도중 추가 공격이 나가지 않도록 한다.
            _bossController?.StopAllPatterns();

            // 사망 컷씬 재생 (fire-and-forget — 사망 애니메이션/폭발 VFX와 병렬로 진행)
            _bossCinematic?.TriggerDeathCutscene();

            // 사망 애니메이션 재생. 폭발 VFX 생성과 보상 소환·오브젝트 파괴는
            // 애니메이션의 Animation Event에서 호출되는 메서드들이 담당한다. (아래 AnimEvent_* 메서드 참고)
            if (animator != null)
            {
                animator.SetTrigger(TriggerDie);
            }
            else
            {
                Debug.LogWarning("[BossStatus] animator가 연결되지 않아 사망 애니메이션을 재생할 수 없습니다. 즉시 사망 처리합니다.");
                AnimEvent_OnDeathComplete();
            }
        }

        /// <summary>
        /// [Animation Event 전용] 사망(Die) 애니메이션 클립의 마지막 프레임에 연결해서 호출하세요.
        /// 보상 오브젝트를 소환하고, 보스 사망 이벤트를 발행한 뒤, 오브젝트를 파괴합니다.
        /// </summary>
        public void AnimEvent_OnDeathComplete()
        {
            SpawnRewardObject();
            OnBossDied?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>
        /// BossData.rewardPrefab을 보스 사망 위치에 고정으로 1개 소환한다.
        /// StageManager의 확률 기반 랜덤 드랍은 더 이상 사용하지 않는다.
        /// </summary>
        private void SpawnRewardObject()
        {
            if (bossData == null || bossData.rewardPrefab == null)
            {
                Debug.LogWarning("[BossStatus] BossData.rewardPrefab이 설정되지 않았습니다. 보상이 소환되지 않습니다.");
                return;
            }

            Instantiate(bossData.rewardPrefab, transform.position, Quaternion.identity);
        }
    }
}
