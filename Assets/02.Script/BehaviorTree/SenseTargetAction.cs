using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Sense Target",
    story: "[Self] senses [Target] and updates",
    category: "Action/AI", id: "24cb70622f80a7812633f21cbf35c323")]
public partial class SenseTargetAction : Action
{
    private const string PlayerTag              = "Player";
    private const string AttackTriggerParameter = "Trigger";
    private const float  DefaultInvestigateArrivalTolerance = 1.2f;
    private const float  DefaultInvestigateTimeoutSeconds   = 8.0f;
    private const float  MaxAttackFacingAngle               = 15f;

    private float nextFallbackAttackTime;
    private float investigateStartedTime      = -1f;
    private bool  wasInvestigatingWithLastKnown;
    private int   fallbackPatrolIndex;
    private float nextFallbackPatrolDecisionTime = -1f;

    // ─── Blackboard 변수 ─────────────────────────────────────────────
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<bool>       CanSeeTarget;
    [SerializeReference] public BlackboardVariable<float>      DistanceToTarget;
    [SerializeReference] public BlackboardVariable<Vector3>    LastKnownPosition;
    [SerializeReference] public BlackboardVariable<bool>       HasLastKnownPosition;
    [SerializeReference] public BlackboardVariable<Vector3>    TargetPosition;

