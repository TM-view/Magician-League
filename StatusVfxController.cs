using UnityEngine;

public class StatusVfxController : MonoBehaviour
{
    private const int UnderfootOrder = RuntimeVfxFactory.UnderfootOrder;
    private const int BodyOrder = RuntimeVfxFactory.BodyOrder;
    private const int HeadOrder = RuntimeVfxFactory.HeadOrder;

    private Status status;
    private Vector3 lastPosition;
    private float time;
    private float shockPulseTimer;

    private GameObject burningRoot;
    private SpriteRenderer[] flames;
    private GameObject freezingRoot;
    private SpriteRenderer iceShell;
    private SpriteRenderer[] iceShards;
    private GameObject poisoningRoot;
    private SpriteRenderer[] poisonBubbles;
    private GameObject wettingRoot;
    private SpriteRenderer wetPuddle;
    private GameObject shockingRoot;
    private LineRenderer[] shockLines;
    private GameObject stunRoot;
    private SpriteRenderer[] stunStars;
    private GameObject ironbodyRoot;
    private SpriteRenderer ironbodyShell;
    private GameObject barrierRoot;
    private SpriteRenderer barrierRing;

    public void Initialize(Status ownerStatus)
    {
        status = ownerStatus;
        lastPosition = transform.position;
        BuildIfNeeded();
    }

    private void Awake()
    {
        if (status == null)
        {
            status = GetComponent<Status>();
        }

        BuildIfNeeded();
    }

    private void LateUpdate()
    {
        if (status == null)
        {
            return;
        }

        BuildIfNeeded();
        time += Time.deltaTime;

        bool moved = ((Vector2)(transform.position - lastPosition)).sqrMagnitude > 0.00008f;
        lastPosition = transform.position;
        if (moved)
        {
            shockPulseTimer = 0.16f;
        }
        else
        {
            shockPulseTimer = Mathf.Max(0f, shockPulseTimer - Time.deltaTime);
        }

        SetActive(burningRoot, status.IsBurning);
        SetActive(freezingRoot, status.IsFreezing);
        SetActive(poisoningRoot, status.IsPoisoning);
        SetActive(wettingRoot, status.IsWetting);
        SetActive(shockingRoot, status.IsShocking);
        SetActive(stunRoot, status.IsStunned);
        SetActive(ironbodyRoot, status.HasIronbody);
        SetActive(barrierRoot, status.CurrentBarrierStacks > 0);

        AnimateBurning();
        AnimateFreezing();
        AnimatePoisoning();
        AnimateWetting();
        AnimateShocking();
        AnimateStun();
        AnimateIronbody();
        AnimateBarrier();
    }

