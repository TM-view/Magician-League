using UnityEngine;

public class SpellProjectileVfxController : MonoBehaviour
{
    private const int ProjectileVfxOrder = RuntimeVfxFactory.ProjectileOrder;

    private SpellVfxMask currentMask;
    private Vector2 direction = Vector2.right;
    private float time;

    private GameObject burnRoot;
    private SpriteRenderer[] flames;
    private GameObject freezeRoot;
    private SpriteRenderer[] iceShards;
    private GameObject poisonRoot;
    private SpriteRenderer[] poisonDrops;
    private GameObject wetRoot;
    private SpriteRenderer[] waterDrops;
    private GameObject shockRoot;
    private LineRenderer[] shockLines;
    private GameObject barrierRoot;
    private SpriteRenderer barrierFill;
    private SpriteRenderer barrierRing;
    private GameObject cureRoot;
    private SpriteRenderer cureVertical;
    private SpriteRenderer cureHorizontal;
    private GameObject ironbodyRoot;
    private SpriteRenderer ironFill;
    private SpriteRenderer ironRing;
    private GameObject smogRoot;
    private SpriteRenderer[] smokePuffs;
    private GameObject magnetRoot;
    private SpriteRenderer[] magnetRings;
    private GameObject shiftRoot;
    private LineRenderer[] windLines;
    private GameObject healRoot;
    private SpriteRenderer[] healVerticals;
    private SpriteRenderer[] healHorizontals;
    private GameObject explodeRoot;
    private SpriteRenderer explodeFill;
    private SpriteRenderer explodeRing;

    private void Awake()
    {
        BuildIfNeeded();
    }

    public void Configure(SpellVfxMask mask, Vector2 forwardDirection)
    {
        BuildIfNeeded();
        currentMask = mask;
        if (forwardDirection.sqrMagnitude > 0.0001f)
        {
            direction = forwardDirection.normalized;
        }

        SetActive(burnRoot, currentMask.Has(SpellVfxMask.Burn));
        SetActive(freezeRoot, currentMask.Has(SpellVfxMask.Freeze));
        SetActive(poisonRoot, currentMask.Has(SpellVfxMask.Poison));
        SetActive(wetRoot, currentMask.Has(SpellVfxMask.Wet));
        SetActive(shockRoot, currentMask.Has(SpellVfxMask.Shock));
        SetActive(barrierRoot, currentMask.Has(SpellVfxMask.Barrier));
        SetActive(cureRoot, currentMask.Has(SpellVfxMask.Cure));
        SetActive(ironbodyRoot, currentMask.Has(SpellVfxMask.Ironbody));
        SetActive(smogRoot, currentMask.Has(SpellVfxMask.Smog));
        SetActive(magnetRoot, currentMask.Has(SpellVfxMask.Magnet));
        SetActive(shiftRoot, currentMask.Has(SpellVfxMask.Shift));
        SetActive(healRoot, currentMask.Has(SpellVfxMask.Heal));
        SetActive(explodeRoot, currentMask.Has(SpellVfxMask.Explode));
    }

    private void LateUpdate()
    {
        time += Time.deltaTime;
        AnimateBurn();
        AnimateFreeze();
        AnimatePoison();
        AnimateWet();
        AnimateShock();
        AnimateBarrier();
        AnimateCure();
        AnimateIronbody();
        AnimateSmog();
        AnimateMagnet();
        AnimateShift();
        AnimateHeal();
        AnimateExplode();
    }

