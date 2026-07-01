using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Hat,
}

[CreateAssetMenu(fileName = "New Item", menuName = "Magician_League/Item")]
public class ItemSO : ScriptableObject
{
    public const int MaxPowerupSlots = 4;

    public int itemId;
    public string itemName;

    [TextArea]
    public string description;
    public ItemType type;
    public Sprite icon;
    public Sprite equippedSprite;
    public ToolStat mainStats;
    public ItemStatBonus[] bonusStats = new ItemStatBonus[3];
    public List<SpellComponentSO> powerups = new List<SpellComponentSO>();

    public ToolStat TotalStats
    {
        get
        {
            ToolStat total = mainStats;
            if (bonusStats == null)
            {
                return total;
            }

            for (int i = 0; i < bonusStats.Length; i++)
            {
                if (bonusStats[i].IsValid)
                {
                    total.Add(bonusStats[i].Stat, bonusStats[i].Amount);
                }
            }

            return total;
        }
    }

    private void OnValidate()
    {
        if (bonusStats == null || bonusStats.Length != 3)
        {
            ItemStatBonus[] resized = new ItemStatBonus[3];
            if (bonusStats != null)
            {
                for (int i = 0; i < Mathf.Min(bonusStats.Length, resized.Length); i++)
                {
                    resized[i] = bonusStats[i];
                }
            }

            bonusStats = resized;
        }

        ClampMainStatsToItemType();
        RemoveInvalidPowerups();
        ClampPowerupSlots();
    }

    private void ClampMainStatsToItemType()
    {
        switch (type)
        {
            case ItemType.Weapon:
                mainStats.HP = 0;
                mainStats.MP = 0;
                mainStats.STR = 0;
                mainStats.LUK = 0;
                break;
            case ItemType.Armor:
                mainStats.MP = 0;
                mainStats.CD = 0;
                mainStats.VAL = 0;
                mainStats.LUK = 0;
                break;
            case ItemType.Hat:
                mainStats.HP = 0;
                mainStats.CD = 0;
                mainStats.VAL = 0;
                mainStats.STR = 0;
                break;
        }
    }

    private void RemoveInvalidPowerups()
    {
        if (powerups == null)
        {
            return;
        }

        for (int i = powerups.Count - 1; i >= 0; i--)
        {
            if (powerups[i] != null && powerups[i].Type != SpellComponentType.PowerUp)
            {
                powerups.RemoveAt(i);
            }
        }
    }

    private void ClampPowerupSlots()
    {
        if (powerups == null)
        {
            return;
        }

        while (powerups.Count > MaxPowerupSlots)
        {
            powerups.RemoveAt(powerups.Count - 1);
        }
    }
}
