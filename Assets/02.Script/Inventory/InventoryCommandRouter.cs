using UnityEngine;
using StarterAssets;

public class InventoryCommandRouter : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private StarterAssetsInputs starterAssetsInputs;
    [SerializeField] private PlayerInventory playerInventory;

    /*[Header("Inventory UI")]
    [SerializeField]
    private GameObject inventoryUIPanel;

    [SerializeField]
    private InventoryGridUI inventoryGridUI;*/

    

    private void Awake()
    {
        if (starterAssetsInputs == null)
            starterAssetsInputs = GetComponent<StarterAssetsInputs>();

        if (playerInventory == null)        
            playerInventory = GetComponent<PlayerInventory>();

        //inventoryUIPanel.SetActive(false);
    }


    private void Update()
    {
        if (starterAssetsInputs == null || playerInventory == null) return;

        if(starterAssetsInputs.Inventory)
        {
            starterAssetsInputs.Inventory = false;
            ToggleInventoryPanel();
        }
    }

    private void ToggleInventoryPanel()
    {
        /*if (inventoryUIPanel == null)
        {
            Debug.Log("inventoryUIPanel == null");
            return;
        }*/

        // 게임오브젝트.activeSelf
        // 해당 게임오브젝트가, 활성화 여부 자기진단 값
        // readonly 읽기전용 값
        //bool willShow = !inventoryUIPanel.activeSelf;
        //inventoryUIPanel.SetActive(willShow);

        /*if (willShow)
        {
            //패널이 꺼져있을 때는, OnDisable 로 이벤트가 구독이 끊길수 있음 
            //열 때 마다, 한번 그리도록 ~ 
            RefreshInventoryGridIfPossible();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }*/

    }


    private void RefreshInventoryGridIfPossible()
    {
        //InventoryGridUI grid = inventoryGridUI;
        /*if (grid == null && inventoryUIPanel != null)
        {
            grid = inventoryUIPanel.GetComponentInChildren<InventoryGridUI>(true);
        }*/

        /*if (grid != null)
        {
            //TODO ::
            grid.RefreshDisplay();
        }*/
    }
}

