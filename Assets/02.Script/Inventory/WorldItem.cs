using UnityEngine;
using _01.Scenes.PhaseValidation;

public class WorldItem : MonoBehaviour
{
    public string itemID;
    public string itemDisplayName;
    public int amount = 1;

    /// <summary>
    /// 줍기 완료 시 ItemPickupCollector에서 호출.
    /// ItemDropPoolManager가 있으면 풀 반환, 없으면 Destroy.
    /// </summary>
    public void ReturnOrDestroy()
    {
        if (ItemDropPoolManager.Instance != null)
            ItemDropPoolManager.Instance.ReturnToPool(this);
        else
            Destroy(gameObject);
    }
}
