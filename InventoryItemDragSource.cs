using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum InventoryItemSourceType
{
    Inventory,
    Equipment,
}

public class InventoryItemDragSource
    : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerClickHandler
{
    [SerializeField]
    private PlayerInventory inventory;

    [SerializeField]
    private InventoryItemSourceType sourceType;

    [SerializeField]
    private int inventorySlotIndex;

    [SerializeField]
    private EquipmentSlotType equipmentSlot;

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private Image draggingIcon;

    [SerializeField]
    private Canvas rootCanvas;

    [SerializeField]
    [Range(0f, 0.5f)]
    private float horizontalDropZonePercent = 0.25f;

    [SerializeField]
    [Range(0f, 0.5f)]
    private float verticalDropZonePercent = 0.1f;

    private GameObject dragIconObject;
    private RectTransform dragIconTransform;
    private bool usingSharedDraggingIcon;
    private bool droppedOnTarget;

    public InventoryItemSourceType SourceType => sourceType;
    public int InventorySlotIndex => inventorySlotIndex;
    public EquipmentSlotType EquipmentSlot => equipmentSlot;

    public void ConfigureInventorySlot(
        PlayerInventory newInventory,
        int newSlotIndex,
        Image newIconImage
    )
    {
        inventory = newInventory;
        sourceType = InventoryItemSourceType.Inventory;
        inventorySlotIndex = newSlotIndex;
        if (newIconImage != null)
        {
            iconImage = newIconImage;
        }

        CacheReferences();
    }

    public void ConfigureEquipmentSlot(
        PlayerInventory newInventory,
        EquipmentSlotType newEquipmentSlot,
        Image newIconImage
    )
    {
        inventory = newInventory;
        sourceType = InventoryItemSourceType.Equipment;
        equipmentSlot = newEquipmentSlot;
        if (newIconImage != null)
        {
            iconImage = newIconImage;
        }

        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
        }

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (draggingIcon != null)
        {
            draggingIcon.raycastTarget = false;
            draggingIcon.gameObject.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        droppedOnTarget = false;
        if (!HasItem())
        {
            return;
        }

        CreateDragIcon();
        MoveDragIcon(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveDragIcon(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyDragIcon();

        if (droppedOnTarget || inventory == null || !IsPointerInDropZone(eventData.position))
        {
            return;
        }

        if (sourceType == InventoryItemSourceType.Inventory)
        {
            inventory.DropInventorySlot(inventorySlotIndex);
            return;
        }

        inventory.DropEquippedItem(equipmentSlot);
    }

    public void MarkDroppedOnTarget()
    {
        droppedOnTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (
            eventData.button != PointerEventData.InputButton.Right
            || inventory == null
            || !HasItem()
        )
        {
            return;
        }

        if (sourceType == InventoryItemSourceType.Inventory)
        {
            inventory.QuickEquipInventorySlot(inventorySlotIndex);
            return;
        }

        inventory.QuickUnequipEquippedItem(equipmentSlot);
    }

    private bool HasItem()
    {
        if (inventory == null)
        {
            return false;
        }

        int itemId =
            sourceType == InventoryItemSourceType.Inventory
                ? inventory.GetInventoryItemId(inventorySlotIndex)
                : inventory.GetEquippedItemId(equipmentSlot);
        return itemId > 0;
    }

    private void CreateDragIcon()
    {
        if (iconImage == null || iconImage.sprite == null)
        {
            return;
        }

        if (draggingIcon != null)
        {
            usingSharedDraggingIcon = true;
            draggingIcon.sprite = iconImage.sprite;
            draggingIcon.color = Color.white;
            draggingIcon.gameObject.SetActive(true);
            dragIconTransform = draggingIcon.rectTransform;
            return;
        }

        if (rootCanvas == null)
        {
            return;
        }

        usingSharedDraggingIcon = false;
        dragIconObject = new GameObject("Inventory Drag Icon");
        dragIconObject.transform.SetParent(rootCanvas.transform, false);
        dragIconObject.transform.SetAsLastSibling();

        Image dragImage = dragIconObject.AddComponent<Image>();
        dragImage.sprite = iconImage.sprite;
        dragImage.raycastTarget = false;
        dragImage.color = Color.white;

        dragIconTransform = dragIconObject.GetComponent<RectTransform>();
        dragIconTransform.sizeDelta = iconImage.rectTransform.rect.size;
    }

    private void MoveDragIcon(PointerEventData eventData)
    {
        if (dragIconTransform == null)
        {
            return;
        }

        if (usingSharedDraggingIcon)
        {
            dragIconTransform.anchoredPosition =
                eventData.position - new Vector2(Screen.width, Screen.height) / 2f;
            return;
        }

        dragIconTransform.position = eventData.position;
    }

    private void DestroyDragIcon()
    {
        if (usingSharedDraggingIcon && draggingIcon != null)
        {
            draggingIcon.gameObject.SetActive(false);
        }

        if (dragIconObject != null)
        {
            Destroy(dragIconObject);
        }

        dragIconObject = null;
        dragIconTransform = null;
        usingSharedDraggingIcon = false;
    }

    private bool IsPointerInDropZone(Vector2 position)
    {
        float horizontalPercent = Mathf.Clamp(horizontalDropZonePercent, 0f, 0.5f);
        float verticalPercent = Mathf.Clamp(verticalDropZonePercent, 0f, 0.5f);

        float leftLimit = Screen.width * horizontalPercent;
        float rightLimit = Screen.width * (1f - horizontalPercent);
        float bottomLimit = Screen.height * verticalPercent;
        float topLimit = Screen.height * (1f - verticalPercent);

        return position.x <= leftLimit
            || position.x >= rightLimit
            || position.y <= bottomLimit
            || position.y >= topLimit;
    }
}
