using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;

public enum PlayerStatId
{
    HP,
    MP,
    CD,
    VAL,
    STR,
    LUK,
    None,
}

public class Status : NetworkBehaviour
{
    private const int MinStatLevel = 0;
    private const int MaxStatLevel = 8;
    private const int BaseMaxHealth = 100;
    private const int HealthPerStatLevel = 20;
    private const int BaseHealthRegen = 1;
    private const int BaseMaxMana = 100;
    private const int ManaPerStatLevel = 20;
    private const int BaseManaRegen = 5;
    private const float DefaultBaseSpeed = 5f;
    private const float SpeedPerStrLevel = 0.25f;
    private const float CooldownReductionPerCdLevel = 0.05f;
    private const float MinCooldownMultiplier = 0.6f;
    private const float ValueBonusPerLevel = 0.1f;
    private const float CollisionDamageBonusPerStrLevel = 0.1f;
    private const float BaseDebuffChance = 0.15f;
    private const float DebuffChancePerLukLevel = 0.005f;

    private const float RegenDelay = 5f;
    private const float RegenTickRate = 1f;
    private const float ManaTickRate = 1f;
    private const float StatusTickRate = 1f;

    private const int BurningDamage = 4;
    private const int PoisoningDamage = 2;
    private const int StrongPoisoningDamage = 4;
    private const int FreezingDamage = 2;
    private const int NormalCollisionDamage = 20;
    private const int FreezingCollisionDamage = 40;
    private const int ShockingMoveDamage = 1;
    private const int BurningWettingDamage = 25;
    private const int FreezingBurningDamage = 20;
    private const int WettingFreezingDamage = 70;
    private const int WettingShockingHitDamage = 16;
    private const int WettingShockingHitCount = 5;

    private const float BurningDuration = 9f;
    private const float BurningSpreadDuration = 5f;
    private const float PoisoningDuration = 15f;
    private const float StrongPoisoningDuration = 30f;
    private const float WettingDuration = 9f;
    private const float ShockingDuration = 6f;
    private const float FreezingDuration = 3f;
    private const float StunDuration = 1f;
    private const float BarrierDuration = 30f;
    private const float IronbodyDuration = 4f;
    private const float WolfDuration = 30f;
    private const float KillCreditDuration = 5f;
    private const int MaxDisplayNameLength = 7;
    private const float WettingFallSpeedThreshold = 7f;
    private const float WettingFallChance = 0.25f;

    [Networked]
    public float speed { get; set; } = 5f;

    [Networked]
    public int Health { get; set; } = 100;

    [Networked]
    public int MaxHealth { get; set; } = 100;

    [Networked]
    public int RegenerationRate { get; set; } = 1;

    [Networked]
    public int Mana { get; set; } = 100;

    [Networked]
    public int MaxMana { get; set; } = 100;

    [Networked]
    public int ManaRegenerationRate { get; set; } = 5;

    [Networked]
    public int Level { get; set; } = 0;

    [Networked]
    public float Experience { get; set; } = 0f;

    [Networked]
    public int Score { get; set; } = 0;

    [Networked]
    public NetworkString<_16> PlayerDisplayName { get; set; }

    [Networked]
    public float NextLevelExperience { get; set; } = 100f;

    [Networked]
    public float ExperienceMultiplierPerLevel { get; set; } = 1.4f;

    [Networked]
    public int SP { get; set; } = 0;

    [Networked]
    public int HPStat { get; set; } = 0;

    [Networked]
    public int MPStat { get; set; } = 0;

    [Networked]
    public int CDStat { get; set; } = 0;

    [Networked]
    public int VALStat { get; set; } = 0;

    [Networked]
    public int STRStat { get; set; } = 0;

    [Networked]
    public int LUKStat { get; set; } = 0;

    [Networked]
    private float BaseSpeed { get; set; }

    [Networked]
    private TickTimer RegenDelayTimer { get; set; }

    [Networked]
    private TickTimer RegenTickTimer { get; set; }

    [Networked]
    private TickTimer ManaRegenTickTimer { get; set; }

    [Networked]
    private TickTimer StatusTickTimer { get; set; }

    [Networked]
    private TickTimer BurningTimer { get; set; }

    [Networked]
    private TickTimer PoisoningTimer { get; set; }

    [Networked]
    private TickTimer WettingTimer { get; set; }

    [Networked]
    private TickTimer ShockingTimer { get; set; }

    [Networked]
    private TickTimer FreezingTimer { get; set; }

    [Networked]
    private TickTimer StunTimer { get; set; }

    [Networked]
    private TickTimer BarrierTimer { get; set; }

    [Networked]
    private TickTimer IronbodyTimer { get; set; }

    [Networked]
    private TickTimer WolfTimer { get; set; }

    [Networked]
    private TickTimer WetFreezeLockTimer { get; set; }

    [Networked]
    private TickTimer LastDamageCreditTimer { get; set; }

    [Networked]
    private int BarrierStacks { get; set; }

    [Networked]
    private NetworkBool StrongPoisoning { get; set; }

    [Networked]
    private NetworkBool BurningSelfOrigin { get; set; }

    [Networked]
    private NetworkBool PoisoningSelfOrigin { get; set; }

    [Networked]
    private NetworkBool FreezingSelfOrigin { get; set; }

    [Networked]
    private NetworkBool ShockingSelfOrigin { get; set; }

    [Networked]
    private NetworkBool WolfMagicDisabled { get; set; }

    [Networked]
    private NetworkBool HasLastDamageDealer { get; set; }

    [Networked]
    private NetworkBool BurningHasOwner { get; set; }

    [Networked]
    private NetworkBool PoisoningHasOwner { get; set; }

    [Networked]
    private NetworkBool WettingHasOwner { get; set; }

    [Networked]
    private NetworkBool ShockingHasOwner { get; set; }

    [Networked]
    private NetworkBool FreezingHasOwner { get; set; }

    [Networked]
    private PlayerRef LastDamageDealer { get; set; }

    [Networked]
    private PlayerRef BurningOwner { get; set; }

    [Networked]
    private PlayerRef PoisoningOwner { get; set; }

    [Networked]
    private PlayerRef WettingOwner { get; set; }

    [Networked]
    private PlayerRef ShockingOwner { get; set; }

