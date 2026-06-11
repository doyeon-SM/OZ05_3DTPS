using UnityEngine;

namespace _01.Scenes.PhaseValidation
{
    public class PlayerMoveInteractionObject : MonoBehaviour, IInteraction
    {
        [Header("상호작용 UI")]
        [SerializeField] private string _interactionLabel = "[E] 이동";

        [SerializeField] private Transform targetTransform;

        [Header("목표 달성 게이팅")]
        [Tooltip("true이면 StageManager 목표 100% 달성 시에만 상호작용이 허용됩니다.")]
        [SerializeField] private bool requireGoalComplete = false;

        [Tooltip("목표 미달성 시 표시할 레이블")]
        [SerializeField] private string lockedLabel = "[목표 미달성]";

        // IInteraction
        public string InteractionLabel
        {
            get
            {
                if (requireGoalComplete && !IsGoalComplete())
                    return lockedLabel;
                return _interactionLabel;
            }
        }

        public void Interaction()
        {
            // 목표 달성 게이팅
            if (requireGoalComplete && !IsGoalComplete())
            {
                float pct = StageManager.Instance != null ? StageManager.Instance.GoalPercent * 100f : 0f;
                Debug.Log($"[PlayerMoveInteractionObject] 목표 미달성({pct:F0}%) — 이동 불가.");
                return;
            }

            if (targetTransform == null)
            {
                Debug.LogWarning("[PlayerMoveInteractionObject] targetTransform이 비어 있습니다.");
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[PlayerMoveInteractionObject] 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다.");
                return;
            }

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
                cc.enabled = true;
            }
            else
            {
                player.transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
            }

            Debug.Log($"[PlayerMoveInteractionObject] Player를 {targetTransform.position}으로 이동했습니다.");
        }

        private bool IsGoalComplete()
        {
            return StageManager.Instance != null && StageManager.Instance.GoalPercent >= 1.0f;
        }
    }
}
