using UnityEngine;

public class RepairObject : MonoBehaviour, IInteraction
{
    // true = 수리됨, false = 수리필요(기본상태)
    private bool _isRepair = false;

    public bool IsRepair => _isRepair;
    public void Interaction()
    {
        Debug.Log("[RepairObject] 수리 오브젝트 상호작용 | 현재 상태: " + (_isRepair ? "수리됨" : "수리필요"));
    }
}