    [Networked]
    private PlayerRef FreezingOwner { get; set; }

    [Networked]
    private float WolfChargeDamageMultiplier { get; set; }

    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Image healthBar;

    [SerializeField]
    private TMP_Text healthText;

    [SerializeField]
    private Image manaBar;

    [SerializeField]
    private TMP_Text manaText;

    [SerializeField]
    private Image expBar;

    [SerializeField]
    private TMP_Text expText;

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private PlayerInventory playerInventory;

    [Header("Collision Damage")]
    [SerializeField]
    private PlayerCollisionDamageHitbox normalCollisionDamageHitbox;

    [SerializeField]
    private float normalCollisionDamageCooldown = 0.5f;

    [Header("Damage Flash")]
    [SerializeField]
    private SpriteRenderer[] damageFlashRenderers;

    [SerializeField]
    private Color damageFlashColor = Color.red;

    [SerializeField]
    private float damageFlashDuration = 0.5f;

    [Header("Wolf Self Visual")]
    [SerializeField]
    private SpriteRenderer bodySpriteRenderer;

    [SerializeField]
    private Sprite normalBodySprite;

    [SerializeField]
    private Sprite wolfBodySprite;

    private readonly Queue<SpellDebuffId> debuffOrder = new Queue<SpellDebuffId>();
    private readonly Dictionary<Status, float> normalCollisionDamageTimes =
        new Dictionary<Status, float>();
    private ChangeDetector _changes;
    private Player player;
    private Coroutine damageFlashCoroutine;
    private SpriteRenderer[] cachedDamageFlashRenderers;
    private Color[] cachedDamageFlashColors;
    private Vector3 lastStatusTickPosition;
    private bool deathHandled;
    private Coroutine shutdownCoroutine;

    public bool IsWolfSelfActive => Runner != null && IsTimerActive(WolfTimer);

    public bool CanMove =>
        !IsTimerActive(FreezingTimer) && !IsTimerActive(IronbodyTimer) && !IsTimerActive(StunTimer);

    public bool CanCastSpells =>
        !IsTimerActive(FreezingTimer) && !IsTimerActive(StunTimer) && !WolfMagicDisabled;

    public bool IsWetting => IsTimerActive(WettingTimer);

    public bool IsBurning => IsTimerActive(BurningTimer);

    public bool IsPoisoning => IsTimerActive(PoisoningTimer);

    public bool IsShocking => IsTimerActive(ShockingTimer);

    public bool IsFreezing => IsTimerActive(FreezingTimer);

    public bool IsStunned => IsTimerActive(StunTimer);

    public float SpellCooldownMultiplier =>
        (IsTimerActive(ShockingTimer) ? 1.5f : 1f) * StatCooldownMultiplier;

    public float CollisionDamageMultiplier =>
        1f + EffectiveSTRStat * CollisionDamageBonusPerStrLevel;

    public float DebuffChance => BaseDebuffChance + EffectiveLUKStat * DebuffChancePerLukLevel;

    public int CurrentBarrierStacks => BarrierStacks;

    public bool HasIronbody => IsTimerActive(IronbodyTimer);

    public event Action StatsChanged;

    private float StatCooldownMultiplier =>
        Mathf.Max(MinCooldownMultiplier, 1f - EffectiveCDStat * CooldownReductionPerCdLevel);

    private float ValueMultiplier => 1f + EffectiveVALStat * ValueBonusPerLevel;

    private ToolStat EquipmentStats =>
        playerInventory != null ? playerInventory.GetEquipmentStats() : default;

    private int EffectiveHPStat => HPStat + EquipmentStats.HP;

    private int EffectiveMPStat => MPStat + EquipmentStats.MP;

    private int EffectiveCDStat => CDStat + EquipmentStats.CD;

    private int EffectiveVALStat => VALStat + EquipmentStats.VAL;

    private int EffectiveSTRStat => STRStat + EquipmentStats.STR;

    private int EffectiveLUKStat => LUKStat + EquipmentStats.LUK;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (playerInventory == null)
        {
            playerInventory = GetComponent<PlayerInventory>();
        }

        if (player == null)
        {
            player = GetComponent<Player>();
        }

        if (normalCollisionDamageHitbox == null)
        {
            normalCollisionDamageHitbox = GetComponentInChildren<PlayerCollisionDamageHitbox>(true);
        }

        if (playerInventory != null)
        {
            playerInventory.EquipmentChanged += OnEquipmentChanged;
        }

        if (Object.HasStateAuthority)
        {
            BaseSpeed = speed > 0f ? speed : DefaultBaseSpeed;
            PlayerDisplayName = GetClampedDisplayName(
                Object.HasInputAuthority ? PlayerData.Name : PlayerDisplayName.ToString()
            );
            ApplyStats();
        }

