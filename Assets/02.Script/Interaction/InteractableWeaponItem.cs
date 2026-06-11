using UnityEngine;

namespace _00.ChoiHeesu._02.RayCastInteract
{
    public class InteractableWeaponItem : MonoBehaviour
    {
        [Header("오브젝트가 가지고있는 아이템 데이터")]
        [SerializeField] private WeaponData currentWeaponData;

        private void Awake()
        {
            if (currentWeaponData == null)
                Debug.LogError($"[InteractableWeaponItem] {gameObject.name}의 currentWeaponData가 null입니다. Inspector에서 WeaponData S.O를 연결해주세요.", this);
        }

        public WeaponData GetWeaponData()
        {
            return currentWeaponData;
        }

        public void Pickup()
        {
            gameObject.SetActive(false);
        }
    }
}
