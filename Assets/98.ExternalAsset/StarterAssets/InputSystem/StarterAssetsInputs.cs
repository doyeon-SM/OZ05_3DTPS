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
		[SerializeField] private bool lookInputBlocked;

		public bool Interact;
		public bool Attack;
		public bool Inventory;
		public bool Map;
		public bool WeaponSelect;
		public bool WeaponSelectPressed;
		public bool WeaponSelectReleased;
		public bool Roll;
		public bool Grenade;
		public bool GrenadePressed;
		public bool GrenadeReleased;
		public bool Reload;
		public bool UIClose;
		public bool AimHold;
		public bool AimHoldPressed;
		public bool AimHoldReleased;
		public bool ADSClick;
		public bool ADSClickPressed;
		public bool ADSClickReleased;

		[SerializeField]
		private bool logWeaponSelectInput;

#if ENABLE_INPUT_SYSTEM
		private const string WeaponSelectActionName = "WeaponSelect";
		private const string AimHoldActionName = "AimHold";
		private const string ADSClickActionName = "ADSClick";

		private PlayerInput playerInput;
		private InputAction weaponSelectAction;
		private InputAction aimHoldAction;
		private InputAction adsClickAction;
		private bool aimInputActionsSubscribed;
#endif

#if ENABLE_INPUT_SYSTEM
		private void Awake()
		{
			CacheWeaponSelectAction();
			CacheAimInputActions();
		}

		private void OnEnable()
		{
			CacheWeaponSelectAction();
			CacheAimInputActions();
			SubscribeAimInputActions();
			SyncWeaponSelectActionState();

			// 씬 전환 후 ActionMap이 비활성화되는 경우 강제 활성화
			if (playerInput != null && playerInput.currentActionMap == null)
				playerInput.SwitchCurrentActionMap("Player");
		}

		private void OnDisable()
		{
			UnsubscribeAimInputActions();
			AimHoldInput(false);
		}

		private void Update()
		{
			SyncWeaponSelectActionState();
		}
