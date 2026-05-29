using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 포탑류 공통 제어 흐름(조준→판정→발사)을 담당하는 추상 부모 클래스.
    /// Projectile은 터렛 루트 아래 풀로 유지하며, 발사 시 방향만 주입합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class BaseTurretController : MonoBehaviour, ITurretAimDebugState
    {
        [Header("References")]
        [SerializeField] private Transform yawPivot;
        [SerializeField] private Transform pitchPivot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Object Pool")]
        [SerializeField]
        [Tooltip("풀 크기. 이 수만큼 Projectile을 터렛 루트 아래 미리 생성합니다.")]
        [Min(1)] private int poolSize = 5;

        [Header("Yaw")]
        [SerializeField] private float yawSpeedDegreesPerSecond = 90f;

        [Header("Pitch")]
        [SerializeField] private float pitchSpeedDegreesPerSecond = 60f;
        [SerializeField] private float minPitchDegrees = -45f;
        [SerializeField] private float maxPitchDegrees = 20f;

        [Header("Fire Control")]
        [SerializeField]
        [Tooltip("MuzzlePoint.forward와 타겟 방향 사이 허용 각(도).")]
        private float fireAngleThresholdDegrees = 5f;

        [SerializeField] private float fireIntervalSeconds = 0.5f;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileLifeTimeSeconds = 3f;
        [SerializeField] private float projectileDamage = 10f;

        [SerializeField]
        [Tooltip("0 이하면 무제한 사거리.")]
        private float engagementRangeWorldUnits;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        // ── 런타임 풀 ──────────────────────────────
        private ProjectileMover[] pool;
        private int poolIndex;

        // ── 런타임 상태 ────────────────────────────
        private float lastFireTimeSeconds = float.NegativeInfinity;
        private float runtimeAimErrorDegrees;
        private bool  runtimeIsAimedWithinThreshold;
        private bool  runtimeIsWithinEngagementRange;
        private bool  runtimeHasTargetWorldPosition;
        private Vector3 runtimeTargetWorldPosition;
        private bool  runtimeIsFireCooldownActive;

        // ── ITurretAimDebugState ───────────────────
        public bool  ShowDebugGizmos             => showDebugGizmos;
        public float FireAngleThresholdDegrees   => fireAngleThresholdDegrees;
        public float EngagementRangeWorldUnits   => engagementRangeWorldUnits;
        public Transform MuzzleTransform         => muzzlePoint;
        public float AimErrorDegrees             => runtimeAimErrorDegrees;
        public bool  IsAimedWithinThreshold      => runtimeIsAimedWithinThreshold;
        public bool  IsFireCooldownActive        => runtimeIsFireCooldownActive;
        public bool  IsWithinEngagementRange     => runtimeIsWithinEngagementRange;

        // ── 파생 클래스 인터페이스 ─────────────────
        /// <summary>현재 프레임의 추적 타겟을 반환합니다. null이면 Patrol 상태.</summary>
        protected abstract Transform GetCurrentTarget();

        protected virtual bool CanFireAdditionalConditions(Transform currentTarget) => true;
        protected virtual void OnProjectileFired(ProjectileMover projectile) { }

        // ── EnemyConfigSO 값 주입 (파생 클래스에서 호출) ──
        protected void ApplyConfig(EnemyConfigSO config)
        {
            if (config == null) return;
            engagementRangeWorldUnits = config.detectRadius;
            fireIntervalSeconds       = config.attackCooldown;
            // 데미지는 EnemyStatus.AttackPower에서 별도로 주입
        }

        /// <summary>EnemyStatus.AttackPower를 발사체 데미지에 적용합니다.</summary>
        protected void ApplyAttackPower(int attackPower)
        {
            projectileDamage = attackPower;
        }

        // ── 라이프사이클 ───────────────────────────
        private void Awake()
        {
            BuildPool();
        }

        private void BuildPool()
        {
            if (projectilePrefab == null) return;

            pool = new ProjectileMover[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = Instantiate(projectilePrefab, transform);
                go.name = $"Projectile_Pool_{i}";
                go.SetActive(false);

                ProjectileMover mover = go.GetComponent<ProjectileMover>();
                if (mover == null)
                    mover = go.AddComponent<ProjectileMover>();

                pool[i] = mover;
            }
        }

        protected virtual void Update()
        {
            Transform currentTarget = GetCurrentTarget();

            runtimeHasTargetWorldPosition    = false;
            runtimeIsWithinEngagementRange   = false;
            runtimeIsAimedWithinThreshold    = false;
            runtimeIsFireCooldownActive      = false;
            runtimeAimErrorDegrees           = 0f;

            if (yawPivot == null || pitchPivot == null || muzzlePoint == null) return;

            if (currentTarget != null)
            {
                // ── 타겟 있음: 조준 + 발사 ──────────
                runtimeHasTargetWorldPosition  = true;
                runtimeTargetWorldPosition     = currentTarget.position;

                float dist = Vector3.Distance(muzzlePoint.position, currentTarget.position);
                runtimeIsWithinEngagementRange =
                    engagementRangeWorldUnits <= 0f || dist <= engagementRangeWorldUnits;

                UpdateYawTowardsTarget(currentTarget);
                UpdatePitchTowardsTarget(currentTarget);
                RefreshAimDiagnostics(currentTarget);

                bool canFire = runtimeIsWithinEngagementRange && CanFireAdditionalConditions(currentTarget);
                bool cooling = Time.time < lastFireTimeSeconds + fireIntervalSeconds;
                runtimeIsFireCooldownActive = runtimeIsAimedWithinThreshold && canFire && cooling;

                if (canFire)
                    TryFireIfAimed();
            }
            // Patrol 회전은 NearestEnemyTurretController에서 직접 처리
        }

        public bool TryGetTargetWorldPosition(out Vector3 pos)
        {
            pos = runtimeHasTargetWorldPosition ? runtimeTargetWorldPosition : default;
            return runtimeHasTargetWorldPosition;
        }

        // ── Yaw / Pitch ────────────────────────────
        protected void RotateYaw(float degreesThisFrame)
        {
            if (yawPivot == null) return;
            yawPivot.Rotate(Vector3.up, degreesThisFrame, Space.World);
        }

        private void UpdateYawTowardsTarget(Transform target)
        {
            Vector3 flat = target.position - yawPivot.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-6f) return;

            flat.Normalize();
            float desired = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            float current = yawPivot.eulerAngles.y;
            float next    = Mathf.MoveTowardsAngle(current, desired,
                                yawSpeedDegreesPerSecond * Time.deltaTime);
            yawPivot.rotation = Quaternion.Euler(0f, next, 0f);
        }

        private void UpdatePitchTowardsTarget(Transform target)
        {
            Vector3 toTarget    = (target.position - pitchPivot.position).normalized;
            Vector3 localDir    = Quaternion.Inverse(yawPivot.rotation) * toTarget;
            float desired       = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;
            desired             = Mathf.Clamp(desired, minPitchDegrees, maxPitchDegrees);

            float current = NormalizeAngle180(pitchPivot.localEulerAngles.x);
            float next    = Mathf.MoveTowardsAngle(current, desired,
                                pitchSpeedDegreesPerSecond * Time.deltaTime);
            pitchPivot.localRotation = Quaternion.Euler(next, 0f, 0f);
        }

        private void RefreshAimDiagnostics(Transform target)
        {
            Vector3 toTarget = target.position - muzzlePoint.position;
            if (toTarget.sqrMagnitude < 1e-8f)
            {
                runtimeAimErrorDegrees = 0f;
                runtimeIsAimedWithinThreshold = true;
                return;
            }
            float angle = Vector3.Angle(muzzlePoint.forward, toTarget.normalized);
            runtimeAimErrorDegrees        = angle;
            runtimeIsAimedWithinThreshold = angle <= fireAngleThresholdDegrees;
        }

        // ── 발사 ───────────────────────────────────
        private void TryFireIfAimed()
        {
            if (!runtimeIsAimedWithinThreshold) return;
            if (Time.time < lastFireTimeSeconds + fireIntervalSeconds) return;
            if (pool == null || pool.Length == 0) return;

            // 풀에서 비활성 Projectile 탐색 (Round-Robin)
            ProjectileMover mover = null;
            for (int i = 0; i < pool.Length; i++)
            {
                int idx = (poolIndex + i) % pool.Length;
                if (pool[idx] != null && !pool[idx].gameObject.activeSelf)
                {
                    mover     = pool[idx];
                    poolIndex = (idx + 1) % pool.Length;
                    break;
                }
            }

            if (mover == null) return; // 모든 풀 슬롯이 활성 중

            mover.Launch(
                muzzlePoint.position,
                muzzlePoint.forward,
                projectileSpeed,
                projectileLifeTimeSeconds,
                projectileDamage);

            OnProjectileFired(mover);
            lastFireTimeSeconds = Time.time;
        }

        private static float NormalizeAngle180(float a)
        {
            return Mathf.Repeat(a + 180f, 360f) - 180f;
        }
    }
}
