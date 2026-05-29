using UnityEngine;
using _01.Scenes.PhaseValidation;

/// <summary>
/// 애니메이션 이벤트를 수신해 근접 히트박스의 활성/비활성을 제어한다.
/// Hit()   — 공격 판정 구간 시작 (히트박스 ON)
/// HitEnd() — 공격 판정 구간 종료 (히트박스 OFF)
/// 히트박스 오브젝트는 Enemy 자식에 미리 배치되어 있어야 한다.
/// </summary>
public class EnemyAnimationEventReceiver : MonoBehaviour
{
    [Tooltip("Enemy 자식에 미리 배치된 MeleeHitbox 오브젝트")]
    [SerializeField] private EnemyMeleeHitbox meleeHitbox;

    private EnemyStatus enemyStatus;

    private void Awake()
    {
        enemyStatus = GetComponent<EnemyStatus>();

        if (meleeHitbox == null)
            Debug.LogWarning($"[EnemyAnimationEventReceiver] {gameObject.name}: meleeHitbox가 연결되지 않았습니다.");
        if (enemyStatus == null)
            Debug.LogWarning($"[EnemyAnimationEventReceiver] {gameObject.name}: EnemyStatus를 찾을 수 없습니다.");
    }

    // RPG controller FootL AnimationEvent
    public void FootL() { }

    // RPG controller FootR AnimationEvent
    public void FootR() { }

    /// <summary>
    /// 공격 판정 구간 시작 — 애니메이션 이벤트에서 호출
    /// </summary>
    public void Hit()
    {
        if (meleeHitbox == null || enemyStatus == null) return;
        meleeHitbox.Activate(enemyStatus.AttackPower);
    }

    /// <summary>
    /// 공격 판정 구간 종료 — 애니메이션 이벤트에서 호출
    /// </summary>
    public void HitEnd()
    {
        if (meleeHitbox == null) return;
        meleeHitbox.Deactivate();
    }

    // Footstep 이벤트 수신 (경고 제거용)
    public void OnFootstep(AnimationEvent animationEvent) { }
}
