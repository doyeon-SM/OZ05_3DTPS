using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 단일 Transform 타겟을 추적하는 기본 포탑 구현입니다.
    /// target이 Inspector에서 비어 있으면 Player 태그 오브젝트를 자동으로 탐색합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutoTargetTurretController : BaseTurretController
    {
        private const string PlayerTag = "Player";

        [Header("Target")]
        [SerializeField]
        [Tooltip("추적할 타겟 Transform. 비워두면 Player 태그 오브젝트를 자동으로 탐색합니다.")]
        private Transform target;

        private void Awake()
        {
            if (target == null)
                TryBindPlayer();
        }

        private void TryBindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
            if (player != null)
                target = player.transform;
            else
                Debug.LogWarning($"[AutoTargetTurretController] '{gameObject.name}': Player 태그 오브젝트를 찾을 수 없습니다.");
        }

        protected override Transform GetCurrentTarget()
        {
            // 런타임 중 타겟이 사라진 경우 재탐색
            if (target == null)
                TryBindPlayer();

            return target;
        }
    }
}
