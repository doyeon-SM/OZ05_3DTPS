using UnityEngine;
using _01.Scenes.PhaseValidation;

namespace TurretDemo
{
    /// <summary>
    /// Player를 추적하는 터렛 구현.
    /// - EnemyConfigSO : 사거리(detectRadius), 쿨타임(attackCooldown)
    /// - EnemyStatus   : 발사체 데미지(attackPower)
    /// - 범위 안  : Player 추적 + 발사
    /// - 범위 밖  : YawPivot 수평 회전 (Patrol)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NearestEnemyTurretController : BaseTurretController
    {
        private const string PlayerTag = "Player";

        [Header("Config")]
        [SerializeField]
        [Tooltip("사거리(detectRadius)·쿨타임(attackCooldown)을 자동 적용합니다.")]
        private EnemyConfigSO config;

        [Header("Patrol Rotation")]
        [SerializeField]
        [Tooltip("Patrol 중 수평 회전 속도(도/초).")]
        private float patrolYawSpeedDegreesPerSecond = 60f;

        // ── 런타임 ─────────────────────────────────
        private Transform cachedPlayer;
        private bool isPatrolling;
        private TurretEffectController effectController;

        // 섹터 활성화 전까지 공격 금지 (적과 동일한 패턴)
        private bool isAIActive = false;

        public void SetAIActive(bool active) => isAIActive = active;

        // ── 초기화 ─────────────────────────────────
        private void Start()
        {
            if (config != null)
                ApplyConfig(config);

            EnemyStatus enemyStatus = GetComponent<EnemyStatus>();
            if (enemyStatus != null)
                ApplyAttackPower(enemyStatus.AttackPower);
            else
                Debug.LogWarning("[NearestEnemyTurretController] EnemyStatus 없음 — 기본 데미지 사용");

            effectController = GetComponent<TurretEffectController>();

            TryBindPlayer();
        }

        // ── 발사 콜백 (BaseTurretController 확장 포인트) ─
        // 발사 직후 머즐 VFX + 공격 SFX 재생
        protected override void OnProjectileFired(ProjectileMover projectile)
        {
            effectController?.PlayAttackEffects(MuzzleTransform);
        }

        private void TryBindPlayer()
        {
            GameObject go = GameObject.FindGameObjectWithTag(PlayerTag);
            if (go != null)
                cachedPlayer = go.transform;
        }

        // ── Update 오버라이드 ───────────────────────
        // BaseTurretController.Update()를 호출해 조준·발사 흐름을 유지하면서
        // Patrol 상태일 때 수평 회전을 추가합니다.
        protected override void Update()
        {
            // 부모의 Update(조준·발사) 먼저 실행
            base.Update();

            // Patrol 상태면 수평 회전
            if (isPatrolling)
                RotateYaw(patrolYawSpeedDegreesPerSecond * Time.deltaTime);
        }

        // ── 타겟 선택 ──────────────────────────────
        protected override Transform GetCurrentTarget()
        {
            // 섹터가 아직 시작되지 않았으면 추적·공격 금지
            if (!isAIActive)
            {
                isPatrolling = true;
                return null;
            }

            if (cachedPlayer == null)
                TryBindPlayer();

            if (cachedPlayer == null)
            {
                isPatrolling = true;
                return null;
            }

            if (IsWithinRange(cachedPlayer.position))
            {
                isPatrolling = false;
                return cachedPlayer;
            }

            isPatrolling = true;
            return null;
        }

        private bool IsWithinRange(Vector3 targetPos)
        {
            float range = EngagementRangeWorldUnits;
            if (range <= 0f) return true;
            return (targetPos - transform.position).sqrMagnitude <= range * range;
        }

        // ── Gizmo 시각화 ───────────────────────────
        private void OnDrawGizmosSelected()
        {
            DrawTurretGizmos();
        }

        private void OnDrawGizmos()
        {
            // 항상 표시 (반투명)
            DrawTurretGizmos();
        }

        private void DrawTurretGizmos()
        {
            float range = config != null ? config.detectRadius : EngagementRangeWorldUnits;
            Vector3 origin = transform.position;

            if (isPatrolling || !Application.isPlaying)
            {
                // ── Patrol: 회색 탐색 범위 원 ──────
                Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.25f);
                DrawWireCircle(origin, range, 36);
                Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                DrawWireCircleEdge(origin, range, 36);

                // Patrol 회전 방향 화살표
                if (MuzzleTransform != null)
                {
                    Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.9f);
                    Gizmos.DrawRay(MuzzleTransform.position, MuzzleTransform.forward * 2f);
                }
            }
            else
            {
                // ── 추적 중: 빨간 범위 + 타겟 라인 ─
                Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.15f);
                DrawWireCircle(origin, range, 36);
                Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.9f);
                DrawWireCircleEdge(origin, range, 36);

                if (cachedPlayer != null && MuzzleTransform != null)
                {
                    // 타겟 연결선
                    Gizmos.color = new Color(1f, 0.4f, 0f, 0.95f);
                    Gizmos.DrawLine(MuzzleTransform.position, cachedPlayer.position);

                    // 타겟 마커
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(cachedPlayer.position, 0.35f);

                    // Muzzle forward
                    Gizmos.color = IsAimedWithinThreshold
                        ? new Color(0.1f, 1f, 0.3f, 0.95f)
                        : new Color(1f, 0.85f, 0.1f, 0.95f);
                    Gizmos.DrawRay(MuzzleTransform.position, MuzzleTransform.forward * 3f);
                }
            }

            // 항상: 터렛 위치 마커
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.9f);
            Gizmos.DrawWireSphere(origin, 0.2f);
        }

        // 바닥면 원 (채움)
        private static void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            if (radius <= 0f) return;
            float step = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step * Mathf.Deg2Rad;
                float a1 = (i + 1) * step * Mathf.Deg2Rad;
                Vector3 p0 = center + new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * radius;
                Vector3 p1 = center + new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * radius;
                Gizmos.DrawLine(center, p0);
                Gizmos.DrawLine(p0, p1);
            }
        }

        // 외곽선만
        private static void DrawWireCircleEdge(Vector3 center, float radius, int segments)
        {
            if (radius <= 0f) return;
            float step = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step * Mathf.Deg2Rad;
                float a1 = (i + 1) * step * Mathf.Deg2Rad;
                Vector3 p0 = center + new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * radius;
                Vector3 p1 = center + new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * radius;
                Gizmos.DrawLine(p0, p1);
            }
        }
    }
}
