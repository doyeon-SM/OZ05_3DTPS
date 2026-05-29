using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 포탑류 공통 제어 흐름(조준→판정→발사)을 담당하는 추상 부모 클래스.
    /// MuzzlePoint.forward 대신 YawPivot.forward를 발사 기준 방향으로 사용합니다.
    /// (모델 축이 뒤틀린 경우에도 안정적으로 동작)
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
        [Tooltip("풀 크기 (수명/쿨타임 기준으로 자동 보정됨).")]
        [Min(1)] private int poolSize = 5;

        [Header("Yaw")]
        [SerializeField] private float yawSpeedDegreesPerSecond = 90f;

        [Header("Pitch")]
        [SerializeField] private float pitchSpeedDegreesPerSecond = 60f;
        [SerializeField] private float minPitchDegrees = -45f;
        [SerializeField] private float maxPitchDegrees = 20f;

        [Header("Fire Control")]
        [SerializeField]
        [Tooltip("YawPivot.forward와 타겟 수평 방향 사이 허용 각(도).")]
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
        public bool  ShowDebugGizmos           => showDebugGizmos;
        public float FireAngleThresholdDegrees => fireAngleThresholdDegrees;
        public float EngagementRangeWorldUnits => engagementRangeWorldUnits;
        public Transform MuzzleTransform       => muzzlePoint;
        public float AimErrorDegrees           => runtimeAimErrorDegrees;
        public bool  IsAimedWithinThreshold    => runtimeIsAimedWithinThreshold;
        public bool  IsFireCooldownActive      => runtimeIsFireCooldownActive;
        public bool  IsWithinEngagementRange   => runtimeIsWithinEngagementRange;

        // ── 파생 클래스 인터페이스 ─────────────────
        protected abstract Transform GetCurrentTarget();
        protected virtual bool CanFireAdditionalConditions(Transform currentTarget) { return true; }
        protected virtual void OnProjectileFired(ProjectileMover projectile) { }

        // ── Config 주입 ────────────────────────────
        protected void ApplyConfig(EnemyConfigSO config)
        {
            if (config == null) return;
            engagementRangeWorldUnits = config.detectRadius;
            fireIntervalSeconds       = config.attackCooldown;
        }

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

            // 수명 동안 소비되는 슬롯 수 + 여유 1개
            int requiredSize = Mathf.Max(poolSize,
                Mathf.CeilToInt(projectileLifeTimeSeconds / Mathf.Max(0.01f, fireIntervalSeconds)) + 1);
            poolSize = requiredSize;

            pool = new ProjectileMover[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = Instantiate(projectilePrefab, transform);
                go.name = "Projectile_Pool_" + i;
                go.SetActive(false);

                ProjectileMover mover = go.GetComponent<ProjectileMover>();
                if (mover == null)
                    mover = go.AddComponent<ProjectileMover>();

                pool[i] = mover;
            }

            Debug.Log("[BaseTurretController] Pool=" + poolSize
                + " (수명=" + projectileLifeTimeSeconds + "s / 쿨타임=" + fireIntervalSeconds + "s)");
        }

        // ── Update ─────────────────────────────────
        protected virtual void Update()
        {
            Transform currentTarget = GetCurrentTarget();

            runtimeHasTargetWorldPosition  = false;
            runtimeIsWithinEngagementRange = false;
            runtimeIsAimedWithinThreshold  = false;
            runtimeIsFireCooldownActive    = false;
            runtimeAimErrorDegrees         = 0f;

            if (yawPivot == null || muzzlePoint == null) return;

            if (currentTarget != null)
            {
                runtimeHasTargetWorldPosition = true;
                runtimeTargetWorldPosition    = currentTarget.position;

                float dist = Vector3.Distance(muzzlePoint.position, currentTarget.position);
                runtimeIsWithinEngagementRange =
                    engagementRangeWorldUnits <= 0f || dist <= engagementRangeWorldUnits;

                UpdateYawTowardsTarget(currentTarget);
                if (pitchPivot != null)
                    UpdatePitchTowardsTarget(currentTarget);

                RefreshAimDiagnostics(currentTarget);

                bool canFire = runtimeIsWithinEngagementRange && CanFireAdditionalConditions(currentTarget);
                bool cooling = Time.time < lastFireTimeSeconds + fireIntervalSeconds;
                runtimeIsFireCooldownActive = runtimeIsAimedWithinThreshold && canFire && cooling;

                if (canFire)
                    TryFireIfAimed();
            }
        }

        public bool TryGetTargetWorldPosition(out Vector3 pos)
        {
            pos = runtimeHasTargetWorldPosition ? runtimeTargetWorldPosition : default(Vector3);
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
            Vector3 toTarget = (target.position - pitchPivot.position).normalized;
            Vector3 localDir = Quaternion.Inverse(yawPivot.rotation) * toTarget;
            float desired    = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;
            desired          = Mathf.Clamp(desired, minPitchDegrees, maxPitchDegrees);

            float current = NormalizeAngle180(pitchPivot.localEulerAngles.x);
            float next    = Mathf.MoveTowardsAngle(current, desired,
                                pitchSpeedDegreesPerSecond * Time.deltaTime);
            pitchPivot.localRotation = Quaternion.Euler(next, 0f, 0f);
        }

        /// <summary>
        /// 조준 판정: MuzzlePoint.forward 대신 YawPivot.forward(수평)와
        /// 타겟 수평 방향의 각도로 판단합니다.
        /// 모델 축이 뒤틀린 경우에도 Yaw 회전 결과만으로 안정적으로 동작합니다.
        /// </summary>
        private void RefreshAimDiagnostics(Transform target)
        {
            // YawPivot 수평 forward
            Vector3 yawForward = yawPivot.forward;
            yawForward.y = 0f;
            if (yawForward.sqrMagnitude < 1e-6f)
            {
                runtimeAimErrorDegrees = 0f;
                runtimeIsAimedWithinThreshold = true;
                return;
            }
            yawForward.Normalize();

            // 타겟 수평 방향
            Vector3 toTarget = target.position - yawPivot.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 1e-6f)
            {
                runtimeAimErrorDegrees = 0f;
                runtimeIsAimedWithinThreshold = true;
                return;
            }
            toTarget.Normalize();

            float angle = Vector3.Angle(yawForward, toTarget);
            runtimeAimErrorDegrees        = angle;
            runtimeIsAimedWithinThreshold = angle <= fireAngleThresholdDegrees;
        }

        // ── 발사 ───────────────────────────────────
        private void TryFireIfAimed()
        {
            if (!runtimeIsAimedWithinThreshold) return;
            if (Time.time < lastFireTimeSeconds + fireIntervalSeconds) return;
            if (pool == null || pool.Length == 0) return;

            // 비활성 슬롯 탐색 (Round-Robin)
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

            // 모두 활성 중이면 가장 오래된 슬롯 강제 회수
            if (mover == null)
            {
                ProjectileMover oldest = pool[poolIndex % pool.Length];
                if (oldest != null)
                {
                    oldest.ForceReturn();
                    mover     = oldest;
                    poolIndex = (poolIndex + 1) % pool.Length;
                }
            }

            if (mover == null) return;

            // 발사 방향: YawPivot.forward 수평 벡터 사용
            // (MuzzlePoint.forward는 모델 축에 따라 신뢰 불가)
            Vector3 fireDir = yawPivot.forward;
            fireDir.y = 0f;
            if (fireDir.sqrMagnitude < 1e-6f)
                fireDir = Vector3.forward;
            fireDir.Normalize();

            // 발사 위치는 MuzzlePoint 사용
            Vector3 spawnPos = muzzlePoint != null ? muzzlePoint.position : yawPivot.position;

            mover.Launch(spawnPos, fireDir, projectileSpeed, projectileLifeTimeSeconds, projectileDamage);
            OnProjectileFired(mover);
            lastFireTimeSeconds = Time.time;
        }

        private static float NormalizeAngle180(float a)
        {
            return Mathf.Repeat(a + 180f, 360f) - 180f;
        }
    }
}
