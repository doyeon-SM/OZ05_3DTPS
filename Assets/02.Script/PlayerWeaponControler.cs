using UnityEngine;
using UnityEngine.InputSystem;

namespace _01.Scenes.PhaseValidation._26._05._14
{
    public class PlayerWeaponControler : MonoBehaviour
    {
        //SO 관리 및 투사체 구현
        
        [Header("무기 목록")]
        [SerializeField] private WeaponData[] weapons;
        [Header("Input (Input System)")]
        [SerializeField] private InputAction equipWeapon1Action = new InputAction("EquipWeapon1", InputActionType.Button , "<keyboard>/1");
        [SerializeField] private InputAction equipWeapon2Action = new InputAction("EquipWeapon2", InputActionType.Button , "<keyboard>/2");
        [SerializeField] private InputAction equipWeapon3Action = new InputAction("EquipWeapon3", InputActionType.Button , "<keyboard>/3");
        [SerializeField] private InputAction attackAction = new InputAction("Attack", InputActionType.Button , "<Mouse>/leftButton");
        [SerializeField] private InputAction reloadAction = new InputAction("Reload", InputActionType.Button , "<keyboard>/r");
        
        WeaponRuntime currentWeapon;
        private int currentWeaponIndex;
        private float nextAttackTime;

        #region UnityFunctions

        private void OnEnable()
        {
            equipWeapon1Action.performed += equipWeapon1ActionPerformed;
            equipWeapon2Action.performed += equipWeapon2ActionPerformed;
            equipWeapon3Action.performed += equipWeapon3ActionPerformed;
            attackAction.performed += attackActionPerformed;
            reloadAction.performed += reloadActionPerformed;
            
            
            equipWeapon1Action.Enable();
            equipWeapon2Action.Enable();
            equipWeapon3Action.Enable();
            attackAction.Enable();
            reloadAction.Enable();
        }

        private void OnDisable()
        {
            equipWeapon1Action.performed -= equipWeapon1ActionPerformed;
            equipWeapon2Action.performed -= equipWeapon2ActionPerformed;
            equipWeapon3Action.performed -= equipWeapon3ActionPerformed;
            attackAction.performed -= attackActionPerformed;
            reloadAction.performed -= reloadActionPerformed;
            
            equipWeapon1Action.Disable();
            equipWeapon2Action.Disable();
            equipWeapon3Action.Disable();
            attackAction.Disable();
            reloadAction.Disable();
        }
        #endregion

        #region Performed

        private void equipWeapon1ActionPerformed(InputAction.CallbackContext _)
        {
            equipWeapon(0);
        }
        private void equipWeapon2ActionPerformed(InputAction.CallbackContext _)
        {
            equipWeapon(1);
        }
        private void equipWeapon3ActionPerformed(InputAction.CallbackContext _)
        {
            equipWeapon(2);
        }

        private void attackActionPerformed(InputAction.CallbackContext _)
        {
            TryAttack();
        }

        private void reloadActionPerformed(InputAction.CallbackContext _)
        {
            Reload();
        }
        #endregion
        
        private void equipWeapon(int index)
        {
            if(weapons == null || weapons.Length == 0) return;
            if(index <0 || index >= weapons.Length) return;
            
            if (weapons[index] == null)
            {
                Debug.LogError("weapons가 등록되어있지 않습니다.");
            }

            currentWeaponIndex = index;
            currentWeapon = new WeaponRuntime(weapons[index]);
            
            Debug.Log($"[{index}] 무기 : {currentWeapon.data.weaponName} | 분류 : {currentWeapon.data.WeaponType.ToString()} \n" +
                      $"해당 무기 데미지 {currentWeapon.data.damage} | 사거리 : {currentWeapon.data.attackRange} 탄창 사이즈 : {currentWeapon.data.magazineSize}\n" +
                      $"근접 무기 여부 : {(currentWeapon.data.useAmmo == true ? "원거리 무기":"근접 무기")}");
        }
        private void TryAttack()
        {
            if(currentWeapon == null) return;
            if (!currentWeapon.data.useAmmo)
            {
                Debug.Log($"[TryAttack] {currentWeapon.data.name}으로 근접 공격을 시도합니다 ! 데미지 : {currentWeapon.data.damage}");
            }

            if (!currentWeapon.HasAmmo())
            {
                Debug.LogWarning($"[TryAttack] 현재 무기 : {currentWeapon.data.name} 의 총알이 없습니다, 재장전 해주세요.");
                return;
            }
            
            currentWeapon.ConsumeAmmo();

            Debug.Log(
                $"[TryAttack]{currentWeapon.data.name}로 공격했습니다 탄환 {currentWeapon.currentAmmo} / {currentWeapon.data.magazineSize}");

        }
        private void Reload()
        {
            currentWeapon.Reload();
            Debug.Log($"[Reload]{currentWeapon.data.name}를 재장전 했습니다. 현재 : {currentWeapon.currentAmmo} / {currentWeapon.data.magazineSize}");
        }

    }
}