using System.Collections;
using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 보스 기본 패턴 컨트롤러 (프로토타입 - 큐브, 애니메이션 미적용).
    ///
    /// [패턴 흐름]
    ///  대기 → 거리 판단
    ///   ├ 근거리(meleeRangeRadius 이내): 부채꼴 공격 (전방/후방 랜덤)
    ///   └ 원거리(범위 밖): 추적 레이저 공격
    ///  → 패턴 종료 → 0.5초 대기 → 반복
    ///
    /// [부채꼴 공격]
    ///  - 보스가 플레이어를 바라보도록 즉시 회전(고정) 후 2초 예고 → 즉시 1회 데미지 판정(OverlapSphere + 각도)
    ///  - 후방 공격은 동일 오브젝트를 180도 회전시켜 재사용
    ///
    /// [레이저 공격]
    ///  - 2초간 raycast로 플레이어 추적 회전 (예고 표시, 판정 없음)
    ///  - 추적 종료 후 0.3초 대기 (회전 정지, 표시 유지)
    ///  - 이후 1초간 고정, 0.2초 간격 5회 틱 데미지
    ///
    /// [데미지 배율]
    ///  - BossData.attackPower 기준으로 패턴별 배율 적용
    /// </summary>
    public class BossController : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [Header("보스 데이터")]
        [SerializeField] private BossStatus bossStatus;

        [Header("판정 자식 오브젝트")]
        [Tooltip("부채꼴 공격 표시/판정용 자식 오브젝트 (보스 정면 기준 배치, Plane)")]
        [SerializeField] private BossFanAttackHitbox fanAttackHitbox;

        [Tooltip("레이저 공격 표시/판정용 자식 오브젝트 (보스 정면, Capsule)")]
        [SerializeField] private BossLaserHitbox laserHitbox;

        [Tooltip("특수패턴(바닥패턴) 컨트롤러")]
        [SerializeField] private BossFloorPatternController floorPatternController;

        [Header("부채꼴 공격 설정")]
        [Tooltip("부채꼴 판정 각도 (전체 각도, 정면 기준 좌우 절반씩)")]
        [SerializeField] private float fanAngle = 90f;

        [Tooltip("부채꼴 공격 예고 시간(초)")]
        [SerializeField] private float fanTelegraphDuration = 2f;

        [Tooltip("부채꼴 공격 표시 유지 시간(초) - 데미지 판정 직후")]
        [SerializeField] private float fanAttackShowDuration = 0.3f;

        [Header("레이저 공격 설정")]
        [Tooltip("레이저 추적(예고) 시간(초)")]
        [SerializeField] private float laserTrackingDuration = 2f;

        [Tooltip("레이저 추적 종료 후 발사 전 대기 시간(초)")]
        [SerializeField] private float laserPreFireDelay = 0.3f;

        [Tooltip("레이저 발사(고정) 시간(초)")]
        [SerializeField] private float laserFireDuration = 1f;

        [Header("패턴 간격")]
        [Tooltip("패턴 종료 후 다음 패턴까지 대기 시간(초)")]
        [SerializeField] private float patternInterval = 0.5f;

        [Header("다음 배율")]
        [SerializeField] private float fanAttackMultiplier = 1.5f;
        [SerializeField] private float laserTickMultiplier = 0.5f;

        [Header("부채꼴 정렬")]
        [Tooltip("FanAttackIndicator의 로컬 Y 오프셋 (바닥 높이 보정용, 크기 동기화 시에도 유지됩니다).")]
        [SerializeField] private float fanIndicatorYOffset = -0.85f;

        [Header("레이저 정렬")]
        [Tooltip("레이저 자식 오브젝트의 고정 로컬 회전(Euler). Capsule Collider Direction이 Y-Axis인 경우 X=90을 권장합니다 (Y축 캡슐을 눕혀 보스 정면(Z축)으로 향하게 함).")]
        [SerializeField] private Vector3 laserLocalRotationEuler = new Vector3(90f, 0f, 0f);

        private Transform _player;
        private bool _isAttacking;

        private void Awake()
        {
            if (bossStatus == null) bossStatus = GetComponent<BossStatus>();
        }

        private void Start()
        {
            TryBindPlayer();
            SyncHitboxesWithBossData();
            StartCoroutine(PatternLoop());
        }

        /// <summary>
        /// BossData의 meleeRangeRadius / laserRange 값에 맞춰
        /// FanAttackIndicator(Plane), LaserHitbox(Capsule)의 크기·위치를 보스 원점 기준으로 재계산합니다.
        ///
        /// - FanAttackIndicator: 보스 원점이 부채꼴의 중심(꼭짓점). Plane 기본 크기 10m → scale = meleeRangeRadius / 5 (반경 기준).
        /// - LaserHitbox: 보스 원점이 캡슐의 시작점(뒤쪽 끝), 정면으로 laserRange만큼 뻗음.
        ///   Capsule 기본 height = 2 → scale.y = laserRange / 2, localPosition.z = laserRange / 2.
        /// </summary>
        private void SyncHitboxesWithBossData()
        {
            float meleeRange = GetMeleeRange();
            float laserRange = GetLaserRange();

            if (fanAttackHitbox != null)
            {
                Transform t = fanAttackHitbox.transform;
                // Plane 기본 크기 10m -> scale = meleeRange / 10 (한 변의 길이 = meleeRange)
                float scaleXZ = meleeRange / 10f;
                t.localScale = new Vector3(scaleXZ, t.localScale.y, scaleXZ);
                // Plane의 한 변이 보스 원점에 닿고 정면으로 meleeRange만큼 펼쳐지도록 중심을 z = meleeRange/2로 이동
                t.localPosition = new Vector3(0f, fanIndicatorYOffset, meleeRange / 2f);
            }

            if (laserHitbox != null)
            {
                Transform t = laserHitbox.transform;
                float scaleY = laserRange / 2f;
                t.localScale = new Vector3(t.localScale.x, scaleY, t.localScale.z);
                t.localPosition = new Vector3(0f, 0f, laserRange / 2f);
                t.localRotation = Quaternion.Euler(laserLocalRotationEuler);
            }
        }

        private void TryBindPlayer()
        {
            GameObject go = GameObject.FindGameObjectWithTag(PlayerTag);
            if (go != null) _player = go.transform;
        }

        // ── 패턴 루프 ────────────────────────────────────────

        private IEnumerator PatternLoop()
        {
            while (true)
            {
                if (_player == null)
                {
                    TryBindPlayer();
                    if (_player == null)
                    {
                        yield return null;
                        continue;
                    }
                }

                _isAttacking = true;

                // 기본패턴: 거리 기반 부채꼴/레이저
                float dist = GetHorizontalDistance(transform.position, _player.position);
                if (dist <= GetMeleeRange())
                {
                    yield return StartCoroutine(DoFanAttack());
                }
                else
                {
                    yield return StartCoroutine(DoLaserAttack());
                }

                yield return new WaitForSeconds(patternInterval);

                // 특수패턴: 바닥패턴(#자/파도/컨테이너)
                if (floorPatternController != null)
                {
                    yield return StartCoroutine(floorPatternController.PlaySpecialPattern());
                }

                _isAttacking = false;
                yield return new WaitForSeconds(patternInterval);
            }
        }

        // ── 부채꼴 공격 (전방/후방 랜덤) ─────────────────────

        private IEnumerator DoFanAttack()
        {
            bool isBackAttack = Random.value < 0.5f;

            // 플레이어를 바라보도록 즉시 회전 후 고정 (예고 시작 시 회전 정지)
            FacePlayerInstant();

            if (fanAttackHitbox != null)
            {
                Transform hitboxTransform = fanAttackHitbox.transform;
                float meleeRange = GetMeleeRange();
                float zOffset = meleeRange / 2f;

                // 전방 기준 배치된 오브젝트를 후방 공격 시 180도 회전 + 반대편(z 반전)으로 이동
                hitboxTransform.localRotation = isBackAttack
                    ? Quaternion.Euler(0f, 180f, 0f)
                    : Quaternion.identity;

                Vector3 pos = hitboxTransform.localPosition;
                pos.z = isBackAttack ? -zOffset : zOffset;
                hitboxTransform.localPosition = pos;

                fanAttackHitbox.ShowTelegraph();
            }

            yield return new WaitForSeconds(fanTelegraphDuration);

            // 즉시 1회 데미지 판정
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
                }
            }
        }

        // ── 원거리 레이저 공격 ───────────────────────────────

        private IEnumerator DoLaserAttack()
        {
            if (laserHitbox != null)
            {
                laserHitbox.transform.localRotation = Quaternion.Euler(laserLocalRotationEuler);
                laserHitbox.ShowTelegraph();
            }

            // 추적(예고) 단계 - 보스가 플레이어를 향해 회전
            float elapsed = 0f;
            while (elapsed < laserTrackingDuration)
            {
                if (_player != null)
                    RotateTowardsPlayer();

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 추적 종료 - 회전 정지, 표시 유지한 채 대기
            yield return new WaitForSeconds(laserPreFireDelay);

            // 고정 후 발사
            int tickDamage = Mathf.RoundToInt(GetAttackPower() * laserTickMultiplier);
            if (laserHitbox != null)
                laserHitbox.StartAttack(tickDamage);

            yield return new WaitForSeconds(laserFireDuration);

            if (laserHitbox != null)
                laserHitbox.Hide();
        }

        // ── 회전 ────────────────────────────────────────────

        /// <summary>레이저 추적 단계 - BossData.rotationSpeed 기준으로 플레이어를 향해 점진 회전.</summary>
        private void RotateTowardsPlayer()
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            float speed = GetRotationSpeed();
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * Time.deltaTime);
        }

        /// <summary>부채꼴 공격 예고 시작 시 - 플레이어를 즉시 바라보도록 회전(고정).</summary>
        private void FacePlayerInstant()
        {
            if (_player == null) return;

            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(dir);
        }

        /// <summary>두 위치 간 수평(XZ) 거리.</summary>
        private float GetHorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector3 diff = a - b;
            diff.y = 0f;
            return diff.magnitude;
        }

        // ── BossData 접근 ───────────────────────────────────

        private float GetMeleeRange()
        {
            BossData data = bossStatus != null ? bossStatus.BossData : null;
            return data != null ? data.meleeRangeRadius : 3f;
        }

        private float GetRotationSpeed()
        {
            BossData data = bossStatus != null ? bossStatus.BossData : null;
            return data != null ? data.rotationSpeed : 90f;
        }

        private float GetLaserRange()
        {
            BossData data = bossStatus != null ? bossStatus.BossData : null;
            return data != null ? data.laserRange : 20f;
        }

        private int GetAttackPower()
        {
            BossData data = bossStatus != null ? bossStatus.BossData : null;
            return data != null ? data.attackPower : 10;
        }

        // ── Gizmo ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            float meleeRange = GetMeleeRange();
            float laserRange = GetLaserRange();

            // 근접 판정 범위 (원)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, meleeRange);

            // 부채꼴 판정 각도 (전방/후방)
            DrawFanGizmo(transform.forward, meleeRange, new Color(1f, 0.5f, 0f, 0.8f));
            DrawFanGizmo(-transform.forward, meleeRange, new Color(0f, 0.6f, 1f, 0.8f));

            // 레이저 사거리 (정면 직선)
            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * laserRange);

            // 플레이어 추적 라인
            if (_player != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _player.position);
            }
        }

        /// <summary>부채꼴(fanAngle) 판정 영역을 와이어로 표시.</summary>
        private void DrawFanGizmo(Vector3 forward, float range, Color color)
        {
            Gizmos.color = color;
            float halfAngle = fanAngle * 0.5f;

            Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
            Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);

            Vector3 leftDir = leftRot * forward;
            Vector3 rightDir = rightRot * forward;

            Vector3 origin = transform.position;
            Gizmos.DrawLine(origin, origin + leftDir * range);
            Gizmos.DrawLine(origin, origin + rightDir * range);
            Gizmos.DrawLine(origin, origin + forward * range);

            // 부채꼴 외곽 호
            const int segments = 12;
            Vector3 prevPoint = origin + leftDir * range;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Quaternion rot = Quaternion.AngleAxis(-halfAngle + fanAngle * t, Vector3.up);
                Vector3 point = origin + (rot * forward) * range;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
    }
}
