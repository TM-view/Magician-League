using UnityEngine;

public static class RuntimeVfxFactory
{
    private const int TextureSize = 32;
    public const int UnderfootOrder = 880;
    public const int BodyOrder = 930;
    public const int HeadOrder = 980;
    public const int ProjectileOrder = 960;
    public const int ProjectileTopOrder = 963;

    private static Sprite circleSprite;
    private static Sprite squareSprite;
    private static Sprite ringSprite;
    private static Material spriteMaterial;
    private static Material lineMaterial;
    private static UIManager cachedUiManager;
    private static float nextUiLookupTime;

    public static Sprite CircleSprite => circleSprite != null ? circleSprite : CreateCircleSprite();
    public static Sprite SquareSprite => squareSprite != null ? squareSprite : CreateSquareSprite();
    public static Sprite RingSprite => ringSprite != null ? ringSprite : CreateRingSprite();

    public static Material SpriteMaterial
    {
        get
        {
            if (spriteMaterial == null)
            {
                spriteMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            return spriteMaterial;
        }
    }

    public static Material LineMaterial
    {
        get
        {
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            return lineMaterial;
        }
    }

    public static SpriteRenderer AddSprite(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        int sortingOrder
    )
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sharedMaterial = SpriteMaterial;
        renderer.sortingOrder = sortingOrder;
        RuntimeVfxSortingAdapter adapter = child.AddComponent<RuntimeVfxSortingAdapter>();
        adapter.Initialize(renderer, null, sortingOrder);
        return renderer;
    }

    public static LineRenderer AddLine(
        Transform parent,
        string name,
        Color color,
        float width,
        int sortingOrder,
        int pointCount
    )
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        LineRenderer line = child.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.sharedMaterial = LineMaterial;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sortingOrder = sortingOrder;
        line.positionCount = Mathf.Max(2, pointCount);
        RuntimeVfxSortingAdapter adapter = child.AddComponent<RuntimeVfxSortingAdapter>();
        adapter.Initialize(null, line, sortingOrder);
        return line;
    }

    public static int GetRenderOrder(int baseOrder)
    {
        if (!IsLocalSpellBookOpen())
        {
            return baseOrder;
        }

        return Mathf.Min(4, baseOrder - 1000);
    }

    private static bool IsLocalSpellBookOpen()
    {
        if (cachedUiManager == null && Time.unscaledTime >= nextUiLookupTime)
        {
            nextUiLookupTime = Time.unscaledTime + 0.5f;
            cachedUiManager = Object.FindObjectOfType<UIManager>();
        }

        return cachedUiManager != null && cachedUiManager.SpellPanelOpen;
    }

    private static Sprite CreateCircleSprite()
    {
        circleSprite = CreateSprite("RuntimeVfx_Circle", DrawCircle);
        return circleSprite;
    }

    private static Sprite CreateSquareSprite()
    {
        squareSprite = CreateSprite("RuntimeVfx_Square", DrawSquare);
        return squareSprite;
    }

    private static Sprite CreateRingSprite()
    {
        ringSprite = CreateSprite("RuntimeVfx_Ring", DrawRing);
        return ringSprite;
    }

    private static Sprite CreateSprite(string name, System.Func<int, int, Color> drawer)
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                texture.SetPixel(x, y, drawer(x, y));
            }
        }

        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize
        );
        sprite.name = name;
        return sprite;
    }

    private static Color DrawCircle(int x, int y)
    {
        Vector2 center = new Vector2(TextureSize * 0.5f - 0.5f, TextureSize * 0.5f - 0.5f);
        float radius = TextureSize * 0.44f;
        float distance = Vector2.Distance(new Vector2(x, y), center);
        float alpha = Mathf.Clamp01(radius + 1f - distance);
        return new Color(1f, 1f, 1f, alpha);
    }

    private static Color DrawSquare(int x, int y)
    {
        int padding = 3;
        bool inside = x >= padding && x < TextureSize - padding && y >= padding && y < TextureSize - padding;
        return inside ? Color.white : Color.clear;
    }

    private static Color DrawRing(int x, int y)
    {
        Vector2 center = new Vector2(TextureSize * 0.5f - 0.5f, TextureSize * 0.5f - 0.5f);
        float distance = Vector2.Distance(new Vector2(x, y), center);
        float outer = TextureSize * 0.45f;
        float inner = TextureSize * 0.34f;
        float outerAlpha = Mathf.Clamp01(outer + 1f - distance);
        float innerAlpha = Mathf.Clamp01(distance - inner);
        return new Color(1f, 1f, 1f, Mathf.Min(outerAlpha, innerAlpha));
    }
}

public class RuntimeVfxSortingAdapter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private LineRenderer lineRenderer;
    private int baseOrder;
    private int appliedOrder = int.MinValue;

    public void Initialize(SpriteRenderer sprite, LineRenderer line, int order)
    {
        spriteRenderer = sprite;
        lineRenderer = line;
        baseOrder = order;
        ApplyOrder();
    }

    private void LateUpdate()
    {
        ApplyOrder();
    }

    private void ApplyOrder()
    {
        int order = RuntimeVfxFactory.GetRenderOrder(baseOrder);
        if (order == appliedOrder)
        {
            return;
        }

        appliedOrder = order;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }

        if (lineRenderer != null)
        {
            lineRenderer.sortingOrder = order;
        }
    }
}
