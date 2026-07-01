using Fusion;
using UnityEngine;

public class Fireball : NetworkBehaviour
{
    [SerializeField]
    private float defaultSpeed = 10f;

    [SerializeField]
    private float defaultLifetime = 2f;

    [Networked]
    private TickTimer LifeTimer { get; set; }

    [Networked]
    private PlayerRef Owner { get; set; }

    [Networked]
    private int Damage { get; set; }

    [Networked]
    private int Heal { get; set; }

    [Networked]
    private int Pierce { get; set; }

    [Networked]
    private int Bounce { get; set; }

    [Networked]
    private float Speed { get; set; }

    [Networked]
    private float Lifetime { get; set; }

    [Networked]
    private Vector2 Direction { get; set; }

    [Networked]
    private int VfxMask { get; set; }

    private SpellCastData spell;
    private SpellSegmentData segment;
    private PlayerFireballCaster caster;
    private Status casterStatus;
    private int segmentIndex;
    private bool initializedWithSpell;
    private SpellProjectileVfxController projectileVfx;
    private int configuredVfxMask = int.MinValue;
    private Vector2 configuredVfxDirection;
    private Collider2D projectileCollider;
    private readonly Collider2D[] initialOverlapBuffer = new Collider2D[16];
    private static Sprite fallbackProjectileSprite;

    private void Awake()
    {
        projectileCollider = GetComponent<Collider2D>();
    }

