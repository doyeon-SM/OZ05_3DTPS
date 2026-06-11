using UnityEngine;

public class PlayerMoveInteractionObject : MonoBehaviour, IInteraction
{
    [SerializeField] private Transform targetTransform;

    public void Interaction()
    {
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
}
