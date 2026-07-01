using System;

[Flags]
public enum SpellVfxMask
{
    None = 0,
    Burn = 1 << 0,
    Freeze = 1 << 1,
    Poison = 1 << 2,
    Wet = 1 << 3,
    Shock = 1 << 4,
    Barrier = 1 << 5,
    Cure = 1 << 6,
    Ironbody = 1 << 7,
    Smog = 1 << 8,
    Magnet = 1 << 9,
    Shift = 1 << 10,
    Heal = 1 << 11,
    Explode = 1 << 12,
}

public static class SpellVfxMaskUtility
{
    public static SpellVfxMask FromSegment(SpellSegmentData segment)
    {
        if (segment == null)
        {
            return SpellVfxMask.None;
        }

        SpellVfxMask mask = SpellVfxMask.None;
        AddIf(ref mask, segment.Burn > 0, SpellVfxMask.Burn);
        AddIf(ref mask, segment.Freeze > 0, SpellVfxMask.Freeze);
        AddIf(ref mask, segment.Poison > 0, SpellVfxMask.Poison);
        AddIf(ref mask, segment.Wet > 0, SpellVfxMask.Wet);
        AddIf(ref mask, segment.Shock > 0, SpellVfxMask.Shock);
        AddIf(ref mask, segment.Barrier > 0, SpellVfxMask.Barrier);
        AddIf(ref mask, segment.Cure > 0, SpellVfxMask.Cure);
        AddIf(ref mask, segment.Ironbody > 0, SpellVfxMask.Ironbody);
        AddIf(ref mask, segment.Smog > 0, SpellVfxMask.Smog);
        AddIf(ref mask, segment.Magnet > 0, SpellVfxMask.Magnet);
        AddIf(ref mask, segment.Shift > 0, SpellVfxMask.Shift);
        AddIf(ref mask, segment.Heal > 0, SpellVfxMask.Heal);
        AddIf(ref mask, segment.Explode > 0, SpellVfxMask.Explode);
        return mask;
    }

    public static bool Has(this SpellVfxMask mask, SpellVfxMask value)
    {
        return (mask & value) != 0;
    }

    private static void AddIf(ref SpellVfxMask mask, bool condition, SpellVfxMask value)
    {
        if (condition)
        {
            mask |= value;
        }
    }
}
