using System.Collections.Generic;
using UnityEngine;

public enum SpellActiveId
{
    None,
    Self,
    Linear,
    Laser,
    Donut,
    Cone,
    Around,
    Touch,
}

public enum SpellEffectId
{
    None,
    Damage,
    Heal,
    Cure,
    Barrier,
    Magnet,
    Duplicate,
    Repeat,
    Shift,
    Smog,
    Ironbody,
    Wolf,
}

public enum SpellPowerupId
{
    None,
    Accelerate,
    Burn,
    Delay1,
    Delay2,
    Pierce,
    Poison,
    Powerup1,
    Powerup2,
    Powerup3,
    Wet,
    Shock,
    Freeze,
    Transfer,
    Explode,
    Leech,
    Slaughter,
    Reverse,
    Bounce,
}

public enum SpellDebuffId
{
    Burning,
    Poisoning,
    Wetting,
    Shocking,
    Freezing,
}

public struct SpellImpact
{
    public int Damage;
    public int Heal;
    public int Cure;
    public int Barrier;
    public int Pierce;
    public int BurnRolls;
    public int PoisonRolls;
    public int WetRolls;
    public int ShockRolls;
    public int FreezeRolls;
    public int Explode;
    public int Leech;
    public int Slaughter;
    public int Powerup2;
    public bool IgnoreBarrierForExplosion;
}

public class SpellSegmentData
{
    public SpellActiveId Active;
    public int ManaCost;
    public int Damage;
    public int Heal;
    public int Cure;
    public int Barrier;
    public int Magnet;
    public int Duplicate;
    public int Repeat;
    public int Shift;
    public int Smog;
    public int Ironbody;
    public int Wolf;
    public int Accelerate;
    public int Burn;
    public int Delay1;
    public int Delay2;
    public int Pierce;
    public int Poison;
    public int Powerup1;
    public int Powerup2;
    public int Powerup3;
    public int Wet;
    public int Shock;
    public int Freeze;
    public int Transfer;
    public int Explode;
    public int Leech;
    public int Slaughter;
    public int Reverse;
    public int Bounce;
    public float Radius;
    public float Range;
    public float Speed;
    public float Lifetime;

    public bool HasAnyEffect =>
        Damage > 0
        || Heal > 0
        || Cure > 0
        || Barrier > 0
        || Magnet > 0
        || Duplicate > 0
        || Repeat > 0
        || Shift > 0
        || Smog > 0
        || Ironbody > 0
        || Wolf > 0;

    public bool HasProjectile => Active == SpellActiveId.Linear || Active == SpellActiveId.Laser;

    public float DelaySeconds => Delay1 * 0.5f + Delay2;

    public int ProjectileCount => Mathf.Max(1, 1 + Duplicate);

    public int RepeatCount => Mathf.Max(1, 1 + Repeat);

    public int BounceCount => Mathf.Max(0, Bounce);

    public float EffectiveRadius => Mathf.Max(0.1f, Radius + Powerup1 * 0.35f);

    public float EffectiveRange => Mathf.Max(0.1f, Range + Powerup3);

    public float EffectiveLifetime => Mathf.Max(0.1f, Lifetime + Powerup3 * 0.25f);

    public float EffectiveSpeed => Mathf.Max(0.1f, Speed + Accelerate * 2f);

    public SpellImpact CreateImpact()
    {
        int strengthBonus = Mathf.Max(0, Powerup2);

        return new SpellImpact
        {
            Damage = Mathf.Max(0, Damage + strengthBonus),
            Heal = Mathf.Max(0, Heal + strengthBonus),
            Cure = Cure,
            Barrier = Barrier + strengthBonus,
            Pierce = Pierce + (Active == SpellActiveId.Laser ? 2 : 0),
            BurnRolls = Burn,
            PoisonRolls = Poison,
            WetRolls = Wet,
            ShockRolls = Shock,
            FreezeRolls = Freeze,
            Explode = Explode,
            Leech = Leech,
            Slaughter = Slaughter,
            Powerup2 = Powerup2,
            IgnoreBarrierForExplosion = Explode > 0,
        };
    }
}

public class SpellCastData
{
    public int ManaCost;
    public float Cooldown;
    public readonly List<SpellSegmentData> Segments = new List<SpellSegmentData>();

