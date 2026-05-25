using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    /// <summary>
    /// 자식 오브젝트(콜라이더가 있는 메시)에 붙여서
    /// 데미지를 루트의 EnemyStatus로 전달하는 브릿지 컴포넌트.
    /// Enemy_Ch35 등 콜라이더가 자식에 분리된 구조에서 사용한다.
    /// </summary>
    public class EnemyDamageReceiver : MonoBehaviour, IDamageable
    {
        private EnemyStatus rootStatus;

        private void Awake()
        {
            // 루트 오브젝트에서 EnemyStatus를 찾아 연결
            rootStatus = GetComponentInParent<EnemyStatus>();

            if (rootStatus == null)
                Debug.LogError($"[EnemyDamageReceiver] {gameObject.name}: 부모에서 EnemyStatus를 찾을 수 없습니다.");
        }

        public void TakeDamage(int value)
        {
            rootStatus?.TakeDamage(value);
        }
    }
}
