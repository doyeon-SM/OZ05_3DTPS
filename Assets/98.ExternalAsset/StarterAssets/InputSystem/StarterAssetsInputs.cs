using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		public bool Interact;
		public bool Attack;
		public bool Inventory;
		public bool WeaponSelect;
		public bool WeaponSelectPressed;
		public bool WeaponSelectReleased;
		public bool Roll;
		public bool Grenade;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		//custom 
		
		public void OnAttack(InputValue value)
		{
			AttackInput(value.isPressed);
		}

		public void OnInteraction(InputValue value)
		{
			InteractInput(value.isPressed);
		}
		public void OnInventory(InputValue value)
        {
			InventoryInput(value.isPressed);
        }

		public void OnWeaponSelect(InputValue value)
		{
			WeaponSelectInput(value.isPressed);
		}

		public void OnRoll(InputValue value)
		{
			RollInput(value.isPressed);
		}

		public void OnGrenade(InputValue value)
		{
			GrenadeInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		//custom 
		
		public void InteractInput(bool newInteractState)
		{
			Interact = newInteractState;
		}

		public void AttackInput(bool newAttackState)
		{
			Attack = newAttackState;
		}
		public void InventoryInput(bool newInventoryState)
        {
			Inventory = newInventoryState;
        }

		public void WeaponSelectInput(bool newWeaponSelectState)
		{
			WeaponSelectPressed = newWeaponSelectState && !WeaponSelect;
			WeaponSelectReleased = !newWeaponSelectState && WeaponSelect;
			WeaponSelect = newWeaponSelectState;
		}

		public void ConsumeWeaponSelectInput()
		{
			WeaponSelectPressed = false;
			WeaponSelectReleased = false;
		}

		public void RollInput(bool newRollState)
		{
			Roll = newRollState;
			//Debug용 임시 ( 기능 추가시 지울것 )
			Debug.Log("Roll 키 입력 들어옴.");
		}

		public void GrenadeInput(bool newGrenadeState)
		{
			Grenade = newGrenadeState;
			Debug.Log("Grenade 키 입력 들어옴.");
		}
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}
