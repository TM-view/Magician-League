using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerFireballCaster : NetworkBehaviour
{
    [SerializeField]
    private UIManager uiManager;

    [SerializeField]
    private Transform spellSlot;

    [SerializeField]
    private NetworkPrefabRef spellProjectilePrefab;

    [SerializeField]
    private NetworkPrefabRef wolfPetPrefab;

    [SerializeField]
    private float wolfPetLifetime = 30f;

    [SerializeField]
    private float wolfSpawnSeparation = 0.8f;

    [SerializeField]
    private GameObject areaVisualPrefab;

    [SerializeField]
    private GameObject donutVisualPrefab;

    [SerializeField]
    private GameObject coneVisualPrefab;

    [SerializeField]
    private GameObject smogPrefab;

    [SerializeField]
    private GameObject magnetPrefab;

    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private InputActionReference fireActionReference;

    [SerializeField]
    private InputActionReference pointActionReference;

    [SerializeField]
    private LayerMask spellTargetMask = ~0;

    [SerializeField]
    private float minimumCooldown = 0.5f;

    [SerializeField]
    private int baseManaCost = 0;

    [SerializeField]
    private float defaultAroundRadius = 1.5f;

    [SerializeField]
    private float defaultTouchRange = 4f;

    [SerializeField]
    private float defaultTouchRadius = 1f;

    [SerializeField]
    private float defaultProjectileSpeed = 10f;

    [SerializeField]
    private float defaultProjectileLifetime = 2f;

    [SerializeField]
    private float coneAngle = 70f;

    [SerializeField]
    private float repeatInterval = 0.3f;

    [SerializeField]
    private float selfShiftBaseDistance = 1.2f;

    [SerializeField]
    private float selfShiftDuration = 0.15f;

    [SerializeField]
    private float selfShiftSpeedPerAccelerate = 0.5f;

    [SerializeField]
    private float magnetBaseForce = 2f;

    [SerializeField]
    private float smogDuration = 10f;

    private TickTimer cooldownTimer;
    private Status status;
    private Player player;
    private PlayerInventory playerInventory;
    private InputAction fallbackFireAction;
    private InputAction fallbackPointAction;
    private Collider2D[] areaHitBuffer = new Collider2D[32];
    private readonly HashSet<Status> appliedTargets = new HashSet<Status>();
    private readonly HashSet<DestructibleStructure> appliedStructures =
        new HashSet<DestructibleStructure>();
    private readonly HashSet<WolfPet> appliedWolves = new HashSet<WolfPet>();
    private readonly List<WolfPet> ownerWolfBuffer = new List<WolfPet>();
    private readonly List<Vector3> reservedWolfSpawnPositions = new List<Vector3>();
    private bool fireWasPressed;
    private static Sprite smogSprite;
    private static Material areaVisualMaterial;

    public float TouchRadius => defaultTouchRadius;
    public LayerMask SpellTargetMask => spellTargetMask;

    private InputAction FireAction =>
        fireActionReference != null ? fireActionReference.action : fallbackFireAction;

    private InputAction PointAction =>
        pointActionReference != null ? pointActionReference.action : fallbackPointAction;

    private void Awake()
    {
        status = GetComponent<Status>();
        player = GetComponent<Player>();
        playerInventory = GetComponent<PlayerInventory>();

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
    }

    private void OnEnable()
    {
        if (fireActionReference != null && fireActionReference.action != null)
        {
            fireActionReference.action.Enable();
        }
        else
        {
            GetOrCreateFallbackFireAction().Enable();
        }

        if (pointActionReference != null && pointActionReference.action != null)
        {
            pointActionReference.action.Enable();
        }
        else
        {
            GetOrCreateFallbackPointAction().Enable();
        }
    }

    private void OnDisable()
    {
        if (fallbackFireAction != null)
        {
            fallbackFireAction.Disable();
        }

        if (fallbackPointAction != null)
        {
            fallbackPointAction.Disable();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
        {
            return;
        }

        bool firePressed = IsFirePressed();
        if (IsCastingBlockedByUI())
        {
            fireWasPressed = firePressed;
            return;
        }

        if (firePressed && !fireWasPressed)
        {
            TryCastSpell();
        }

        fireWasPressed = firePressed;
    }

    public void ContinueSpell(
        SpellCastData spell,
        int segmentIndex,
        Vector3 origin,
        Vector2 direction
    )
    {
        if (spell == null || segmentIndex >= spell.Segments.Count)
        {
            return;
        }

        CastSegmentWithDelay(spell, segmentIndex, origin, direction.normalized, false);
    }

    private void TryCastSpell()
    {
        if (IsCastingBlockedByUI())
        {
            return;
        }

        if (!cooldownTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        if (
            status == null
            || !status.CanCastSpells
            || !TryGetSelectedSpell(out CompleteSpellSO selectedSpell)
        )
        {
            return;
        }

        if (
            !SpellParser.TryBuild(
                selectedSpell.Components,
                baseManaCost,
                defaultAroundRadius,
                defaultTouchRange,
                defaultProjectileSpeed,
                defaultProjectileLifetime,
                out SpellCastData spell
            )
        )
        {
            return;
        }

        if (playerInventory != null)
        {
            playerInventory.ApplyEquipmentPowerups(spell);
        }

        if (!status.TrySpendManaWithTransfer(spell.ManaCost, spell.UsesTransfer))
        {
            return;
        }

        Vector2 direction = GetAimDirection();
        CastSegmentWithDelay(spell, 0, transform.position, direction, true);
    }

    private void CastSegmentWithDelay(
        SpellCastData spell,
        int segmentIndex,
        Vector3 origin,
        Vector2 direction,
        bool startCooldownAfterCast
    )
    {
        SpellSegmentData segment = spell.Segments[segmentIndex];
        float delay = segment.DelaySeconds;

        if (delay <= 0f)
        {
            CastSegmentRepeats(spell, segmentIndex, origin, direction);
            if (startCooldownAfterCast)
            {
                StartCooldown(spell);
            }
            return;
        }

        StartCoroutine(
            CastAfterDelay(spell, segmentIndex, origin, direction, delay, startCooldownAfterCast)
        );
    }

    private IEnumerator CastAfterDelay(
        SpellCastData spell,
        int segmentIndex,
        Vector3 origin,
        Vector2 direction,
        float delay,
        bool startCooldownAfterCast
    )
    {
        yield return new WaitForSeconds(delay);

        CastSegmentRepeats(spell, segmentIndex, origin, direction);
        if (startCooldownAfterCast)
        {
            StartCooldown(spell);
        }
    }

    private void CastSegmentRepeats(
        SpellCastData spell,
        int segmentIndex,
        Vector3 origin,
        Vector2 direction
    )
    {
        SpellSegmentData segment = spell.Segments[segmentIndex];

        if (segment.RepeatCount <= 1)
        {
            CastSegment(spell, segmentIndex, origin, direction);
            return;
        }

        StartCoroutine(CastRepeats(spell, segmentIndex, origin, direction, segment.RepeatCount));
    }

    private IEnumerator CastRepeats(
        SpellCastData spell,
        int segmentIndex,
        Vector3 origin,
        Vector2 direction,
        int count
    )
    {
        for (int i = 0; i < count; i++)
        {
            CastSegment(spell, segmentIndex, origin, direction);
            if (i < count - 1)
            {
                yield return new WaitForSeconds(repeatInterval);
            }
        }
    }

    private void StartCooldown(SpellCastData spell)
    {
        float multiplier = status != null ? status.SpellCooldownMultiplier : 1f;
        float spellCooldown = spell != null ? spell.Cooldown : 0f;
        cooldownTimer = TickTimer.CreateFromSeconds(
            Runner,
            Mathf.Max(minimumCooldown, spellCooldown) * multiplier
        );
    }

    private bool TryGetSelectedSpell(out CompleteSpellSO selectedSpell)
    {
        selectedSpell = null;

        if (uiManager == null || spellSlot == null)
        {
            return false;
        }

        int selectedSpellIndex = uiManager.latestSpellIndex;
        if (selectedSpellIndex < 0)
        {
            selectedSpellIndex = 0;
            uiManager.latestSpellIndex = selectedSpellIndex;
        }

        if (selectedSpellIndex >= spellSlot.childCount)
        {
            return false;
        }

        CompleteSpellButton spellButton = spellSlot
            .GetChild(selectedSpellIndex)
            .GetComponent<CompleteSpellButton>();

        if (spellButton == null)
        {
            return false;
        }

        spellButton.EnsureInitialized();
        if (spellButton.completeSpells == null)
        {
            return false;
        }

        selectedSpell = spellButton.completeSpells;
        return selectedSpell.Components != null;
    }

    private void CastSegment(
        SpellCastData spell,
        int segmentIndex,
        Vector3 origin,
        Vector2 direction
    )
    {
        SpellSegmentData segment = spell.Segments[segmentIndex];

        switch (segment.Active)
        {
            case SpellActiveId.Self:
                SpawnSpellVfxBurst(transform, transform.position, segment, direction, 0.6f, 1f);
                ApplyToStatus(status, segment);
                ContinueSpell(spell, segmentIndex + 1, transform.position, direction);
                break;
            case SpellActiveId.Linear:
            case SpellActiveId.Laser:
                CastProjectiles(spell, segmentIndex, origin, direction);
                break;
            case SpellActiveId.Touch:
                CastTouch(spell, segmentIndex, direction);
                break;
            case SpellActiveId.Around:
                SpawnAreaVisual(origin, segment.EffectiveRadius, segment.Active, direction);
                SpawnSpellVfxBurst(null, origin, segment, direction, 0.6f, segment.EffectiveRadius);
                SummonWolvesAt(origin, segment);
                ApplyArea(origin, segment.EffectiveRadius, 0f, segment, true, true);
                ContinueSpell(spell, segmentIndex + 1, origin, direction);
                break;
            case SpellActiveId.Cone:
                SpawnAreaVisual(origin, segment.EffectiveRange, segment.Active, direction);
                SpawnSpellVfxBurst(
                    null,
                    origin + (Vector3)(direction.normalized * segment.EffectiveRange * 0.45f),
                    segment,
                    direction,
                    0.6f,
                    Mathf.Max(0.8f, segment.EffectiveRange * 0.35f)
                );
                SummonWolvesAt(
                    origin + (Vector3)(direction.normalized * segment.EffectiveRange * 0.5f),
                    segment
                );
                ApplyCone(origin, direction, segment);
                ContinueSpell(spell, segmentIndex + 1, origin, direction);
                break;
            case SpellActiveId.Donut:
                SpawnAreaVisual(origin, segment.EffectiveRadius, segment.Active, direction);
                SpawnSpellVfxBurst(null, origin, segment, direction, 0.6f, segment.EffectiveRadius);
                SummonWolvesAt(origin, segment);
                ApplyArea(
                    origin,
                    segment.EffectiveRadius,
                    segment.EffectiveRadius * 0.45f,
                    segment,
                    true,
                    true
                );
                ContinueSpell(spell, segmentIndex + 1, origin, direction);
                break;
        }
    }

    private void CastProjectiles(
        SpellCastData spell,
        int segmentIndex,
        Vector3 origin,
        Vector2 direction
    )
    {
        if (firePoint == null || !spellProjectilePrefab.IsValid)
        {
            return;
        }

        SpellSegmentData segment = spell.Segments[segmentIndex];
        int count = segment.ProjectileCount;
        float spread = count > 1 ? 12f : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = (i - (count - 1) * 0.5f) * spread;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction.normalized;
            Quaternion rotation = Quaternion.LookRotation(Vector3.forward, -shotDirection);

            NetworkObject projectileObject = Runner.Spawn(
                spellProjectilePrefab,
                origin == transform.position ? firePoint.position : origin,
                rotation,
                Object.InputAuthority
            );

            Fireball projectile = projectileObject.GetComponent<Fireball>();
            if (projectile == null)
            {
                Runner.Despawn(projectileObject);
                continue;
            }

            projectile.InitSpell(
                spell,
                segmentIndex,
                shotDirection,
                Object.InputAuthority,
                this,
                status
            );
        }
    }

    private void CastTouch(SpellCastData spell, int segmentIndex, Vector2 direction)
    {
        SpellSegmentData segment = spell.Segments[segmentIndex];
        Vector3 targetPosition = GetMouseWorldPosition();
        Vector2 fromPlayer = targetPosition - transform.position;

        if (fromPlayer.magnitude > segment.EffectiveRange)
        {
            targetPosition =
                transform.position + (Vector3)(fromPlayer.normalized * segment.EffectiveRange);
        }

        SpawnAreaVisual(targetPosition, segment.EffectiveRadius, segment.Active, direction);
        SpawnSpellVfxBurst(
            null,
            targetPosition,
            segment,
            direction,
            0.6f,
            segment.EffectiveRadius
        );
        SummonWolvesAt(targetPosition, segment);
        ApplyArea(targetPosition, segment.EffectiveRadius, 0f, segment, true, true);
        ContinueSpell(spell, segmentIndex + 1, targetPosition, direction);
    }

    private void SpawnSpellVfxBurst(
        Transform followTarget,
        Vector3 position,
        SpellSegmentData segment,
        Vector2 direction,
        float duration,
        float scale
    )
    {
        SpellVfxBurst.Play(followTarget, position, segment, direction, duration, scale);
    }

    private void ApplyCone(Vector3 origin, Vector2 direction, SpellSegmentData segment)
    {
        int hitCount = GetAreaHits(origin, segment.EffectiveRange);
        int piercePool = segment.CreateImpact().Pierce;
        appliedTargets.Clear();
        appliedStructures.Clear();
        appliedWolves.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = areaHitBuffer[i];
            Vector2 toTarget = hit.transform.position - origin;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            Vector2 normalizedToTarget = toTarget.normalized;
            if (
                Vector2.Dot(direction.normalized, normalizedToTarget) <= 0f
                || Vector2.Angle(direction, normalizedToTarget) > coneAngle * 0.5f
            )
            {
                continue;
            }

            ApplyColliderTarget(hit, segment, true, ref piercePool);
        }
    }

    private void ApplyArea(
        Vector2 center,
        float outerRadius,
        float innerRadius,
        SpellSegmentData segment,
        bool ignoreOwner,
        bool triggerPersistentEffects
    )
    {
        int hitCount = GetAreaHits(center, outerRadius);
        int piercePool = segment.CreateImpact().Pierce;
        appliedTargets.Clear();
        appliedStructures.Clear();
        appliedWolves.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = areaHitBuffer[i];
            if (innerRadius > 0f && Vector2.Distance(center, hit.transform.position) < innerRadius)
            {
                continue;
            }

            ApplyColliderTarget(hit, segment, ignoreOwner, ref piercePool);
        }

        if (triggerPersistentEffects && segment.Smog > 0)
        {
            StartCoroutine(ApplySmog(center, segment));
        }

        if (triggerPersistentEffects && segment.Magnet > 0)
        {
            StartCoroutine(ApplyMagnet(center, segment));
        }
    }

    private void ApplyColliderTarget(
        Collider2D hit,
        SpellSegmentData segment,
        bool ignoreOwner,
        ref int piercePool
    )
    {
        if (!IsInSpellTargetMask(hit))
        {
            return;
        }

        NetworkObject hitObject = hit.GetComponentInParent<NetworkObject>();
        if (ignoreOwner && hitObject != null && hitObject.InputAuthority == Object.InputAuthority)
        {
            return;
        }

        Status targetStatus = hit.GetComponentInParent<Status>();
        if (targetStatus != null)
        {
            if (appliedTargets.Contains(targetStatus))
            {
                return;
            }

            appliedTargets.Add(targetStatus);
            ApplyToStatus(targetStatus, segment, ref piercePool);
            return;
        }

        DestructibleStructure targetStructure = hit.GetComponentInParent<DestructibleStructure>();
        if (targetStructure != null)
        {
            if (appliedStructures.Contains(targetStructure))
            {
                return;
            }

            appliedStructures.Add(targetStructure);
            ApplyToStructure(targetStructure, segment, ref piercePool);
            return;
        }

        WolfPet targetWolf = hit.GetComponentInParent<WolfPet>();
        if (targetWolf == null || appliedWolves.Contains(targetWolf))
        {
            return;
        }

        appliedWolves.Add(targetWolf);
        ApplyToWolf(targetWolf, segment, ref piercePool);
    }

    private bool IsInSpellTargetMask(Collider2D hit)
    {
        if (hit == null)
        {
            return false;
        }

        int layerMask = 1 << hit.gameObject.layer;
        return (spellTargetMask.value & layerMask) != 0;
    }

    private void ApplyToStatus(Status targetStatus, SpellSegmentData segment)
    {
        int piercePool = segment.CreateImpact().Pierce;
        ApplyToStatus(targetStatus, segment, ref piercePool);
    }

    private void ApplyToStatus(Status targetStatus, SpellSegmentData segment, ref int piercePool)
    {
        if (targetStatus == null)
        {
            return;
        }

        SpellImpact impact = segment.CreateImpact();
        impact.Pierce = piercePool;
        targetStatus.ApplySpellImpact(ref impact, status, defaultTouchRadius, spellTargetMask);
        piercePool = impact.Pierce;

        if (segment.Ironbody > 0)
        {
            targetStatus.ApplyIronbody();
        }

        if (segment.Wolf > 0 && segment.Active == SpellActiveId.Self)
        {
            ApplyWolf(targetStatus, segment);
        }

        if (segment.Shift > 0)
        {
            ApplyShift(targetStatus, segment, segment.Active == SpellActiveId.Self);
        }
    }

    private void ApplyToStructure(
        DestructibleStructure targetStructure,
        SpellSegmentData segment,
        ref int piercePool
    )
    {
        if (targetStructure == null)
        {
            return;
        }

        SpellImpact impact = segment.CreateImpact();
        impact.Pierce = piercePool;
        targetStructure.ApplySpellImpact(ref impact, status, defaultTouchRadius, spellTargetMask);
        piercePool = impact.Pierce;
    }

    private void ApplyToWolf(WolfPet targetWolf, SpellSegmentData segment, ref int piercePool)
    {
        if (targetWolf == null || targetWolf.IsOwnedBy(Object.InputAuthority))
        {
            return;
        }

        SpellImpact impact = segment.CreateImpact();
        impact.Pierce = piercePool;
        targetWolf.ApplySpellImpact(ref impact, status);
        piercePool = impact.Pierce;
    }

    public void SummonWolvesAt(Vector3 position, SpellSegmentData segment)
    {
        if (segment == null || segment.Wolf <= 0 || segment.Active == SpellActiveId.Self)
        {
            return;
        }

        if (!wolfPetPrefab.IsValid)
        {
            return;
        }

        CollectOwnerWolves(ownerWolfBuffer);
        reservedWolfSpawnPositions.Clear();

        int existingWolfCount = ownerWolfBuffer.Count;
        int totalWolfCount = existingWolfCount + segment.Wolf;

        for (int i = 0; i < segment.Wolf; i++)
        {
            int formationIndex = existingWolfCount + i;
            Vector3 spawnPosition = GetWolfSpawnPosition(
                position,
                formationIndex,
                totalWolfCount,
                ownerWolfBuffer,
                reservedWolfSpawnPositions
            );
            NetworkObject pet = Runner.Spawn(
                wolfPetPrefab,
                spawnPosition,
                Quaternion.identity,
                Object.InputAuthority
            );

            WolfPet wolf = pet != null ? pet.GetComponent<WolfPet>() : null;
            if (wolf != null)
            {
                wolf.Initialize(
                    status,
                    Object.InputAuthority,
                    totalWolfCount,
                    formationIndex,
                    wolfPetLifetime
                );
                ownerWolfBuffer.Add(wolf);
                reservedWolfSpawnPositions.Add(spawnPosition);
            }
            else
            {
                StartCoroutine(DespawnAfter(pet, wolfPetLifetime));
            }
        }

        ReassignOwnerWolfFormation(ownerWolfBuffer);
    }

    private void CollectOwnerWolves(List<WolfPet> wolves)
    {
        wolves.Clear();
        WolfPet[] allWolves = FindObjectsOfType<WolfPet>();
        for (int i = 0; i < allWolves.Length; i++)
        {
            WolfPet wolf = allWolves[i];
            if (wolf == null || wolf.Health <= 0 || !wolf.IsOwnedBy(Object.InputAuthority))
            {
                continue;
            }

            wolves.Add(wolf);
        }
    }

    private Vector3 GetWolfSpawnPosition(
        Vector3 center,
        int slotIndex,
        int totalSlots,
        List<WolfPet> existingWolves,
        List<Vector3> reservedPositions
    )
    {
        int slots = Mathf.Max(1, totalSlots);
        float separation = Mathf.Max(0.1f, wolfSpawnSeparation);
        float separationSqr = separation * separation;

        for (int ring = 0; ring < 4; ring++)
        {
            float radius = separation * (ring + 1);
            int samples = Mathf.Max(8, slots * 2);
            for (int step = 0; step < samples; step++)
            {
                float angle =
                    (slotIndex + step * 0.5f) / samples * Mathf.PI * 2f + ring * 0.37f;
                Vector3 candidate =
                    center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                if (IsWolfSpawnPositionFree(candidate, existingWolves, reservedPositions, separationSqr))
                {
                    return candidate;
                }
            }
        }

        float fallbackAngle = slotIndex / (float)slots * Mathf.PI * 2f;
        return center + new Vector3(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle), 0f)
            * separation
            * 4f;
    }

    private bool IsWolfSpawnPositionFree(
        Vector3 candidate,
        List<WolfPet> existingWolves,
        List<Vector3> reservedPositions,
        float separationSqr
    )
    {
        for (int i = 0; i < existingWolves.Count; i++)
        {
            WolfPet wolf = existingWolves[i];
            if (wolf == null || wolf.Health <= 0)
            {
                continue;
            }

            if (Vector2.SqrMagnitude(wolf.transform.position - candidate) < separationSqr)
            {
                return false;
            }
        }

        for (int i = 0; i < reservedPositions.Count; i++)
        {
            if (Vector2.SqrMagnitude(reservedPositions[i] - candidate) < separationSqr)
            {
                return false;
            }
        }

        return true;
    }

    private void ReassignOwnerWolfFormation(List<WolfPet> wolves)
    {
        int count = wolves.Count;
        for (int i = 0; i < count; i++)
        {
            WolfPet wolf = wolves[i];
            if (wolf == null || wolf.Health <= 0)
            {
                continue;
            }

            wolf.ReassignFormation(count, i);
        }
    }

    private void ApplyWolf(Status targetStatus, SpellSegmentData segment)
    {
        if (segment.Active == SpellActiveId.Self)
        {
            targetStatus.ApplyWolfSelf();
            return;
        }

        SummonWolvesAt(targetStatus.transform.position, segment);
    }

    public void ApplyProjectileHitEffects(Status targetStatus, SpellSegmentData segment)
    {
        if (targetStatus == null || segment == null)
        {
            return;
        }

        if (segment.Shift > 0 && segment.Active != SpellActiveId.Self)
        {
            ApplyNonSelfShift(targetStatus, segment);
        }
    }

    private IEnumerator DespawnAfter(NetworkObject networkObject, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (networkObject != null && Runner != null)
        {
            Runner.Despawn(networkObject);
        }
    }

    private void ApplyShift(Status targetStatus, SpellSegmentData segment, bool selfShift)
    {
        if (targetStatus == null || segment == null)
        {
            return;
        }

        Vector2 direction;
        if (selfShift)
        {
            direction = GetMouseWorldPosition() - targetStatus.transform.position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = GetAimDirection();
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }

            Vector2 normalizedDirection = direction.normalized;
            float distance = selfShiftBaseDistance * Mathf.Max(1, segment.Shift);
            float speedMultiplier =
                1f + Mathf.Max(0, segment.Accelerate) * selfShiftSpeedPerAccelerate;
            float duration = selfShiftDuration / Mathf.Max(0.1f, speedMultiplier);

            if (targetStatus == status && player != null)
            {
                if (player.StartShiftDash(normalizedDirection, distance, duration))
                {
                    return;
                }
            }

            targetStatus.MoveBy(normalizedDirection * distance);
            return;
        }

        direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        ApplyNonSelfShift(targetStatus, segment, direction);
    }

    private void ApplyNonSelfShift(Status targetStatus, SpellSegmentData segment)
    {
        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        ApplyNonSelfShift(targetStatus, segment, direction);
    }

    private void ApplyNonSelfShift(
        Status targetStatus,
        SpellSegmentData segment,
        Vector2 direction
    )
    {
        if (targetStatus == null || segment == null)
        {
            return;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        Vector2 normalizedDirection = direction.normalized;
        float distance = selfShiftBaseDistance * Mathf.Max(1, segment.Shift);
        float speedMultiplier =
            1f + Mathf.Max(0, segment.Accelerate) * selfShiftSpeedPerAccelerate;
        float duration = selfShiftDuration / Mathf.Max(0.1f, speedMultiplier);

        Player targetPlayer = targetStatus.GetComponent<Player>();
        if (targetPlayer != null && targetPlayer.StartExternalShiftDash(
            normalizedDirection,
            distance,
            duration
        ))
        {
            return;
        }

        targetStatus.MoveBy(normalizedDirection * distance);
    }

    private IEnumerator ApplySmog(Vector2 center, SpellSegmentData segment)
    {
        GameObject smog = smogPrefab != null ? Instantiate(smogPrefab) : new GameObject("Smog");
        smog.transform.position = new Vector3(center.x, center.y, 20f);
        smog.transform.localScale = new Vector3(1.5f, 1.5f, 1) * segment.EffectiveRadius * 2f;
        SpriteRenderer renderer = smog.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = smog.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSmogSprite();
            renderer.color = new Color(0.08f, 0.08f, 0.08f, 1);
            renderer.sortingOrder = 1;
        }

        float endTime = Time.time + smogDuration + segment.Powerup3;
        while (Time.time < endTime)
        {
            ApplySmogTick(center, segment);
            yield return new WaitForSeconds(1f);
        }

        Destroy(smog);
    }

    private static Sprite GetSmogSprite()
    {
        if (smogSprite != null)
        {
            return smogSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        smogSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        return smogSprite;
    }

    private void ApplySmogTick(Vector2 center, SpellSegmentData segment)
    {
        int hitCount = GetAreaHits(center, segment.EffectiveRadius);
        appliedTargets.Clear();
        appliedStructures.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Status targetStatus = areaHitBuffer[i].GetComponentInParent<Status>();
            if (targetStatus != null)
            {
                if (appliedTargets.Contains(targetStatus))
                {
                    continue;
                }

                appliedTargets.Add(targetStatus);

                SpellImpact impact = segment.CreateImpact();
                impact.Damage = 0;
                impact.Heal = 0;
                impact.Cure = 0;
                impact.Barrier = 0;
                impact.Explode = 0;
                impact.Leech = 0;
                impact.Slaughter = 0;

                targetStatus.ApplySpellImpact(ref impact, status, defaultTouchRadius, spellTargetMask);
                continue;
            }

            DestructibleStructure targetStructure =
                areaHitBuffer[i].GetComponentInParent<DestructibleStructure>();
            if (targetStructure == null || appliedStructures.Contains(targetStructure))
            {
                continue;
            }

            appliedStructures.Add(targetStructure);

            SpellImpact structureImpact = segment.CreateImpact();
            structureImpact.Damage = 0;
            structureImpact.Heal = 0;
            structureImpact.Cure = 0;
            structureImpact.Barrier = 0;
            structureImpact.Explode = 0;
            structureImpact.Leech = 0;
            structureImpact.Slaughter = 0;

            targetStructure.ApplySpellImpact(
                ref structureImpact,
                status,
                defaultTouchRadius,
                spellTargetMask
            );
        }
    }

    private IEnumerator ApplyMagnet(Vector2 center, SpellSegmentData segment)
    {
        float endTime = Time.time + 5f + segment.Powerup3;
        float radius = segment.EffectiveRadius + segment.Magnet;
        float force = magnetBaseForce * Mathf.Max(1, segment.Magnet);
        GameObject magnetVisual = magnetPrefab != null ? Instantiate(magnetPrefab) : null;
        if (magnetVisual != null)
        {
            magnetVisual.transform.position = new Vector3(center.x, center.y, -10f);
            magnetVisual.transform.localScale = Vector3.one * radius * 2f;
        }

        while (Time.time < endTime)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, spellTargetMask);
            for (int i = 0; i < hits.Length; i++)
            {
                NetworkObject hitObject = hits[i].GetComponentInParent<NetworkObject>();
                if (hitObject != null && hitObject.InputAuthority == Object.InputAuthority)
                {
                    continue;
                }

                Status targetStatus = hits[i].GetComponentInParent<Status>();
                if (targetStatus == status)
                {
                    continue;
                }

                if (targetStatus != null)
                {
                    Vector2 pull = (center - (Vector2)targetStatus.transform.position).normalized;
                    targetStatus.Push(pull * force * Time.deltaTime);
                    continue;
                }

                Rigidbody2D body = hits[i].attachedRigidbody;
                if (body != null)
                {
                    Vector2 pull = (center - body.position).normalized * force;
                    body.AddForce(pull * Time.deltaTime, ForceMode2D.Force);
                    continue;
                }

                Transform hitTransform = hits[i].transform;
                if (targetStatus != null)
                {
                    hitTransform = targetStatus.transform;
                }

                hitTransform.position = Vector3.MoveTowards(
                    hitTransform.position,
                    center,
                    force * Time.deltaTime
                );
            }

            yield return null;
        }

        if (magnetVisual != null)
        {
            Destroy(magnetVisual);
        }
    }

    private void SpawnAreaVisual(
        Vector3 center,
        float radius,
        SpellActiveId active,
        Vector2 direction
    )
    {
        GameObject prefab = GetAreaVisualPrefab(active);
        GameObject visual = prefab != null ? Instantiate(prefab) : CreateDefaultAreaVisual(active);

        if (visual == null)
        {
            return;
        }

        Vector2 visualDirection =
            direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        if (active == SpellActiveId.Cone)
        {
            Vector2 forwardCenter = (Vector2)center + visualDirection * radius * 0.2f;
            visual.transform.position = new Vector3(forwardCenter.x, forwardCenter.y, -5f);
        }
        else
        {
            visual.transform.position = new Vector3(center.x, center.y, -5f);
        }

        visual.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(visualDirection.y, visualDirection.x) * Mathf.Rad2Deg
        );

        switch (active)
        {
            case SpellActiveId.Cone:
                visual.transform.localScale = Vector3.one * radius;
                break;
            case SpellActiveId.Donut:
                visual.transform.localScale = Vector3.one * radius * 2f;
                break;
            default:
                visual.transform.localScale = Vector3.one * radius * 2f;
                break;
        }

        Destroy(visual, 0.35f);
    }

    private GameObject CreateDefaultAreaVisual(SpellActiveId active)
    {
        if (active == SpellActiveId.Donut)
        {
            return CreateDefaultDonutVisual();
        }

        if (active == SpellActiveId.Cone)
        {
            return CreateDefaultConeVisual();
        }

        GameObject visual = new GameObject(active + "Visual");
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSmogSprite();
        renderer.color =
            active == SpellActiveId.Donut
                ? new Color(0.3f, 0.75f, 1f, 0.35f)
                : new Color(1f, 0.85f, 0.2f, 0.35f);
        renderer.sortingOrder = 1;
        return visual;
    }

    private GameObject CreateDefaultConeVisual()
    {
        GameObject visual = new GameObject("ConeVisual");
        MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();
        meshRenderer.material = GetAreaVisualMaterial();
        meshRenderer.sortingOrder = 1;

        float halfAngle = coneAngle * 0.5f * Mathf.Deg2Rad;
        int arcSteps = 24;
        Vector3[] vertices = new Vector3[arcSteps + 2];
        int[] triangles = new int[arcSteps * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i <= arcSteps; i++)
        {
            float t = i / (float)arcSteps;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }

        for (int i = 0; i < arcSteps; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        Mesh mesh = new Mesh
        {
            name = "ConeVisualMesh",
            vertices = vertices,
            triangles = triangles,
        };
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;

        return visual;
    }

    private GameObject CreateDefaultDonutVisual()
    {
        GameObject visual = new GameObject("DonutVisual");
        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 72;
        line.widthMultiplier = 0.08f;
        line.material = GetAreaVisualMaterial();
        line.startColor = new Color(0.3f, 0.75f, 1f, 0.9f);
        line.endColor = new Color(0.3f, 0.75f, 1f, 0.9f);
        line.sortingOrder = 1;

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f));
        }

        return visual;
    }

    private GameObject GetAreaVisualPrefab(SpellActiveId active)
    {
        switch (active)
        {
            case SpellActiveId.Donut:
                return donutVisualPrefab != null ? donutVisualPrefab : areaVisualPrefab;
            case SpellActiveId.Cone:
                return coneVisualPrefab != null ? coneVisualPrefab : areaVisualPrefab;
            default:
                return areaVisualPrefab;
        }
    }

    private static Material GetAreaVisualMaterial()
    {
        if (areaVisualMaterial != null)
        {
            return areaVisualMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        areaVisualMaterial =
            shader != null ? new Material(shader) : new Material(Shader.Find("Default-Line"));
        return areaVisualMaterial;
    }

    private int GetAreaHits(Vector2 center, float radius)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            center,
            radius,
            areaHitBuffer,
            spellTargetMask
        );

        while (hitCount == areaHitBuffer.Length)
        {
            areaHitBuffer = new Collider2D[areaHitBuffer.Length * 2];
            hitCount = Physics2D.OverlapCircleNonAlloc(
                center,
                radius,
                areaHitBuffer,
                spellTargetMask
            );
        }

        return hitCount;
    }

    private Vector2 GetAimDirection()
    {
        Vector2 fromPlayer = GetMouseWorldPosition() - transform.position;
        if (fromPlayer.sqrMagnitude <= 0.0001f && firePoint != null)
        {
            return -firePoint.up;
        }

        return fromPlayer.normalized;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Camera castCamera = playerCamera != null ? playerCamera : Camera.main;
        if (castCamera == null)
        {
            return transform.position;
        }

        Vector3 mousePosition = castCamera.ScreenToWorldPoint(ReadPointInput());
        mousePosition.z = 0f;
        return mousePosition;
    }

    private bool IsFirePressed()
    {
        InputAction fireAction = FireAction;
        return fireAction != null && fireAction.IsPressed();
    }

    private bool IsCastingBlockedByUI()
    {
        if (uiManager != null && uiManager.BlocksSpellCasting)
        {
            return true;
        }

        return IsPointerOverUI();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (Mouse.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        if (Touchscreen.current == null)
        {
            return false;
        }

        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (
                touch.press.isPressed
                && EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue())
            )
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 ReadPointInput()
    {
        InputAction pointAction = PointAction;
        return pointAction != null ? pointAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private InputAction GetOrCreateFallbackFireAction()
    {
        if (fallbackFireAction != null)
        {
            return fallbackFireAction;
        }

        fallbackFireAction = new InputAction(
            "Fire",
            InputActionType.Button,
            expectedControlType: "Button"
        );

        fallbackFireAction.AddBinding("<Mouse>/leftButton");
        fallbackFireAction.AddBinding("<Touchscreen>/primaryTouch/press");

        return fallbackFireAction;
    }

    private InputAction GetOrCreateFallbackPointAction()
    {
        if (fallbackPointAction != null)
        {
            return fallbackPointAction;
        }

        fallbackPointAction = new InputAction(
            "Point",
            InputActionType.PassThrough,
            expectedControlType: "Vector2"
        );

        fallbackPointAction.AddBinding("<Pointer>/position");
        fallbackPointAction.AddBinding("<Touchscreen>/primaryTouch/position");

        return fallbackPointAction;
    }
}
