using System;
using UnityEngine;

[Serializable]
public struct ToolStat
{
    public int HP;
    public int MP;
    public int CD;
    public int VAL;
    public int STR;
    public int LUK;

    public static ToolStat operator +(ToolStat left, ToolStat right)
    {
        return new ToolStat
        {
            HP = left.HP + right.HP,
            MP = left.MP + right.MP,
            CD = left.CD + right.CD,
            VAL = left.VAL + right.VAL,
            STR = left.STR + right.STR,
            LUK = left.LUK + right.LUK,
        };
    }

    public void Add(PlayerStatId stat, int amount)
    {
        switch (stat)
        {
            case PlayerStatId.HP:
                HP += amount;
                break;
            case PlayerStatId.MP:
                MP += amount;
                break;
            case PlayerStatId.CD:
                CD += amount;
                break;
            case PlayerStatId.VAL:
                VAL += amount;
                break;
            case PlayerStatId.STR:
                STR += amount;
                break;
            case PlayerStatId.LUK:
                LUK += amount;
                break;
        }
    }
}

[Serializable]
public struct ItemStatBonus
{
    public PlayerStatId Stat;
    public int Amount;

    public bool IsValid => Amount != 0;
}

public static class ToolStatUtility
{
    public static ToolStat ClampNonNegative(ToolStat stat)
    {
        return new ToolStat
        {
            HP = Mathf.Max(0, stat.HP),
            MP = Mathf.Max(0, stat.MP),
            CD = Mathf.Max(0, stat.CD),
            VAL = Mathf.Max(0, stat.VAL),
            STR = Mathf.Max(0, stat.STR),
            LUK = Mathf.Max(0, stat.LUK),
        };
    }
}
