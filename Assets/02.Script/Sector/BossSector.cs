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
    ///  4. BossStatus.OnBossDied → ClearSector() + BossExitObject를 보스 사망 위치 근처에 소환
    ///  5. 플레이어가 BossExitObject와 상호작용([E]) → BossClearUI.Show()
    ///  6. BossClearUI [확인] 버튼 → 로비 씬 복귀
    ///
    /// 보스 처치와 로비 복귀를 분리하기 위해, 처치 즉시 클리어 UI를 띄우지 않고
    /// 상호작용 가능한 BossExitObject를 통해서만 클리어 UI에 진입하도록 한다.
    /// 아이템 드랍은 보스가 별도로 보스 위치에 드랍하는 GameObject(BossData.rewardPrefab)로 처리되며,
    /// 이 섹터의 BossExitObject와는 무관하다. 단, 두 오브젝트가 동일 좌표에 겹치면 상호작용 판정이
    /// 잘못된 오브젝트를 향할 수 있어, exitObjectSpawnOffset만큼 떨어진 곳에 BossExitObject를 소환한다.
    /// </summary>
    public class BossSector : SectorBase
    {
        [Header("보스 설정")]
        [SerializeField] private BossData bossData;
        [Tooltip("보스가 소환될 위치")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("보스의 이름 (HUD에 표시됩니다)")]
        [SerializeField] private string bossName = "BOSS";

        [Header("클리어 진입 오브젝트")]
        [Tooltip("보스 처치 후 소환할 상호작용 오브젝트 프리팹 (BossExitObject 컴포넌트 포함). 플레이어가 이 오브젝트와 상호작용하면 클리어 UI가 표시됩니다.")]
        [SerializeField] private GameObject bossExitObjectPrefab;

        [Tooltip("BossExitObject를 보스 사망 위치에서 얼마나 떨어진 곳에 소환할지. 보상 아이템(BossData.rewardPrefab)과 같은 위치에 겹쳐 상호작용이 잘못 인식되는 문제를 막기 위함.")]
        [SerializeField] private Vector3 exitObjectSpawnOffset = new Vector3(0f, 0f, 2f);

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
            // BossStatus.Die()에서 Destroy(gameObject) 전에 이벤트가 발행되므로
            // 이 시점에는 transform이 아직 유효함 — 사망 위치를 먼저 캡처한다.
            Vector3 deathPosition = _bossInstance != null ? _bossInstance.transform.position : transform.position;

            if (_bossInstance != null)
            {
                if (BossHUDManager.Instance != null)
                    _bossInstance.OnHPChanged -= BossHUDManager.Instance.OnHPChanged;
                _bossInstance.OnBossDied -= HandleBossDied;
            }

            ClearSector();

            // 클리어 UI를 바로 띄우지 않고, 상호작용 가능한 BossExitObject를 소환한다.
            // 플레이어가 이 오브젝트와 상호작용해야 BossClearUI.Show()가 호출된다.
            SpawnBossExitObject(deathPosition);
        }

        private void SpawnBossExitObject(Vector3 position)
        {
            if (bossExitObjectPrefab == null)
            {
                Debug.LogError("[BossSector] bossExitObjectPrefab이 설정되지 않았습니다. 클리어 UI에 진입할 방법이 없습니다.");
                return;
            }

            // 보상 아이템(BossData.rewardPrefab)도 같은 사망 위치에 소환되므로, 콜라이더 중첩으로 인한
            // 상호작용 오인을 막기 위해 오프셋만큼 떨어진 곳에 소환한다.
            GameObject obj = Instantiate(bossExitObjectPrefab, position + exitObjectSpawnOffset, Quaternion.identity);

            BossExitObject exitObject = obj.GetComponent<BossExitObject>();
            if (exitObject != null)
                exitObject.Initialize(this);
            else
                Debug.LogWarning("[BossSector] bossExitObjectPrefab에 BossExitObject 컴포넌트가 없습니다.");

            Debug.Log("[BossSector] BossExitObject 소환 완료 — 플레이어 상호작용 시 클리어 UI가 표시됩니다.");
        }

        protected override void OnSectorCleared()
        {
            Debug.Log("[BossSector] 보스 룸 클리어!");
        }
    }
}
