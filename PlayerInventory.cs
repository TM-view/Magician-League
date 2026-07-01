using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Action = System.Action;

public enum EquipmentSlotType
{
    Weapon,
    Armor,
    Hat,
}

public class PlayerInventory : NetworkBehaviour
{
    public const int InventorySlotCount = 12;

    [SerializeField]
    private ItemDatabase itemDatabase;

    [SerializeField]
    private NetworkPrefabRef groundItemPrefab;

    [SerializeField]
    private TMP_Text inventoryMessageText;

    [SerializeField]
    private InputActionReference pickupActionReference;

    [Header("Debug")]
    [SerializeField]
    private int debugItemId;

    [Networked]
    public int Slot0 { get; set; }

    [Networked]
    public int Slot1 { get; set; }

    [Networked]
    public int Slot2 { get; set; }

    [Networked]
    public int Slot3 { get; set; }

    [Networked]
    public int Slot4 { get; set; }

    [Networked]
    public int Slot5 { get; set; }

    [Networked]
    public int Slot6 { get; set; }

    [Networked]
    public int Slot7 { get; set; }

    [Networked]
    public int Slot8 { get; set; }

    [Networked]
    public int Slot9 { get; set; }

    [Networked]
    public int Slot10 { get; set; }

    [Networked]
    public int Slot11 { get; set; }

    [Networked]
    public int EquippedWeaponId { get; set; }

    [Networked]
    public int EquippedArmorId { get; set; }

    [Networked]
    public int EquippedHatId { get; set; }

    public event Action InventoryChanged;
    public event Action EquipmentChanged;

    private readonly List<SpellComponentSO> equipmentPowerups = new List<SpellComponentSO>();
    private ChangeDetector changes;
    private InputAction fallbackPickupAction;
    private GroundItem nearestGroundItem;
    private Coroutine messageRoutine;

    private InputAction PickupAction =>
        pickupActionReference != null ? pickupActionReference.action : fallbackPickupAction;

    public ItemDatabase Database => itemDatabase;

    private void OnEnable()
    {
        if (pickupActionReference != null && pickupActionReference.action != null)
        {
            pickupActionReference.action.Enable();
        }
        else
        {
            GetOrCreateFallbackPickupAction().Enable();
        }
    }

    private void OnDisable()
    {
        if (fallbackPickupAction != null)
        {
            fallbackPickupAction.Disable();
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid || !Object.HasInputAuthority)
        {
            return;
        }

        InputAction pickupAction = PickupAction;
        if (pickupAction != null && pickupAction.WasPressedThisFrame())
        {
            PickupNearestItem();
        }
    }

    public override void Spawned()
    {
        changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        NotifyAllChanged();
    }