    public void Init(
        Vector2 direction,
        PlayerRef owner,
        int damage,
        int heal,
        int pierce,
        float speed,
        float lifetime
    )
    {
        Direction = direction.normalized;
        Owner = owner;
        Damage = Mathf.Max(0, damage);
        Heal = Mathf.Max(0, heal);
        Pierce = Mathf.Max(0, pierce);
        Bounce = 0;
        Speed = speed > 0f ? speed : defaultSpeed;
        Lifetime = lifetime > 0f ? lifetime : defaultLifetime;
        VfxMask = 0;

        if (Runner != null)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, Lifetime);
        }
    }

    public void InitSpell(
        SpellCastData spellData,
        int activeSegmentIndex,
        Vector2 direction,
        PlayerRef owner,
        PlayerFireballCaster ownerCaster,
        Status ownerStatus
    )
    {
        spell = spellData;
        segmentIndex = activeSegmentIndex;
        segment = spellData.Segments[activeSegmentIndex];
        caster = ownerCaster;
        casterStatus = ownerStatus;
        initializedWithSpell = true;

        Direction = segment.Reverse > 0 ? -direction.normalized : direction.normalized;
        Owner = owner;
        Damage = segment.Damage;
        Heal = segment.Heal;
        Pierce = segment.CreateImpact().Pierce;
        Bounce = segment.BounceCount;
        Speed = segment.EffectiveSpeed;
        Lifetime = segment.EffectiveLifetime;
        VfxMask = (int)SpellVfxMaskUtility.FromSegment(segment);

        if (segment.Active == SpellActiveId.Laser)
        {
            transform.localScale = new Vector3(0.55f, 4.4f, 1f);
            EnsureProjectileVisual(new Color(0.35f, 0.85f, 1f, 0.9f), true);
        }
        else if (segment.Active == SpellActiveId.Linear)
        {
            EnsureProjectileVisual(new Color(1f, 0.4f, 0.15f, 0.9f), false);
        }

        if (Runner != null)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, Lifetime);
        }

        RefreshProjectileVfx();
        ProcessInitialOverlaps();
    }

    public override void Spawned()
    {
        float timerLifetime = Lifetime > 0f ? Lifetime : defaultLifetime;
        LifeTimer = TickTimer.CreateFromSeconds(Runner, timerLifetime);
        RefreshProjectileVfx();
    }

    public override void FixedUpdateNetwork()
    {
        transform.position += (Vector3)(Direction * Speed * Runner.DeltaTime);
        RefreshProjectileVfx();

        if (LifeTimer.Expired(Runner))
        {
            CompleteAt(transform.position);
        }
    }

    private void RefreshProjectileVfx()
    {
        if (VfxMask == 0)
        {
            return;
        }

        if (projectileVfx == null)
        {
            projectileVfx = GetComponent<SpellProjectileVfxController>();
            if (projectileVfx == null)
            {
                projectileVfx = gameObject.AddComponent<SpellProjectileVfxController>();
            }
        }

        if (
            configuredVfxMask == VfxMask
            && Vector2.Dot(configuredVfxDirection, Direction) > 0.999f
        )
        {
            return;
        }

        configuredVfxMask = VfxMask;
        configuredVfxDirection = Direction.sqrMagnitude > 0.0001f ? Direction.normalized : Vector2.right;
        projectileVfx.Configure((SpellVfxMask)VfxMask, Direction);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsStateAuthorityReady())
        {
            return;
        }

        HandleTriggerHit(other);
    }

    private void HandleTriggerHit(Collider2D other)
    {
        if (!IsStateAuthorityReady() || other == null)
        {
            return;
        }

        if (initializedWithSpell && !IsInSpellTargetMask(other))
        {
            return;
        }

        NetworkObject hitObject = other.GetComponentInParent<NetworkObject>();
        if (hitObject != null && hitObject.InputAuthority == Owner)
        {
            return;
        }

        if (initializedWithSpell)
        {
            Status targetStatus = other.GetComponentInParent<Status>();
            DestructibleStructure targetStructure =
                targetStatus == null ? other.GetComponentInParent<DestructibleStructure>() : null;
            WolfPet targetWolf =
                targetStatus == null && targetStructure == null
                    ? other.GetComponentInParent<WolfPet>()
                    : null;
            bool hitStatus = targetStatus != null;
            if (hitStatus)
            {
                SpellImpact impact = segment.CreateImpact();
                impact.Pierce = Pierce;
                targetStatus.ApplySpellImpact(
                    ref impact,
                    casterStatus,
                    caster != null ? caster.TouchRadius : 1f,
                    caster != null ? caster.SpellTargetMask : ~0
                );
                Pierce = impact.Pierce;

                if (caster != null)
                {
                    caster.ApplyProjectileHitEffects(targetStatus, segment);
                }

                if (targetStatus.HasIronbody)
                {
                    Pierce = 0;
                    Direction = -Direction;
                    transform.position += (Vector3)(Direction * 0.2f);
                    return;
                }
            }
            else if (targetStructure != null)
            {
                SpellImpact impact = segment.CreateImpact();
                impact.Pierce = Pierce;
                targetStructure.ApplySpellImpact(
                    ref impact,
                    casterStatus,
                    caster != null ? caster.TouchRadius : 1f,
                    caster != null ? caster.SpellTargetMask : ~0
                );
                Pierce = impact.Pierce;
            }
            else if (targetWolf != null)
            {
                SpellImpact impact = segment.CreateImpact();
                impact.Pierce = Pierce;
                targetWolf.ApplySpellImpact(ref impact, casterStatus);
                Pierce = impact.Pierce;
            }

            if (Pierce > 0)
            {
                Pierce--;
                return;
            }

            if (Bounce > 0)
            {
                Bounce--;
                Direction = GetPerpendicularBounceDirection(other);
                transform.position += (Vector3)(Direction * 0.2f);
                return;
            }

            CompleteAt(transform.position);
            return;
        }

        Status legacyTargetStatus = other.GetComponentInParent<Status>();
        ApplyLegacySpellEffect(legacyTargetStatus);

        if (Pierce > 0)
        {
            Pierce--;
            return;
        }

        Runner.Despawn(Object);
    }

    private bool IsStateAuthorityReady()
    {
        return Object != null && Object.IsValid && Object.HasStateAuthority;
    }

    private void ProcessInitialOverlaps()
    {
        if (!IsStateAuthorityReady())
        {
            return;
        }

        if (projectileCollider == null)
        {
            projectileCollider = GetComponent<Collider2D>();
        }

        if (projectileCollider == null)
        {
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.SetLayerMask(caster != null ? caster.SpellTargetMask : ~0);

        Physics2D.SyncTransforms();
        int hitCount = projectileCollider.OverlapCollider(filter, initialOverlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            if (!IsStateAuthorityReady())
            {
                return;
            }

            Collider2D hit = initialOverlapBuffer[i];
            if (hit == null || hit == projectileCollider)
            {
                continue;
            }

            HandleTriggerHit(hit);
        }
    }

    private bool IsInSpellTargetMask(Collider2D other)
    {
        if (other == null || caster == null)
        {
            return true;
        }

        int layerMask = 1 << other.gameObject.layer;
        return (caster.SpellTargetMask.value & layerMask) != 0;
    }

    private Vector2 GetPerpendicularBounceDirection(Collider2D other)
    {
        Vector2 currentDirection =
            Direction.sqrMagnitude > 0.0001f ? Direction.normalized : Vector2.right;
        Vector2 clockwise = new Vector2(currentDirection.y, -currentDirection.x);
        Vector2 counterClockwise = new Vector2(-currentDirection.y, currentDirection.x);
        Vector2 awayFromHit = GetAwayFromHitDirection(other);

        if (awayFromHit.sqrMagnitude <= 0.0001f)
        {
            return clockwise;
        }

        return Vector2.Dot(clockwise, awayFromHit) >= Vector2.Dot(counterClockwise, awayFromHit)
            ? clockwise
            : counterClockwise;
    }

    private Vector2 GetAwayFromHitDirection(Collider2D other)
    {
        if (other == null)
        {
            return Vector2.zero;
        }

        Vector2 position = transform.position;
        Vector2 closestPoint = other.ClosestPoint(position);
        Vector2 awayFromHit = position - closestPoint;
        if (awayFromHit.sqrMagnitude > 0.0001f)
        {
            return awayFromHit.normalized;
        }

        Vector2 awayFromCenter = position - (Vector2)other.bounds.center;
        return awayFromCenter.sqrMagnitude > 0.0001f ? awayFromCenter.normalized : Vector2.zero;
    }

    private void CompleteAt(Vector3 position)
    {
        if (initializedWithSpell && caster != null)
        {
            caster.SummonWolvesAt(position, segment);
            caster.ContinueSpell(spell, segmentIndex + 1, position, Direction);
        }

        if (Object != null && Runner != null)
        {
            Runner.Despawn(Object);
        }
    }

    private void ApplyLegacySpellEffect(Status targetStatus)
    {
        if (targetStatus == null)
        {
            return;
        }

        if (Damage > 0)
        {
            targetStatus.RPC_TakeDamage(Damage);
        }

        if (Heal > 0)
        {
            targetStatus.RPC_Heal(Heal);
        }
    }

    private void EnsureProjectileVisual(Color fallbackColor, bool forceSquareSprite)
    {
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetFallbackProjectileSprite();
            renderer.color = fallbackColor;
            renderer.sortingOrder = 950;
            return;
        }

        if (forceSquareSprite || renderer.sprite == null)
        {
            renderer.sprite = GetFallbackProjectileSprite();
        }

        if (forceSquareSprite || renderer.color.a <= 0.01f)
        {
            renderer.color = fallbackColor;
        }
    }

    private static Sprite GetFallbackProjectileSprite()
    {
        if (fallbackProjectileSprite != null)
        {
            return fallbackProjectileSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallbackProjectileSprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        return fallbackProjectileSprite;
    }
}