        lastStatusTickPosition = transform.position;
        CacheNormalBodySprite();
        UpdateWolfSelfVisual();

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(Object.HasInputAuthority);
        }

        if (expBar != null)
        {
            expBar.fillAmount = Experience / NextLevelExperience;
        }

        if (expText != null)
        {
            expText.text = $"{Experience:F0} / {NextLevelExperience:F0}";
        }

        if (levelText != null)
        {
            levelText.text = Level.ToString();
        }

        NotifyStatsChanged();
        EnsureStatusVfx();

        if (!Object.HasStateAuthority)
        {
            return;
        }

        ManaRegenTickTimer = TickTimer.CreateFromSeconds(Runner, ManaTickRate);
        StatusTickTimer = TickTimer.CreateFromSeconds(Runner, StatusTickRate);
    }

    private void EnsureStatusVfx()
    {
        StatusVfxController controller = GetComponent<StatusVfxController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<StatusVfxController>();
        }

        controller.Initialize(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (playerInventory != null)
        {
            playerInventory.EquipmentChanged -= OnEquipmentChanged;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        ExpireFinishedTimers();
        UpdateRegen();
        UpdateTimedStatuses();
        UpdateEffectiveSpeed();
    }

    public override void Render()
    {
        UpdateWolfSelfVisual();

        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(Health):
                case nameof(MaxHealth):
                    UpdateHealthUI();
                    break;
                case nameof(Mana):
                case nameof(MaxMana):
                    UpdateManaUI();
                    break;
                case nameof(Experience):
                case nameof(NextLevelExperience):
                    UpdateExperienceUI();
                    break;
                case nameof(Level):
                    UpdateLevelUI();
                    break;
                case nameof(SP):
                case nameof(HPStat):
                case nameof(MPStat):
                case nameof(CDStat):
                case nameof(VALStat):
                case nameof(STRStat):
                case nameof(LUKStat):
                    NotifyStatsChanged();
                    break;
            }
        }
    }

    private void CacheNormalBodySprite()
    {
        if (normalBodySprite == null && bodySpriteRenderer != null)
        {
            normalBodySprite = bodySpriteRenderer.sprite;
        }
    }

    private void UpdateWolfSelfVisual()
    {
        if (bodySpriteRenderer == null)
        {
            return;
        }

        CacheNormalBodySprite();

        bool wolfActive = IsWolfSelfActive;
        Sprite targetSprite = wolfActive ? wolfBodySprite : normalBodySprite;
        if (targetSprite == null || bodySpriteRenderer.sprite == targetSprite)
        {
            return;
        }

        bodySpriteRenderer.sprite = targetSprite;
    }

    public int GetStatLevel(PlayerStatId stat)
    {
        switch (stat)
        {
            case PlayerStatId.HP:
                return HPStat;
            case PlayerStatId.MP:
                return MPStat;
            case PlayerStatId.CD:
                return CDStat;
            case PlayerStatId.VAL:
                return VALStat;
            case PlayerStatId.STR:
                return STRStat;
            case PlayerStatId.LUK:
                return LUKStat;
            default:
                return MinStatLevel;
        }
    }

    public void UpgradeStat(PlayerStatId stat)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_UpgradeStat((int)stat);
            return;
        }

        TryUpgradeStat(stat);
    }

    public void DowngradeStat(PlayerStatId stat)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_DowngradeStat((int)stat);
            return;
        }

        TryDowngradeStat(stat);
    }

    public void AddExperience(float amount)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_AddExperience(amount);
            return;
        }

        GainExperience(amount);
    }

    public void AddScore(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0)
        {
            return;
        }

        if (!Object.HasStateAuthority)
        {
            RPC_AddScore(amount);
            return;
        }

        Score += amount;
    }

    public void RecordDamageDealer(Status attacker)
    {
        if (attacker == null)
        {
            return;
        }

        NetworkObject attackerObject = attacker.Object;
        if (attackerObject == null || !attackerObject.IsValid)
        {
            return;
        }

        RecordDamageDealer(attackerObject.InputAuthority, true);
    }

    public int ApplyOutgoingDamageBonus(int amount)
    {
        return Mathf.CeilToInt(Mathf.Max(0, amount) * ValueMultiplier);
    }

    public int ApplyOutgoingHealBonus(int amount)
    {
        return Mathf.CeilToInt(Mathf.Max(0, amount) * ValueMultiplier);
    }

    public bool TrySpendManaWithTransfer(int amount, bool transferAllowed)
    {
        if (!Object.HasStateAuthority)
        {
            return false;
        }

        amount = Mathf.Max(0, amount);
        if (Mana >= amount)
        {
            Mana -= amount;
            return true;
        }

        if (!transferAllowed)
        {
            return false;
        }

        int missingMana = amount - Mana;
        if (Health - missingMana <= 0)
        {
            return false;
        }

        Mana = 0;
        Health -= missingMana;
        return true;
    }

    public int ApplySpellImpact(
        ref SpellImpact impact,
        Status caster,
        float explosionRadius,
        LayerMask hitMask
    )
    {
        PlayerRef casterRef = GetPlayerRef(caster);
        bool hasCaster = caster != null;
        if (!Object.HasStateAuthority)
        {
            RPC_ApplySpellImpact(
                impact.Damage,
                impact.Heal,
                impact.Cure,
                impact.Barrier,
                impact.Pierce,
                impact.BurnRolls,
                impact.PoisonRolls,
                impact.WetRolls,
                impact.ShockRolls,
                impact.FreezeRolls,
                impact.Explode,
                impact.Leech,
                impact.Slaughter,
                impact.Powerup2,
                casterRef,
                hasCaster
            );
            return 0;
        }

        bool selfOrigin = caster == this;
        bool blockedByBarrier = !impact.IgnoreBarrierForExplosion && ConsumeBarrier(ref impact);
        bool blockedByIronbody = IsTimerActive(IronbodyTimer);
        bool blocksNewNegativeStatuses = blockedByBarrier || blockedByIronbody;
        int incomingHeal =
            caster != null ? caster.ApplyOutgoingHealBonus(impact.Heal) : impact.Heal;
        int incomingDamage =
            caster != null ? caster.ApplyOutgoingDamageBonus(impact.Damage) : impact.Damage;
        int totalDamage = 0;

        if (!blockedByBarrier)
        {
            if (incomingHeal > 0)
            {
                Heal(incomingHeal);
            }

            if (impact.Cure > 0)
            {
                CureDebuffs(impact.Cure);
            }

            if (impact.Barrier > 0)
            {
                AddBarrier(impact.Barrier);
            }

            if (impact.Slaughter > 0 && !selfOrigin && RandomChance(impact.Slaughter))
            {
                totalDamage += ApplyDamage(Health, false, caster);
            }

            if (incomingDamage > 0 && !selfOrigin)
            {
                int damage = blockedByIronbody
                    ? Mathf.CeilToInt(incomingDamage * 0.1f)
                    : incomingDamage;
                totalDamage += ApplyDamageWithWetting(damage, caster);
            }

            if (!blocksNewNegativeStatuses)
            {
                ApplyPowerupDebuffs(impact, selfOrigin, caster);
            }
        }

        if (impact.Explode > 0)
        {
            ApplyExplosion(caster, explosionRadius, hitMask, impact);
        }

        if (caster != null && impact.Leech > 0 && totalDamage > 0)
        {
            int leechAmount = caster.ApplyOutgoingHealBonus(
                Mathf.CeilToInt(totalDamage * 0.15f * impact.Leech)
            );
            caster.Heal(leechAmount);
        }

        return totalDamage;
    }

    public void AddMagnet(float duration, float radius, float force, LayerMask hitMask)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, hitMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Rigidbody2D body = hits[i].attachedRigidbody;
            if (body != null)
            {
                Vector2 direction = ((Vector2)transform.position - body.position).normalized;
                body.AddForce(direction * force, ForceMode2D.Impulse);
                continue;
            }

            Transform hitTransform = hits[i].transform;
            if (hitTransform != transform)
            {
                hitTransform.position = Vector3.MoveTowards(
                    hitTransform.position,
                    transform.position,
                    force * Runner.DeltaTime
                );
            }
        }
    }

    public void ApplyIronbody()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        IronbodyTimer = TickTimer.CreateFromSeconds(Runner, IronbodyDuration);
    }

    public void ApplyWolfSelf()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        WolfTimer = TickTimer.CreateFromSeconds(Runner, WolfDuration);
        WolfMagicDisabled = true;
        WolfChargeDamageMultiplier = 2.5f;
    }

    public void Push(Vector2 force, bool resetVelocityBeforePush = false)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_Push(force, resetVelocityBeforePush);
            return;
        }

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            if (resetVelocityBeforePush)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            body.AddForce(force, ForceMode2D.Impulse);
            return;
        }

        transform.position += (Vector3)force;
    }

    private static void StopBodyVelocity(Rigidbody2D body)
    {
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    public void MoveBy(Vector2 offset)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_MoveBy(offset);
            return;
        }

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position += offset;
            return;
        }

        transform.position += (Vector3)offset;
    }

    public void InflictDebuff(SpellDebuffId debuff, float duration, bool selfOrigin)
    {
        InflictDebuff(debuff, duration, selfOrigin, null);
    }

    public void InflictDebuff(
        SpellDebuffId debuff,
        float duration,
        bool selfOrigin,
        Status attacker
    )
    {
        if (!Object.HasStateAuthority)
        {
            RPC_InflictDebuff((int)debuff, duration, selfOrigin, GetPlayerRef(attacker), attacker != null);
            return;
        }

        ApplyDebuff(debuff, duration, selfOrigin, attacker);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TrySpreadCollisionStatuses(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrySpreadCollisionStatuses(other);
    }

    public void TryApplyNormalCollisionDamageFromHitbox(
        PlayerCollisionDamageHitbox hitbox,
        Collider2D other
    )
    {
        if (
            hitbox == null
            || hitbox != normalCollisionDamageHitbox
            || Object == null
            || !Object.IsValid
            || !Object.HasStateAuthority
            || other == null
        )
        {
            return;
        }

        Status target = other.GetComponentInParent<Status>();
        TryApplyNormalCollisionDamage(target);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage)
    {
        ApplyDamage(damage, false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamageFrom(int damage, PlayerRef attackerRef, NetworkBool hasAttacker)
    {
        ApplyDamage(damage, false, attackerRef, hasAttacker);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Heal(int amount)
    {
        Heal(amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_UseMana(int amount)
    {
        Mana = Mathf.Max(Mana - amount, 0);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UpgradeStat(int stat)
    {
        TryUpgradeStat((PlayerStatId)stat);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DowngradeStat(int stat)
    {
        TryDowngradeStat((PlayerStatId)stat);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_AddExperience(float amount)
    {
        GainExperience(amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_AddScore(int amount)
    {
        Score += Mathf.Max(0, amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Push(Vector2 force, NetworkBool resetVelocityBeforePush)
    {
        Push(force, resetVelocityBeforePush);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_MoveBy(Vector2 offset)
    {
        MoveBy(offset);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InflictDebuff(
        int debuff,
        float duration,
        NetworkBool selfOrigin,
        PlayerRef attackerRef,
        NetworkBool hasAttacker
    )
    {
        ApplyDebuff(
            (SpellDebuffId)debuff,
            duration,
            selfOrigin,
            ResolveStatus(attackerRef, hasAttacker)
        );
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ApplySpellImpact(
        int damage,
        int heal,
        int cure,
        int barrier,
        int pierce,
        int burnRolls,
        int poisonRolls,
        int wetRolls,
        int shockRolls,
        int freezeRolls,
        int explode,
        int leech,
        int slaughter,
        int powerup2,
        PlayerRef casterRef,
        NetworkBool hasCaster
    )
    {
        SpellImpact impact = new SpellImpact
        {
            Damage = damage,
            Heal = heal,
            Cure = cure,
            Barrier = barrier,
            Pierce = pierce,
            BurnRolls = burnRolls,
            PoisonRolls = poisonRolls,
            WetRolls = wetRolls,
            ShockRolls = shockRolls,
            FreezeRolls = freezeRolls,
            Explode = explode,
            Leech = leech,
            Slaughter = slaughter,
            Powerup2 = powerup2,
        };

        ApplySpellImpact(ref impact, ResolveStatus(casterRef, hasCaster), 1f, ~0);
    }

    public void TakeDamage(int damage)
    {
        if (!Object.HasStateAuthority)
            return;
        ApplyDamage(damage, false);
    }

    public void TakeDamageFrom(int damage, Status attacker)
    {
        if (!Object.HasStateAuthority)
        {
            RPC_TakeDamageFrom(damage, GetPlayerRef(attacker), attacker != null);
            return;
        }

        ApplyDamage(damage, false, attacker);
    }

    public void Heal(int amount)
    {
        if (!Object.HasStateAuthority)
            return;
        Health = Mathf.Min(Health + Mathf.Max(0, amount), MaxHealth);
    }

    public void UseMana(int amount)
    {
        if (!Object.HasStateAuthority)
            return;
        Mana = Mathf.Max(Mana - amount, 0);
    }

    public void RegenerateMana(int amount)
    {
        if (!Object.HasStateAuthority)
            return;
        Mana = Mathf.Min(Mana + amount, MaxMana);
    }

    private bool TryUpgradeStat(PlayerStatId stat)
    {
        int currentLevel = GetStatLevel(stat);
        if (currentLevel >= MaxStatLevel)
        {
            return false;
        }

        int nextLevel = currentLevel + 1;
        if (SP < nextLevel)
        {
            return false;
        }

        SP -= nextLevel;
        SetStatLevel(stat, nextLevel);
        ApplyStats();
        NotifyStatsChanged();
        return true;
    }

    private bool TryDowngradeStat(PlayerStatId stat)
    {
        int currentLevel = GetStatLevel(stat);
        if (currentLevel <= MinStatLevel)
        {
            return false;
        }

        SP += currentLevel;
        SetStatLevel(stat, currentLevel - 1);
        ApplyStats();
        NotifyStatsChanged();
        return true;
    }

    private void SetStatLevel(PlayerStatId stat, int level)
    {
        level = Mathf.Clamp(level, MinStatLevel, MaxStatLevel);

        switch (stat)
        {
            case PlayerStatId.HP:
                HPStat = level;
                break;
            case PlayerStatId.MP:
                MPStat = level;
                break;
            case PlayerStatId.CD:
                CDStat = level;
                break;
            case PlayerStatId.VAL:
                VALStat = level;
                break;
            case PlayerStatId.STR:
                STRStat = level;
                break;
            case PlayerStatId.LUK:
                LUKStat = level;
                break;
        }
    }

    private void ApplyStats()
    {
        int previousMaxHealth = MaxHealth;
        int previousMaxMana = MaxMana;

        MaxHealth = BaseMaxHealth + EffectiveHPStat * HealthPerStatLevel;
        RegenerationRate = BaseHealthRegen + EffectiveHPStat;
        MaxMana = BaseMaxMana + EffectiveMPStat * ManaPerStatLevel;
        ManaRegenerationRate = BaseManaRegen + EffectiveMPStat;
        BaseSpeed = DefaultBaseSpeed + EffectiveSTRStat * SpeedPerStrLevel;

        if (MaxHealth > previousMaxHealth)
        {
            Health += MaxHealth - previousMaxHealth;
        }

        if (MaxMana > previousMaxMana)
        {
            Mana += MaxMana - previousMaxMana;
        }

        Health = Mathf.Clamp(Health, 0, MaxHealth);
        Mana = Mathf.Clamp(Mana, 0, MaxMana);
        UpdateEffectiveSpeed();
        UpdateHealthUI();
        UpdateManaUI();
    }

    private void OnEquipmentChanged()
    {
        if (Object != null && Object.IsValid && Object.HasStateAuthority)
        {
            ApplyStats();
        }

        NotifyStatsChanged();
    }

    private void GainExperience(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (amount <= 0f)
        {
            return;
        }

        Experience += amount;
        while (Experience >= NextLevelExperience)
        {
            Experience -= NextLevelExperience;
            Level++;
            SP++;
            NextLevelExperience *= ExperienceMultiplierPerLevel;
        }

        UpdateExperienceUI();
        UpdateLevelUI();
        NotifyStatsChanged();
    }

    private void UpdateRegen()
    {
        bool canRegen = RegenDelayTimer.IsRunning && RegenDelayTimer.Expired(Runner);

        if (canRegen && Health < MaxHealth && RegenTickTimer.ExpiredOrNotRunning(Runner))
        {
            Health = Mathf.Min(Health + RegenerationRate, MaxHealth);
            RegenTickTimer = TickTimer.CreateFromSeconds(Runner, RegenTickRate);
        }

        if (ManaRegenTickTimer.Expired(Runner) && Mana < MaxMana)
        {
            Mana = Mathf.Min(Mana + ManaRegenerationRate, MaxMana);
            ManaRegenTickTimer = TickTimer.CreateFromSeconds(Runner, ManaTickRate);
        }
    }

    private void UpdateTimedStatuses()
    {
        if (!StatusTickTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        StatusTickTimer = TickTimer.CreateFromSeconds(Runner, StatusTickRate);

        ExpireFinishedTimers();

        if (IsTimerActive(BurningTimer) && !BurningSelfOrigin)
        {
            ApplyDamage(BurningDamage, true, BurningOwner, BurningHasOwner);
        }

        if (IsTimerActive(PoisoningTimer) && !PoisoningSelfOrigin)
        {
            ApplyDamage(
                StrongPoisoning ? StrongPoisoningDamage : PoisoningDamage,
                true,
                PoisoningOwner,
                PoisoningHasOwner
            );
        }

        if (IsTimerActive(FreezingTimer) && !FreezingSelfOrigin)
        {
            ApplyDamage(FreezingDamage, true, FreezingOwner, FreezingHasOwner);
        }

        Vector2 statusDelta = transform.position - lastStatusTickPosition;
        bool moved = statusDelta.sqrMagnitude > 0.0001f;
        if (IsTimerActive(ShockingTimer) && moved && !ShockingSelfOrigin)
        {
            ApplyDamage(ShockingMoveDamage, true, ShockingOwner, ShockingHasOwner);
        }

        float movementSpeed = statusDelta.magnitude / StatusTickRate;
        if (
            IsTimerActive(WettingTimer)
            && movementSpeed >= WettingFallSpeedThreshold
            && Random.value <= WettingFallChance
        )
        {
            StunTimer = TickTimer.CreateFromSeconds(Runner, StunDuration);
        }

        lastStatusTickPosition = transform.position;

        if (BarrierStacks > 0 && !IsTimerActive(BarrierTimer))
        {
            BarrierStacks = 0;
        }

        if (!IsTimerActive(WolfTimer))
        {
            WolfMagicDisabled = false;
            WolfChargeDamageMultiplier = 1f;
        }

        if (!IsTimerActive(PoisoningTimer))
        {
            StrongPoisoning = false;
        }
    }

    private bool IsTimerActive(TickTimer timer)
    {
        return timer.IsRunning && !timer.ExpiredOrNotRunning(Runner);
    }

    private string GetClampedDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Guest00";
        }

        displayName = displayName.Trim();
        return displayName.Length <= MaxDisplayNameLength
            ? displayName
            : displayName.Substring(0, MaxDisplayNameLength);
    }

    private PlayerRef GetPlayerRef(Status source)
    {
        if (source == null || source.Object == null || !source.Object.IsValid)
        {
            return default;
        }

        return source.Object.InputAuthority;
    }

    private Status ResolveStatus(PlayerRef playerRef, bool hasPlayerRef)
    {
        if (!hasPlayerRef)
        {
            return null;
        }

        Status[] statuses = FindObjectsOfType<Status>();
        for (int i = 0; i < statuses.Length; i++)
        {
            Status candidate = statuses[i];
            if (
                candidate != null
                && candidate.Object != null
                && candidate.Object.IsValid
                && candidate.Object.InputAuthority == playerRef
            )
            {
                return candidate;
            }
        }

        return null;
    }

    private void RecordDamageDealer(PlayerRef attackerRef, NetworkBool hasAttacker)
    {
        if (!Object.HasStateAuthority || !hasAttacker)
        {
            return;
        }

        if (Object != null && Object.IsValid && attackerRef == Object.InputAuthority)
        {
            return;
        }

        LastDamageDealer = attackerRef;
        HasLastDamageDealer = true;
        LastDamageCreditTimer = TickTimer.CreateFromSeconds(Runner, KillCreditDuration);
    }

    private void HandleDeathScoreTransfer()
    {
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;
        if (!HasLastDamageDealer || !IsTimerActive(LastDamageCreditTimer))
        {
            return;
        }

        if (Object != null && Object.IsValid && LastDamageDealer == Object.InputAuthority)
        {
            return;
        }

        Status attacker = ResolveStatus(LastDamageDealer, true);
        if (attacker == null)
        {
            return;
        }

        int stolenScore = Mathf.Max(1, Mathf.FloorToInt(Score / 5f));
        if (stolenScore <= 0)
        {
            return;
        }

        Score = Mathf.Max(0, Score - Mathf.Min(Score, stolenScore));
        attacker.AddScore(stolenScore);
    }

    private void ExpireFinishedTimers()
    {
        if (BurningTimer.IsRunning && !IsTimerActive(BurningTimer))
        {
            ClearDebuff(SpellDebuffId.Burning);
        }

        if (PoisoningTimer.IsRunning && !IsTimerActive(PoisoningTimer))
        {
            ClearDebuff(SpellDebuffId.Poisoning);
        }

        if (WettingTimer.IsRunning && !IsTimerActive(WettingTimer))
        {
            ClearDebuff(SpellDebuffId.Wetting);
        }

        if (ShockingTimer.IsRunning && !IsTimerActive(ShockingTimer))
        {
            ClearDebuff(SpellDebuffId.Shocking);
        }

        if (FreezingTimer.IsRunning && !IsTimerActive(FreezingTimer))
        {
            ClearDebuff(SpellDebuffId.Freezing);
        }

        if (StunTimer.IsRunning && !IsTimerActive(StunTimer))
        {
            StunTimer = default;
        }

        if (IronbodyTimer.IsRunning && !IsTimerActive(IronbodyTimer))
        {
            IronbodyTimer = default;
        }

        if (WetFreezeLockTimer.IsRunning && !IsTimerActive(WetFreezeLockTimer))
        {
            WetFreezeLockTimer = default;
        }
    }

    private void UpdateEffectiveSpeed()
    {
        float multiplier = 1f;

        if (IsTimerActive(ShockingTimer))
        {
            multiplier *= 0.4f;
        }

        if (IsTimerActive(WolfTimer))
        {
            multiplier *= 1.6f;
        }

        if (!CanMove)
        {
            multiplier = 0f;
        }

        speed = BaseSpeed * multiplier;
    }

    private bool ConsumeBarrier(ref SpellImpact impact)
    {
        if (BarrierStacks <= 0)
        {
            return false;
        }

        if (impact.Pierce <= 0)
        {
            BarrierStacks = Mathf.Max(0, BarrierStacks - 1);
            return true;
        }

        int consumed = Mathf.Min(BarrierStacks, impact.Pierce);
        BarrierStacks -= consumed;
        impact.Pierce -= consumed;

        return BarrierStacks > 0;
    }

    private int ApplyDamageWithWetting(int amount, Status attacker)
    {
        return ApplyDamage(amount, false, attacker);
    }

    private void TrySpreadCollisionStatuses(Collider2D other)
    {
        if (!Object.HasStateAuthority || other == null)
        {
            return;
        }

        Status target = other.GetComponentInParent<Status>();
        if (target == null || target == this)
        {
            return;
        }

        if (IsTimerActive(BurningTimer))
        {
            target.InflictDebuff(SpellDebuffId.Burning, BurningSpreadDuration, false, this);
        }

        if (IsTimerActive(ShockingTimer))
        {
            target.InflictDebuff(SpellDebuffId.Shocking, ShockingDuration, false, this);
        }

        if (IsTimerActive(FreezingTimer))
        {
            target.TakeDamageFrom(
                Mathf.CeilToInt(FreezingCollisionDamage * CollisionDamageMultiplier),
                this
            );
        }
    }

    private void TryApplyNormalCollisionDamage(Status target)
    {
        if (
            target == null
            || target == this
            || player == null
            || target.GetComponent<Player>() == null
        )
        {
            return;
        }

        if (IsNormalCollisionDamageOnCooldown(target))
        {
            return;
        }

        normalCollisionDamageTimes[target] = Time.time;
        int damage = Mathf.CeilToInt(NormalCollisionDamage * CollisionDamageMultiplier);
        target.TakeDamageFrom(damage, this);
    }

    private bool IsNormalCollisionDamageOnCooldown(Status target)
    {
        return normalCollisionDamageTimes.TryGetValue(target, out float lastHitTime)
            && Time.time - lastHitTime < Mathf.Max(0.05f, normalCollisionDamageCooldown);
    }

    private int ApplyDamage(int damage, bool statusDamage)
    {
        return ApplyDamage(damage, statusDamage, default, false);
    }

    private int ApplyDamage(int damage, bool statusDamage, Status attacker)
    {
        return ApplyDamage(damage, statusDamage, GetPlayerRef(attacker), attacker != null);
    }

    private int ApplyDamage(
        int damage,
        bool statusDamage,
        PlayerRef attackerRef,
        NetworkBool hasAttacker
    )
    {
        damage = Mathf.Max(0, damage);
        if (damage <= 0)
        {
            return 0;
        }

        RecordDamageDealer(attackerRef, hasAttacker);
        Health = Mathf.Max(Health - damage, 0);
        PlayDamageFlash();

        if (!statusDamage)
        {
            RegenDelayTimer = TickTimer.CreateFromSeconds(Runner, RegenDelay);
        }

        if (Health <= 0)
        {
            HandleDeathScoreTransfer();
            if (Object.HasInputAuthority)
            {
                Die();
            }
        }

        return damage;
    }

    private void PlayDamageFlash()
    {
        if (Object != null && Object.IsValid)
        {
            RPC_PlayDamageFlash();
            return;
        }

        StartDamageFlash();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDamageFlash()
    {
        StartDamageFlash();
    }

    private void StartDamageFlash()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
            RestoreDamageFlashColors();
        }

        damageFlashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        SpriteRenderer[] renderers = GetDamageFlashRenderers();
        if (renderers.Length == 0 || damageFlashDuration <= 0f)
        {
            damageFlashCoroutine = null;
            yield break;
        }

        cachedDamageFlashColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            cachedDamageFlashColors[i] = renderer.color;
            renderer.color = new Color(
                damageFlashColor.r,
                damageFlashColor.g,
                damageFlashColor.b,
                renderer.color.a
            );
        }

        yield return new WaitForSeconds(damageFlashDuration);
        RestoreDamageFlashColors();
        damageFlashCoroutine = null;
    }

    private SpriteRenderer[] GetDamageFlashRenderers()
    {
        if (damageFlashRenderers != null && damageFlashRenderers.Length > 0)
        {
            return damageFlashRenderers;
        }

        if (cachedDamageFlashRenderers == null || cachedDamageFlashRenderers.Length == 0)
        {
            cachedDamageFlashRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        return cachedDamageFlashRenderers;
    }

    private void RestoreDamageFlashColors()
    {
        SpriteRenderer[] renderers = GetDamageFlashRenderers();
        if (cachedDamageFlashColors == null)
        {
            return;
        }

        int count = Mathf.Min(renderers.Length, cachedDamageFlashColors.Length);
        for (int i = 0; i < count; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = cachedDamageFlashColors[i];
            }
        }
    }

    private void ApplyPowerupDebuffs(SpellImpact impact, bool selfOrigin, Status caster)
    {
        float chance = caster != null ? caster.DebuffChance : BaseDebuffChance;
        RollDebuff(SpellDebuffId.Burning, impact.BurnRolls, selfOrigin, BurningDuration, chance, caster);
        RollDebuff(
            SpellDebuffId.Poisoning,
            impact.PoisonRolls,
            selfOrigin,
            PoisoningDuration,
            chance,
            caster
        );
        RollDebuff(SpellDebuffId.Wetting, impact.WetRolls, selfOrigin, WettingDuration, chance, caster);
        RollDebuff(
            SpellDebuffId.Shocking,
            impact.ShockRolls,
            selfOrigin,
            ShockingDuration,
            chance,
            caster
        );
        RollDebuff(
            SpellDebuffId.Freezing,
            impact.FreezeRolls,
            selfOrigin,
            FreezingDuration,
            chance,
            caster
        );
    }

    private void RollDebuff(
        SpellDebuffId debuff,
        int rolls,
        bool selfOrigin,
        float duration,
        float chance,
        Status attacker
    )
    {
        for (int i = 0; i < rolls; i++)
        {
            if (Random.value <= chance)
            {
                ApplyDebuff(debuff, duration, selfOrigin, attacker);
            }
        }
    }

    private void ApplyDebuff(
        SpellDebuffId debuff,
        float duration,
        bool selfOrigin,
        Status attacker
    )
    {
        if (
            IsTimerActive(WetFreezeLockTimer)
            && (debuff == SpellDebuffId.Wetting || debuff == SpellDebuffId.Freezing)
        )
        {
            return;
        }

        if (ResolveCombination(debuff, attacker))
        {
            return;
        }

        PlayerRef attackerRef = GetPlayerRef(attacker);
        bool hasAttacker = attacker != null;

        switch (debuff)
        {
            case SpellDebuffId.Burning:
                BurningTimer = TickTimer.CreateFromSeconds(Runner, duration);
                BurningSelfOrigin = selfOrigin;
                BurningOwner = attackerRef;
                BurningHasOwner = hasAttacker;
                break;
            case SpellDebuffId.Poisoning:
                PoisoningTimer = TickTimer.CreateFromSeconds(Runner, duration);
                PoisoningSelfOrigin = selfOrigin;
                PoisoningOwner = attackerRef;
                PoisoningHasOwner = hasAttacker;
                break;
            case SpellDebuffId.Wetting:
                WettingTimer = TickTimer.CreateFromSeconds(Runner, duration);
                WettingOwner = attackerRef;
                WettingHasOwner = hasAttacker;
                break;
            case SpellDebuffId.Shocking:
                ShockingTimer = TickTimer.CreateFromSeconds(Runner, duration);
                ShockingSelfOrigin = selfOrigin;
                ShockingOwner = attackerRef;
                ShockingHasOwner = hasAttacker;
                break;
            case SpellDebuffId.Freezing:
                FreezingTimer = TickTimer.CreateFromSeconds(Runner, duration);
                FreezingSelfOrigin = selfOrigin;
                FreezingOwner = attackerRef;
                FreezingHasOwner = hasAttacker;
                break;
        }

        debuffOrder.Enqueue(debuff);
    }

    private bool ResolveCombination(SpellDebuffId incoming, Status attacker)
    {
        PlayerRef attackerRef = GetPlayerRef(attacker);
        bool hasAttacker = attacker != null;

        if (
            incoming == SpellDebuffId.Burning && IsTimerActive(WettingTimer)
            || incoming == SpellDebuffId.Wetting && IsTimerActive(BurningTimer)
        )
        {
            ApplyDamage(BurningWettingDamage, true, attackerRef, hasAttacker);
            ClearDebuff(SpellDebuffId.Burning);
            ClearDebuff(SpellDebuffId.Wetting);
            return true;
        }

        if (
            incoming == SpellDebuffId.Burning && IsTimerActive(FreezingTimer)
            || incoming == SpellDebuffId.Freezing && IsTimerActive(BurningTimer)
        )
        {
            ApplyDamage(FreezingBurningDamage, true, attackerRef, hasAttacker);
            ClearDebuff(SpellDebuffId.Burning);
            ClearDebuff(SpellDebuffId.Freezing);
            ApplyDebuff(SpellDebuffId.Wetting, WettingDuration, false, attacker);
            return true;
        }

        if (
            incoming == SpellDebuffId.Freezing && IsTimerActive(WettingTimer)
            || incoming == SpellDebuffId.Wetting && IsTimerActive(FreezingTimer)
        )
        {
            ApplyDamage(WettingFreezingDamage, true, attackerRef, hasAttacker);
            ClearDebuff(SpellDebuffId.Wetting);
            FreezingTimer = TickTimer.CreateFromSeconds(Runner, FreezingDuration + 1f);
            FreezingOwner = attackerRef;
            FreezingHasOwner = hasAttacker;
            WetFreezeLockTimer = TickTimer.CreateFromSeconds(Runner, FreezingDuration + 1f);
            return true;
        }

        if (
            incoming == SpellDebuffId.Shocking && IsTimerActive(WettingTimer)
            || incoming == SpellDebuffId.Wetting && IsTimerActive(ShockingTimer)
        )
        {
            for (int i = 0; i < WettingShockingHitCount; i++)
            {
                ApplyDamage(WettingShockingHitDamage, true, attackerRef, hasAttacker);
            }

            ClearDebuff(SpellDebuffId.Wetting);
            ClearDebuff(SpellDebuffId.Shocking);
            FreezingTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
            FreezingOwner = attackerRef;
            FreezingHasOwner = hasAttacker;
            return true;
        }

        if (
            incoming == SpellDebuffId.Burning && IsTimerActive(PoisoningTimer)
            || incoming == SpellDebuffId.Poisoning && IsTimerActive(BurningTimer)
        )
        {
            ClearDebuff(SpellDebuffId.Burning);
            ClearDebuff(SpellDebuffId.Poisoning);

            if (Random.value <= 0.25f)
            {
                StrongPoisoning = true;
                PoisoningTimer = TickTimer.CreateFromSeconds(Runner, StrongPoisoningDuration);
                PoisoningOwner = attackerRef;
                PoisoningHasOwner = hasAttacker;
                debuffOrder.Enqueue(SpellDebuffId.Poisoning);
            }

            return true;
        }

        return false;
    }

    private void CureDebuffs(int amount)
    {
        for (int i = 0; i < amount && debuffOrder.Count > 0; i++)
        {
            ClearDebuff(debuffOrder.Dequeue());
        }
    }

    private void ClearDebuff(SpellDebuffId debuff)
    {
        switch (debuff)
        {
            case SpellDebuffId.Burning:
                BurningTimer = default;
                BurningSelfOrigin = false;
                BurningHasOwner = false;
                break;
            case SpellDebuffId.Poisoning:
                PoisoningTimer = default;
                StrongPoisoning = false;
                PoisoningSelfOrigin = false;
                PoisoningHasOwner = false;
                break;
            case SpellDebuffId.Wetting:
                WettingTimer = default;
                WettingHasOwner = false;
                break;
            case SpellDebuffId.Shocking:
                ShockingTimer = default;
                ShockingSelfOrigin = false;
                ShockingHasOwner = false;
                break;
            case SpellDebuffId.Freezing:
                FreezingTimer = default;
                FreezingSelfOrigin = false;
                FreezingHasOwner = false;
                break;
        }
    }

    private void AddBarrier(int amount)
    {
        BarrierStacks += Mathf.Max(1, amount);
        BarrierTimer = TickTimer.CreateFromSeconds(Runner, BarrierDuration);
    }

    private bool RandomChance(int percent)
    {
        return Random.Range(0, 100) < Mathf.Clamp(percent, 0, 100);
    }

    private void ApplyExplosion(
        Status caster,
        float radius,
        LayerMask hitMask,
        SpellImpact sourceImpact
    )
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, hitMask);
        HashSet<Status> statusTargets = new HashSet<Status>();
        HashSet<DestructibleStructure> structureTargets = new HashSet<DestructibleStructure>();

        for (int i = 0; i < hits.Length; i++)
        {
            Status target = hits[i].GetComponentInParent<Status>();
            if (target != null)
            {
                if (!statusTargets.Add(target))
                {
                    continue;
                }

                SpellImpact explosionImpact = CreateExplosionImpact(sourceImpact);
                target.ApplySpellImpact(ref explosionImpact, caster, radius, hitMask);
                continue;
            }

            DestructibleStructure structure = hits[i].GetComponentInParent<DestructibleStructure>();
            if (structure == null || !structureTargets.Add(structure))
            {
                continue;
            }

            SpellImpact structureImpact = CreateExplosionImpact(sourceImpact);
            structure.ApplySpellImpact(ref structureImpact, caster, radius, hitMask);
        }
    }

    private SpellImpact CreateExplosionImpact(SpellImpact sourceImpact)
    {
        return new SpellImpact
        {
            Damage = 5,
            BurnRolls = sourceImpact.BurnRolls,
            PoisonRolls = sourceImpact.PoisonRolls,
            WetRolls = sourceImpact.WetRolls,
            ShockRolls = sourceImpact.ShockRolls,
            FreezeRolls = sourceImpact.FreezeRolls,
            Powerup2 = sourceImpact.Powerup2,
            IgnoreBarrierForExplosion = true,
        };
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = Health / (float)MaxHealth;

        if (healthText != null)
            healthText.text = $"{Health:F0} / {MaxHealth:F0}";
    }

    private void UpdateManaUI()
    {
        if (manaBar != null)
            manaBar.fillAmount = Mana / (float)MaxMana;

        if (manaText != null)
            manaText.text = $"{Mana:F0} / {MaxMana:F0}";
    }

    private void UpdateExperienceUI()
    {
        if (expBar != null)
            expBar.fillAmount = NextLevelExperience > 0f ? Experience / NextLevelExperience : 0f;

        if (expText != null)
            expText.text = $"{Experience:F0} / {NextLevelExperience:F0}";
    }

    private void UpdateLevelUI()
    {
        if (levelText != null)
            levelText.text = Level.ToString();
    }

    private void NotifyStatsChanged()
    {
        StatsChanged?.Invoke();
    }

    private void Die()
    {
        if (shutdownCoroutine != null)
        {
            return;
        }

        shutdownCoroutine = StartCoroutine(ShutdownAfterScoreSync());
    }

    private IEnumerator ShutdownAfterScoreSync()
    {
        yield return new WaitForSeconds(0.15f);

        if (Runner != null)
        {
            Runner.Shutdown();
        }
    }
}
