using UnityEngine;

public class EquipmentVisual : MonoBehaviour
{
    [SerializeField]
    private PlayerInventory inventory;

    [SerializeField]
    private SpriteRenderer weaponRenderer;

    [SerializeField]
    private SpriteRenderer armorRenderer;

    [SerializeField]
    private SpriteRenderer hatRenderer;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
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

    public void Refresh()
    {
        if (!IsInventoryReady())
        {
            SetSprite(weaponRenderer, null);
            SetSprite(armorRenderer, null);
            SetSprite(hatRenderer, null);
            return;
        }

        SetSprite(weaponRenderer, GetSprite(inventory.EquippedWeaponId));
        SetSprite(armorRenderer, GetSprite(inventory.EquippedArmorId));
        SetSprite(hatRenderer, GetSprite(inventory.EquippedHatId));
    }

    private Sprite GetSprite(int itemId)
    {
        ItemSO item = inventory.GetItem(itemId);
        return item != null ? item.equippedSprite : null;
    }

    private bool IsInventoryReady()
    {
        return inventory != null && inventory.Object != null && inventory.Object.IsValid;
    }

    private void SetSprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer != null)
        {
            renderer.sprite = sprite;
        }
    }
}
