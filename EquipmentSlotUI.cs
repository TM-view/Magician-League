using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI
    : MonoBehaviour,
        IDropHandler,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
{
    [SerializeField]
    private PlayerInventory inventory;

    [SerializeField]
    private EquipmentSlotType slotType;

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private Sprite emptySprite;

    [SerializeField]
    private EquipmentDescriptionUI equipmentDescription;

    private RectTransform myRectTransform;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
        }

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        if (equipmentDescription == null)
        {
            equipmentDescription = FindObjectOfType<EquipmentDescriptionUI>(true);
        }

        myRectTransform = GetComponent<RectTransform>();

        InventoryItemDragSource dragSource = GetComponent<InventoryItemDragSource>();
        if (dragSource == null)
        {
            dragSource = GetComponentInChildren<InventoryItemDragSource>(true);
        }

        if (dragSource != null)
        {
            dragSource.ConfigureEquipmentSlot(inventory, slotType, iconImage);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (equipmentDescription == null)
        {
            return;
        }

        equipmentDescription.Show(GetCurrentItem(), myRectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (equipmentDescription != null)
        {
            equipmentDescription.Hide();
        }
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.EquipmentChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.EquipmentChanged -= Refresh;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItemDragSource source =
            eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponentInParent<InventoryItemDragSource>()
                : null;

        if (source == null || inventory == null)
        {
            return;
        }

        source.MarkDroppedOnTarget();
        if (source.SourceType == InventoryItemSourceType.Inventory)
        {
            inventory.EquipFromInventory(source.InventorySlotIndex, slotType);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && inventory != null)
        {
            inventory.QuickUnequipEquippedItem(slotType);
        }
    }

    public void Refresh()
    {
        if (inventory == null || inventory.Object == null || !inventory.Object.IsValid)
        {
            SetIcon(null);
            return;
        }

        ItemSO item = GetCurrentItem();
        SetIcon(item != null ? item.icon : null);
    }

    private ItemSO GetCurrentItem()
    {
        if (inventory == null || inventory.Object == null || !inventory.Object.IsValid)
        {
            return null;
        }

        return inventory.GetItem(inventory.GetEquippedItemId(slotType));
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconImage == null)
        {
            return;
        }

        Sprite displaySprite = sprite != null ? sprite : emptySprite;
        iconImage.sprite = displaySprite;
        iconImage.enabled = true;
        Color color = iconImage.color;
        color.a = displaySprite != null ? 1f : 0f;
        iconImage.color = color;
    }
}
