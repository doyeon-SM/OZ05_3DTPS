using UnityEngine;
using _01.Scenes.PhaseValidation._26._05._14;

/// <summary>
/// Enemy 자식 오브젝트에 미리 배치해두는 근접 공격 히트박스.
/// 평소에는 비활성화 상태로 유지하다가, 공격 애니메이션의
/// Hit 이벤트 타이밍에만 활성화하여 Player에게 데미지를 전달한다.
/// 메모리 낭비 없이 오브젝트를 재사용하는 방식으로 동작한다.
/// </summary>
public class EnemyMeleeHitbox : MonoBehaviour
{
    // 히트박스가 활성화된 동안 한 번만 피격 판정하도록 막는 플래그
    private bool hasHitThisSwing;

    // 데미지 값은 EnemyStatus에서 런타임에 주입받는다
    private int attackPower;

    // 이미 이번 스윙에서 히트한 콜라이더 중복 방지
    private Collider lastHitCollider;

    /// <summary>
    /// 공격 시작 전 EnemyAnimationEventReceiver에서 호출.
    /// 데미지 값을 주입하고 히트박스를 활성화한다.
    /// </summary>
    public void Activate(int damage)
    {
        attackPower = damage;
        hasHitThisSwing = false;
        lastHitCollider = null;
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
        // 한 스윙에 한 번만 피격 판정 (같은 콜라이더 중복 방지)
        if (hasHitThisSwing && other == lastHitCollider) return;

        // Player 레이어만 판정
        if (!other.CompareTag("Player")) return;

        PlayerStatus playerStatus = other.GetComponentInParent<PlayerStatus>();
        if (playerStatus == null) return;

        playerStatus.TakeDamage(attackPower);
        hasHitThisSwing = true;
        lastHitCollider = other;

        Debug.Log($"[EnemyMeleeHitbox] Player 피격 | damage={attackPower}");
    }

    private void OnDisable()
    {
        hasHitThisSwing = false;
        lastHitCollider = null;
    }
}