    private void BuildIfNeeded()
    {
        if (burnRoot != null)
        {
            return;
        }

        burnRoot = CreateRoot("SpellBurnVfx");
        flames = CreateSprites(burnRoot.transform, "Flame", 6, RuntimeVfxFactory.CircleSprite, new Color(1f, 0.32f, 0.04f, 0.82f));

        freezeRoot = CreateRoot("SpellFreezeVfx");
        iceShards = CreateSprites(freezeRoot.transform, "IceShard", 6, RuntimeVfxFactory.SquareSprite, new Color(0.35f, 0.95f, 1f, 0.82f));

        poisonRoot = CreateRoot("SpellPoisonVfx");
        poisonDrops = CreateSprites(poisonRoot.transform, "PoisonDrop", 5, RuntimeVfxFactory.CircleSprite, new Color(0.35f, 1f, 0.08f, 0.78f));

        wetRoot = CreateRoot("SpellWetVfx");
        waterDrops = CreateSprites(wetRoot.transform, "WaterDrop", 5, RuntimeVfxFactory.CircleSprite, new Color(0.15f, 0.72f, 1f, 0.75f));

        shockRoot = CreateRoot("SpellShockVfx");
        shockLines = new LineRenderer[5];
        for (int i = 0; i < shockLines.Length; i++)
        {
            shockLines[i] = RuntimeVfxFactory.AddLine(
                shockRoot.transform,
                "ShockLine",
                new Color(1f, 0.95f, 0.08f, 0.95f),
                0.04f,
                RuntimeVfxFactory.ProjectileTopOrder,
                5
            );
        }

        barrierRoot = CreateRoot("SpellBarrierVfx");
        barrierFill = RuntimeVfxFactory.AddSprite(barrierRoot.transform, "BarrierFill", RuntimeVfxFactory.CircleSprite, new Color(0.75f, 1f, 1f, 0.18f), ProjectileVfxOrder + 1);
        barrierRing = RuntimeVfxFactory.AddSprite(barrierRoot.transform, "BarrierRing", RuntimeVfxFactory.RingSprite, new Color(0.8f, 1f, 1f, 0.7f), ProjectileVfxOrder + 2);

        cureRoot = CreateRoot("SpellCureVfx");
        cureVertical = RuntimeVfxFactory.AddSprite(cureRoot.transform, "CureVertical", RuntimeVfxFactory.SquareSprite, new Color(0.75f, 1f, 0.95f, 0.9f), ProjectileVfxOrder + 2);
        cureHorizontal = RuntimeVfxFactory.AddSprite(cureRoot.transform, "CureHorizontal", RuntimeVfxFactory.SquareSprite, new Color(0.75f, 1f, 0.95f, 0.9f), ProjectileVfxOrder + 2);

        ironbodyRoot = CreateRoot("SpellIronbodyVfx");
        ironFill = RuntimeVfxFactory.AddSprite(ironbodyRoot.transform, "IronFill", RuntimeVfxFactory.CircleSprite, new Color(0.65f, 0.68f, 0.7f, 0.22f), ProjectileVfxOrder + 1);
        ironRing = RuntimeVfxFactory.AddSprite(ironbodyRoot.transform, "IronRing", RuntimeVfxFactory.RingSprite, new Color(0.75f, 0.78f, 0.8f, 0.78f), ProjectileVfxOrder + 2);

        smogRoot = CreateRoot("SpellSmogVfx");
        smokePuffs = CreateSprites(smogRoot.transform, "SmokePuff", 6, RuntimeVfxFactory.CircleSprite, new Color(0.08f, 0.08f, 0.08f, 0.62f));

        magnetRoot = CreateRoot("SpellMagnetVfx");
        magnetRings = CreateSprites(magnetRoot.transform, "MagnetRing", 3, RuntimeVfxFactory.RingSprite, new Color(1f, 0.2f, 0.55f, 0.58f));

        shiftRoot = CreateRoot("SpellShiftVfx");
        windLines = new LineRenderer[5];
        for (int i = 0; i < windLines.Length; i++)
        {
            windLines[i] = RuntimeVfxFactory.AddLine(
                shiftRoot.transform,
                "WindLine",
                new Color(0.85f, 0.95f, 1f, 0.85f),
                0.035f,
                ProjectileVfxOrder + 2,
                2
            );
        }

        healRoot = CreateRoot("SpellHealVfx");
        healVerticals = new SpriteRenderer[4];
        healHorizontals = new SpriteRenderer[4];
        for (int i = 0; i < healVerticals.Length; i++)
        {
            healVerticals[i] = RuntimeVfxFactory.AddSprite(healRoot.transform, "HealVertical", RuntimeVfxFactory.SquareSprite, new Color(0.35f, 1f, 0.3f, 0.85f), ProjectileVfxOrder + 2);
            healHorizontals[i] = RuntimeVfxFactory.AddSprite(healRoot.transform, "HealHorizontal", RuntimeVfxFactory.SquareSprite, new Color(0.35f, 1f, 0.3f, 0.85f), ProjectileVfxOrder + 2);
        }

        explodeRoot = CreateRoot("SpellExplodeVfx");
        explodeFill = RuntimeVfxFactory.AddSprite(explodeRoot.transform, "ExplodeFill", RuntimeVfxFactory.CircleSprite, new Color(1f, 0.25f, 0.02f, 0.26f), ProjectileVfxOrder + 1);
        explodeRing = RuntimeVfxFactory.AddSprite(explodeRoot.transform, "ExplodeRing", RuntimeVfxFactory.RingSprite, new Color(1f, 0.5f, 0.04f, 0.9f), ProjectileVfxOrder + 2);

        HideAll();
    }

