using UnityEngine;

public class PlayerMoveInteractionObject : MonoBehaviour, IInteraction
{
    [SerializeField] private Transform targetTransform;
    public void Interaction()
    {
        Debug.Log("Boss Entrance");
    }
}
