using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

/// <summary>
/// Enemy 자식 오브젝트에 미리 배치해두는 근접 공격 히트박스.
/// 평소에는 비활성화 상태로 유지하다가, 공격 애니메이션의
/// Hit 이벤트 타이밍에만 활성화하여 Player에게 데미지를 전달한다.
/// 
/// [버그 수정] 스윙 1회당 1회 피격만 허용.
///   - 기존: hasHitThisSwing && other==lastHitCollider (AND 조건) →
///            다른 콜라이더이거나 lastHitCollider가 null이면 hasHitThisSwing=true여도 통과
///   - 수정: hasHitThisSwing 단독 체크 → 이미 한 번 맞았으면 무조건 차단
/// </summary>
public class EnemyMeleeHitbox : MonoBehaviour
{
    // 스윙 1회에 한 번만 피격되도록 막는 플래그
    private bool hasHitThisSwing;

    // 데미지 값은 EnemyStatus에서 런타임에 주입받는다
    private int attackPower;

    /// <summary>
    /// 공격 시작 전 EnemyAnimationEventReceiver에서 호출.
    /// 데미지 값을 주입하고 히트박스를 활성화한다.
    /// </summary>
    public void Activate(int damage)
    {
        attackPower = damage;
        hasHitThisSwing = false;   // 매 스윙마다 반드시 초기화
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 공격 종료 후 EnemyAnimationEventReceiver에서 호출.
    /// 히트박스를 비활성화하여 판정을 끈다.
    /// </summary>
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // [핵심 수정] hasHitThisSwing 단독 검사 → 이미 한 번 맞았으면 모든 콜라이더 차단
        if (hasHitThisSwing) return;

        // Player 태그만 판정
        if (!other.CompareTag("Player")) return;

        PlayerStatus playerStatus = other.GetComponentInParent<PlayerStatus>();
        if (playerStatus == null) return;

        // 피격 처리 전에 플래그를 true로 → 콜백이 여러 번 들어와도 안전
        hasHitThisSwing = true;

        playerStatus.TakeDamage(attackPower);
        Debug.Log($"[EnemyMeleeHitbox] Player 피격 | damage={attackPower}");
    }

    private void OnDisable()
    {
        // 비활성화 시 반드시 초기화 (오브젝트 풀링 대응)
        hasHitThisSwing = false;
    }
}