    public override void Render()
    {
        foreach (var change in changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(EquippedWeaponId):
                case nameof(EquippedArmorId):
                case nameof(EquippedHatId):
                    EquipmentChanged?.Invoke();
                    InventoryChanged?.Invoke();
                    break;
                default:
                    InventoryChanged?.Invoke();
                    break;
            }
        }
    }

    public int GetInventoryItemId(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0:
                return Slot0;
            case 1:
                return Slot1;
            case 2:
                return Slot2;
            case 3:
                return Slot3;
            case 4:
                return Slot4;
            case 5:
                return Slot5;
            case 6:
                return Slot6;
            case 7:
                return Slot7;
            case 8:
                return Slot8;
            case 9:
                return Slot9;
            case 10:
                return Slot10;
            case 11:
                return Slot11;
            default:
                return 0;
        }
    }

    public int GetEquippedItemId(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon:
                return EquippedWeaponId;
            case EquipmentSlotType.Armor:
                return EquippedArmorId;
            case EquipmentSlotType.Hat:
                return EquippedHatId;
            default:
                return 0;
        }
    }

    public ItemSO GetItem(int itemId)
    {
        return itemDatabase != null ? itemDatabase.GetItem(itemId) : null;
    }

    public void EquipFromInventory(int inventorySlotIndex, EquipmentSlotType equipmentSlot)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_EquipFromInventory(inventorySlotIndex, (int)equipmentSlot);
            return;
        }

        TryEquipFromInventory(inventorySlotIndex, equipmentSlot);
    }

    public void UnequipToInventory(EquipmentSlotType equipmentSlot, int inventorySlotIndex)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_UnequipToInventory((int)equipmentSlot, inventorySlotIndex);
            return;
        }

        TryUnequipToInventory(equipmentSlot, inventorySlotIndex);
    }

    public void SwapInventorySlots(int fromSlotIndex, int toSlotIndex)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_SwapInventorySlots(fromSlotIndex, toSlotIndex);
            return;
        }

        TrySwapInventorySlots(fromSlotIndex, toSlotIndex);
    }

    public void DropInventorySlot(int slotIndex)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_DropInventorySlot(slotIndex);
            return;
        }

        TryDropInventorySlot(slotIndex);
    }

    public void DropEquippedItem(EquipmentSlotType equipmentSlot)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_DropEquippedItem((int)equipmentSlot);
            return;
        }

        TryDropEquippedItem(equipmentSlot);
    }

    public void QuickEquipInventorySlot(int inventorySlotIndex)
    {
        int itemId = GetInventoryItemId(inventorySlotIndex);
        ItemSO item = GetItem(itemId);
        if (item == null)
        {
            return;
        }

        if (!TryGetEquipmentSlotForItem(item, out EquipmentSlotType equipmentSlot))
        {
            return;
        }

        EquipFromInventory(inventorySlotIndex, equipmentSlot);
    }

    public void QuickUnequipEquippedItem(EquipmentSlotType equipmentSlot)
    {
        if (GetEquippedItemId(equipmentSlot) <= 0)
        {
            return;
        }

        int emptySlotIndex = GetFirstEmptySlotIndex();
        if (emptySlotIndex < 0)
        {
            ShowMessage("Inventory full");
            return;
        }

        UnequipToInventory(equipmentSlot, emptySlotIndex);
    }

    public void PickupNearestItem()
    {
        if (nearestGroundItem == null)
        {
            ShowMessage("No item nearby");
            return;
        }

        nearestGroundItem.RequestPickup(Object);
    }

    [ContextMenu("Debug/Add Item To Inventory")]
    public void DebugAddItemToInventory()
    {
        AddItemById(debugItemId);
    }

    [ContextMenu("Debug/Spawn Item On Ground")]
    public void DebugSpawnItemOnGround()
    {
        SpawnItemOnGroundById(debugItemId);
    }

    public void AddItemById(int itemId)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_DebugAddItem(itemId);
            return;
        }

        TryAddItem(itemId);
    }

    public void SpawnItemOnGroundById(int itemId)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_DebugSpawnItemOnGround(itemId);
            return;
        }

        SpawnGroundItem(itemId);
    }

    public bool TryAddItem(int itemId)
    {
        if (!Object.HasStateAuthority)
        {
            return false;
        }

        int slotIndex = GetFirstEmptySlotIndex();
        if (slotIndex < 0)
        {
            ShowMessage("Inventory full");
            return false;
        }

        SetInventoryItemId(slotIndex, itemId);
        NotifyAllChanged();
        return true;
    }

    public ToolStat GetEquipmentStats()
    {
        ToolStat total = default;
        AddItemStats(EquippedWeaponId, ref total);
        AddItemStats(EquippedArmorId, ref total);
        AddItemStats(EquippedHatId, ref total);
        return ToolStatUtility.ClampNonNegative(total);
    }

    public void ApplyEquipmentPowerups(SpellCastData spell)
    {
        if (spell == null)
        {
            return;
        }

        equipmentPowerups.Clear();
        AddItemPowerups(EquippedWeaponId, equipmentPowerups);
        AddItemPowerups(EquippedArmorId, equipmentPowerups);
        AddItemPowerups(EquippedHatId, equipmentPowerups);

        if (equipmentPowerups.Count == 0)
        {
            return;
        }

        for (int i = 0; i < spell.Segments.Count; i++)
        {
            SpellSegmentData segment = spell.Segments[i];
            if (segment == null || !segment.HasAnyEffect)
            {
                continue;
            }

            for (int j = 0; j < equipmentPowerups.Count; j++)
            {
                SpellParser.ApplyPowerupWithoutManaCost(equipmentPowerups[j], segment);
            }
        }
    }

    public void SetNearestGroundItem(GroundItem groundItem)
    {
        nearestGroundItem = groundItem;
    }

    public void ClearNearestGroundItem(GroundItem groundItem)
    {
        if (nearestGroundItem == groundItem)
        {
            nearestGroundItem = null;
        }
    }

    public void ShowMessage(string message)
    {
        if (inventoryMessageText != null)
        {
            inventoryMessageText.text = message;
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(ClearMessageAfterDelay());
        }
    }

    private IEnumerator ClearMessageAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        if (inventoryMessageText != null)
        {
            inventoryMessageText.text = string.Empty;
        }

        messageRoutine = null;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_EquipFromInventory(int inventorySlotIndex, int equipmentSlot)
    {
        TryEquipFromInventory(inventorySlotIndex, (EquipmentSlotType)equipmentSlot);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UnequipToInventory(int equipmentSlot, int inventorySlotIndex)
    {
        TryUnequipToInventory((EquipmentSlotType)equipmentSlot, inventorySlotIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SwapInventorySlots(int fromSlotIndex, int toSlotIndex)
    {
        TrySwapInventorySlots(fromSlotIndex, toSlotIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DropInventorySlot(int slotIndex)
    {
        TryDropInventorySlot(slotIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DropEquippedItem(int equipmentSlot)
    {
        TryDropEquippedItem((EquipmentSlotType)equipmentSlot);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DebugAddItem(int itemId)
    {
        TryAddItem(itemId);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DebugSpawnItemOnGround(int itemId)
    {
        SpawnGroundItem(itemId);
    }

    private bool TryEquipFromInventory(int inventorySlotIndex, EquipmentSlotType equipmentSlot)
    {
        int itemId = GetInventoryItemId(inventorySlotIndex);
        ItemSO item = GetItem(itemId);
        if (item == null || !CanEquip(item, equipmentSlot))
        {
            return false;
        }

        int equippedItemId = GetEquippedItemId(equipmentSlot);
        SetInventoryItemId(inventorySlotIndex, equippedItemId);
        SetEquippedItemId(equipmentSlot, itemId);
        NotifyAllChanged();
        return true;
    }

    private bool TryUnequipToInventory(EquipmentSlotType equipmentSlot, int inventorySlotIndex)
    {
        int equippedItemId = GetEquippedItemId(equipmentSlot);
        if (equippedItemId <= 0 || !IsValidSlotIndex(inventorySlotIndex))
        {
            return false;
        }

        int inventoryItemId = GetInventoryItemId(inventorySlotIndex);
        if (inventoryItemId > 0)
        {
            ItemSO inventoryItem = GetItem(inventoryItemId);
            if (inventoryItem == null || !CanEquip(inventoryItem, equipmentSlot))
            {
                return false;
            }
        }

        SetEquippedItemId(equipmentSlot, inventoryItemId);
        SetInventoryItemId(inventorySlotIndex, equippedItemId);
        NotifyAllChanged();
        return true;
    }

    private bool TrySwapInventorySlots(int fromSlotIndex, int toSlotIndex)
    {
        if (!IsValidSlotIndex(fromSlotIndex) || !IsValidSlotIndex(toSlotIndex))
        {
            return false;
        }

        int fromItemId = GetInventoryItemId(fromSlotIndex);
        int toItemId = GetInventoryItemId(toSlotIndex);
        SetInventoryItemId(fromSlotIndex, toItemId);
        SetInventoryItemId(toSlotIndex, fromItemId);
        NotifyAllChanged();
        return true;
    }

    private bool TryDropInventorySlot(int slotIndex)
    {
        int itemId = GetInventoryItemId(slotIndex);
        if (itemId <= 0 || !SpawnGroundItem(itemId))
        {
            return false;
        }

        SetInventoryItemId(slotIndex, 0);
        NotifyAllChanged();
        return true;
    }

    private bool TryDropEquippedItem(EquipmentSlotType equipmentSlot)
    {
        int itemId = GetEquippedItemId(equipmentSlot);
        if (itemId <= 0 || !SpawnGroundItem(itemId))
        {
            return false;
        }

        SetEquippedItemId(equipmentSlot, 0);
        NotifyAllChanged();
        return true;
    }

    private bool SpawnGroundItem(int itemId)
    {
        if (GetItem(itemId) == null)
        {
            ShowMessage("Invalid item id");
            return false;
        }

        if (!groundItemPrefab.IsValid)
        {
            ShowMessage("Missing ground item prefab");
            return false;
        }

        NetworkObject groundObject = Runner.Spawn(
            groundItemPrefab,
            transform.position,
            Quaternion.identity
        );

        GroundItem groundItem =
            groundObject != null ? groundObject.GetComponent<GroundItem>() : null;
        if (groundItem == null)
        {
            return false;
        }

        groundItem.SetItem(itemId);
        return true;
    }

    private bool CanEquip(ItemSO item, EquipmentSlotType slotType)
    {
        return item != null
            && (
                slotType == EquipmentSlotType.Weapon && item.type == ItemType.Weapon
                || slotType == EquipmentSlotType.Armor && item.type == ItemType.Armor
                || slotType == EquipmentSlotType.Hat && item.type == ItemType.Hat
            );
    }

    private bool TryGetEquipmentSlotForItem(ItemSO item, out EquipmentSlotType slotType)
    {
        slotType = EquipmentSlotType.Weapon;
        if (item == null)
        {
            return false;
        }

        switch (item.type)
        {
            case ItemType.Weapon:
                slotType = EquipmentSlotType.Weapon;
                return true;
            case ItemType.Armor:
                slotType = EquipmentSlotType.Armor;
                return true;
            case ItemType.Hat:
                slotType = EquipmentSlotType.Hat;
                return true;
            default:
                return false;
        }
    }

    private void AddItemStats(int itemId, ref ToolStat total)
    {
        ItemSO item = GetItem(itemId);
        if (item != null)
        {
            total += item.TotalStats;
        }
    }

    private void AddItemPowerups(int itemId, List<SpellComponentSO> powerups)
    {
        ItemSO item = GetItem(itemId);
        if (item == null || item.powerups == null)
        {
            return;
        }

        int powerupCount = Mathf.Min(item.powerups.Count, ItemSO.MaxPowerupSlots);
        for (int i = 0; i < powerupCount; i++)
        {
            SpellComponentSO powerup = item.powerups[i];
            if (powerup != null && powerup.Type == SpellComponentType.PowerUp)
            {
                powerups.Add(powerup);
            }
        }
    }

    private int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < InventorySlotCount; i++)
        {
            if (GetInventoryItemId(i) == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < InventorySlotCount;
    }

    private void SetInventoryItemId(int slotIndex, int itemId)
    {
        switch (slotIndex)
        {
            case 0:
                Slot0 = itemId;
                break;
            case 1:
                Slot1 = itemId;
                break;
            case 2:
                Slot2 = itemId;
                break;
            case 3:
                Slot3 = itemId;
                break;
            case 4:
                Slot4 = itemId;
                break;
            case 5:
                Slot5 = itemId;
                break;
            case 6:
                Slot6 = itemId;
                break;
            case 7:
                Slot7 = itemId;
                break;
            case 8:
                Slot8 = itemId;
                break;
            case 9:
                Slot9 = itemId;
                break;
            case 10:
                Slot10 = itemId;
                break;
            case 11:
                Slot11 = itemId;
                break;
        }
    }

    private void SetEquippedItemId(EquipmentSlotType slotType, int itemId)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon:
                EquippedWeaponId = itemId;
                break;
            case EquipmentSlotType.Armor:
                EquippedArmorId = itemId;
                break;
            case EquipmentSlotType.Hat:
                EquippedHatId = itemId;
                break;
        }
    }

    private InputAction GetOrCreateFallbackPickupAction()
    {
        if (fallbackPickupAction != null)
        {
            return fallbackPickupAction;
        }

        fallbackPickupAction = new InputAction("Pickup", InputActionType.Button);
        fallbackPickupAction.AddBinding("<Keyboard>/f");
        return fallbackPickupAction;
    }

    private void NotifyAllChanged()
    {
        InventoryChanged?.Invoke();
        EquipmentChanged?.Invoke();
    }
}
