using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class WolfPet : NetworkBehaviour
{
    [SerializeField]
    private int maxHealth = 30;

    [SerializeField]
    private int attackDamage = 10;

    [SerializeField]
    private float attackCooldown = 0.5f;

    [SerializeField]
    private float attackRangeBase = 10f;

    [SerializeField]
    private float attackRangePerExtraWolf = 5f;

    [SerializeField]
    private float followRadius = 2f;

    [SerializeField]
    private float minimumOwnerDistance = 1.5f;

    [SerializeField]
    private float followArriveDistance = 0.12f;

    [SerializeField]
    private float ownerDistanceBuffer = 0.25f;

    [SerializeField]
    private float followSpeed = 5f;

    [SerializeField]
    private float chargeSpeed = 9f;

    [SerializeField]
    private float targetSearchInterval = 0.25f;

    [SerializeField]
    private LayerMask targetMask = ~0;

    [Header("Facing")]
    [SerializeField]
    private bool faceOwnerWhileFollowing = true;

    [SerializeField]
    private float facingAngleOffset = -90f;

    [Networked]
    public int Health { get; private set; }

    [Networked]
    private PlayerRef Owner { get; set; }

    [Networked]
    private TickTimer LifeTimer { get; set; }

    private readonly Collider2D[] targetBuffer = new Collider2D[32];
    private readonly Dictionary<Status, float> hitCooldowns = new Dictionary<Status, float>();
    private readonly Dictionary<WolfPet, float> wolfHitCooldowns = new Dictionary<WolfPet, float>();
    private Rigidbody2D body;
    private Collider2D wolfCollider;
    private Status ownerStatus;
    private LookAtMouse ownerLookAtMouse;
    private Status currentStatusTarget;
    private WolfPet currentWolfTarget;
    private float attackRange;
    private float nextTargetSearchTime;
    private float nextOwnerResolveTime;
    private int followIndex;
    private int followCount = 1;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        wolfCollider = GetComponent<Collider2D>();

        if (wolfCollider != null)
        {
            wolfCollider.isTrigger = true;
        }

        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = false;
            body.constraints = RigidbodyConstraints2D.None;
        }
    }

    public void Initialize(
        Status owner,
        PlayerRef ownerRef,
        int wolfCount,
        int wolfIndex,
        float duration
    )
    {
        ownerStatus = owner;
        ownerLookAtMouse = ownerStatus != null ? ownerStatus.GetComponent<LookAtMouse>() : null;
        Owner = ownerRef;
        followCount = Mathf.Max(1, wolfCount);
        followIndex = Mathf.Clamp(wolfIndex, 0, followCount - 1);
        attackRange = attackRangeBase + Mathf.Max(0, followCount - 1) * attackRangePerExtraWolf;
        Health = Mathf.Max(1, maxHealth);

        if (Runner != null)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.1f, duration));
        }
    }

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (Owner == default)
        {
            Owner = Object.InputAuthority;
        }

        if (Health <= 0)
        {
            Health = Mathf.Max(1, maxHealth);
        }

        if (attackRange <= 0f)
        {
            attackRange = attackRangeBase;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!IsStateAuthorityReady())
        {
            return;
        }

        if (LifeTimer.Expired(Runner) || Health <= 0)
        {
            Runner.Despawn(Object);
            return;
        }

        if (ownerStatus == null && !TryResolveOwnerStatus())
        {
            Move(Vector2.zero, followSpeed);
            return;
        }

        UpdateTarget();

        if (currentStatusTarget != null)
        {
            MoveToward(currentStatusTarget.transform.position, chargeSpeed);
            return;
        }

        if (currentWolfTarget != null)
        {
            MoveToward(currentWolfTarget.transform.position, chargeSpeed);
            return;
        }

        FollowOwner();
    }

    public bool IsOwnedBy(PlayerRef playerRef)
    {
        return Owner == playerRef;
    }

    public void ReassignFormation(int wolfCount, int wolfIndex)
    {
        followCount = Mathf.Max(1, wolfCount);
        followIndex = Mathf.Clamp(wolfIndex, 0, followCount - 1);
        attackRange = attackRangeBase + Mathf.Max(0, followCount - 1) * attackRangePerExtraWolf;
    }

    public int ApplySpellImpact(ref SpellImpact impact, Status caster)
    {
        if (caster != null)
        {
            NetworkObject casterObject = caster.GetComponent<NetworkObject>();
            if (casterObject != null && casterObject.InputAuthority == Owner)
            {
                return 0;
            }
        }

        if (Object == null || !Object.IsValid)
        {
            return 0;
        }

        if (!Object.HasStateAuthority)
        {
            RPC_TakeDamage(Mathf.Max(0, impact.Damage));
            return 0;
        }

        return TakeDamage(Mathf.Max(0, impact.Damage));
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TakeDamage(int damage)
    {
        TakeDamage(damage);
    }

    private int TakeDamage(int damage)
    {
        if (damage <= 0 || Health <= 0)
        {
            return 0;
        }

        int appliedDamage = Mathf.Min(Health, damage);
        Health -= appliedDamage;
        if (Health <= 0 && Runner != null && Object != null)
        {
            Runner.Despawn(Object);
        }

        return appliedDamage;
    }

    private void UpdateTarget()
    {
        if (currentStatusTarget != null && IsStatusTargetValid(currentStatusTarget))
        {
            return;
        }

        if (currentWolfTarget != null && IsWolfTargetValid(currentWolfTarget))
        {
            return;
        }

        currentStatusTarget = null;
        currentWolfTarget = null;
        if (Time.time < nextTargetSearchTime)
        {
            return;
        }

        nextTargetSearchTime = Time.time + Mathf.Max(0.05f, targetSearchInterval);
        FindBestTarget();
    }

    private void FindBestTarget()
    {
        Vector2 center = ownerStatus.transform.position;
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, attackRange, targetBuffer, targetMask);
        Status bestStatusTarget = null;
        WolfPet bestWolfTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Status statusTarget = targetBuffer[i].GetComponentInParent<Status>();
            if (IsStatusTargetValid(statusTarget))
            {
                float statusDistance = Vector2.SqrMagnitude(
                    statusTarget.transform.position - transform.position
                );
                if (statusDistance < bestDistance)
                {
                    bestDistance = statusDistance;
                    bestStatusTarget = statusTarget;
                    bestWolfTarget = null;
                }

                continue;
            }

            WolfPet wolfTarget = targetBuffer[i].GetComponentInParent<WolfPet>();
            if (!IsWolfTargetValid(wolfTarget))
            {
                continue;
            }

            float wolfDistance = Vector2.SqrMagnitude(
                wolfTarget.transform.position - transform.position
            );
            if (wolfDistance >= bestDistance)
            {
                continue;
            }

            bestDistance = wolfDistance;
            bestStatusTarget = null;
            bestWolfTarget = wolfTarget;
        }

        currentStatusTarget = bestStatusTarget;
        currentWolfTarget = bestWolfTarget;
    }

    private bool IsStatusTargetValid(Status target)
    {
        if (target == null || target == ownerStatus)
        {
            return false;
        }

        NetworkObject targetObject = target.GetComponent<NetworkObject>();
        if (targetObject != null && targetObject.InputAuthority == Owner)
        {
            return false;
        }

        if (ownerStatus == null)
        {
            return false;
        }

        float distanceFromOwner = Vector2.Distance(
            ownerStatus.transform.position,
            target.transform.position
        );
        return distanceFromOwner <= attackRange;
    }

    private bool IsWolfTargetValid(WolfPet target)
    {
        if (target == null || target == this || target.Health <= 0)
        {
            return false;
        }

        if (target.IsOwnedBy(Owner))
        {
            return false;
        }

        if (ownerStatus == null)
        {
            return false;
        }

        float distanceFromOwner = Vector2.Distance(
            ownerStatus.transform.position,
            target.transform.position
        );
        return distanceFromOwner <= attackRange;
    }

    private void FollowOwner()
    {
        Vector2 ownerPosition = ownerStatus.transform.position;
        FaceWithOwner();

        Vector2 toWolf = (Vector2)transform.position - ownerPosition;
        float minDistance = Mathf.Max(0.1f, minimumOwnerDistance);
        float buffer = Mathf.Max(0f, ownerDistanceBuffer);

        if (toWolf.magnitude < minDistance - buffer)
        {
            Vector2 away = toWolf.sqrMagnitude > 0.0001f ? toWolf.normalized : GetFollowOffset().normalized;
            MoveToward(ownerPosition + away * minDistance, followSpeed);
            return;
        }

        Vector2 followPosition = ownerPosition + GetFollowOffset();
        MoveToward(followPosition, followSpeed);
    }

    private Vector2 GetFollowOffset()
    {
        float angle = followIndex / (float)Mathf.Max(1, followCount) * Mathf.PI * 2f;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Max(followRadius, minimumOwnerDistance);
    }

    private void MoveToward(Vector2 targetPosition, float speed)
    {
        Vector2 currentPosition = transform.position;
        if (Vector2.Distance(currentPosition, targetPosition) <= Mathf.Max(0.01f, followArriveDistance))
        {
            return;
        }

        float step = Mathf.Max(0f, speed) * Runner.DeltaTime;
        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, step);

        if (Vector2.SqrMagnitude(nextPosition - currentPosition) <= 0.000001f)
        {
            return;
        }

        SetPosition(nextPosition);
    }

    private void Move(Vector2 direction, float speed)
    {
        Vector2 currentPosition = transform.position;
        Vector2 nextPosition =
            currentPosition + direction * Mathf.Max(0f, speed) * Runner.DeltaTime;

        SetPosition(nextPosition);
    }

    private void SetPosition(Vector2 nextPosition)
    {
        if (body != null)
        {
            body.position = nextPosition;
        }

        transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
    }

    private void FaceWithOwner()
    {
        if (!faceOwnerWhileFollowing)
        {
            return;
        }

        if (ownerLookAtMouse == null && ownerStatus != null)
        {
            ownerLookAtMouse = ownerStatus.GetComponent<LookAtMouse>();
        }

        if (ownerLookAtMouse == null)
        {
            FaceToward(ownerStatus.transform.position);
            return;
        }

        transform.rotation = Quaternion.Euler(
            0f,
            0f,
            ownerLookAtMouse.FacingAngle + facingAngleOffset
        );
    }

    private void FaceToward(Vector2 targetPosition)
    {
        if (!faceOwnerWhileFollowing)
        {
            return;
        }

        Vector2 direction = targetPosition - (Vector2)transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + facingAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHitTarget(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHitTarget(other);
    }

    private void TryHitTarget(Collider2D other)
    {
        if (!IsStateAuthorityReady() || other == null)
        {
            return;
        }

        Status statusTarget = other.GetComponentInParent<Status>();
        if (IsStatusTargetValid(statusTarget))
        {
            if (IsHitOnCooldown(statusTarget))
            {
                return;
            }

            hitCooldowns[statusTarget] = Time.time;
            statusTarget.RPC_TakeDamage(Mathf.Max(0, attackDamage));
            return;
        }

        WolfPet wolfTarget = other.GetComponentInParent<WolfPet>();
        if (!IsWolfTargetValid(wolfTarget) || IsWolfHitOnCooldown(wolfTarget))
        {
            return;
        }

        wolfHitCooldowns[wolfTarget] = Time.time;
        SpellImpact impact = new SpellImpact { Damage = Mathf.Max(0, attackDamage) };
        wolfTarget.ApplySpellImpact(ref impact, ownerStatus);
    }

    private bool IsHitOnCooldown(Status target)
    {
        return hitCooldowns.TryGetValue(target, out float lastHitTime)
            && Time.time - lastHitTime < Mathf.Max(0.05f, attackCooldown);
    }

    private bool IsWolfHitOnCooldown(WolfPet target)
    {
        return wolfHitCooldowns.TryGetValue(target, out float lastHitTime)
            && Time.time - lastHitTime < Mathf.Max(0.05f, attackCooldown);
    }

    private bool TryResolveOwnerStatus()
    {
        if (ownerStatus != null)
        {
            return true;
        }

        if (Time.time < nextOwnerResolveTime)
        {
            return false;
        }

        nextOwnerResolveTime = Time.time + 0.25f;
        Status[] statuses = FindObjectsOfType<Status>();
        for (int i = 0; i < statuses.Length; i++)
        {
            Status candidate = statuses[i];
            NetworkObject candidateObject =
                candidate != null ? candidate.GetComponent<NetworkObject>() : null;
            if (candidateObject == null || candidateObject.InputAuthority != Owner)
            {
                continue;
            }

            ownerStatus = candidate;
            ownerLookAtMouse = ownerStatus.GetComponent<LookAtMouse>();
            return true;
        }

        return false;
    }

    private bool IsStateAuthorityReady()
    {
        return Object != null && Object.IsValid && Object.HasStateAuthority;
    }
}
