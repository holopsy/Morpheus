using System;
using UnityEngine;

public enum WallChannel { Red, Purple, Cyan, Blue, Yellow }
public enum WallMoveAxis { Vertical, Horizontal }

[Serializable]
public class WallVisualSet
{
    public WallChannel channel;
    public Sprite top;
    public Sprite middle;
    public Sprite bottom;
}

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MovableWall2D : MonoBehaviour
{
    [Header("Visuals")]
    public Transform visualRoot;                 // empty child transform
    public WallChannel channel = WallChannel.Red;
    public WallVisualSet[] visualSets;           // assign sets in inspector

    [Header("Build")]
    [Min(2)] public int heightSegments = 4;      // bottom + middles + top
    public Vector2 visualOffset = Vector2.zero;

    [Header("Movement")]
    public WallMoveAxis moveAxis = WallMoveAxis.Vertical;

    [Tooltip("If Vertical: how many segments UP to move when opened.\nIf Horizontal: how many segments RIGHT to move when opened.")]
    [Min(0)] public int moveDistanceSegments = 4;

    public float moveSpeed = 4f;
    public bool startRaised = false;             // raised=open (vertical) OR moved-right=open (horizontal)

    [Header("Crush")]
    public bool crushKillsPlayer = true;
    public bool crushOnlyWhileMoving = true;
    public string[] crushTags = { "Player" };
    public Vector2 crushPadding = new Vector2(0.02f, 0.02f);

    [Header("Optional")]
    public bool rebuildInPlay = false;

    BoxCollider2D box;
    Rigidbody2D rb;

    float segmentHeight;
    float segmentWidth;

    Vector3 closedPos;
    Vector3 openPos;

    bool isOpen;
    bool isMoving;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        SetupRB();

        if (!visualRoot)
        {
            var found = transform.Find("VisualRoot");
            if (found) visualRoot = found;
        }

        Rebuild();
        CachePositions();
        ApplyStartState();
    }

    void OnEnable()
    {
        if (!box) box = GetComponent<BoxCollider2D>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        SetupRB();

        Rebuild();
        CachePositions();
        ApplyStartState();
    }

    void SetupRB()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Rebuild();
            CachePositions();
            ApplyStartState();
        }
#endif

        if (Application.isPlaying && rebuildInPlay)
        {
            Rebuild();
            CachePositions();
            rebuildInPlay = false;
        }
    }

    WallVisualSet GetSet()
    {
        if (visualSets == null) return null;
        for (int i = 0; i < visualSets.Length; i++)
            if (visualSets[i].channel == channel)
                return visualSets[i];
        return null;
    }

    public void Rebuild()
    {
        if (!box) box = GetComponent<BoxCollider2D>();
        if (!visualRoot) return;

        var set = GetSet();
        if (set == null || set.middle == null || set.top == null || set.bottom == null)
            return;

        // Clear old visuals
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

        segmentHeight = set.middle.bounds.size.y;
        segmentWidth  = set.middle.bounds.size.x;

        // Build vertical segments (same as before)
        for (int i = 0; i < heightSegments; i++)
        {
            Sprite s = (i == 0) ? set.bottom : (i == heightSegments - 1) ? set.top : set.middle;

            var go = new GameObject($"Seg_{i}");
            go.transform.SetParent(visualRoot, false);
            go.transform.localPosition = new Vector3(visualOffset.x, (i * segmentHeight) + visualOffset.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
        }

        // Collider matches the pillar size
        float totalHeight = heightSegments * segmentHeight;
        box.size = new Vector2(segmentWidth, totalHeight);
        box.offset = new Vector2(0f, (totalHeight * 0.5f) - (segmentHeight * 0.5f));
    }

    void CachePositions()
    {
        closedPos = transform.position;

        Vector3 delta;
        if (moveAxis == WallMoveAxis.Vertical)
            delta = Vector3.up * (moveDistanceSegments * segmentHeight);
        else
            delta = Vector3.right * (moveDistanceSegments * segmentWidth);

        openPos = closedPos + delta;
    }

    void ApplyStartState()
    {
        isOpen = startRaised;
        rb.position = isOpen ? (Vector2)openPos : (Vector2)closedPos;
    }

    // Open/close API (keeps your existing button/plate scripts working)
    public void SetRaised(bool raised)
    {
        if (!Application.isPlaying)
        {
            startRaised = raised;
            ApplyStartState();
            return;
        }

        isOpen = raised;
        StopAllCoroutines();
        StartCoroutine(MoveTo(isOpen ? openPos : closedPos));
    }

    public void Toggle() => SetRaised(!isOpen);

    System.Collections.IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;

        while (Vector2.Distance(rb.position, (Vector2)target) > 0.001f)
        {
            Vector2 next = Vector2.MoveTowards(rb.position, (Vector2)target, moveSpeed * Time.deltaTime);
            rb.MovePosition(next);

            if (crushKillsPlayer && (!crushOnlyWhileMoving || isMoving))
                CrushCheckAtPosition(next);

            yield return null;
        }

        rb.MovePosition((Vector2)target);

        if (crushKillsPlayer && (!crushOnlyWhileMoving || isMoving))
            CrushCheckAtPosition((Vector2)target);

        isMoving = false;
    }

    void CrushCheckAtPosition(Vector2 wallPos)
    {
        Vector2 worldCenter = wallPos + box.offset;
        Vector2 size = box.size + crushPadding;

        var hits = Physics2D.OverlapBoxAll(worldCenter, size, 0f);
        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h) continue;

            if (!HasAllowedTag(h)) continue;

            var ph = h.GetComponent<PlayerHealth>() ?? h.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.Kill();
                return;
            }
        }
    }

    bool HasAllowedTag(Collider2D c)
    {
        if (crushTags == null || crushTags.Length == 0) return false;
        for (int i = 0; i < crushTags.Length; i++)
            if (c.CompareTag(crushTags[i]))
                return true;
        return false;
    }
}
