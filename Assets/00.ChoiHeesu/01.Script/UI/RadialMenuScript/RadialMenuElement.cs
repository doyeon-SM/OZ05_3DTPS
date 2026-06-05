using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

namespace ProjectSpedex
{
    [AddComponentMenu("Radial Menu Framework/RMF Element")]
    public class RadialMenuElement : MonoBehaviour {

        [HideInInspector]
        public RectTransform rt;
        [HideInInspector]
        public RadialMenu parentRM;

    [Tooltip("각 방사형 요소에는 Button이 필요합니다. 일반적으로 이 방사형 요소 GameObject의 한 단계 아래 자식에 배치됩니다.")]
    public Button button;

    [Tooltip("이 옵션에 마우스를 올렸을 때 방사형 메뉴 중앙에 표시될 텍스트 라벨입니다. 짧게 작성하는 것이 좋습니다.")]
    public string label;

    [Tooltip("이 옵션에 마우스를 올렸을 때 방사형 메뉴 중앙에 icon입니다.")]
    public Image Icon;

    [Tooltip("이 무기 타입이 사용하는 Ammo 수량을 표시할 TextMeshPro 텍스트입니다.")]
    public TMP_Text AmmoText;
    
    [Tooltip("S.O 데이터를 저장할 string 입니다")]
    public string ItemID;

    [HideInInspector]
    public float angleMin, angleMax;

    [HideInInspector]
    public float angleOffset;

    [HideInInspector]
    public bool active = false;

    [HideInInspector]
    public int assignedIndex = 0;
    //초기화에 사용됩니다.

    private CanvasGroup cg;

    void Awake() {

        rt = gameObject.GetComponent<RectTransform>();

        if (gameObject.GetComponent<CanvasGroup>() == null)
            cg = gameObject.AddComponent<CanvasGroup>();
        else
            cg = gameObject.GetComponent<CanvasGroup>();


        if (rt == null)
            Debug.LogError("Radial Menu: 방사형 요소 " + gameObject.name + "의 RectTransform을 찾을 수 없습니다. 이 오브젝트가 Canvas의 자식인지 확인하세요.");

        if (button == null)
            Debug.LogError("Radial Menu: " + gameObject.name + "에 Button이 연결되어 있지 않습니다!");
        if (Icon == null)
            Debug.LogError("Radial Menu: " + gameObject.name + "에 Icon이 없습니다.");
    }

    void Start () {

        if (parentRM.rotateElementsByAngle)
            rt.localRotation = Quaternion.Euler(0, 0, -angleOffset); //부모 방사형 메뉴에서 결정한 회전을 적용합니다.
        else
            ResetButtonRotation();

        //Lazy Selection을 사용하는 경우 일반 마우스 오버 효과가 방해되지 않도록 raycast를 끕니다.
        if (parentRM.useLazySelection)
            cg.blocksRaycasts = false;
        else {

            //그렇지 않으면 마우스 오버 시 라벨이 동작하도록 EventTrigger를 설정해야 합니다.

            EventTrigger t;

            if (button.GetComponent<EventTrigger>() == null) {
                t = button.gameObject.AddComponent<EventTrigger>();
                t.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
            } else
                t = button.GetComponent<EventTrigger>();



            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((eventData) => { setParentMenuLable(label,Icon.sprite); });


            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((eventData) => { setParentMenuLable("",null); });

            t.triggers.Add(enter);
            t.triggers.Add(exit);



        }

    }

    private void ResetButtonRotation() {

        if (button == null)
            return;

        RectTransform buttonRectTransform = button.GetComponent<RectTransform>();
        if (buttonRectTransform != null)
            buttonRectTransform.localRotation = Quaternion.identity;

    }
	
    //부모 방사형 메뉴가 필요한 각도를 설정할 때 사용합니다. 전체 Z 회전과 Lazy Selection의 활성 각도에 영향을 줍니다.
    public void setAllAngles(float offset, float baseOffset) {

        angleOffset = offset;
        angleMin = offset - (baseOffset / 2f);
        angleMax = offset + (baseOffset / 2f);

    }

    //이 버튼을 하이라이트합니다. Unity 기본 Button은 코드로 제어하는 용도로 설계된 것이 아니기 때문에 여기서는 이벤트 핸들러가 필요합니다.
    //이벤트 핸들러 하나만 잘못되어도 전체 동작이 깨질 수 있으므로, 구조를 정확히 이해하지 않았다면 이 부분은 되도록 수정하지 않는 것을 권장합니다.
    public void highlightThisElement(PointerEventData p) {

        ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.selectHandler);
        active = true;
        setParentMenuLable(label , Icon.sprite);

    }

    //부모 메뉴의 라벨을 설정합니다. 특정 상황에서 별도 라벨을 표시해야 할 수 있어 public으로 열어두었습니다.
    public void setParentMenuLable(string l , Sprite i) {

        if (parentRM.textLabel != null)
            parentRM.textLabel.text = l;
        if(parentRM.IconLabel != null)
            parentRM.IconLabel.sprite = i;

    }


    //버튼의 하이라이트를 해제합니다. Lazy Selection이 꺼져 있다면 메뉴 라벨도 초기화합니다.
    public void unHighlightThisElement(PointerEventData p) {

        ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.deselectHandler);
        active = false;

        if (!parentRM.useLazySelection)
            setParentMenuLable(" ",null);


    }

    //동작이 정상인지 빠르게 확인할 수 있는 간단한 테스트 함수입니다.
        public void clickMeTest() {

            Debug.Log(assignedIndex);


        }




    }
}
