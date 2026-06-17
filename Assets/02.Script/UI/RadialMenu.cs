using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectSpedex
{
    [AddComponentMenu("Radial Menu Framework/RMF Core Script")]
    public class RadialMenu : MonoBehaviour
    {
        [HideInInspector] public RectTransform rt;

        [Tooltip("게임패드 또는 조이스틱으로 사용할 수 있도록 방사형 메뉴를 조정합니다.")]
        public bool useGamepad = false;

        [SerializeField]
        [Tooltip("이 값보다 작은 게임패드 스틱 입력은 무시됩니다.")]
        private float gamepadDeadZone = 0.2f;

        [Tooltip("마우스나 조이스틱을 요소 방향으로 가리키기만 해도 선택되도록 합니다.")]
        public bool useLazySelection = true;

        [Tooltip("true로 설정하면 지정한 그래픽 포인터가 선택 방향을 향합니다.")]
        public bool useSelectionFollower = true;

        [Tooltip("Selection Follower를 사용할 경우 연결할 RectTransform입니다.")]
        public RectTransform selectionFollowerContainer;

        [Tooltip("외곽 방향 선택에 사용할 방사형 메뉴 요소 목록입니다.")]
        public List<RadialMenuElement> elements = new List<RadialMenuElement>();

        [Header("Center Element")]
        [SerializeField]
        [Tooltip("중앙에서 선택할 요소입니다. 기본 무기인 Pistol처럼 방향 입력 없이 선택할 슬롯을 연결합니다.")]
        private RadialMenuElement centerElement;

        [SerializeField]
        [Min(0f)]
        [Tooltip("포인터가 메뉴 중심에서 이 거리 안에 있으면 중앙 요소를 선택합니다.")]
        private float centerSelectionRadius = 80f;

        [Tooltip("모든 외곽 요소에 적용되는 전체 각도 오프셋입니다.")]
        public float globalOffset = 0f;

        [Tooltip("true로 설정하면 각 요소 Transform을 각도에 맞게 회전합니다.")]
        public bool rotateElementsByAngle = false;

        [HideInInspector] public float currentAngle = 0f;
        [HideInInspector] public int index = 0;

        public float CurrentPointerDistance { get; private set; }
        public RadialMenuElement CenterElement => centerElement;
        public bool HasCenterElement => centerElement != null;
        public RadialMenuElement CurrentSelectedElement { get; private set; }
        public bool IsCenterSelected { get; private set; }

        private int elementCount;
        private float angleOffset;
        private PointerEventData pointer;
        private Canvas parentCanvas;
        private Vector2 virtualPointerPosition;
        private Vector2 lastRawMousePosition;
        private bool hasVirtualPointerPosition;

        private void Awake()
        {
            pointer = new PointerEventData(EventSystem.current);
            rt = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();

            if (rt == null)
                Debug.LogError("Radial Menu: " + gameObject.name + "의 RectTransform을 찾을 수 없습니다. 이 오브젝트가 Canvas의 자식인지 확인하세요.", this);

            if (parentCanvas == null)
                Debug.LogError("Radial Menu: " + gameObject.name + "에서 부모 Canvas를 찾을 수 없습니다. 방사형 메뉴가 Canvas 아래에 있는지 확인하세요.", this);

            if (useSelectionFollower && selectionFollowerContainer == null)
                Debug.LogError("Radial Menu: " + gameObject.name + "에서 Selection Follower가 활성화되어 있지만 Selection Follower 컨테이너가 할당되지 않았습니다.", this);

            if (elements == null)
                elements = new List<RadialMenuElement>();

            elementCount = elements.Count;
            if (elementCount <= 0 && centerElement == null)
            {
                Debug.LogError("Radial Menu: " + gameObject.name + "에 등록된 요소가 없습니다. Elements 또는 Center Element를 연결해주세요.", this);
                enabled = false;
                return;
            }

            angleOffset = elementCount > 0 ? 360f / elementCount : 0f;

            for (int i = 0; i < elementCount; i++)
            {
                if (elements[i] == null)
                {
                    Debug.LogError("Radial Menu: " + gameObject.name + "의 Elements[" + i + "]가 null입니다.", this);
                    continue;
                }

                elements[i].parentRM = this;
                elements[i].setAllAngles((angleOffset * i) + globalOffset, angleOffset);
                elements[i].assignedIndex = i;
            }

            if (centerElement != null)
            {
                centerElement.parentRM = this;
                centerElement.assignedIndex = -1;
                centerElement.setAllAngles(0f, 0f);
            }
        }

        private void Start()
        {
            if (!useGamepad)
                return;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject, null);

            if (useSelectionFollower && selectionFollowerContainer != null)
                SetSelectionFollowerRotation(-globalOffset);
        }

        private void Update()
        {
            Vector2 gamepadDirection = GetGamepadDirection();
            bool joystickMoved = gamepadDirection != Vector2.zero;
            float rawAngle;
            bool hasDirectionInput = true;

            if (!useGamepad)
            {
                Vector2 pointerPosition = GetPointerPosition();
                if (pointer != null)
                    pointer.position = pointerPosition;

                hasDirectionInput = TryGetPointerLocalDirection(pointerPosition, out Vector2 pointerDirection);
                rawAngle = hasDirectionInput ? Mathf.Atan2(pointerDirection.y, pointerDirection.x) * Mathf.Rad2Deg : 0f;
            }
            else
            {
                CurrentPointerDistance = joystickMoved ? gamepadDirection.magnitude : 0f;
                rawAngle = joystickMoved ? Mathf.Atan2(gamepadDirection.y, gamepadDirection.x) * Mathf.Rad2Deg : 0f;
            }

            bool shouldSelectCenter = ShouldSelectCenter(joystickMoved);
            if (useLazySelection && shouldSelectCenter)
            {
                SelectCenterElement();
            }
            else
            {
                UpdateDirectionalSelection(rawAngle, hasDirectionInput, joystickMoved);
            }

            UpdateSelectionFollower(rawAngle, shouldSelectCenter, joystickMoved);
        }

        private void UpdateDirectionalSelection(float rawAngle, bool hasDirectionInput, bool joystickMoved)
        {
            if (!useGamepad && hasDirectionInput)
                currentAngle = normalizeAngle(-rawAngle + 90f - globalOffset + (angleOffset / 2f));
            else if (joystickMoved)
                currentAngle = normalizeAngle(-rawAngle + 90f - globalOffset + (angleOffset / 2f));

            if (angleOffset == 0f || !useLazySelection || elementCount <= 0)
                return;

            index = Mathf.Clamp((int)(currentAngle / angleOffset), 0, elementCount - 1);
            if (elements[index] != null)
                SelectElement(elements[index], index);
        }

        private void UpdateSelectionFollower(float rawAngle, bool shouldSelectCenter, bool joystickMoved)
        {
            if (!useSelectionFollower || selectionFollowerContainer == null)
                return;

            SetSelectionFollowerVisible(!shouldSelectCenter);

            if (!shouldSelectCenter && (!useGamepad || joystickMoved))
                SetSelectionFollowerRotation(rawAngle + 270f);
        }

        private bool ShouldSelectCenter(bool joystickMoved)
        {
            if (centerElement == null)
                return false;

            if (useGamepad)
                return !joystickMoved;

            return CurrentPointerDistance <= Mathf.Max(0f, centerSelectionRadius);
        }

        private void SelectCenterElement()
        {
            SelectElement(centerElement, -1);
        }

        private void SelectElement(RadialMenuElement element, int directionalIndex)
        {
            if (element == null)
                return;

            if (CurrentSelectedElement != null && CurrentSelectedElement != element)
                CurrentSelectedElement.unHighlightThisElement(pointer);

            if (!element.active)
                element.highlightThisElement(pointer);

            CurrentSelectedElement = element;
            IsCenterSelected = element == centerElement;
            index = directionalIndex >= 0 ? directionalIndex : -1;
        }

        public void ResetSelectionToCenter()
        {
            if (centerElement == null)
                return;

            virtualPointerPosition = GetRadialMenuScreenCenter();
            hasVirtualPointerPosition = true;
            CurrentPointerDistance = 0f;
            SelectCenterElement();

            if (useSelectionFollower && selectionFollowerContainer != null)
                SetSelectionFollowerVisible(false);
        }

        private float normalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f)
                angle += 360f;

            return angle;
        }

        private bool TryGetPointerLocalDirection(Vector2 screenPosition, out Vector2 direction)
        {
            direction = Vector2.zero;

            if (rt == null)
                return false;

            Camera eventCamera = GetCanvasEventCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPosition, eventCamera, out Vector2 localPointerPosition))
            {
                CurrentPointerDistance = 0f;
                return false;
            }

            direction = localPointerPosition - rt.rect.center;
            CurrentPointerDistance = direction.magnitude;
            return direction.sqrMagnitude > 0.0001f;
        }

        private Camera GetCanvasEventCamera()
        {
            if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (parentCanvas.worldCamera != null)
                return parentCanvas.worldCamera;

            return Camera.main;
        }

        private Vector2 GetRadialMenuScreenCenter()
        {
            if (rt == null)
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Camera eventCamera = GetCanvasEventCamera();
            Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
            return RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
        }

        private Vector2 GetLockedCursorPointerPosition(Vector2 fallbackPosition)
        {
            if (!hasVirtualPointerPosition)
            {
                virtualPointerPosition = fallbackPosition;

                if (virtualPointerPosition == Vector2.zero)
                    virtualPointerPosition = GetRadialMenuScreenCenter();

                hasVirtualPointerPosition = true;
            }

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                virtualPointerPosition += mouseDelta;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            virtualPointerPosition += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif

            virtualPointerPosition.x = Mathf.Clamp(virtualPointerPosition.x, 0f, Screen.width);
            virtualPointerPosition.y = Mathf.Clamp(virtualPointerPosition.y, 0f, Screen.height);
            return virtualPointerPosition;
        }

        private void SetSelectionFollowerRotation(float zRotation)
        {
            if (selectionFollowerContainer == null)
                return;

            selectionFollowerContainer.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }

        private void SetSelectionFollowerVisible(bool isVisible)
        {
            if (selectionFollowerContainer == null)
                return;

            if (selectionFollowerContainer.gameObject.activeSelf != isVisible)
                selectionFollowerContainer.gameObject.SetActive(isVisible);
        }

        private Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();

                bool mousePositionStopped = hasVirtualPointerPosition &&
                    (mousePosition - lastRawMousePosition).sqrMagnitude < 0.0001f &&
                    mouseDelta.sqrMagnitude > 0.0001f;

                lastRawMousePosition = mousePosition;

                if (Cursor.lockState == CursorLockMode.Locked || mousePositionStopped)
                    return GetLockedCursorPointerPosition(mousePosition);

                virtualPointerPosition = mousePosition;
                hasVirtualPointerPosition = true;
                return mousePosition;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            Vector2 legacyMousePosition = Input.mousePosition;

            if (Cursor.lockState == CursorLockMode.Locked)
                return GetLockedCursorPointerPosition(legacyMousePosition);

            virtualPointerPosition = legacyMousePosition;
            hasVirtualPointerPosition = true;
            return legacyMousePosition;
#else
            return rt != null ? rt.position : Vector3.zero;
#endif
        }

        private Vector2 GetGamepadDirection()
        {
            Vector2 direction = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null)
                direction = Gamepad.current.leftStick.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (direction == Vector2.zero)
                direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
#endif

            if (direction.sqrMagnitude < gamepadDeadZone * gamepadDeadZone)
                return Vector2.zero;

            return direction;
        }
    }
}
