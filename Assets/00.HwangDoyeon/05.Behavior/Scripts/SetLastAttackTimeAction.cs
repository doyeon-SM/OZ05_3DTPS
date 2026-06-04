using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Last Attack Time",
    story: "Set [LastAttackTime] to current time",
    category: "Action/AI", id: "e89cdca60acaeefbc45f1d0852eab428")]
public partial class SetLastAttackTimeAction : Action
{
    [SerializeReference] public BlackboardVariable<float>      LastAttackTime;
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    private bool  isWaitingForAttackWindow;
    private float attackWindowEndTime;

    private const float MinAttackWindowSeconds = 0.4f;
    private const float MaxAttackWindowSeconds = 0.8f;
    private const float MaxAttackFacingAngle   = 15f;

    private GameObject ResolveFallbackSelf() => GameObject;

    protected override Status OnUpdate()
    {
        if (Self != null && Self.Value == null)
            Self.Value = ResolveFallbackSelf();

        EnemyAIDebugVisualizer debugVisualizer = Self?.Value != null
            ? Self.Value.GetComponent<EnemyAIDebugVisualizer>() : null;
        EnemyAIConsoleMonitor monitor = Self?.Value != null
            ? Self.Value.GetComponent<EnemyAIConsoleMonitor>() : null;

        // ── 노드 진입 직후: NavMesh 회전 제어권 즉시 확보 ──────────────
        // windup 시작 전부터 회전을 시작해 공격 전 딜레이를 최소화한다.
        SetAgentRotationControl(false);

        // ── windup 시작 ──────────────────────────────────────────────
        if (!isWaitingForAttackWindow)
        {
            float wait          = UnityEngine.Random.Range(MinAttackWindowSeconds, MaxAttackWindowSeconds);
            attackWindowEndTime = Time.time + wait;
            isWaitingForAttackWindow = true;

            if (debugVisualizer != null)
                debugVisualizer.ReportBranch("Attack", $"Attack windup {wait:F2}s");
            if (monitor != null)
            {
                monitor.ReportAction("SetLastAttackTimeAction", $"windup={wait:F2}s");
                monitor.ReportBranch("Attack", "Attack windup");
            }
        }

        // ── 매 틱: 플레이어 방향으로 회전 (windup 중 / 완료 후 모두) ───
        RotateTowardTarget();

        // ── windup 대기 중 ────────────────────────────────────────────
        if (Time.time < attackWindowEndTime)
            return Status.Running;

        // ── windup 완료 — 정면 체크 ──────────────────────────────────
        // ±15° 맞을 때까지 회전하며 대기 → 등 뒤 공격 방지 유지
        if (!IsFacingTarget(out float facingAngle))
        {
            if (monitor != null)
                monitor.ReportAction("SetLastAttackTimeAction",
                    $"waiting for facing | angle={facingAngle:F1}°", false);
            return Status.Running;
        }

        // ── 공격 Trigger 발행 ─────────────────────────────────────────
        EnemyRpgAnimatorDriver animatorDriver = Self?.Value != null
            ? Self.Value.GetComponent<EnemyRpgAnimatorDriver>() : null;
        if (animatorDriver != null)
            animatorDriver.PrepareAttackParameters();

        LastAttackTime.Value     = Time.time;
        isWaitingForAttackWindow = false;

        if (debugVisualizer != null)
            debugVisualizer.ReportAttackTriggered();
        if (monitor != null)
        {
            monitor.ReportAction("SetLastAttackTimeAction",
                $"set LastAttackTime={LastAttackTime.Value:F2}", true);
            monitor.ReportBranch("Attack", "Attack committed");
        }
        return Status.Success;
    }

    protected override void OnEnd()
    {
        // 노드 종료(성공·실패·중단 모두) 시 NavMesh 회전 제어권 반환
        isWaitingForAttackWindow = false;
        SetAgentRotationControl(true);
    }

    // ─── 헬퍼 ─────────────────────────────────────────────────────────

    /// <summary>NavMeshAgent의 자동 회전을 켜거나 끈다.</summary>
    private void SetAgentRotationControl(bool enabled)
    {
        if (Self?.Value == null) return;
        NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.updateRotation = enabled;
    }

    /// <summary>rotationSpeed로 플레이어를 향해 회전한다.</summary>
    private void RotateTowardTarget()
    {
        if (Self?.Value == null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        Transform self    = Self.Value.transform;
        Vector3   toTarget = playerObj.transform.position - self.position;
        Vector3   flat    = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.sqrMagnitude < 0.001f) return;

        EnemyBehaviorBridge bridge = Self.Value.GetComponent<EnemyBehaviorBridge>();
        EnemyConfigSO config       = bridge != null ? bridge.Config : null;
        float rotSpeed             = config != null ? config.rotationSpeed : 720f;

        Quaternion targetRot = Quaternion.LookRotation(flat.normalized);
        self.rotation = Quaternion.RotateTowards(self.rotation, targetRot, rotSpeed * Time.deltaTime);
    }

    /// <summary>플레이어가 정면 ±15° 이내에 있는지 확인한다.</summary>
    private bool IsFacingTarget(out float facingAngle)
    {
        facingAngle = 0f;
        if (Self?.Value == null) return true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return true;

        Transform self    = Self.Value.transform;
        Vector3   toTarget = playerObj.transform.position - self.position;
        Vector3   flat    = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.sqrMagnitude < 0.001f) return true;

        facingAngle = Vector3.Angle(self.forward, flat.normalized);
        return facingAngle <= MaxAttackFacingAngle;
    }
}
