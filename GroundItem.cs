using Fusion;
using TMPro;
using UnityEngine;

public class GroundItem : NetworkBehaviour
{
    private const float GroundLifetimeSeconds = 60f;

    [SerializeField]
    private ItemDatabase itemDatabase;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private GameObject pickupPrompt;

    [SerializeField]
    private TMP_Text pickupPromptText;

    [SerializeField]
    private float targetVisualSize = 0.6f;

    [Networked]
    public int ItemId { get; private set; }

    [Networked]
    private TickTimer DespawnTimer { get; set; }

    private ChangeDetector changes;

    private void Awake()
    {
        SetPickupPromptVisible(false);
    }

    public override void Spawned()
    {
        changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasStateAuthority)
        {
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, GroundLifetimeSeconds);
        }

        RefreshVisual();
        SetPickupPromptVisible(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (
            Object.HasStateAuthority
            && DespawnTimer.IsRunning
            && DespawnTimer.ExpiredOrNotRunning(Runner)
        )
        {
            Runner.Despawn(Object);
        }
    }

    public override void Render()
    {
        foreach (var change in changes.DetectChanges(this))
        {
            if (change == nameof(ItemId))
            {
                RefreshVisual();
            }
        }
    }

    public void SetItem(int itemId)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        ItemId = itemId;
        RefreshVisual();
    }

    public void RequestPickup(NetworkObject inventoryObject)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_RequestPickup(inventoryObject);
            return;
        }

        TryPickup(inventoryObject);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPickup(NetworkObject inventoryObject)
    {
        TryPickup(inventoryObject);
    }

    private void TryPickup(NetworkObject inventoryObject)
    {
        if (ItemId <= 0 || inventoryObject == null)
        {
            return;
        }

        PlayerInventory inventory = inventoryObject.GetComponent<PlayerInventory>();
        if (inventory == null || !inventory.TryAddItem(ItemId))
        {
            return;
        }

        Runner.Despawn(Object);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory != null && inventory.Object != null && inventory.Object.HasInputAuthority)
        {
            inventory.SetNearestGroundItem(this);
            SetPickupPromptVisible(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory != null && inventory.Object != null && inventory.Object.HasInputAuthority)
        {
            inventory.ClearNearestGroundItem(this);
            SetPickupPromptVisible(false);
        }
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null || itemDatabase == null)
        {
            return;
        }

        ItemSO item = itemDatabase.GetItem(ItemId);
        spriteRenderer.sprite = item != null ? item.icon : null;
        NormalizeVisualSize(spriteRenderer.sprite);
        SetItemName(item);
        SetPickupPromptText();
    }

    private void SetItemName(ItemSO item)
    {
        if (itemNameText != null)
        {
            itemNameText.text = item != null ? item.itemName : "";
        }
    }

    private void SetPickupPromptText()
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.text = "Press [F] to pick up";
        }
    }

    private void SetPickupPromptVisible(bool visible)
    {
        if (pickupPrompt != null && pickupPrompt.activeSelf != visible)
        {
            pickupPrompt.SetActive(visible);
        }
    }

    private void NormalizeVisualSize(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (sprite == null)
        {
            spriteRenderer.transform.localScale = Vector3.one;
            return;
        }

        Vector2 spriteSize = sprite.bounds.size;
        float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        if (largestSide <= 0f)
        {
            spriteRenderer.transform.localScale = Vector3.one;
            return;
        }

        float scale = targetVisualSize / largestSide;
        spriteRenderer.transform.localScale = Vector3.one * scale;
    }
}
