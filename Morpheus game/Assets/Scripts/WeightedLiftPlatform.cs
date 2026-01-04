using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class WeightedLiftPlatform : MonoBehaviour
{
    [Header("Visual Build (repeat 1 sprite)")]
    public Transform visualRoot;           // child transform that holds sprite tiles
    public Sprite tileSprite;              // sprite to repeat horizontally
    [Min(1)] public int widthSegments = 4; // how many tiles wide

    [Header("Tile Size (must match your art)")]
    [Tooltip("Pixels Per Unit used by the sprite sheet (you said PPU = 8).")]
    public float pixelsPerUnit = 8f;

    [Tooltip("Tile width in pixels (e.g. 16 if each tile is 16x16).")]
    public int spritePixelWidth = 16;

    [Tooltip("Tile height in pixels (e.g. 16 if each tile is 16x16).")]
    public int spritePixelHeight = 16;

    [Header("Colliders")]
    [Tooltip("Height of the solid collider you stand on (world units).")]
    public float solidColliderHeight = 0.30f;

    [Tooltip("How thick the heavy detector strip is (world units).")]
    public float detectorHeight = 0.12f;

    [Tooltip("How far above the solid top the detector starts.")]
    public float detectorYOffset = 0.02f;

    [Tooltip("Inset the detector from left/right edges to avoid corner touches counting.")]
    public float detectorSideInset = 0.08f;

    [Header("Lift Travel")]
    [Min(0)] public int upSegments = 4;
    [Min(0)] public int downSegments = 0;

    [Tooltip("Height of 1 segment in world units. If 0, uses one tile height.")]
    public float segmentHeight = 0f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;

    [Header("Tags")]
    [Tooltip("Tag of the heavy pushable block.")]
    public string heavyBlockTag = "BigBox";

    [Header("Respawn")]
    public bool resetOnCheckpointRespawn = true;

    // Components
    Rigidbody2D rb;
    BoxCollider2D solidCol;
    BoxCollider2D detectorCol;

    // State
    Vector2 startPos, topPos, bottomPos;
    bool plateActive;
    int heavyCount;

    float TileW => spritePixelWidth / Mathf.Max(0.0001f, pixelsPerUnit);
    float TileH => spritePixelHeight / Mathf.Max(0.0001f, pixelsPerUnit);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        solidCol = GetComponent<BoxCollider2D>();
        SetupRB();

        EnsureChildren();
        Rebuild();
        CacheTravelPositions();
    }

    void OnEnable()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!solidCol) solidCol = GetComponent<BoxCollider2D>();
        SetupRB();

        EnsureChildren();
        Rebuild();
        CacheTravelPositions();

        if (resetOnCheckpointRespawn)
            CheckpointEvents.OnPlayerRespawned += ResetPlatform;
    }

    void OnDisable()
    {
        if (resetOnCheckpointRespawn)
            CheckpointEvents.OnPlayerRespawned -= ResetPlatform;
    }

    void SetupRB()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void EnsureChildren()
    {
        // VisualRoot
        if (!visualRoot)
        {
            var found = transform.Find("VisualRoot");
            if (found) visualRoot = found;
        }
        if (!visualRoot)
        {
            var vr = new GameObject("VisualRoot");
            vr.transform.SetParent(transform, false);
            visualRoot = vr.transform;
        }

        // HeavyDetector child with trigger collider + forwarder
        var hd = transform.Find("HeavyDetector");
        if (!hd)
        {
            var go = new GameObject("HeavyDetector");
            go.transform.SetParent(transform, false);
            hd = go.transform;
        }

        detectorCol = hd.GetComponent<BoxCollider2D>();
        if (!detectorCol) detectorCol = hd.gameObject.AddComponent<BoxCollider2D>();
        detectorCol.isTrigger = true;

        var fwd = hd.GetComponent<HeavyDetectorForwarder>();
        if (!fwd) fwd = hd.gameObject.AddComponent<HeavyDetectorForwarder>();
        fwd.parent = this;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureChildren();
            Rebuild();
            CacheTravelPositions();
        }
#endif
    }

    public void Rebuild()
    {
        if (!visualRoot || !tileSprite) return;

        // Clear existing tiles
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            var child = visualRoot.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child.gameObject);
            else Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }

        float w = TileW;
        float totalWidth = widthSegments * w;

        // Build centered tiles
        float leftX = -totalWidth * 0.5f + w * 0.5f;
        for (int i = 0; i < widthSegments; i++)
        {
            var go = new GameObject($"Tile_{i}");
            go.transform.SetParent(visualRoot, false);
            go.transform.localPosition = new Vector3(leftX + i * w, 0f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tileSprite;
        }

        // Solid collider (stand surface)
        solidCol.offset = new Vector2(0f, solidColliderHeight * 0.5f);
        solidCol.size = new Vector2(totalWidth, solidColliderHeight);

        // Heavy detector strip (on top)
        float detectorWidth = totalWidth - detectorSideInset * 2f;
        if (detectorWidth < 0.01f) detectorWidth = 0.01f;

        detectorCol.size = new Vector2(detectorWidth, detectorHeight);
        detectorCol.offset = new Vector2(0f, solidColliderHeight + detectorYOffset + detectorHeight * 0.5f);
    }

    void CacheTravelPositions()
    {
        startPos = rb.position;

        float segH = (segmentHeight > 0f) ? segmentHeight : TileH;
        topPos = startPos + Vector2.up * (upSegments * segH);
        bottomPos = startPos + Vector2.down * (downSegments * segH);
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying) return;

        Vector2 target;

        if (plateActive)
            target = topPos;
        else if (heavyCount > 0)
            target = bottomPos;
        else
            return; // pause where it is

        Vector2 next = Vector2.MoveTowards(rb.position, target, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
    }

    // Called by your pressure plate script
    public void SetPlateActive(bool active)
    {
        plateActive = active;
    }

    // Called by the detector forwarder
    public void RegisterHeavyEnter(Collider2D other)
    {
        if (other.CompareTag(heavyBlockTag))
            heavyCount++;
    }

    public void RegisterHeavyExit(Collider2D other)
    {
        if (other.CompareTag(heavyBlockTag))
            heavyCount = Mathf.Max(0, heavyCount - 1);
    }

    void ResetPlatform()
    {
        plateActive = false;
        heavyCount = 0;
        rb.position = startPos;
    }

    // Child helper to forward trigger events to this script (keeps everything in one file)
    public class HeavyDetectorForwarder : MonoBehaviour
    {
        public WeightedLiftPlatform parent;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (parent) parent.RegisterHeavyEnter(other);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (parent) parent.RegisterHeavyExit(other);
        }
    }
}
