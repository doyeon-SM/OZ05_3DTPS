using System.Collections.Generic;
using UnityEngine;

// PlayerInventory의 Slot 프리팹을 Grid 알에ㅔ 만들고 변경 시, 다시 전체를 그립니다.
public class InventoryGridUI : MonoBehaviour
{
    /*[Header("Ref")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private RectTransform slotContainer; //슬롯들의 부모 객체 
    [SerializeField] private InventorySlotView slotPrefab;
    [SerializeField] private ItemCatalogManager itemCatalogManager;
    [SerializeField] private InventoryItemInfoView itemInfoView;

    private readonly List<InventorySlotView> slotViewInstances = new List<InventorySlotView>();
    private InventorySlotView _selectedSlot;

    private void Awake()
    {
        if (inventory == null)
        {
            //FindFirstObjectByType
            // < > 해당 타입의 첫번째 오브젝트 찾기 ?            
            inventory = FindFirstObjectByType<PlayerInventory>();
            Debug.LogWarning("인벤토리 참조 안해줬다 ~ 확인해봐라 ~ ");
        }
    }
    private void OnDestroy()
    {
        foreach(InventorySlotView slot in slotViewInstances)
        {
            slot.OnSlotClicked -= HandleSlotClicked;
        }
    }

    private void Start()
    {
        BuildSlotViews(); // 슬롯들 정보 빌드하기
        RedrawAllSlots(); // 슬롯들 모두 다시 그리기
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged += OnInventoryChanged;
        }

        if (slotViewInstances.Count > 0)
        {
            RedrawAllSlots();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= OnInventoryChanged;
        }
    }


    //구독하고 있는 인벤토리로 부터 알림이 오면 
    //구독자 (InventoryGridUI) 가 할 행동  
    private void OnInventoryChanged()
    {
        RedrawAllSlots();
    }

    public void RefreshDisplay()
    {
        RedrawAllSlots();
    }

    //전체 슬롯 드로우 (새로고침)
    private void RedrawAllSlots()
    {
        //인벤토리가 없거나, 슬롯 뷰 인스턴스(슬롯이) 없거나 ~ 
        if (inventory == null || slotViewInstances.Count == 0)
        {
            return;
        }

        //습관
        //존재의 이유가 : 개발자들이 실수 하는것때문에 ->값이 오염
        IReadOnlyList<InventorySlotData> slots = inventory.InventorySlots;
        for (int viewIndex = 0; viewIndex < slotViewInstances.Count; viewIndex++)
        {
            InventorySlotData slotData =
                viewIndex < slots.Count
                ? slots[viewIndex] :
                new InventorySlotData { itemId = string.Empty, amount = 0 };

            slotViewInstances[viewIndex].Bind(slotData, itemCatalogManager);
        }
        // 뷰인덱스 =0 ~ 
    }

    //
    private void BuildSlotViews()
    {
        // UI 요소 방어코드
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.Log("slotContainer == null || slotPrefab == null");
            return;

        }
        if (inventory == null)
        {
            Debug.Log("inventory == null");
            return;
        }

        //만들기전에, 만들 칸 깨끗하게 청소
        //시작이 childIndex -> slotContainer (사이즈-1) 부터시작
        for (int childIndex = slotContainer.childCount - 1; childIndex >= 0; childIndex--)
        {
            Destroy(slotContainer.GetChild(childIndex).gameObject);
        }
        slotViewInstances.Clear();

        int capacity = Mathf.Max(0, inventory.SlotCapacity);
        for (int slotIndex = 0; slotIndex < capacity; slotIndex++)
        {
            InventorySlotView slotInstnace = Instantiate(slotPrefab, slotContainer);
            slotInstnace.gameObject.name = $"Slot_{slotIndex:D2}";
            slotInstnace.OnSlotClicked += HandleSlotClicked;
            slotViewInstances.Add(slotInstnace);
        }
    }

    private void HandleSlotClicked(InventorySlotView clickedSlot)
    {
        //Debug.Log($"[InventorygirdUI] {clickedSlot} HandleSlotClicked");
        if (_selectedSlot == clickedSlot)
        {
            _selectedSlot.SetSelected(false);
            _selectedSlot = null;
            itemInfoView?.Bind(default, null);
            return;
        }
        if (_selectedSlot != null)
            _selectedSlot.SetSelected(false);

        _selectedSlot = clickedSlot;
        _selectedSlot.SetSelected(true);

        itemInfoView?.Bind(clickedSlot.CachedSlot, clickedSlot.CachedCatalog);
    }*/
}