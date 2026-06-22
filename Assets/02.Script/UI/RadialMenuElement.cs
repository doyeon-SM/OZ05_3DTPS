using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectSpedex
{
    [AddComponentMenu("Radial Menu Framework/RMF Element")]
    public class RadialMenuElement : MonoBehaviour
    {
        [HideInInspector] public RectTransform rt;
        [HideInInspector] public RadialMenu parentRM;

        [Tooltip("각 방사형 요소에는 Button이 필요합니다.")]
        public Button button;

        [Tooltip("이 요소에 표시할 이름입니다.")]
        public string label;

        [Tooltip("이 요소에 표시할 아이콘입니다.")]
        public Image Icon;

        [Tooltip("Unlock 상태일 때 표시할 아이콘입니다.")]
        public Sprite UnLockIcon;

        [Tooltip("Lock 상태일 때 표시할 아이콘입니다.")]
        public Sprite LockIcon;

        [Tooltip("이 무기 타입이 사용하는 Ammo 수량을 표시할 TextMeshPro 텍스트입니다.")]
        public TMP_Text AmmoText;

        [Tooltip("S.O 데이터를 식별할 WeaponId입니다.")]
        public string ItemID;

        [HideInInspector] public float angleMin, angleMax;
        [HideInInspector] public float angleOffset;
        [HideInInspector] public bool active = false;
        [HideInInspector] public int assignedIndex = 0;

        private CanvasGroup cg;
        private Graphic buttonTargetGraphic;
        private Color defaultButtonTargetColor;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();

            if (!TryGetComponent(out cg))
                cg = gameObject.AddComponent<CanvasGroup>();

            if (rt == null)
                Debug.LogError("Radial Menu: " + gameObject.name + "의 RectTransform을 찾을 수 없습니다. 이 오브젝트가 Canvas의 자식인지 확인하세요.", this);

            if (button == null)
                Debug.LogError("Radial Menu: " + gameObject.name + "에 Button이 연결되어 있지 않습니다.", this);

            if (button != null)
                CacheButtonTargetGraphic();

            if (Icon == null)
                Debug.LogError("Radial Menu: " + gameObject.name + "에 Icon이 연결되어 있지 않습니다.", this);
        }

        private void Start()
        {
            if (parentRM == null)
            {
                Debug.LogError("Radial Menu: " + gameObject.name + "에 부모 RadialMenu가 연결되지 않았습니다. RadialMenu의 Elements 또는 Center Element에 등록해주세요.", this);
                return;
            }

            if (parentRM.rotateElementsByAngle)
                rt.localRotation = Quaternion.Euler(0f, 0f, -angleOffset);
            else
                ResetButtonRotation();

            if (cg != null)
                cg.blocksRaycasts = !parentRM.useLazySelection;
        }

        private void ResetButtonRotation()
        {
            if (button == null)
                return;

            RectTransform buttonRectTransform = button.GetComponent<RectTransform>();
            if (buttonRectTransform != null)
                buttonRectTransform.localRotation = Quaternion.identity;
        }

        public void setAllAngles(float offset, float baseOffset)
        {
            angleOffset = offset;
            angleMin = offset - (baseOffset / 2f);
            angleMax = offset + (baseOffset / 2f);
        }

        public void highlightThisElement(PointerEventData p)
        {
            if (button == null)
                return;

            ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.selectHandler);
            ApplyManualSelectionColor(true);
            active = true;
        }

        public void unHighlightThisElement(PointerEventData p)
        {
            if (button == null)
                return;

            ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.deselectHandler);
            ApplyManualSelectionColor(false);
            active = false;
        }

        private void CacheButtonTargetGraphic()
        {
            if (button == null || button.targetGraphic == null)
                return;

            buttonTargetGraphic = button.targetGraphic;
            defaultButtonTargetColor = buttonTargetGraphic.color;
        }

        private void ApplyManualSelectionColor(bool isSelected)
        {
            if (button == null || button.transition != Selectable.Transition.None)
                return;

            if (buttonTargetGraphic == null)
                CacheButtonTargetGraphic();

            if (buttonTargetGraphic == null)
                return;

            buttonTargetGraphic.color = isSelected ? button.colors.selectedColor : defaultButtonTargetColor;
        }

        public void clickMeTest()
        {
            Debug.Log(assignedIndex);
        }
    }
}
