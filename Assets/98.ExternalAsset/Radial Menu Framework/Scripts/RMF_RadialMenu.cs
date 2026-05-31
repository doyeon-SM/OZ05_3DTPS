using UnityEngine;
using UnityEngine.EventSystems;

using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


[AddComponentMenu("Radial Menu Framework/RMF Core Script")]
public class RMF_RadialMenu : MonoBehaviour {

    [HideInInspector]
    public RectTransform rt;
    //public RectTransform baseCircleRT;
    //public Image selectionFollowerImage;

    [Tooltip("게임패드 또는 조이스틱으로 사용할 수 있도록 방사형 메뉴를 조정합니다.")]
    public bool useGamepad = false;

    [SerializeField]
    [Tooltip("이 값보다 작은 게임패드 스틱 입력은 무시됩니다.")]
    private float gamepadDeadZone = 0.2f;

    [Tooltip("Lazy Selection을 사용하면 마우스나 조이스틱을 요소 방향으로 가리키기만 해도 선택됩니다. 요소 위에 정확히 마우스를 올릴 필요가 없습니다.")]
    public bool useLazySelection = true;


    [Tooltip("true로 설정하면 지정한 그래픽 포인터가 마우스 방향을 향합니다. Selection Follower의 컨테이너를 지정해야 합니다.")]
    public bool useSelectionFollower = true;

    [Tooltip("Selection Follower를 사용할 경우, 이 값은 Selection Follower 컨테이너의 RectTransform을 가리켜야 합니다.")]
    public RectTransform selectionFollowerContainer;

    [Tooltip("방사형 요소에 마우스를 올렸을 때 라벨을 표시할 Text 오브젝트입니다. 라벨을 사용하지 않으려면 비워두세요.")]
    public Text textLabel;

    [Tooltip("방사형 메뉴 요소 목록입니다. 순서가 중요하며, 목록의 첫 번째 요소가 가장 먼저 생성됩니다.")]
    public List<RMF_RadialMenuElement> elements = new List<RMF_RadialMenuElement>();


    [Tooltip("모든 요소에 적용되는 전체 각도 오프셋을 제어합니다. 예를 들어 45로 설정하면 모든 요소가 +45도 이동합니다. 일반적으로 45, 90, 180이 사용하기 좋습니다.")]
    public float globalOffset = 0f;


    [HideInInspector]
    public float currentAngle = 0f; //방사형 메뉴 중심을 기준으로 한 현재 각도입니다.


    [HideInInspector]
    public int index = 0; //현재 가리키고 있는 요소의 인덱스입니다.

    private int elementCount;

    private float angleOffset; //기본 오프셋입니다. 예를 들어 요소가 4개라면 오프셋은 360/4 = 90입니다.

    private int previousActiveIndex = 0; //Lazy Selection에서 어떤 버튼의 하이라이트를 해제할지 판단하는 데 사용됩니다.

    private PointerEventData pointer;

    void Awake() {

        pointer = new PointerEventData(EventSystem.current);

        rt = GetComponent<RectTransform>();

        if (rt == null)
            Debug.LogError("Radial Menu: 방사형 메뉴 " + gameObject.name + "의 RectTransform을 찾을 수 없습니다. 이 오브젝트가 Canvas의 자식인지 확인하세요.");

        if (useSelectionFollower && selectionFollowerContainer == null)
            Debug.LogError("Radial Menu: " + gameObject.name + "에서 Selection Follower가 활성화되어 있지만 Selection Follower 컨테이너가 할당되지 않았습니다.");

        elementCount = elements.Count;

        angleOffset = (360f / (float)elementCount);

        //요소들을 순회하며 초기 설정을 적용합니다.
        for (int i = 0; i < elementCount; i++) {
            if (elements[i] == null) {
                Debug.LogError("Radial Menu: 방사형 메뉴 " + gameObject.name + "의 요소 " + i.ToString() + "이 null입니다!");
                continue;
            }
            elements[i].parentRM = this;

            elements[i].setAllAngles((angleOffset * i) + globalOffset, angleOffset);

            elements[i].assignedIndex = i;

        }

    }


    void Start() {


        if (useGamepad) {
            EventSystem.current.SetSelectedGameObject(gameObject, null); //시작할 때 이 오브젝트를 활성 오브젝트로 설정합니다. 다른 스크립트에서 수동으로 설정하려면 이 줄을 주석 처리하세요.
            if (useSelectionFollower && selectionFollowerContainer != null)
                selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, -globalOffset); //Selection Follower가 첫 번째 요소를 가리키도록 합니다.
        }

    }

    //매 프레임 한 번씩 호출됩니다.
    void Update() {

        Vector2 gamepadDirection = GetGamepadDirection();
        bool joystickMoved = gamepadDirection != Vector2.zero;


        float rawAngle;
        
        if (!useGamepad) {
            Vector2 pointerPosition = GetPointerPosition();
            rawAngle = Mathf.Atan2(pointerPosition.y - rt.position.y, pointerPosition.x - rt.position.x) * Mathf.Rad2Deg;
        }
        else
            rawAngle = Mathf.Atan2(gamepadDirection.y, gamepadDirection.x) * Mathf.Rad2Deg;

        //게임패드를 사용하지 않으면 항상 각도를 갱신합니다. 게임패드를 사용하는 경우 조이스틱이 움직였을 때만 갱신합니다.
        if (!useGamepad)
            currentAngle = normalizeAngle(-rawAngle + 90 - globalOffset + (angleOffset / 2f));
        else if (joystickMoved)
            currentAngle = normalizeAngle(-rawAngle + 90 - globalOffset + (angleOffset / 2f));

        //Lazy Selection을 처리합니다. 현재 각도를 확인해 요소 인덱스와 매칭한 뒤 해당 요소를 하이라이트합니다.
        if (angleOffset != 0 && useLazySelection) {

            //현재 가리키고 있는 요소의 인덱스입니다.
            index = (int)(currentAngle / angleOffset);

            if (elements[index] != null) {

                //해당 요소를 선택합니다.
                selectButton(index);

                //클릭하거나 Submit 버튼(조이스틱 버튼, Enter, Space)을 누르면 해당 버튼의 OnClick() 함수를 실행합니다.
                if (WasSubmitPressed()) {

                    ExecuteEvents.Execute(elements[index].button.gameObject, pointer, ExecuteEvents.submitHandler);


                }
            }

        }

        //Selection Follower를 사용 중이라면 위치 방향을 갱신합니다.
        if (useSelectionFollower && selectionFollowerContainer != null) {
            if (!useGamepad || joystickMoved)
                selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, rawAngle + 270);
           

        } 

    }


    //지정한 인덱스의 버튼을 선택합니다.
    private void selectButton(int i) {

          if (elements[i].active == false) {

            elements[i].highlightThisElement(pointer); //이 요소를 선택합니다.

            if (previousActiveIndex != i) 
                elements[previousActiveIndex].unHighlightThisElement(pointer); //이전에 선택된 요소를 선택 해제합니다.
            

          }

          previousActiveIndex = i;

    }

    //각도를 0에서 360 사이로 유지합니다.
    private float normalizeAngle(float angle) {

        angle = angle % 360f;

        if (angle < 0)
            angle += 360;

        return angle;

    }

    private Vector2 GetPointerPosition() {

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return rt != null ? rt.position : Vector3.zero;
#endif

    }

    private Vector2 GetGamepadDirection() {

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

    private bool WasSubmitPressed() {

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame))
            return true;

        if (Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame ||
             Gamepad.current.startButton.wasPressedThisFrame))
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0) || Input.GetButtonDown("Submit");
#else
        return false;
#endif

    }


}
