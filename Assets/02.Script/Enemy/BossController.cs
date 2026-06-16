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

        [Header("부채꼴 정렬")]
        [SerializeField] private float fanIndicatorYOffset = -0.85f;

        [Header("레이저 정렬")]
        [SerializeField] private Vector3 laserLocalRotationEuler = new Vector3(90f, 0f, 0f);

        private Transform _player;
        private bool _isAttacking;

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
        }

        private void SyncHitboxesWithBossData()
        {
            float meleeRange = GetMeleeRange();

            if (fanAttackHitbox != null)
            {
                Transform t = fanAttackHitbox.transform;
                float scaleXZ = meleeRange / 10f;
                t.localScale = new Vector3(scaleXZ, t.localScale.y, scaleXZ);
                t.localPosition = new Vector3(0f, fanIndicatorYOffset, meleeRange / 2f);

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

                // 특수패턴: 바닥패턴
                if (floorPatternController != null)
                    yield return StartCoroutine(floorPatternController.PlaySpecialPattern());

                _isAttacking = false;
                yield return new WaitForSeconds(patternInterval);
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
                Transform hitboxTransform = fanAttackHitbox.transform;
                float meleeRange = GetMeleeRange();
                float zOffset = meleeRange / 2f;

                hitboxTransform.localRotation = isBackAttack
                    ? Quaternion.Euler(0f, 180f, 0f)
                    : Quaternion.identity;

                Vector3 pos = hitboxTransform.localPosition;
                pos.z = isBackAttack ? -zOffset : zOffset;
                hitboxTransform.localPosition = pos;

                // 전방(0도)/후방(180도) 방향을 셰이더에도 동기화
                fanAttackHitbox.SetDirection(isBackAttack ? 180f : 0f);

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
        private float GetRotationSpeed() { var d = bossStatus?.BossData; return d != null ? d.rotationSpeed    : 90f; }
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