    protected override Status OnUpdate()
    {
        EnemyAIDebugVisualizer debugVisualizer = Self?.Value != null
            ? Self.Value.GetComponent<EnemyAIDebugVisualizer>() : null;
        EnemyAIConsoleMonitor consoleMonitor = Self?.Value != null
            ? Self.Value.GetComponent<EnemyAIConsoleMonitor>() : null;

        // Target 자동 탐색
        if (Target != null && Target.Value == null)
        {
            try
            {
                GameObject found = GameObject.FindGameObjectWithTag(PlayerTag);
                if (found != null) Target.Value = found;
            }
            catch (UnityException) { }
        }

        // 유효성 검사
        if (Self?.Value == null || Target?.Value == null)
        {
            CanSeeTarget.Value     = false;
            DistanceToTarget.Value = 9999f;
            if (debugVisualizer != null)
                debugVisualizer.ReportSense(false, 9999f, HasLastKnownPosition.Value,
                    LastKnownPosition.Value, Vector3.zero);
            if (consoleMonitor != null)
            {
                consoleMonitor.ReportFailure("SenseTargetAction", "Self or Target is null");
                consoleMonitor.ReportSenseEvaluation(false, 9999f, HasLastKnownPosition.Value,
                    HasLastKnownPosition.Value ? "Investigate" : "Patrol");
            }
            return Status.Success;
        }

        Transform self   = Self.Value.transform;
        Transform target = Target.Value.transform;

        EnemyBehaviorBridge bridge = Self.Value.GetComponent<EnemyBehaviorBridge>();
        EnemyConfigSO config       = bridge != null ? bridge.Config : null;

        float detectRadius      = config != null ? config.detectRadius      : 10f;
        float viewAngle         = config != null ? config.viewAngle         : 120f;
        LayerMask obstacleLayer = config != null ? config.obstacleLayer     : ~0;
        float attackRange       = config != null ? config.attackRange       : 2.1f;
        float rotSpeed          = config != null ? config.rotationSpeed     : 720f;
        float chaseSpeed        = config != null ? config.chaseSpeed        : 3.8f;

        // ─── 거리·방향 계산 ──────────────────────────────────────────
        Vector3 toTarget      = target.position - self.position;
        Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        float   planarDistance = flatDirection.magnitude;

        DistanceToTarget.Value = planarDistance;
        TargetPosition.Value   = target.position;

        bool visible  = false;
        bool inRadius = false;
        bool inFov    = false;
        bool blocked  = false;

        // ─── 시야 감지 ───────────────────────────────────────────────
        if (planarDistance <= detectRadius)
        {
            inRadius = true;
            float angle = Vector3.Angle(self.forward, flatDirection.normalized);

            if (angle <= viewAngle * 0.5f)
            {
                inFov = true;
                Vector3 origin     = self.position + Vector3.up * 1.5f;
                Vector3 targetEyes = target.position + Vector3.up * 1.0f;
                float   rayDist    = Vector3.Distance(origin, targetEyes);

                blocked = Physics.Raycast(origin, (targetEyes - origin).normalized, rayDist, obstacleLayer);
                visible = !blocked;
            }
        }

        CanSeeTarget.Value = visible;

        if (visible)
        {
            LastKnownPosition.Value       = target.position;
            HasLastKnownPosition.Value    = true;
            investigateStartedTime        = -1f;
            wasInvestigatingWithLastKnown = false;
        }

        // ─── NavMeshAgent Fallback 처리 ──────────────────────────────
        NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();

        if (visible)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                if (planarDistance > attackRange)
                {
                    // ── 추격 ─────────────────────────────────────────
                    // 공격 범위를 벗어났으므로 NavMesh 회전 제어권 반환
                    agent.updateRotation = true;
                    agent.isStopped      = false;
                    agent.speed          = chaseSpeed;
                    agent.SetDestination(target.position);

                    if (consoleMonitor != null)
                        consoleMonitor.ReportAction("SenseTargetAction",
                            $"fallback chase | speed={agent.speed:F2} dest={target.position}", false);
                }
                else
                {
                    // ── 공격 범위 내 — 회전 후 트리거 ────────────────
                    agent.isStopped = true;

                    // [핵심] NavMeshAgent의 자동 회전을 끄고 스크립트가 직접 제어
                    // → agent.angularSpeed가 rotationSpeed를 덮어쓰는 문제 해결
                    agent.updateRotation = false;

                    if (flatDirection.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(flatDirection.normalized);
                        self.rotation = Quaternion.RotateTowards(
                            self.rotation, targetRot, rotSpeed * Time.deltaTime);
                    }

                    float facingAngle = Vector3.Angle(self.forward, flatDirection.normalized);
                    bool  isFacing    = facingAngle <= MaxAttackFacingAngle;

                    if (isFacing && Time.time >= nextFallbackAttackTime)
                    {
                        EnemyRpgAnimatorDriver animatorDriver =
                            Self.Value.GetComponent<EnemyRpgAnimatorDriver>();
                        if (animatorDriver != null)
                            animatorDriver.PrepareAttackParameters();

                        Animator selfAnimator = Self.Value.GetComponent<Animator>();
                        if (selfAnimator != null)
                        {
                            selfAnimator.SetTrigger(AttackTriggerParameter);
                            float cooldown         = config != null ? Mathf.Max(0.2f, config.attackCooldown) : 0.8f;
                            nextFallbackAttackTime = Time.time + cooldown;

                            if (debugVisualizer != null)
                                debugVisualizer.ReportBranch("Attack", "Sense fallback attack trigger");
                            if (consoleMonitor != null)
                            {
                                consoleMonitor.ReportAction("SenseTargetAction",
                                    $"fallback attack trigger | planar={planarDistance:F2} cooldown={cooldown:F2}", true);
                                consoleMonitor.ReportBranch("Attack", "Sense fallback attack trigger");
                            }
                        }
                    }
                    else if (!isFacing && consoleMonitor != null)
                    {
                        consoleMonitor.ReportAction("SenseTargetAction",
                            $"rotating to face target | angle={facingAngle:F1}°", false);
                    }
                }
            }
        }
        else
        {
            // 시야를 잃었을 때 NavMesh 회전 제어권 반환
            if (agent != null)
                agent.updateRotation = true;

            if (HasLastKnownPosition.Value)
            {
                if (!wasInvestigatingWithLastKnown)
                {
                    investigateStartedTime        = Time.time;
                    wasInvestigatingWithLastKnown = true;
                    if (consoleMonitor != null)
                        consoleMonitor.ReportAction("SenseTargetAction",
                            $"investigate started | lastKnown={LastKnownPosition.Value}", true);
                }

                float investigateArrivalTolerance = Mathf.Max(
                    0.3f,
                    config != null ? config.attackRange * 0.5f : DefaultInvestigateArrivalTolerance);
                float investigateTimeoutSeconds = DefaultInvestigateTimeoutSeconds;
                if (config != null)
                {
                    float cs = Mathf.Max(0.1f, config.chaseSpeed);
                    investigateTimeoutSeconds = Mathf.Max(
                        DefaultInvestigateTimeoutSeconds,
                        (config.detectRadius / cs) + 2.0f);
                }

                float planarToLastKnown = Vector3.Distance(
                    new Vector3(self.position.x, 0f, self.position.z),
                    new Vector3(LastKnownPosition.Value.x, 0f, LastKnownPosition.Value.z));

                bool reachedByDistance = planarToLastKnown <= investigateArrivalTolerance;
                bool reachedByAgent    = agent != null && agent.isOnNavMesh &&
                                         !agent.pathPending &&
                                         agent.remainingDistance <= investigateArrivalTolerance;
                bool investigateTimedOut = investigateStartedTime > 0f &&
                                           Time.time - investigateStartedTime >= investigateTimeoutSeconds;

                if (reachedByDistance || reachedByAgent || investigateTimedOut)
                {
                    HasLastKnownPosition.Value    = false;
                    investigateStartedTime        = -1f;
                    wasInvestigatingWithLastKnown = false;

                    if (agent != null && agent.isOnNavMesh) agent.ResetPath();

                    if (debugVisualizer != null)
                        debugVisualizer.ReportSenseHint("Patrol",
                            investigateTimedOut ? "Investigate timeout -> Patrol" : "Investigate reached -> Patrol");
                    if (consoleMonitor != null)
                    {
                        string cr = investigateTimedOut ? "timeout" : "reached-last-known";
                        consoleMonitor.ReportAction("SenseTargetAction",
                            $"clear last known | reason={cr} planarToLastKnown={planarToLastKnown:F2}", true);
                        consoleMonitor.ReportBranch("Patrol", $"Investigate complete ({cr})");
                    }
                }

                if (HasLastKnownPosition.Value && agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed     = chaseSpeed;
                    agent.SetDestination(LastKnownPosition.Value);

                    if (consoleMonitor != null)
                        consoleMonitor.ReportAction("SenseTargetAction",
                            $"fallback investigate chase | speed={agent.speed:F2} dest={LastKnownPosition.Value}", false);
                }
            }
            else
            {
                investigateStartedTime        = -1f;
                wasInvestigatingWithLastKnown = false;
                DriveFallbackPatrol(self, bridge, config, consoleMonitor, debugVisualizer);
            }
        }

        // ─── 디버그 리포트 ────────────────────────────────────────────
        if (debugVisualizer != null)
        {
            debugVisualizer.ReportSense(visible, planarDistance, HasLastKnownPosition.Value,
                LastKnownPosition.Value, target.position);
            debugVisualizer.ReportSenseHint(
                GetSuggestedBranch(visible, planarDistance, attackRange), "Sense evaluation");
        }
        if (consoleMonitor != null)
        {
            string branch = GetSuggestedBranch(visible, planarDistance, attackRange);
            consoleMonitor.ReportSenseEvaluation(visible, planarDistance, HasLastKnownPosition.Value, branch);
            consoleMonitor.ReportSenseBreakdown(
                visible, planarDistance, detectRadius, attackRange,
                inRadius, inFov, blocked, HasLastKnownPosition.Value, branch);
        }

        return Status.Success;
    }

    // ─── 헬퍼 ────────────────────────────────────────────────────────
    private string GetSuggestedBranch(bool visible, float distance, float attackRange)
    {
        if (visible && distance <= attackRange) return "Attack";
        if (visible)                             return "Chase";
        if (HasLastKnownPosition.Value)          return "Investigate";
        return "Patrol";
    }

    private void DriveFallbackPatrol(
        Transform self,
        EnemyBehaviorBridge bridge,
        EnemyConfigSO config,
        EnemyAIConsoleMonitor consoleMonitor,
        EnemyAIDebugVisualizer debugVisualizer)
    {
        NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh) return;
        if (Time.time < nextFallbackPatrolDecisionTime) return;

        float patrolSpeed      = config != null ? config.patrolSpeed : agent.speed;
        float arrivalThreshold = Mathf.Max(0.25f, agent.stoppingDistance + 0.15f);
        bool  reachedCurrent   = agent.hasPath && !agent.pathPending &&
                                  agent.remainingDistance <= arrivalThreshold;
        bool  hasBrokenPath    = agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete;
        bool  shouldSelectNew  = !agent.hasPath || reachedCurrent || hasBrokenPath || agent.isStopped;

        if (!shouldSelectNew) return;

        fallbackPatrolIndex++;
        agent.isStopped = false;
        agent.speed     = patrolSpeed;
        nextFallbackPatrolDecisionTime = Time.time + 0.35f;

        if (debugVisualizer != null)
            debugVisualizer.ReportSenseHint("Patrol", "Sense fallback patrol steering");
        if (consoleMonitor != null)
            consoleMonitor.ReportBranch("Patrol", "Sense fallback patrol steering");
    }
}
