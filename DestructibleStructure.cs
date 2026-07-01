using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class DestructibleStructure : NetworkBehaviour
{
    private const float BaseDebuffChance = 0.15f;
    private const float StatusTickRate = 1f;
    private const int BurningDamage = 4;
    private const int PoisoningDamage = 2;
    private const int FreezingDamage = 2;
    private const float BurningDuration = 9f;
    private const float PoisoningDuration = 15f;
    private const float WettingDuration = 9f;
    private const float ShockingDuration = 6f;
    private const float FreezingDuration = 3f;
    private const float BarrierDuration = 30f;

    [Header("Health")]
    [SerializeField]
    private int maxHealth = 100;

    [SerializeField]
    private float respawnCooldown = 30f;

    [SerializeField]
    private NetworkPrefabRef respawnPrefab;

    [SerializeField]
    private Transform respawnParent;

    [Header("Accepted Effects")]
    [SerializeField]
    private bool receiveDamage = true;

    [SerializeField]
    private bool receiveHeal = true;

    [SerializeField]
    private bool receiveCure = true;

    [SerializeField]
    private bool receiveBarrier = true;

    [Header("Accepted Debuffs")]
    [SerializeField]
    private bool canBurn = true;

    [SerializeField]
    private bool canPoison = true;

    [SerializeField]
    private bool canWet = true;

    [SerializeField]
    private bool canShock = true;

    [SerializeField]
    private bool canFreeze = true;

    [Networked]
    public int Health { get; private set; }

    [Networked]
    private int BarrierStacks { get; set; }

    [Networked]
    private NetworkBool IsDestroyed { get; set; }

    [Networked]
    private TickTimer BarrierTimer { get; set; }

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
    private TickTimer StatusTickTimer { get; set; }

    public bool IsBurning => IsTimerActive(BurningTimer);
    public bool IsPoisoning => IsTimerActive(PoisoningTimer);
    public bool IsWetting => IsTimerActive(WettingTimer);
    public bool IsShocking => IsTimerActive(ShockingTimer);
    public bool IsFreezing => IsTimerActive(FreezingTimer);

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsDestroyed = false;
            Health = Mathf.Max(1, maxHealth);
            StatusTickTimer = TickTimer.CreateFromSeconds(Runner, StatusTickRate);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        UpdateTimedStatuses();
    }

    public int ApplySpellImpact(
        ref SpellImpact impact,
        Status caster,
        float explosionRadius,
        LayerMask hitMask
    )
    {
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
                impact.Powerup2
            );
            return 0;
        }

        if (IsDestroyed)
        {
            return 0;
        }

        bool blockedByBarrier = !impact.IgnoreBarrierForExplosion && ConsumeBarrier(ref impact);
        int incomingHeal =
            caster != null ? caster.ApplyOutgoingHealBonus(impact.Heal) : impact.Heal;
        int incomingDamage =
            caster != null ? caster.ApplyOutgoingDamageBonus(impact.Damage) : impact.Damage;
        int totalDamage = 0;

        if (!blockedByBarrier)
        {
            if (receiveHeal && incomingHeal > 0)
            {
                Heal(incomingHeal);
            }

            if (receiveCure && impact.Cure > 0)
            {
                CureDebuffs(impact.Cure);
            }

            if (receiveBarrier && impact.Barrier > 0)
            {
                AddBarrier(impact.Barrier);
            }

            if (receiveDamage && impact.Slaughter > 0 && RandomChance(impact.Slaughter))
            {
                totalDamage += ApplyDamage(Health);
            }

            if (receiveDamage && incomingDamage > 0)
            {
                totalDamage += ApplyDamage(incomingDamage);
            }

            ApplyPowerupDebuffs(impact, caster);
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
        int powerup2
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

        ApplySpellImpact(ref impact, null, 1f, ~0);
    }

    private void UpdateTimedStatuses()
    {
        if (!StatusTickTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        StatusTickTimer = TickTimer.CreateFromSeconds(Runner, StatusTickRate);
        ExpireFinishedTimers();

        if (IsTimerActive(BurningTimer))
        {
            ApplyDamage(BurningDamage);
        }

        if (IsTimerActive(PoisoningTimer))
        {
            ApplyDamage(PoisoningDamage);
        }

        if (IsTimerActive(FreezingTimer))
        {
            ApplyDamage(FreezingDamage);
        }

        if (BarrierStacks > 0 && !IsTimerActive(BarrierTimer))
        {
            BarrierStacks = 0;
        }
    }

    private void ExpireFinishedTimers()
    {
        if (BurningTimer.IsRunning && !IsTimerActive(BurningTimer))
        {
            BurningTimer = default;
        }

        if (PoisoningTimer.IsRunning && !IsTimerActive(PoisoningTimer))
        {
            PoisoningTimer = default;
        }

        if (WettingTimer.IsRunning && !IsTimerActive(WettingTimer))
        {
            WettingTimer = default;
        }

        if (ShockingTimer.IsRunning && !IsTimerActive(ShockingTimer))
        {
            ShockingTimer = default;
        }

        if (FreezingTimer.IsRunning && !IsTimerActive(FreezingTimer))
        {
            FreezingTimer = default;
        }
    }

    private bool ConsumeBarrier(ref SpellImpact impact)
    {
        if (!receiveBarrier || BarrierStacks <= 0)
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

    private void Heal(int amount)
    {
        Health = Mathf.Min(Health + Mathf.Max(0, amount), Mathf.Max(1, maxHealth));
    }

    private int ApplyDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0 || IsDestroyed)
        {
            return 0;
        }

        Health = Mathf.Max(Health - amount, 0);
        if (Health <= 0)
        {
            DespawnAndScheduleRespawn();
        }

        return amount;
    }

    private void AddBarrier(int amount)
    {
        BarrierStacks += Mathf.Max(1, amount);
        BarrierTimer = TickTimer.CreateFromSeconds(Runner, BarrierDuration);
    }

    private void ApplyPowerupDebuffs(SpellImpact impact, Status caster)
    {
        float chance = caster != null ? caster.DebuffChance : BaseDebuffChance;
        RollDebuff(SpellDebuffId.Burning, impact.BurnRolls, BurningDuration, chance);
        RollDebuff(SpellDebuffId.Poisoning, impact.PoisonRolls, PoisoningDuration, chance);
        RollDebuff(SpellDebuffId.Wetting, impact.WetRolls, WettingDuration, chance);
        RollDebuff(SpellDebuffId.Shocking, impact.ShockRolls, ShockingDuration, chance);
        RollDebuff(SpellDebuffId.Freezing, impact.FreezeRolls, FreezingDuration, chance);
    }

    private void RollDebuff(SpellDebuffId debuff, int rolls, float duration, float chance)
    {
        if (rolls <= 0 || !CanReceiveDebuff(debuff))
        {
            return;
        }

        for (int i = 0; i < rolls; i++)
        {
            if (Random.value <= chance)
            {
                ApplyDebuff(debuff, duration);
            }
        }
    }

    private bool CanReceiveDebuff(SpellDebuffId debuff)
    {
        switch (debuff)
        {
            case SpellDebuffId.Burning:
                return canBurn;
            case SpellDebuffId.Poisoning:
                return canPoison;
            case SpellDebuffId.Wetting:
                return canWet;
            case SpellDebuffId.Shocking:
                return canShock;
            case SpellDebuffId.Freezing:
                return canFreeze;
            default:
                return false;
        }
    }

    private void ApplyDebuff(SpellDebuffId debuff, float duration)
    {
        switch (debuff)
        {
            case SpellDebuffId.Burning:
                BurningTimer = TickTimer.CreateFromSeconds(Runner, duration);
                break;
            case SpellDebuffId.Poisoning:
                PoisoningTimer = TickTimer.CreateFromSeconds(Runner, duration);
                break;
            case SpellDebuffId.Wetting:
                WettingTimer = TickTimer.CreateFromSeconds(Runner, duration);
                break;
            case SpellDebuffId.Shocking:
                ShockingTimer = TickTimer.CreateFromSeconds(Runner, duration);
                break;
            case SpellDebuffId.Freezing:
                FreezingTimer = TickTimer.CreateFromSeconds(Runner, duration);
                break;
        }
    }

    private void CureDebuffs(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (IsTimerActive(BurningTimer) && amount-- > 0)
        {
            BurningTimer = default;
        }

        if (IsTimerActive(PoisoningTimer) && amount-- > 0)
        {
            PoisoningTimer = default;
        }

        if (IsTimerActive(WettingTimer) && amount-- > 0)
        {
            WettingTimer = default;
        }

        if (IsTimerActive(ShockingTimer) && amount-- > 0)
        {
            ShockingTimer = default;
        }

        if (IsTimerActive(FreezingTimer) && amount > 0)
        {
            FreezingTimer = default;
        }
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
            SpellImpact explosionImpact = CreateExplosionImpact(sourceImpact);
            Status targetStatus = hits[i].GetComponentInParent<Status>();
            if (targetStatus != null && statusTargets.Add(targetStatus))
            {
                targetStatus.ApplySpellImpact(ref explosionImpact, caster, radius, hitMask);
                continue;
            }

            DestructibleStructure targetStructure =
                hits[i].GetComponentInParent<DestructibleStructure>();
            if (targetStructure != null && structureTargets.Add(targetStructure))
            {
                targetStructure.ApplySpellImpact(ref explosionImpact, caster, radius, hitMask);
            }
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

    private void DespawnAndScheduleRespawn()
    {
        if (Runner == null || Object == null || !Object.IsValid)
        {
            return;
        }

        if (IsDestroyed)
        {
            return;
        }

        IsDestroyed = true;
        NetworkPrefabRef prefab = respawnPrefab;
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        float cooldown = Mathf.Max(0f, respawnCooldown);
        NetworkRunner runner = Runner;
        Transform parent = respawnParent;

        if (prefab.IsValid)
        {
            StructureRespawnHost.Schedule(runner, prefab, position, rotation, parent, cooldown);
        }

        runner.Despawn(Object);
    }

    private bool IsTimerActive(TickTimer timer)
    {
        return timer.IsRunning && !timer.ExpiredOrNotRunning(Runner);
    }

    private bool RandomChance(int percent)
    {
        return Random.Range(0, 100) < Mathf.Clamp(percent, 0, 100);
    }

    private class StructureRespawnHost : MonoBehaviour
    {
        private static StructureRespawnHost instance;

        public static void Schedule(
            NetworkRunner runner,
            NetworkPrefabRef prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            float delay
        )
        {
            if (runner == null || !prefab.IsValid)
            {
                return;
            }

            if (instance == null)
            {
                GameObject host = new GameObject("StructureRespawnHost");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<StructureRespawnHost>();
            }

            instance.StartCoroutine(
                instance.RespawnAfterDelay(runner, prefab, position, rotation, parent, delay)
            );
        }

        private IEnumerator RespawnAfterDelay(
            NetworkRunner runner,
            NetworkPrefabRef prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            float delay
        )
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (runner != null && runner.IsRunning && prefab.IsValid)
            {
                NetworkObject spawnedObject = runner.Spawn(prefab, position, rotation);
                if (spawnedObject != null && parent != null)
                {
                    spawnedObject.transform.SetParent(parent, true);
                }
            }
        }
    }
}
