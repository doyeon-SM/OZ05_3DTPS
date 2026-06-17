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

        public bool TryInteract(PlayerInventory playerInventory, SingleStringEventChannel fallbackItemIDEventChannel = null)
        {
            if (!CanUseWeaponData())
                return false;

            if (playerInventory != null)
            {
                if (!playerInventory.TryUnlockWeaponFromPickup(currentWeaponData, out bool alreadyUnlocked))
                    return false;

                if (alreadyUnlocked)
                    Debug.Log($"[InteractableWeaponItem] 이미 언락된 무기입니다. WeaponId: {currentWeaponData.WeaponId}", this);
                else
                    Debug.Log($"[InteractableWeaponItem] 무기 언락 완료. WeaponId: {currentWeaponData.WeaponId}", this);

                Pickup();
                return true;
            }

            if (fallbackItemIDEventChannel != null)
            {
                fallbackItemIDEventChannel.Raise(currentWeaponData.WeaponId);
                Pickup();
                return true;
            }

            Debug.LogError("[InteractableWeaponItem] PlayerInventory와 fallback ItemIDEventChannel이 모두 없어 무기를 언락할 수 없습니다.", this);
            return false;
        }

        public void Pickup()
        {
            Destroy(gameObject);
        }

        private bool CanUseWeaponData()
        {
            if (currentWeaponData == null)
            {
                Debug.LogError($"[InteractableWeaponItem] {gameObject.name}의 currentWeaponData가 null입니다. Inspector에서 WeaponData S.O를 연결해주세요.", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(currentWeaponData.WeaponId))
            {
                Debug.LogError($"[InteractableWeaponItem] {currentWeaponData.name}의 WeaponId가 비어 있습니다. WeaponData S.O의 WeaponId를 입력해주세요.", currentWeaponData);
                return false;
            }

            return true;
        }
    }
}
