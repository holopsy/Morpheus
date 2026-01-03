using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PowerFormController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Wall Check (prevents sticking)")]
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float wallCheckRadius = 0.15f;
    public LayerMask wallLayer;

    [Header("Carrying System")]
    public Transform carryPoint;
    public LayerMask blockLayer;   // layer of pick-up blocks
    public float pickupRadius = 1f;

    [Header("Support Check")]
    [Tooltip("Require ground under the placed block (prevents placing into 'void' behind walls).")]
    public bool requireSupportUnderBlock = true;

    [Tooltip("How far down to check for support under the block.")]
    public float supportCheckDepth = 0.35f;

    [Tooltip("Layers considered solid support (usually Ground + Wall if walls are also solid floor).")]
    public LayerMask supportLayers;

    [Tooltip("If true, drop returns the block to the exact position it was picked from. If false, drops near you safely.")]
    public bool returnToPickupPosition = false;

    [Tooltip("Horizontal distance in front of player to try placing the block.")]
    public float placeForwardDistance = 0.8f;

    [Header("Safe Drop / Preview")]
    [Tooltip("How much extra padding to add when checking if a placement overlaps colliders.")]
    public float placementPadding = 0.02f;

    [Tooltip("Preview line width in world units.")]
    public float previewLineWidth = 0.04f;

    [Tooltip("How far above ground we allow search offsets.")]
    public float maxPreviewUp = 1.2f;

    [Header("Push Detection")]
    public LayerMask pushBlockLayer;
    public Vector2 pushCheckSize = new Vector2(0.45f, 0.8f);
    public float pushCheckDistance = 0.35f;
    public float inputThreshold = 0.2f;

    [Header("Visuals / Animator")]
    public Transform visualToFlip;
    public Animator animator;
    public string pushingBool = "IsPushing";

    [Header("Spawn")]
    public float spawnLockDuration = 0.6f;
    public string spawnStateName = "Spawn_Powerform";

    // Internal
    private Rigidbody2D rb;
    private int facing = 1;
    private float moveInput;
    private bool isGrounded;
    private bool inSpawn;
    private float spawnUnlockTime;

    private bool isPushing;

    private bool onLeftWall, onRightWall;

    private GameObject carriedObject;
    private Vector3 pickedWorldPosition;
    private Transform pickedOriginalParent;

    private Collider2D[] playerColliders;
    private readonly List<(Collider2D a, Collider2D b)> ignoredPairs = new();

    // Preview
    private LineRenderer previewLine;
    private bool previewValid;
    private Vector2 previewPos;

    // Placement info for carried block
    private Vector2 carriedCheckSize;     // world size for overlap box
    private Vector2 carriedOffsetWorld;   // IMPORTANT: collider offset in world units

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        EnsurePreview();
        SetPreviewVisible(false);
    }

    void EnsurePreview()
    {
        if (previewLine) return;

        var go = new GameObject("DropPreview");
        go.transform.SetParent(transform, false);

        previewLine = go.AddComponent<LineRenderer>();
        previewLine.useWorldSpace = true;
        previewLine.loop = true;
        previewLine.positionCount = 4;
        previewLine.startWidth = previewLineWidth;
        previewLine.endWidth = previewLineWidth;
        previewLine.numCapVertices = 0;
        previewLine.numCornerVertices = 0;

        previewLine.material = new Material(Shader.Find("Sprites/Default"));
    }

    void SetPreviewVisible(bool on)
    {
        if (previewLine) previewLine.enabled = on;
    }

    void OnEnable()
    {
        ApplyFacingVisual();

        inSpawn = true;
        if (animator && !string.IsNullOrEmpty(spawnStateName))
            animator.Play(spawnStateName, 0, 0f);

        spawnUnlockTime = (spawnLockDuration > 0f) ? Time.time + spawnLockDuration : 0f;
    }

    public void InitializeFacing(int dir)
    {
        facing = (dir >= 0) ? 1 : -1;
        ApplyFacingVisual();
    }

    public void EndSpawn() => inSpawn = false;

    void Update()
    {
        if (inSpawn && spawnLockDuration > 0f && Time.time >= spawnUnlockTime)
            inSpawn = false;

        moveInput = inSpawn ? 0f : Input.GetAxisRaw("Horizontal");

        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        ApplyFacingVisual();

        // Pickup / Drop
        if (!inSpawn && Input.GetKeyDown(KeyCode.Space))
        {
            if (carriedObject == null) TryPickup();
            else DropObject();
        }

        // Preview update
        if (carriedObject != null)
            UpdateDropPreview();
        else
            SetPreviewVisible(false);

        // Animator
        if (animator)
        {
            animator.SetBool("IsRunning", !inSpawn && Mathf.Abs(moveInput) > 0.01f);
            animator.SetBool("IsCarrying", carriedObject != null);
            if (!string.IsNullOrEmpty(pushingBool))
                animator.SetBool(pushingBool, isPushing);
        }
    }

    void FixedUpdate()
    {
        isGrounded = groundCheck
            ? Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer)
            : false;

        onLeftWall = wallCheckLeft && Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);
        onRightWall = wallCheckRight && Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);

        float x = inSpawn ? 0f : moveInput * moveSpeed;

        // prevent pushing into wall (no sticking)
        if ((onLeftWall && x < 0f) || (onRightWall && x > 0f))
            x = 0f;

        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);

        isPushing = ComputeIsPushing();
    }

    bool ComputeIsPushing()
    {
        if (carriedObject != null) return false;
        if (!isGrounded) return false;
        if (Mathf.Abs(moveInput) < inputThreshold) return false;

        Vector2 center = (Vector2)transform.position + new Vector2(facing * pushCheckDistance, 0f);

        var hit = Physics2D.OverlapBox(center, pushCheckSize, 0f, pushBlockLayer);
        if (!hit) return false;

        var pushable = hit.GetComponentInParent<PushableBlock>();
        if (!pushable) return false;

        bool pressingTowardBlock = Mathf.Sign(moveInput) == Mathf.Sign(facing);
        if (!pressingTowardBlock) return false;

        return true;
    }

    // ---------------- Pickup ----------------
    void TryPickup()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, blockLayer);
        if (hits == null || hits.Length == 0) return;

        Collider2D best = hits[0];
        float bestDist = (hits[0].transform.position - transform.position).sqrMagnitude;
        for (int i = 1; i < hits.Length; i++)
        {
            float d = (hits[i].transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { best = hits[i]; bestDist = d; }
        }

        carriedObject = best.gameObject;
        pickedWorldPosition = carriedObject.transform.position;
        pickedOriginalParent = carriedObject.transform.parent;

        // ✅ Get size + offset correctly (fixes composite edge false-green)
        GetWorldBoxInfo(carriedObject, out Vector2 wSize, out Vector2 wOffset);
        carriedCheckSize = wSize + Vector2.one * placementPadding;
        carriedOffsetWorld = wOffset;

        var blockRb = carriedObject.GetComponent<Rigidbody2D>();
        var blockCols = carriedObject.GetComponentsInChildren<Collider2D>(includeInactive: true);

        // Ignore collisions player <-> carried block
        ignoredPairs.Clear();
        foreach (var pc in playerColliders)
        {
            if (!pc || !pc.enabled) continue;
            foreach (var bc in blockCols)
            {
                if (!bc || !bc.enabled) continue;
                Physics2D.IgnoreCollision(pc, bc, true);
                ignoredPairs.Add((pc, bc));
            }
        }

        // Disable physics while carried
        if (blockRb)
        {
            blockRb.linearVelocity = Vector2.zero;
            blockRb.angularVelocity = 0f;
            blockRb.simulated = false;
        }

        carriedObject.transform.SetParent(carryPoint, worldPositionStays: false);
        carriedObject.transform.localPosition = Vector3.zero;
        carriedObject.transform.localRotation = Quaternion.identity;

        SetPreviewVisible(true);
    }

    void GetWorldBoxInfo(GameObject obj, out Vector2 worldSize, out Vector2 worldOffset)
    {
        var box = obj.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            var ls = obj.transform.lossyScale;
            worldSize = new Vector2(box.size.x * Mathf.Abs(ls.x), box.size.y * Mathf.Abs(ls.y));
            worldOffset = new Vector2(box.offset.x * ls.x, box.offset.y * ls.y);
            return;
        }

        var col = obj.GetComponent<Collider2D>();
        if (col != null)
        {
            worldSize = col.bounds.size;
            worldOffset = (Vector2)(col.bounds.center - obj.transform.position);
            return;
        }

        worldSize = new Vector2(0.5f, 0.5f);
        worldOffset = Vector2.zero;
    }

    // ---------------- Drop ----------------
    void DropObject()
    {
        if (carriedObject == null) return;

        // Use the same result as preview
        if (!previewValid) return;

        var blockRb = carriedObject.GetComponent<Rigidbody2D>();

        carriedObject.transform.SetParent(null);
        carriedObject.transform.position = previewPos;

        if (blockRb)
        {
            blockRb.simulated = true;
            blockRb.bodyType = RigidbodyType2D.Dynamic;
            if (blockRb.gravityScale <= 0f) blockRb.gravityScale = 1f;
            blockRb.linearVelocity = Vector2.zero;
            blockRb.angularVelocity = 0f;
        }

        // Restore collisions
        foreach (var pair in ignoredPairs)
            if (pair.a && pair.b) Physics2D.IgnoreCollision(pair.a, pair.b, false);
        ignoredPairs.Clear();

        if (pickedOriginalParent != null)
            carriedObject.transform.SetParent(pickedOriginalParent);

        carriedObject = null;
        pickedOriginalParent = null;

        SetPreviewVisible(false);
    }

    // ---------------- Preview logic ----------------
    void UpdateDropPreview()
    {
        if (!previewLine || carriedObject == null) return;

        Vector2 desired =
            returnToPickupPosition
            ? (Vector2)pickedWorldPosition
            : (Vector2)transform.position + new Vector2(facing * placeForwardDistance, 0f);

        previewValid = FindBestDropPosition(desired, carriedCheckSize, out previewPos);

        DrawPreviewRect(previewPos, carriedCheckSize);

        previewLine.startColor = previewValid ? Color.green : Color.red;
        previewLine.endColor = previewValid ? Color.green : Color.red;
        previewLine.enabled = true;
    }

    void DrawPreviewRect(Vector2 centerPos, Vector2 size)
    {
        // ✅ draw at TRUE collider center (pos + offset)
        Vector2 center = centerPos + carriedOffsetWorld;

        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;

        Vector3 p0 = new Vector3(center.x - hx, center.y - hy, 0f);
        Vector3 p1 = new Vector3(center.x + hx, center.y - hy, 0f);
        Vector3 p2 = new Vector3(center.x + hx, center.y + hy, 0f);
        Vector3 p3 = new Vector3(center.x - hx, center.y + hy, 0f);

        previewLine.SetPosition(0, p0);
        previewLine.SetPosition(1, p1);
        previewLine.SetPosition(2, p2);
        previewLine.SetPosition(3, p3);
    }

    bool FindBestDropPosition(Vector2 desired, Vector2 checkSize, out Vector2 bestPos)
    {
        float upMax = Mathf.Min(maxPreviewUp, 1.2f);

        Vector2[] offsets =
        {
            Vector2.zero,
            new Vector2(-facing * 0.6f, 0f),
            new Vector2(0f, 0.25f),
            new Vector2(-facing * 0.6f, 0.25f),
            new Vector2(0f, 0.5f),
            new Vector2(-facing * 0.6f, 0.5f),
            new Vector2(0f, 0.75f),
            new Vector2(0f, upMax),
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2 p = desired + offsets[i];
            if (!IsBlockedAt(p, checkSize))
            {
                bestPos = p;
                return true;
            }
        }

        bestPos = desired;
        return false;
    }

    bool IsBlockedAt(Vector2 pos, Vector2 size)
    {
        // ✅ use true collider center for overlap checks too
        Vector2 center = pos + carriedOffsetWorld;

        // Any solid collider blocks placement (ignore triggers, ignore player, ignore carried block)
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, ~0);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h) continue;
            if (h.isTrigger) continue;

            if (h.transform.IsChildOf(transform)) continue; // ignore player
            if (carriedObject != null && h.transform.IsChildOf(carriedObject.transform)) continue; // ignore carried

            return true;
        }

        // ✅ Support check prevents placing into void behind a wall edge
        if (requireSupportUnderBlock)
        {
            float halfY = size.y * 0.5f;

            // Ray from just inside bottom of the block, straight down
            Vector2 rayStart = new Vector2(center.x, center.y - halfY + 0.02f);
            var supportHit = Physics2D.Raycast(rayStart, Vector2.down, supportCheckDepth, supportLayers);

            if (supportHit.collider == null)
                return true;
        }

        return false;
    }

    private void ApplyFacingVisual()
    {
        if (!visualToFlip) return;
        var s = visualToFlip.localScale;
        s.x = Mathf.Abs(s.x) * (facing >= 0 ? 1 : -1);
        visualToFlip.localScale = s;
    }
}
