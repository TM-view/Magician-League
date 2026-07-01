using Fusion;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class Player : NetworkBehaviour
{
    [SerializeField]
    private Status playerStatus;

    [SerializeField]
    private float wetAcceleration = 12f;

    [SerializeField]
    private float wetFriction = 2.5f;

    [SerializeField]
    private float wetMaxSpeedMultiplier = 2f;

    [SerializeField]
    private LayerMask solidCollisionMask = ~0;

    [SerializeField]
    private float maxMovementStep = 0.08f;

    [SerializeField]
    private float depenetrationPadding = 0.002f;

    [SerializeField]
    private int depenetrationIterations = 4;

    private Vector3 wetSlipVelocity;
    private Vector3 shiftDashVelocity;
    private float shiftDashTimeRemaining;
    private Collider2D playerCollider;
    private readonly Collider2D[] overlapBuffer = new Collider2D[16];

    private void Awake()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<Status>();
        }

        playerCollider = GetComponent<Collider2D>();
    }

    public override void Spawned()
    {
        // PlayerDataManager example code was intentionally left out of runtime movement.
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
        {
            return;
        }

        Vector3 move = Vector3.zero;
        if (GetInput(out NetworkInputData data))
        {
            move = new Vector3(data.Direction.x, data.Direction.y, 0f);
        }

        if (playerStatus == null || !playerStatus.CanMove)
        {
            wetSlipVelocity = Vector3.zero;
            shiftDashVelocity = Vector3.zero;
            shiftDashTimeRemaining = 0f;
            return;
        }

        float deltaTime = Runner.DeltaTime;
        if (MoveWithShiftDash(deltaTime))
        {
            return;
        }

        if (playerStatus.IsWetting)
        {
            MoveWithWetting(move, deltaTime);
            return;
        }

        wetSlipVelocity = Vector3.zero;
        MoveWithSolidCollision(move.normalized * playerStatus.speed * deltaTime);
    }

    public bool StartShiftDash(Vector2 direction, float distance, float duration)
    {
        if (!Object.HasInputAuthority)
        {
            return false;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        float fallbackDeltaTime = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;
        float safeDuration = Mathf.Max(duration, fallbackDeltaTime);
        shiftDashVelocity = (Vector3)(direction.normalized * Mathf.Max(0f, distance) / safeDuration);
        shiftDashTimeRemaining = safeDuration;
        wetSlipVelocity = Vector3.zero;
        return true;
    }

    public bool StartExternalShiftDash(Vector2 direction, float distance, float duration)
    {
        if (Object == null)
        {
            return false;
        }

        if (Object.HasInputAuthority)
        {
            return StartShiftDash(direction, distance, duration);
        }

        RPC_StartExternalShiftDash(direction, distance, duration);
        return true;
    }

    private bool MoveWithShiftDash(float deltaTime)
    {
        if (shiftDashTimeRemaining <= 0f)
        {
            shiftDashVelocity = Vector3.zero;
            return false;
        }

        float stepTime = Mathf.Min(deltaTime, shiftDashTimeRemaining);
        MoveWithSolidCollision(shiftDashVelocity * stepTime);
        shiftDashTimeRemaining -= stepTime;

        if (shiftDashTimeRemaining <= 0f)
        {
            shiftDashVelocity = Vector3.zero;
        }

        return true;
    }

    private void MoveWithWetting(Vector3 move, float deltaTime)
    {
        if (move.sqrMagnitude > 0.0001f)
        {
            wetSlipVelocity += move.normalized * playerStatus.speed * wetAcceleration * deltaTime;
        }
        else
        {
            wetSlipVelocity = Vector3.MoveTowards(
                wetSlipVelocity,
                Vector3.zero,
                wetFriction * deltaTime
            );
        }

        float maxSpeed = playerStatus.speed * wetMaxSpeedMultiplier;
        wetSlipVelocity = Vector3.ClampMagnitude(wetSlipVelocity, maxSpeed);
        MoveWithSolidCollision(wetSlipVelocity * deltaTime);
    }

    private void MoveWithSolidCollision(Vector3 delta)
    {
        if (delta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        if (playerCollider == null || solidCollisionMask.value == 0)
        {
            transform.position += delta;
            return;
        }

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(delta.magnitude / Mathf.Max(0.01f, maxMovementStep))
        );
        Vector3 step = delta / steps;

        for (int i = 0; i < steps; i++)
        {
            transform.position += step;
            ResolveSolidOverlaps();
        }
    }

    private void ResolveSolidOverlaps()
    {
        int iterations = Mathf.Max(1, depenetrationIterations);
        for (int i = 0; i < iterations; i++)
        {
            Physics2D.SyncTransforms();

            int overlapCount = OverlapSolidColliders();
            bool moved = false;
            for (int j = 0; j < overlapCount; j++)
            {
                Collider2D other = overlapBuffer[j];
                if (!IsSolidCollider(other))
                {
                    continue;
                }

                ColliderDistance2D distance = playerCollider.Distance(other);
                if (!distance.isOverlapped || distance.normal.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                Vector2 correction =
                    distance.normal * (distance.distance - Mathf.Max(0f, depenetrationPadding));
                transform.position += (Vector3)correction;
                moved = true;
            }

            if (!moved)
            {
                break;
            }
        }

        Physics2D.SyncTransforms();
    }

    private int OverlapSolidColliders()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(solidCollisionMask);
        filter.useTriggers = false;
        return playerCollider.OverlapCollider(filter, overlapBuffer);
    }

    private bool IsSolidCollider(Collider2D other)
    {
        if (other == null || other == playerCollider || other.isTrigger)
        {
            return false;
        }

        if (other.transform == transform || other.transform.IsChildOf(transform))
        {
            return false;
        }

        int layerMask = 1 << other.gameObject.layer;
        return (solidCollisionMask.value & layerMask) != 0;
    }

    [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
    private void RPC_StartExternalShiftDash(Vector2 direction, float distance, float duration)
    {
        StartShiftDash(direction, distance, duration);
    }
}