    private void BuildIfNeeded()
    {
        if (burningRoot != null)
        {
            return;
        }

        burningRoot = CreateRoot("BurningVfx");
        flames = new SpriteRenderer[8];
        for (int i = 0; i < flames.Length; i++)
        {
            Color color =
                i % 3 == 0
                    ? new Color(1f, 0.12f, 0.02f, 0.82f)
                    : i % 3 == 1
                        ? new Color(1f, 0.44f, 0.03f, 0.78f)
                        : new Color(1f, 0.92f, 0.08f, 0.72f);
            flames[i] = RuntimeVfxFactory.AddSprite(
                burningRoot.transform,
                "Flame",
                RuntimeVfxFactory.CircleSprite,
                color,
                BodyOrder
            );
        }

        freezingRoot = CreateRoot("FreezingVfx");
        iceShell = RuntimeVfxFactory.AddSprite(
            freezingRoot.transform,
            "IceShell",
            RuntimeVfxFactory.CircleSprite,
            new Color(0.5f, 0.95f, 1f, 0.32f),
            BodyOrder + 1
        );
        iceShards = new SpriteRenderer[8];
        for (int i = 0; i < iceShards.Length; i++)
        {
            iceShards[i] = RuntimeVfxFactory.AddSprite(
                freezingRoot.transform,
                "IceShard",
                RuntimeVfxFactory.SquareSprite,
                new Color(0.2f, 0.85f, 1f, 0.72f),
                BodyOrder + 2
            );
        }

        poisoningRoot = CreateRoot("PoisoningVfx");
        poisonBubbles = new SpriteRenderer[6];
        for (int i = 0; i < poisonBubbles.Length; i++)
        {
            poisonBubbles[i] = RuntimeVfxFactory.AddSprite(
                poisoningRoot.transform,
                "PoisonBubble",
                RuntimeVfxFactory.CircleSprite,
                new Color(0.35f, 1f, 0.1f, 0.68f),
                BodyOrder + 2
            );
        }

        wettingRoot = CreateRoot("WettingVfx");
        wetPuddle = RuntimeVfxFactory.AddSprite(
            wettingRoot.transform,
            "WetPuddle",
            RuntimeVfxFactory.CircleSprite,
            new Color(0.1f, 0.62f, 1f, 0.55f),
            UnderfootOrder
        );

        shockingRoot = CreateRoot("ShockingVfx");
        shockLines = new LineRenderer[4];
        for (int i = 0; i < shockLines.Length; i++)
        {
            shockLines[i] = RuntimeVfxFactory.AddLine(
                shockingRoot.transform,
                "ShockArc",
                new Color(1f, 0.96f, 0.08f, 0.9f),
                0.045f,
                BodyOrder + 5,
                4
            );
        }

        stunRoot = CreateRoot("StunVfx");
        stunStars = new SpriteRenderer[3];
        for (int i = 0; i < stunStars.Length; i++)
        {
            stunStars[i] = RuntimeVfxFactory.AddSprite(
                stunRoot.transform,
                "StunDot",
                RuntimeVfxFactory.CircleSprite,
                new Color(1f, 0.9f, 0.15f, 0.85f),
                HeadOrder
            );
        }

        ironbodyRoot = CreateRoot("IronbodyVfx");
        ironbodyShell = RuntimeVfxFactory.AddSprite(
            ironbodyRoot.transform,
            "IronbodyShell",
            RuntimeVfxFactory.RingSprite,
            new Color(0.7f, 0.75f, 0.78f, 0.65f),
            BodyOrder + 3
        );

        barrierRoot = CreateRoot("BarrierVfx");
        barrierRing = RuntimeVfxFactory.AddSprite(
            barrierRoot.transform,
            "BarrierRing",
            RuntimeVfxFactory.RingSprite,
            new Color(0.85f, 1f, 1f, 0.55f),
            BodyOrder + 4
        );

        HideAll();
    }

