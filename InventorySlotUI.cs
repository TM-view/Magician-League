using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI
    : MonoBehaviour,
        IDropHandler,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
{
    [SerializeField]
    private PlayerInventory inventory;

    [SerializeField]
    private int slotIndex;

    [SerializeField]
    private Image iconImage;

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
            dragSource.ConfigureInventorySlot(inventory, slotIndex, iconImage);
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
            inventory.InventoryChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= Refresh;
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
            inventory.SwapInventorySlots(source.InventorySlotIndex, slotIndex);
            return;
        }

        inventory.UnequipToInventory(source.EquipmentSlot, slotIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && inventory != null)
        {
            inventory.QuickEquipInventorySlot(slotIndex);
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

        return inventory.GetItem(inventory.GetInventoryItemId(slotIndex));
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = sprite;
        iconImage.enabled = true;
        Color color = iconImage.color;
        color.a = sprite != null ? 1f : 0f;
        iconImage.color = color;
    }
}
