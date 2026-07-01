using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerCollisionDamageHitbox : MonoBehaviour
{
    [SerializeField]
    private Status ownerStatus;

    private Collider2D hitboxCollider;

    public Collider2D HitboxCollider => hitboxCollider;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }

        if (ownerStatus == null)
        {
            ownerStatus = GetComponentInParent<Status>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerStatus == null)
        {
            return;
        }

        ownerStatus.TryApplyNormalCollisionDamageFromHitbox(this, other);
    }
}
