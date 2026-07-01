using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Magician_League/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private List<ItemSO> items = new List<ItemSO>();

    private Dictionary<int, ItemSO> itemById;

    public ItemSO GetItem(int itemId)
    {
        if (itemId <= 0)
        {
            return null;
        }

        EnsureCache();
        itemById.TryGetValue(itemId, out ItemSO item);
        return item;
    }

    private void EnsureCache()
    {
        if (itemById != null)
        {
            return;
        }

        itemById = new Dictionary<int, ItemSO>();
        for (int i = 0; i < items.Count; i++)
        {
            ItemSO item = items[i];
            if (item == null || item.itemId <= 0)
            {
                continue;
            }

            itemById[item.itemId] = item;
        }
    }

    private void OnValidate()
    {
        itemById = null;
    }
}