    private GameObject CreateRoot(string rootName)
    {
        GameObject root = new GameObject(rootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        return root;
    }

    private void HideAll()
    {
        SetActive(burningRoot, false);
        SetActive(freezingRoot, false);
        SetActive(poisoningRoot, false);
        SetActive(wettingRoot, false);
        SetActive(shockingRoot, false);
        SetActive(stunRoot, false);
        SetActive(ironbodyRoot, false);
        SetActive(barrierRoot, false);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private void AnimateBurning()
    {
        if (!burningRoot.activeSelf)
        {
            return;
        }

        for (int i = 0; i < flames.Length; i++)
        {
            float phase = time * 6.2f + i * 1.35f;
            float side = (i - (flames.Length - 1) * 0.5f) / flames.Length;
            float x = side * 0.9f + Mathf.Sin(phase) * 0.08f;
            float y = -0.35f + Mathf.Repeat(time * 0.75f + i * 0.12f, 1.15f);
            flames[i].transform.localPosition = new Vector3(x, y, 0f);
            flames[i].transform.localScale = new Vector3(
                0.28f + Mathf.Sin(phase) * 0.04f,
                0.58f + Mathf.Cos(phase * 0.8f) * 0.08f,
                1f
            );
        }
    }

    private void AnimateFreezing()
    {
        if (!freezingRoot.activeSelf)
        {
            return;
        }

        iceShell.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        iceShell.transform.localScale = new Vector3(1.3f, 1.65f, 1f);

        for (int i = 0; i < iceShards.Length; i++)
        {
            float angle = (360f / iceShards.Length) * i + time * 10f;
            Vector2 offset = AngleToVector(angle) * new Vector2(0.58f, 0.75f);
            iceShards[i].transform.localPosition = new Vector3(offset.x, offset.y + 0.08f, 0f);
            iceShards[i].transform.localRotation = Quaternion.Euler(0f, 0f, angle + 45f);
            iceShards[i].transform.localScale = new Vector3(0.14f, 0.42f, 1f);
        }
    }

    private void AnimatePoisoning()
    {
        if (!poisoningRoot.activeSelf)
        {
            return;
        }

        for (int i = 0; i < poisonBubbles.Length; i++)
        {
            float rise = Mathf.Repeat(time * 0.28f + i * 0.17f, 1f);
            float x = Mathf.Sin(time * 1.7f + i) * 0.38f;
            poisonBubbles[i].transform.localPosition = new Vector3(x, -0.42f + rise * 1.05f, 0f);
            float scale = Mathf.Lerp(0.2f, 0.38f, Mathf.Sin(rise * Mathf.PI));
            poisonBubbles[i].transform.localScale = Vector3.one * scale;
        }
    }

    private void AnimateWetting()
    {
        if (!wettingRoot.activeSelf)
        {
            return;
        }

        wetPuddle.transform.localPosition = new Vector3(0f, -0.62f, 0f);
        wetPuddle.transform.localScale = new Vector3(1.18f, 0.18f + Mathf.Sin(time * 3f) * 0.015f, 1f);
    }

    private void AnimateShocking()
    {
        if (!shockingRoot.activeSelf)
        {
            return;
        }

        float intensity = Mathf.Lerp(0.28f, 1f, Mathf.Clamp01(shockPulseTimer / 0.16f));
        Color color = new Color(1f, 0.95f, 0.05f, 0.25f + intensity * 0.65f);
        for (int i = 0; i < shockLines.Length; i++)
        {
            shockLines[i].startColor = color;
            shockLines[i].endColor = color;
            float angle = time * 460f + i * 90f;
            Vector2 start = AngleToVector(angle) * 0.2f;
            Vector2 end = AngleToVector(angle + 70f) * (0.45f + intensity * 0.2f);
            shockLines[i].SetPosition(0, start);
            shockLines[i].SetPosition(1, Vector2.Lerp(start, end, 0.35f) + Jitter(i, 0.09f * intensity));
            shockLines[i].SetPosition(2, Vector2.Lerp(start, end, 0.7f) + Jitter(i + 3, 0.09f * intensity));
            shockLines[i].SetPosition(3, end);
        }
    }

    private void AnimateStun()
    {
        if (!stunRoot.activeSelf)
        {
            return;
        }

        for (int i = 0; i < stunStars.Length; i++)
        {
            float angle = time * 160f + i * 120f;
            Vector2 offset = AngleToVector(angle) * new Vector2(0.35f, 0.12f);
            stunStars[i].transform.localPosition = new Vector3(offset.x, 0.88f + offset.y, 0f);
            stunStars[i].transform.localScale = Vector3.one * 0.12f;
        }
    }

    private void AnimateIronbody()
    {
        if (!ironbodyRoot.activeSelf)
        {
            return;
        }

        ironbodyShell.transform.localScale = Vector3.one * (1.2f + Mathf.Sin(time * 4f) * 0.03f);
    }

    private void AnimateBarrier()
    {
        if (!barrierRoot.activeSelf)
        {
            return;
        }

        barrierRing.transform.localScale = Vector3.one * (1.35f + Mathf.Sin(time * 5f) * 0.04f);
    }

    private static Vector2 AngleToVector(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private Vector2 Jitter(int salt, float amount)
    {
        return new Vector2(
            Mathf.Sin(time * 41f + salt * 12.989f),
            Mathf.Cos(time * 37f + salt * 78.233f)
        ) * amount;
    }
}
