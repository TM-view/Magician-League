using UnityEngine;

public class EquipmentVisual : MonoBehaviour
{
    [SerializeField]
    private PlayerInventory inventory;

    [SerializeField]
    private Status status;

    [SerializeField]
    private SpriteRenderer weaponRenderer;

    [SerializeField]
    private SpriteRenderer armorRenderer;

    [SerializeField]
    private SpriteRenderer hatRenderer;

    private bool wasWolfSelfActive;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
        }

        if (status == null)
        {
            status = GetComponentInParent<Status>();
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

    private void Update()
    {
        bool wolfSelfActive = IsEquipmentHiddenByWolf();
        if (wolfSelfActive == wasWolfSelfActive)
        {
            return;
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
        wasWolfSelfActive = IsEquipmentHiddenByWolf();
        SetEquipmentVisible(!wasWolfSelfActive);
        if (wasWolfSelfActive)
        {
            return;
        }

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

    private bool IsEquipmentHiddenByWolf()
    {
        return status != null && status.IsWolfSelfActive;
    }

    private void SetSprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer != null)
        {
            renderer.sprite = sprite;
        }
    }

    private void SetEquipmentVisible(bool visible)
    {
        SetRendererVisible(weaponRenderer, visible);
        SetRendererVisible(armorRenderer, visible);
        SetRendererVisible(hatRenderer, visible);
    }

    private void SetRendererVisible(SpriteRenderer renderer, bool visible)
    {
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }
}
