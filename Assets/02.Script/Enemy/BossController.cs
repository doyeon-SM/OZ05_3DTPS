using System.Collections;
using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 기본 패턴 컨트롤러.
    ///
    /// [애니메이션 Trigger]
    ///  - A_attack  : 부채꼴 전방
    ///  - B_attack  : 부채꼴 후방
    ///  - C_attack  : 레이저
    ///  - SA_attack : 바닥패턴 #자
    ///  - SB_attack : 바닥패턴 파도
    ///  - SC_attack : 바닥패턴 컨테이너
    /// </summary>
    public class BossController : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        private const string BossSpawnName = "BossSpawn";

        // ── Animator Trigger 상수 ────────────────────────────
        private const string TriggerFanFront      = "A_attack";
        private const string TriggerFanBack       = "B_attack";
        private const string TriggerLaser         = "C_attack";
        private const string TriggerFloorHash     = "SA_attack";
        private const string TriggerFloorWave     = "SB_attack";
        private const string TriggerFloorContainer = "SC_attack";

        [Header("보스 데이터")]
        [SerializeField] private BossStatus bossStatus;

        [Tooltip("공격 VFX/SFX 처리 컴포넌트 (비워두면 자동으로 GetComponent 시도)")]
        [SerializeField] private BossEffectController effectController;

        [Header("애니메이션")]
        [Tooltip("보스 Animator (비워두면 자동으로 GetComponent 시도)")]
        [SerializeField] private Animator animator;

        [Header("판정 자식 오브젝트")]
        [Tooltip("부채꼴 공격 표시/판정용 자식 오브젝트 (보스 정면 기준 배치, Plane)")]
        [SerializeField] private BossFanAttackHitbox fanAttackHitbox;

        [Tooltip("레이저 공격 표시/판정용 자식 오브젝트 (보스 정면, Capsule)")]
        [SerializeField] private BossLaserHitbox laserHitbox;

        [Tooltip("특수패턴(바닥패턴) 컨트롤러")]
        [SerializeField] private BossFloorPatternController floorPatternController;

        [Header("부채꼴 공격 설정")]
        [SerializeField] private float fanAngle = 90f;
        [SerializeField] private float fanTelegraphDuration = 2f;
        [SerializeField] private float fanAttackShowDuration = 0.3f;

        [Header("레이저 공격 설정")]
        [SerializeField] private float laserTrackingDuration = 2f;
        [SerializeField] private float laserPreFireDelay = 0.3f;
        [SerializeField] private float laserFireDuration = 1f;

        [Header("패턴 간격")]
        [SerializeField] private float patternInterval = 0.5f;

        [Header("데미지 배율")]
        [SerializeField] private float fanAttackMultiplier = 1.5f;
        [SerializeField] private float laserTickMultiplier = 0.5f;

                [Header("레이저 정렬")]
        [SerializeField] private Vector3 laserLocalRotationEuler = new Vector3(90f, 0f, 0f);

        [Header("2페이즈 설정")]
        [Tooltip("2페이즈 전환 HP 비율 (0~1). 이 값 이하로 떨어지면 분기패턴 후 2페이즈로 전환된다.")]
        [SerializeField] private float phase2HpThreshold = 0.5f;

        [Tooltip("2페이즈에서의 공격 예고 시간(초). 부채꼴/레이저/바닥패턴 모두 이 값으로 통일된다.")]
        [SerializeField] private float phase2TelegraphDuration = 1f;

        [Tooltip("2페이즈 회전속도 배율")]
        [SerializeField] private float phase2RotationSpeedMultiplier = 1.5f;

        [Tooltip("2페이즈 애니메이션 재생 속도 배율 (예고시간이 절반이 되는 것에 맞춰 기본 2배 권장)")]
        [SerializeField] private float phase2AnimatorSpeedMultiplier = 2f;

        [Tooltip("2페이즈 진입 시 보스 머티리얼에 적용할 baseMap 색상")]
        [SerializeField] private Color phase2BaseColor = Color.red;

        [Tooltip("2페이즈 진입 시 플레이어를 보스 쪽으로 끌어당기는 시간(초)")]
        [SerializeField] private float phase2PullDuration = 1f;

        [Tooltip("2페이즈 진입 시 맵 제한용 벽 오브젝트")]
        [SerializeField] private GameObject wall;

        private Transform _player;
        private bool _isAttacking;
        private bool _isPhase2;
        private bool _branchPatternDone;

        private void Awake()
        {
            if (bossStatus == null)       bossStatus       = GetComponent<BossStatus>();
            if (effectController == null) effectController = GetComponent<BossEffectController>();
            if (animator == null)         animator         = GetComponent<Animator>();
        }

        private void Start()
        {
            TryBindPlayer();
            SyncHitboxesWithBossData();
            StartCoroutine(PatternLoop());
            SetupWall();
        }

        /// <summary>
        /// 벽을 BossSpawn 오브젝트를 기준으로 재배치한다.
        /// BossFloorPatternController가 격자 인디케이터를 BossSpawn 기준으로 재배치하는 것과 동일한 이유로,
        /// 벽이 보스(회전하는 오브젝트)의 자식이거나 영향을 받는 경우 보스가 회전해도 벽이 같이 돌지 않도록
        /// BossSpawn으로 재부모화하고 회전을 0으로 고정한다. 위치(오프셋)는 기존 배치를 그대로 유지한다.
        /// </summary>
        private void SetupWall()
        {
            if (wall == null) return;

            GameObject spawnObj = GameObject.Find(BossSpawnName);
            if (spawnObj != null)
            {
                Transform spawnTransform = spawnObj.transform;
                Vector3 worldPos = wall.transform.position;
                wall.transform.SetParent(spawnTransform, true);
                wall.transform.position = worldPos;
                wall.transform.rotation = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning($"[BossController] '{BossSpawnName}' 오브젝트를 찾지 못해 벽 위치를 보정하지 못했습니다.");
            }

            wall.SetActive(false);
        }

        private void SyncHitboxesWithBossData()
        {
            float meleeRange = GetMeleeRange();

            if (fanAttackHitbox != null)
            {
                Transform t = fanAttackHitbox.transform;
                // 고정 배치: position(0,1,0), scale(2,1,2). 회전만 SetDirection으로 제어.
                t.localPosition = new Vector3(0f, -0.85f, 0f);
                t.localScale = new Vector3(2f, 1f, 2f);

                // 셰이더 부채꼴 각도 동기화
                fanAttackHitbox.SetFanAngle(fanAngle);
            }
        }

        private void TryBindPlayer()
        {
            GameObject go = GameObject.FindGameObjectWithTag(PlayerTag);
            if (go != null) _player = go.transform;
        }

        // ── 애니메이션 헬퍼 ─────────────────────────────────

        private void SetAttackTrigger(string triggerName)
        {
            if (animator != null)
                animator.SetTrigger(triggerName);
        }

        // ── 패턴 루프 ────────────────────────────────────────

        private IEnumerator PatternLoop()
        {
            bool loop = true;
            while (loop)
            {
                if (_player == null)
                {
                    TryBindPlayer();
                    if (_player == null) { yield return null; continue; }
                }

                _isAttacking = true;

                // 기본패턴: 거리 기반 부채꼴/레이저
                float dist = GetHorizontalDistance(transform.position, _player.position);
                if (dist <= GetMeleeRange())
                    yield return StartCoroutine(DoFanAttack());
                else
                    yield return StartCoroutine(DoLaserAttack());

                yield return new WaitForSeconds(patternInterval);

                yield return StartCoroutine(CheckPhase2Transition());

                // 특수패턴: 바닥패턴
                if (floorPatternController != null)
                    yield return StartCoroutine(floorPatternController.PlaySpecialPattern());

                _isAttacking = false;
                yield return new WaitForSeconds(patternInterval);

                yield return StartCoroutine(CheckPhase2Transition());
            }
        }

        /// <summary>
        /// HP가 phase2HpThreshold 이하이고 아직 분기패턴을 실행하지 않았다면,
        /// 무적 → 플레이어 끌어당기기 → 벽 활성화 → 분기패턴(전환패턴) 순으로 실행한 뒤
        /// 2페이즈로 전환한다. 한 패턴 종료 지점마다 호출된다.
        /// </summary>
        private IEnumerator CheckPhase2Transition()
        {
            if (_branchPatternDone || _isPhase2) yield break;
            if (bossStatus == null) yield break;
            if (bossStatus.MaxHP <= 0) yield break;

            float hpRatio = (float)bossStatus.CurrentHP / bossStatus.MaxHP;
            if (hpRatio > phase2HpThreshold) yield break;

            _branchPatternDone = true;

            // 1) 무적
            bossStatus.IsInvincible = true;
            BossHUDManager.Instance?.SetInvincibleVisual(true);

            // 2) 플레이어를 보스 쪽으로 끌어당기기
            yield return StartCoroutine(PullPlayerTowardsBoss(phase2PullDuration));

            // 3) 벽 활성화 (위치/회전은 SetupWall()에서 BossSpawn 기준으로 이미 보정됨)
            if (wall != null)
                wall.SetActive(true);

            // 4) 2페이즈 전환패턴(분기패턴) 시작
            if (floorPatternController != null)
                yield return StartCoroutine(floorPatternController.PlayBranchPattern());

            bossStatus.IsInvincible = false;
            BossHUDManager.Instance?.SetInvincibleVisual(false);

            EnterPhase2();
        }

        /// <summary>
        /// 플레이어를 duration(초) 동안 보스 위치(transform.position)로 끌어당긴다.
        /// 끌어당기는 동안 ThirdPersonController를 비활성화하여 일반 이동 입력을 차단하고,
        /// CharacterController가 있다면 Move()로 이동시켜 충돌 처리를 유지한다.
        /// </summary>
        private IEnumerator PullPlayerTowardsBoss(float duration)
        {
            if (_player == null) yield break;
            if (duration <= 0f) yield break;

            StarterAssets.ThirdPersonController playerController = _player.GetComponent<StarterAssets.ThirdPersonController>();
            CharacterController characterController = _player.GetComponent<CharacterController>();

            if (playerController != null)
                playerController.enabled = false;

            Vector3 startPos = _player.position;
            Vector3 targetPos = transform.position;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 nextPos = Vector3.Lerp(startPos, targetPos, t);

                if (characterController != null)
                    characterController.Move(nextPos - _player.position);
                else
                    _player.position = nextPos;

                yield return null;
            }

            if (playerController != null)
                playerController.enabled = true;
        }

        /// <summary>2페이즈 변화 적용: 예고시간 단축, 회전속도 증가, 애니메이션 속도 증가, 머티리얼 색상 변경.</summary>
        private void EnterPhase2()
        {
            _isPhase2 = true;

            fanTelegraphDuration = phase2TelegraphDuration;
            laserTrackingDuration = phase2TelegraphDuration;

            if (floorPatternController != null)
                floorPatternController.SetTelegraphDurationOverride(phase2TelegraphDuration);

            if (animator != null)
                animator.speed = phase2AnimatorSpeedMultiplier;

            ApplyPhase2Material();

            Debug.Log("[BossController] ★ 2페이즈 진입 ★");
        }

        /// <summary>보스 전체 Renderer의 머티리얼 baseMap 색상을 phase2BaseColor로 변경.</summary>
        private void ApplyPhase2Material()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                foreach (var mat in rend.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", phase2BaseColor);
                    else if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", phase2BaseColor);
                }
            }
        }

        // ── 부채꼴 공격 (전방/후방 랜덤) ─────────────────────

        private IEnumerator DoFanAttack()
        {
            bool isBackAttack = Random.value < 0.5f;

            // 애니메이션: 전방 A_attack / 후방 B_attack
            SetAttackTrigger(isBackAttack ? TriggerFanBack : TriggerFanFront);

            FacePlayerInstant();

            if (fanAttackHitbox != null)
            {
                // 보스 중앙 고정 배치이므로 위치 이동은 불필요, 방향만 셰이더로 전달
                // 셰이더 마스크가 반전되어 있어 전달값을 180도 뒤집어 보정
                fanAttackHitbox.SetDirection(isBackAttack ? 0f : 180f);

                fanAttackHitbox.ShowTelegraph();
            }

            if (effectController != null)
                effectController.OnTelegraphSFX_Melee();

            yield return new WaitForSeconds(fanTelegraphDuration);

            Vector3 attackForward = isBackAttack ? -transform.forward : transform.forward;
            ApplyFanDamage(attackForward);

            if (fanAttackHitbox != null)
            {
                fanAttackHitbox.ShowAttack();
                yield return new WaitForSeconds(fanAttackShowDuration);
                fanAttackHitbox.Hide();
            }
        }

        private void ApplyFanDamage(Vector3 attackForward)
        {
            float range = GetMeleeRange();
            Collider[] hits = Physics.OverlapSphere(transform.position, range);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(PlayerTag)) continue;

                PlayerStatus playerStatus = hit.GetComponentInParent<PlayerStatus>();
                if (playerStatus == null) continue;

                Vector3 toTarget = hit.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.0001f) continue;

                float angle = Vector3.Angle(attackForward, toTarget);
                if (angle <= fanAngle * 0.5f)
                {
                    int damage = Mathf.RoundToInt(GetAttackPower() * fanAttackMultiplier);
                    playerStatus.TakeDamage(damage);
                    Debug.Log($"[BossController] 부채꼴 공격 적중 | damage={damage} angle={angle:F1}");

                    if (effectController != null)
                    {
                        effectController.PlayMeleeVfx(hit.transform.position, Quaternion.LookRotation(attackForward));
                        effectController.OnHitSFX_Melee();
                    }
                }
            }
        }

        // ── 레이저 공격 ─────────────────────────────────────

        private IEnumerator DoLaserAttack()
        {
            // 애니메이션: C_attack
            SetAttackTrigger(TriggerLaser);

            if (laserHitbox != null)
                laserHitbox.ShowTelegraph();

            if (effectController != null)
                effectController.OnTelegraphSFX_Laser();

            float elapsed = 0f;
            while (elapsed < laserTrackingDuration)
            {
                if (_player != null) RotateTowardsPlayer();
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(laserPreFireDelay);

            int tickDamage = Mathf.RoundToInt(GetAttackPower() * laserTickMultiplier);
            if (laserHitbox != null)
                laserHitbox.StartAttack(tickDamage);

            if (effectController != null)
                effectController.OnHitSFX_Laser();

            yield return new WaitForSeconds(laserFireDuration);

            if (laserHitbox != null)
                laserHitbox.Hide();
        }

        // ── 회전 ────────────────────────────────────────────

        private void RotateTowardsPlayer()
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, GetRotationSpeed() * Time.deltaTime);
        }

        private void FacePlayerInstant()
        {
            if (_player == null) return;
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        private float GetHorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector3 diff = a - b; diff.y = 0f; return diff.magnitude;
        }

        // ── BossData 접근 ───────────────────────────────────

        private float GetMeleeRange()    { var d = bossStatus?.BossData; return d != null ? d.meleeRangeRadius : 3f; }
        private float GetRotationSpeed()
        {
            var d = bossStatus?.BossData;
            float baseSpeed = d != null ? d.rotationSpeed : 90f;
            return _isPhase2 ? baseSpeed * phase2RotationSpeedMultiplier : baseSpeed;
        }
        private float GetLaserRange()    { var d = bossStatus?.BossData; return d != null ? d.laserRange       : 20f; }
        private int   GetAttackPower()   { var d = bossStatus?.BossData; return d != null ? d.attackPower      : 10; }

        // ── Gizmo ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            float meleeRange = GetMeleeRange();
            float laserRange = GetLaserRange();

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, meleeRange);

            DrawFanGizmo( transform.forward, meleeRange, new Color(1f, 0.5f, 0f, 0.8f));
            DrawFanGizmo(-transform.forward, meleeRange, new Color(0f, 0.6f, 1f, 0.8f));

            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * laserRange);

            if (_player != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _player.position);
            }
        }

        private void DrawFanGizmo(Vector3 forward, float range, Color color)
        {
            Gizmos.color = color;
            float halfAngle = fanAngle * 0.5f;

            Vector3 leftDir  = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
            Vector3 rightDir = Quaternion.AngleAxis( halfAngle, Vector3.up) * forward;
            Vector3 origin   = transform.position;

            Gizmos.DrawLine(origin, origin + leftDir  * range);
            Gizmos.DrawLine(origin, origin + rightDir * range);
            Gizmos.DrawLine(origin, origin + forward  * range);

            const int segments = 12;
            Vector3 prevPoint = origin + leftDir * range;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 point = origin + (Quaternion.AngleAxis(-halfAngle + fanAngle * t, Vector3.up) * forward) * range;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
    }
}