    private GameObject CreateRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        return root;
    }

    private SpriteRenderer[] CreateSprites(
        Transform parent,
        string name,
        int count,
        Sprite sprite,
        Color color
    )
    {
        SpriteRenderer[] renderers = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            renderers[i] = RuntimeVfxFactory.AddSprite(parent, name, sprite, color, ProjectileVfxOrder + 1);
        }

        return renderers;
    }

    private void HideAll()
    {
        SetActive(burnRoot, false);
        SetActive(freezeRoot, false);
        SetActive(poisonRoot, false);
        SetActive(wetRoot, false);
        SetActive(shockRoot, false);
        SetActive(barrierRoot, false);
        SetActive(cureRoot, false);
        SetActive(ironbodyRoot, false);
        SetActive(smogRoot, false);
        SetActive(magnetRoot, false);
        SetActive(shiftRoot, false);
        SetActive(healRoot, false);
        SetActive(explodeRoot, false);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private void AnimateBurn()
    {
        if (!burnRoot.activeSelf)
        {
            return;
        }

        Vector2 backward = -direction;
        Vector2 side = new Vector2(-direction.y, direction.x);
        for (int i = 0; i < flames.Length; i++)
        {
            float phase = time * 7.5f + i * 1.25f;
            Vector2 offset = backward * (0.18f + i * 0.08f) + side * Mathf.Sin(phase) * 0.18f;
            flames[i].transform.localPosition = offset;
            flames[i].transform.localScale = new Vector3(0.26f, 0.45f + Mathf.Sin(phase) * 0.06f, 1f);
        }
    }

    private void AnimateFreeze()
    {
        if (!freezeRoot.activeSelf)
        {
            return;
        }

        for (int i = 0; i < iceShards.Length; i++)
        {
            float angle = time * 100f + i * (360f / iceShards.Length);
            Vector2 offset = AngleToVector(angle) * 0.42f;
            iceShards[i].transform.localPosition = offset;
            iceShards[i].transform.localRotation = Quaternion.Euler(0f, 0f, angle + 45f);
            iceShards[i].transform.localScale = new Vector3(0.14f, 0.42f, 1f);
        }
    }

    private void AnimatePoison()
    {
        AnimateDrops(poisonRoot, poisonDrops, -direction, new Vector3(0.18f, 0.28f, 1f));
    }

    private void AnimateWet()
    {
        AnimateDrops(wetRoot, waterDrops, -direction, new Vector3(0.16f, 0.3f, 1f));
    }

    private void AnimateDrops(GameObject root, SpriteRenderer[] drops, Vector2 backward, Vector3 scale)
    {
        if (!root.activeSelf)
        {
            return;
        }

        Vector2 side = new Vector2(-backward.y, backward.x);
        for (int i = 0; i < drops.Length; i++)
        {
            float phase = Mathf.Repeat(time * 2.2f + i * 0.22f, 1f);
            drops[i].transform.localPosition = backward * (phase * 0.75f) + side * Mathf.Sin(phase * Mathf.PI * 2f + i) * 0.18f;
            drops[i].transform.localScale = scale * (1f - phase * 0.25f);
        }
    }

    private void AnimateShock()
    {
        if (!shockRoot.activeSelf)
        {
            return;
        }

        Vector2 side = new Vector2(-direction.y, direction.x);
        for (int lineIndex = 0; lineIndex < shockLines.Length; lineIndex++)
        {
            LineRenderer line = shockLines[lineIndex];
            float lineOffset = (lineIndex - (shockLines.Length - 1) * 0.5f) * 0.13f;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector2 point = Vector2.Lerp(-direction * 0.52f, direction * 0.52f, t);
                point += side * (lineOffset + Mathf.Sin(time * 52f + i * 2.1f + lineIndex) * 0.11f);
                line.SetPosition(i, point);
            }
        }
    }

    private void AnimateBarrier()
    {
        if (!barrierRoot.activeSelf)
        {
            return;
        }

        float scale = 1.05f + Mathf.Sin(time * 5f) * 0.04f;
        barrierFill.transform.localScale = Vector3.one * scale;
        barrierRing.transform.localScale = Vector3.one * (scale + 0.08f);
    }

    private void AnimateCure()
    {
        if (!cureRoot.activeSelf)
        {
            return;
        }

        cureRoot.transform.localRotation = Quaternion.Euler(0f, 0f, time * 120f);
        cureVertical.transform.localScale = new Vector3(0.12f, 0.75f, 1f);
        cureHorizontal.transform.localScale = new Vector3(0.52f, 0.12f, 1f);
    }

    private void AnimateIronbody()
    {
        if (!ironbodyRoot.activeSelf)
        {
            return;
        }

        float scale = 1f + Mathf.Sin(time * 6f) * 0.03f;
        ironFill.transform.localScale = Vector3.one * scale;
        ironRing.transform.localScale = Vector3.one * (scale + 0.08f);
    }

    private void AnimateSmog()
    {
        if (!smogRoot.activeSelf)
        {
            return;
        }

        for (int i = 0; i < smokePuffs.Length; i++)
        {
            float angle = time * 28f + i * (360f / smokePuffs.Length);
            Vector2 offset = AngleToVector(angle) * (0.2f + i * 0.045f);
            smokePuffs[i].transform.localPosition = offset;
            smokePuffs[i].transform.localScale = Vector3.one * (0.34f + Mathf.Sin(time * 2.8f + i) * 0.05f);
        }
    }

    private void AnimateMagnet()
    {
        if (!magnetRoot.activeSelf)
        {
            return;
        }

        for (int i = 0; i < magnetRings.Length; i++)
        {
            float scale = 0.45f + Mathf.Repeat(time * 0.75f + i * 0.33f, 1f) * 0.9f;
            magnetRings[i].transform.localScale = Vector3.one * scale;
        }
    }

    private void AnimateShift()
    {
        if (!shiftRoot.activeSelf)
        {
            return;
        }

        Vector2 backward = -direction;
        Vector2 side = new Vector2(-direction.y, direction.x);
        for (int i = 0; i < windLines.Length; i++)
        {
            float offset = (i - (windLines.Length - 1) * 0.5f) * 0.16f;
            Vector2 start = backward * 0.75f + side * offset;
            Vector2 end = backward * 0.05f + side * (offset + Mathf.Sin(time * 9f + i) * 0.08f);
            windLines[i].SetPosition(0, start);
            windLines[i].SetPosition(1, end);
        }
    }

    private void AnimateHeal()
    {
        if (!healRoot.activeSelf)
        {
            return;
        }

        for (int i = 0; i < healVerticals.Length; i++)
        {
            float phase = Mathf.Repeat(time * 0.8f + i * 0.25f, 1f);
            float angle = i * 90f + time * 45f;
            Vector2 offset = AngleToVector(angle) * (0.18f + phase * 0.28f);
            SpriteRenderer vertical = healVerticals[i];
            SpriteRenderer horizontal = healHorizontals[i];
            vertical.transform.localPosition = offset;
            horizontal.transform.localPosition = offset;
            vertical.transform.localScale = new Vector3(0.06f, 0.3f, 1f) * (1f - phase * 0.25f);
            horizontal.transform.localScale = new Vector3(0.22f, 0.06f, 1f) * (1f - phase * 0.25f);
        }
    }

    private void AnimateExplode()
    {
        if (!explodeRoot.activeSelf)
        {
            return;
        }

        float pulse = Mathf.Repeat(time * 1.8f, 1f);
        explodeFill.transform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.05f, pulse);
        explodeRing.transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.35f, pulse);
    }

    private static Vector2 AngleToVector(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }
}