#endif

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

		public void OnMap(InputValue value)
		{
			MapInput(value.isPressed);
		}

		public void OnWeaponSelect(InputValue value)
		{
			bool isPressed = value.isPressed;

			if (logWeaponSelectInput)
				Debug.Log($"[StarterAssetsInputs] OnWeaponSelect 호출됨. isPressed: {isPressed}", this);

			WeaponSelectInput(isPressed);
		}

		public void OnRoll(InputValue value)
		{
			RollInput(value.isPressed);
		}

		public void OnGrenade(InputValue value)
		{
			GrenadeInput(value.isPressed);
		}

		public void OnReload(InputValue value)
		{
			ReloadInput(value.isPressed);
		}

		public void OnUIClose(InputValue value)
		{
			// TODO : 구현 완료시 지울것
			Debug.Log("[StarterAssetsInputs] UIClose 입력", this);
			UICloseInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			if (lookInputBlocked)
			{
				look = Vector2.zero;
				return;
			}

			look = newLookDirection;
		}

		public void SetLookInputBlocked(bool isBlocked)
		{
			lookInputBlocked = isBlocked;

			if (lookInputBlocked)
				look = Vector2.zero;
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

		public void MapInput(bool newMapState)
		{
			Map = newMapState;
		}

		public void WeaponSelectInput(bool newWeaponSelectState)
		{
			WeaponSelectPressed = newWeaponSelectState && !WeaponSelect;
			WeaponSelectReleased = !newWeaponSelectState && WeaponSelect;
			WeaponSelect = newWeaponSelectState;
		}

		public void UICloseInput(bool newUICloseState)
		{
			UIClose = newUICloseState;
		}

		public void AimHoldInput(bool newAimHoldState)
		{
			if (newAimHoldState && ADSClick)
				return;

			// TODO : 구현 완료시 지울것
			Debug.Log($"[StarterAssetsInputs] AimHold 입력: {newAimHoldState}", this);

			AimHoldPressed = newAimHoldState && !AimHold;
			AimHoldReleased = !newAimHoldState && AimHold;
			AimHold = newAimHoldState;
		}

		public void ADSClickInput()
		{
			// TODO : 구현 완료시 지울것
			Debug.Log("[StarterAssetsInputs] ADSClick 입력", this);
			SetADSClickState(!ADSClick);
		}

		public void SetADSClickState(bool newADSClickState)
		{
			if (newADSClickState && AimHold)
				AimHoldInput(false);

			ADSClickPressed = newADSClickState && !ADSClick;
			ADSClickReleased = !newADSClickState && ADSClick;
			ADSClick = newADSClickState;
		}

#if ENABLE_INPUT_SYSTEM
		private void CacheWeaponSelectAction()
		{
			if (playerInput == null)
				TryGetComponent(out playerInput);

			if (playerInput == null || playerInput.actions == null)
			{
				weaponSelectAction = null;
				return;
			}

			weaponSelectAction = playerInput.actions.FindAction(WeaponSelectActionName, false);
		}

		private void CacheAimInputActions()
		{
			if (playerInput == null)
				TryGetComponent(out playerInput);

			if (playerInput == null || playerInput.actions == null)
			{
				aimHoldAction = null;
				adsClickAction = null;
				return;
			}

			aimHoldAction = playerInput.actions.FindAction(AimHoldActionName, false);
			adsClickAction = playerInput.actions.FindAction(ADSClickActionName, false);
		}

		private void SubscribeAimInputActions()
		{
			if (aimInputActionsSubscribed)
				return;

			bool subscribedAny = false;

			if (aimHoldAction != null)
			{
				aimHoldAction.performed += OnAimHoldPerformed;
				aimHoldAction.canceled += OnAimHoldCanceled;
				subscribedAny = true;
			}

			if (adsClickAction != null)
			{
				adsClickAction.performed += OnADSClickPerformed;
				subscribedAny = true;
			}

			aimInputActionsSubscribed = subscribedAny;
		}

		private void UnsubscribeAimInputActions()
		{
			if (!aimInputActionsSubscribed)
				return;

			if (aimHoldAction != null)
			{
				aimHoldAction.performed -= OnAimHoldPerformed;
				aimHoldAction.canceled -= OnAimHoldCanceled;
			}

			if (adsClickAction != null)
			{
				adsClickAction.performed -= OnADSClickPerformed;
			}

			aimInputActionsSubscribed = false;
		}

		private void OnAimHoldPerformed(InputAction.CallbackContext context)
		{
			AimHoldInput(true);
		}

		private void OnAimHoldCanceled(InputAction.CallbackContext context)
		{
			AimHoldInput(false);
		}

		private void OnADSClickPerformed(InputAction.CallbackContext context)
		{
			ADSClickInput();
		}

		private void SyncWeaponSelectActionState()
		{
			if (weaponSelectAction == null)
			{
				CacheWeaponSelectAction();

				if (weaponSelectAction == null)
					return;
			}

			bool isPressed = weaponSelectAction.ReadValue<float>() > 0.5f;

			if (isPressed == WeaponSelect)
				return;

			if (logWeaponSelectInput)
				Debug.Log($"[StarterAssetsInputs] WeaponSelect Action 상태 보정. isPressed: {isPressed}", this);

			WeaponSelectInput(isPressed);
		}
#endif

		public void ConsumeWeaponSelectInput()
		{
			WeaponSelectPressed = false;
			WeaponSelectReleased = false;
		}

		public void ConsumeGrenadeInput()
		{
			GrenadePressed = false;
			GrenadeReleased = false;
		}

		public void ConsumeAimInput()
		{
			AimHoldPressed = false;
			AimHoldReleased = false;
			ADSClickPressed = false;
			ADSClickReleased = false;
		}

		public void ClearAimInputState()
		{
			AimHold = false;
			AimHoldPressed = false;
			AimHoldReleased = false;
			ADSClick = false;
			ADSClickPressed = false;
			ADSClickReleased = false;
		}

		public void RollInput(bool newRollState)
		{
			Roll = newRollState;
			//Debug용 임시 ( 기능 추가시 지울것 )
			Debug.Log("Roll 키 입력 들어옴.");
		}

		public void GrenadeInput(bool newGrenadeState)
		{
			if (newGrenadeState)
				GrenadePressed = !Grenade;
			else
				GrenadeReleased = Grenade;

			Grenade = newGrenadeState;
			Debug.Log("Grenade 키 입력 들어옴.");
		}

		public void ReloadInput(bool newReloadState)
		{
			Reload = newReloadState;
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