    public bool UsesTransfer
    {
        get
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                if (Segments[i].Transfer > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

public static class SpellRuntimeIds
{
    public static SpellActiveId GetActiveId(SpellComponentSO component)
    {
        if (component == null)
        {
            return SpellActiveId.None;
        }

        switch (component.Name)
        {
            case "Self":
                return SpellActiveId.Self;
            case "Linear":
                return SpellActiveId.Linear;
            case "Laser":
                return SpellActiveId.Laser;
            case "Donut":
                return SpellActiveId.Donut;
            case "Cone":
                return SpellActiveId.Cone;
            case "Around":
                return SpellActiveId.Around;
            case "Touch":
                return SpellActiveId.Touch;
            default:
                return SpellActiveId.None;
        }
    }

    public static SpellEffectId GetEffectId(SpellComponentSO component)
    {
        if (component == null)
        {
            return SpellEffectId.None;
        }

        switch (component.Name)
        {
            case "Damage":
                return SpellEffectId.Damage;
            case "Heal":
                return SpellEffectId.Heal;
            case "Cure":
                return SpellEffectId.Cure;
            case "Barrier":
                return SpellEffectId.Barrier;
            case "Magnet":
                return SpellEffectId.Magnet;
            case "Duplicate":
                return SpellEffectId.Duplicate;
            case "Repeat":
                return SpellEffectId.Repeat;
            case "Shift":
                return SpellEffectId.Shift;
            case "Smog":
                return SpellEffectId.Smog;
            case "Ironbody":
                return SpellEffectId.Ironbody;
            case "Wolf":
                return SpellEffectId.Wolf;
            default:
                return SpellEffectId.None;
        }
    }

    public static SpellPowerupId GetPowerupId(SpellComponentSO component)
    {
        if (component == null)
        {
            return SpellPowerupId.None;
        }

        switch (component.Name)
        {
            case "Accelerate":
                return SpellPowerupId.Accelerate;
            case "Burn":
                return SpellPowerupId.Burn;
            case "Delay1":
                return SpellPowerupId.Delay1;
            case "Delay2":
                return SpellPowerupId.Delay2;
            case "Pierce":
                return SpellPowerupId.Pierce;
            case "Poison":
                return SpellPowerupId.Poison;
            case "Powerup1":
                return SpellPowerupId.Powerup1;
            case "Powerup2":
                return SpellPowerupId.Powerup2;
            case "Powerup3":
                return SpellPowerupId.Powerup3;
            case "Wet":
                return SpellPowerupId.Wet;
            case "Shock":
                return SpellPowerupId.Shock;
            case "Freeze":
                return SpellPowerupId.Freeze;
            case "Transfer":
                return SpellPowerupId.Transfer;
            case "Explode":
                return SpellPowerupId.Explode;
            case "Leech":
                return SpellPowerupId.Leech;
            case "Slaughter":
                return SpellPowerupId.Slaughter;
            case "Reverse":
                return SpellPowerupId.Reverse;
            case "Bounce":
                return SpellPowerupId.Bounce;
            default:
                return SpellPowerupId.None;
        }
    }
}

public static class SpellParser
{
    public static bool TryBuild(
        List<SpellComponentSO> components,
        int baseManaCost,
        float defaultRadius,
        float defaultRange,
        float defaultSpeed,
        float defaultLifetime,
        out SpellCastData spell
    )
    {
        spell = new SpellCastData { ManaCost = baseManaCost };

        if (components == null || components.Count == 0)
        {
            return false;
        }

        SpellSegmentData current = null;
        bool hasEffectInSegment = false;
        bool foundFirstComponent = false;

        for (int i = 0; i < components.Count; i++)
        {
            SpellComponentSO component = components[i];
            if (component == null)
            {
                continue;
            }

            spell.Cooldown += Mathf.Max(0f, component.cooldown);

            if (!foundFirstComponent)
            {
                foundFirstComponent = true;
                if (component.Type != SpellComponentType.Active)
                {
                    return false;
                }
            }

            if (component.Type == SpellComponentType.Active)
            {
                current = CreateSegment(
                    component,
                    defaultRadius,
                    defaultRange,
                    defaultSpeed,
                    defaultLifetime
                );
                spell.Segments.Add(current);
                spell.ManaCost += component.manaCost;
                hasEffectInSegment = false;
                continue;
            }

            if (current == null)
            {
                return false;
            }

            if (component.Type == SpellComponentType.Effect)
            {
                ApplyEffect(component, current);
                spell.ManaCost += component.manaCost;
                hasEffectInSegment = true;
                continue;
            }

            if (component.Type == SpellComponentType.PowerUp)
            {
                if (!hasEffectInSegment)
                {
                    return false;
                }

                ApplyPowerupWithoutManaCost(component, current);
                spell.ManaCost += component.manaCost;
            }
        }

        return spell.Segments.Count > 0;
    }

    private static SpellSegmentData CreateSegment(
        SpellComponentSO component,
        float defaultRadius,
        float defaultRange,
        float defaultSpeed,
        float defaultLifetime
    )
    {
        SpellSegmentData segment = new SpellSegmentData
        {
            Active = SpellRuntimeIds.GetActiveId(component),
            Radius = component.radius > 0f ? component.radius : defaultRadius,
            Range = component.range > 0f ? component.range : defaultRange,
            Speed = component.speed > 0f ? component.speed : defaultSpeed,
            Lifetime = component.lifetime > 0f ? component.lifetime : defaultLifetime,
        };

        return segment;
    }

    private static void ApplyEffect(SpellComponentSO component, SpellSegmentData segment)
    {
        int amount = Mathf.Max(1, component.value);

        switch (SpellRuntimeIds.GetEffectId(component))
        {
            case SpellEffectId.Damage:
                segment.Damage += amount;
                break;
            case SpellEffectId.Heal:
                segment.Heal += amount;
                break;
            case SpellEffectId.Cure:
                segment.Cure += amount;
                break;
            case SpellEffectId.Barrier:
                segment.Barrier += amount;
                break;
            case SpellEffectId.Magnet:
                segment.Magnet += amount;
                break;
            case SpellEffectId.Duplicate:
                segment.Duplicate += amount;
                break;
            case SpellEffectId.Repeat:
                segment.Repeat += amount;
                break;
            case SpellEffectId.Shift:
                segment.Shift += amount;
                break;
            case SpellEffectId.Smog:
                segment.Smog += amount;
                break;
            case SpellEffectId.Ironbody:
                segment.Ironbody += amount;
                break;
            case SpellEffectId.Wolf:
                segment.Wolf += amount;
                break;
        }
    }

    public static void ApplyPowerupWithoutManaCost(
        SpellComponentSO component,
        SpellSegmentData segment
    )
    {
        if (component == null || segment == null || component.Type != SpellComponentType.PowerUp)
        {
            return;
        }

        int amount = Mathf.Max(1, component.value);

        switch (SpellRuntimeIds.GetPowerupId(component))
        {
            case SpellPowerupId.Accelerate:
                segment.Accelerate += amount;
                break;
            case SpellPowerupId.Burn:
                segment.Burn += amount;
                break;
            case SpellPowerupId.Delay1:
                segment.Delay1 += amount;
                break;
            case SpellPowerupId.Delay2:
                segment.Delay2 += amount;
                break;
            case SpellPowerupId.Pierce:
                segment.Pierce += amount;
                break;
            case SpellPowerupId.Poison:
                segment.Poison += amount;
                break;
            case SpellPowerupId.Powerup1:
                segment.Powerup1 += amount;
                break;
            case SpellPowerupId.Powerup2:
                segment.Powerup2 += amount;
                break;
            case SpellPowerupId.Powerup3:
                segment.Powerup3 += amount;
                break;
            case SpellPowerupId.Wet:
                segment.Wet += amount;
                break;
            case SpellPowerupId.Shock:
                segment.Shock += amount;
                break;
            case SpellPowerupId.Freeze:
                segment.Freeze += amount;
                break;
            case SpellPowerupId.Transfer:
                segment.Transfer += amount;
                break;
            case SpellPowerupId.Explode:
                segment.Explode += amount;
                break;
            case SpellPowerupId.Leech:
                segment.Leech += amount;
                break;
            case SpellPowerupId.Slaughter:
                segment.Slaughter += amount;
                break;
            case SpellPowerupId.Reverse:
                segment.Reverse += amount;
                break;
            case SpellPowerupId.Bounce:
                segment.Bounce += amount;
                break;
        }
    }
}
