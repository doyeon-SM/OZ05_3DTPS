using UnityEngine;
using _01.Scenes.PhaseValidation.UI;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 룸 섹터 — SectorBase 상속.
    /// 
    /// [흐름]
    ///  1. PlayerMoveInteractionObject로 플레이어가 진입
    ///  2. OnTriggerEnter → StartBattle() 호출
    ///  3. 목표 달성(GoalPercent == 1.0) 확인 후 보스 소환 + HUD 표시
    ///  4. BossStatus.OnBossDied → ClearSector() + BossClearUI.Show()
    /// </summary>
    public class BossSector : SectorBase
    {
        [Header("보스 설정")]
        [SerializeField] private BossData bossData;
        [Tooltip("보스가 소환될 위치")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("보스의 이름 (HUD에 표시됩니다)")]
        [SerializeField] private string bossName = "BOSS";

        private BossStatus _bossInstance;

        // ── SectorBase 추상 메서드 구현 ──────────────────────

        protected override void OnSectorStart()
        {
            // 보스 룸은 씬 로드 시 별도 초기화 불필요
        }

        protected override void StartBattle()
        {
            // 목표 달성 여부 확인
            if (StageManager.Instance == null)
            {
                Debug.LogError("[BossSector] StageManager.Instance가 없습니다.");
                return;
            }

            if (StageManager.Instance.GoalPercent < 1.0f)
            {
                Debug.Log($"[BossSector] 목표 미달성({StageManager.Instance.GoalPercent * 100f:F0}%) — 보스 소환 불가.");
                // 진입 차단: 플레이어를 다시 내보내지 않고 전투만 시작하지 않음
                // (PlayerMoveInteractionObject에서 이미 게이팅했으나 물리 진입 대비)
                return;
            }

            SpawnBoss();
        }

        // ── 보스 소환 ─────────────────────────────────────────

        private void SpawnBoss()
        {
            if (bossData == null || bossData.prefab == null)
            {
                Debug.LogError("[BossSector] BossData 또는 prefab이 설정되지 않았습니다.");
                return;
            }

            Vector3    pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GameObject obj = Instantiate(bossData.prefab, pos, rot);

            _bossInstance = obj.GetComponent<BossStatus>();
            if (_bossInstance == null)
                _bossInstance = obj.AddComponent<BossStatus>();

            _bossInstance.InitializeBoss(bossData);

            // HUD 연결
            if (BossHUDManager.Instance != null)
            {
                BossHUDManager.Instance.Show(bossName, bossData.maxHealth);
                _bossInstance.OnHPChanged += BossHUDManager.Instance.OnHPChanged;
            }
            else
            {
                Debug.LogWarning("[BossSector] BossHUDManager.Instance가 없습니다. HUD를 표시할 수 없습니다.");
            }

            // 사망 연결
            _bossInstance.OnBossDied += HandleBossDied;

            Debug.Log($"[BossSector] 보스 '{bossName}' 소환 완료.");
        }

        // ── 보스 사망 처리 ────────────────────────────────────

        private void HandleBossDied()
        {
            if (_bossInstance != null)
            {
                if (BossHUDManager.Instance != null)
                _bossInstance.OnHPChanged -= BossHUDManager.Instance.OnHPChanged;
                _bossInstance.OnBossDied   -= HandleBossDied;
            }

            ClearSector();

            // 클리어 UI 표시 — [확인] 클릭 시 로비 씬으로 이동
            BossClearUI.Instance?.Show();
        }

        protected override void OnSectorCleared()
        {
            Debug.Log("[BossSector] 보스 룸 클리어!");
        }
    }
}
