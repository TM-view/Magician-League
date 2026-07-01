using UnityEngine;

public class SpellVfxBurst : MonoBehaviour
{
    private SpellProjectileVfxController controller;
    private float remainingTime;
    private bool followParent;

    public static void Play(
        Transform followTarget,
        Vector3 worldPosition,
        SpellSegmentData segment,
        Vector2 direction,
        float duration,
        float scale
    )
    {
        SpellVfxMask mask = SpellVfxMaskUtility.FromSegment(segment);
        if (mask == SpellVfxMask.None)
        {
            return;
        }

        if (mask.Has(SpellVfxMask.Smog))
        {
            duration = Mathf.Max(duration, 10f + (segment != null ? segment.Powerup3 : 0f));
        }

        GameObject burstObject = new GameObject("SpellVfxBurst");
        if (followTarget != null)
        {
            burstObject.transform.SetParent(followTarget, false);
            burstObject.transform.localPosition = Vector3.zero;
        }
        else
        {
            burstObject.transform.position = worldPosition;
        }

        burstObject.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        SpellVfxBurst burst = burstObject.AddComponent<SpellVfxBurst>();
        burst.Initialize(mask, direction, duration, followTarget != null);
    }

    private void Initialize(
        SpellVfxMask mask,
        Vector2 direction,
        float duration,
        bool shouldFollowParent
    )
    {
        remainingTime = Mathf.Max(0.1f, duration);
        followParent = shouldFollowParent;
        controller = gameObject.AddComponent<SpellProjectileVfxController>();
        controller.Configure(mask, direction);
    }

    private void Update()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime > 0f)
        {
            return;
        }

        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (followParent)
        {
            transform.localPosition = Vector3.zero;
        }
    }
}
