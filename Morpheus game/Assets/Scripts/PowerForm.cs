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

    [Header("Carrying System")]
    public Transform carryPoint;                // where the block sits while held
    public LayerMask blockLayer;                // layer of SMALL movable blocks (pickup)
    public float pickupRadius = 1f;             // how far to search for a block

    [Tooltip("If true, drop returns the block to the exact position it was picked from. If false, drops at your feet snapped to ground.")]
    public bool returnToPickupPosition = true;

    [Tooltip("Horizontal distance in front of player to try placing the block when returnToPickupPosition = false.")]
    public float placeForwardDistance = 0.6f;

    private GameObject carriedObject;
    private Vector3 pickedWorldPosition;
    private Transform pickedOriginalParent;

    [Header("Push Detection")]
    [Tooltip("Layer for LARGE push-only blocks.")]
    public LayerMask pushBlockLayer;            // set this to your LARGE push-only blocks
    [Tooltip("Box cast size (width x height) used to sense a block in front.")]
    public Vector2 pushCheckSize = new Vector2(0.45f, 0.8f);
    [Tooltip("Distance from the player center to check in front.")]
    public float pushCheckDistance = 0.35f;
    [Tooltip("Only consider pushing if you're actually pressing in that direction.")]
    public float inputThreshold = 0.2f;

    [Header("Visuals / Animator")]
    public Transform visualToFlip;              // "Visual" child
    public Animator animator;                   // Animator on Visual

    [Header("Spawn")]
    public float spawnLockDuration = 0.6f;
    public string spawnStateName = "Spawn_Powerform";

    // Internal
    private Rigidbody2D rb;
    private int facing = 1;                     // 1 = right, -1 = left
    private float moveInput;
    private bool isGrounded;
    private bool inSpawn;
    private float spawnUnlockTime;

    // Push state
    private bool isPushing;

    // --- Carry collision bookkeeping ---
    private Collider2D[] playerColliders;                                   // all colliders on player (root + children)
    private readonly List<(Collider2D a, Collider2D b)> ignoredPairs = new(); // pairs we ignored to restore later

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // cache all player colliders so we can safely ignore/restore
        playerColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
    }

    void OnEnable()
    {
        ApplyFacingVisual();

        // play spawn & lock input
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

        if (!inSpawn && Input.GetKeyDown(KeyCode.Space))
        {
            if (carriedObject == null) TryPickup();
            else DropObject();
        }

        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        ApplyFacingVisual();

        // Animator params common to all states
        if (animator)
        {
            animator.SetBool("IsRunning", !inSpawn && Mathf.Abs(moveInput) > 0.01f);
            animator.SetBool("IsCarrying", carriedObject != null);
            animator.SetBool("IsPushing", isPushing); // << NEW
            // You already have "Die"/"Attack"/"Speed" if needed elsewhere
        }
    }

    void FixedUpdate()
    {
        isGrounded = groundCheck
            ? Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer)
            : false;

        float x = inSpawn ? 0f : moveInput * moveSpeed;
        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);

        // --- PUSH DETECTION ---
        isPushing = ComputeIsPushing();
    }

    // Checks if we are in front of a pushable block and pressing into it (while grounded and not carrying)
    bool ComputeIsPushing()
    {
        if (carriedObject != null)
            return false;
        if (!isGrounded)
            return false;

        // Must be pressing some direction
        if (Mathf.Abs(moveInput) < inputThreshold)
            return false;

        // Cast a small box in front of us to see if there's a pushable block
        Vector2 center = (Vector2)transform.position + new Vector2(facing * pushCheckDistance, 0f);
        var hit = Physics2D.OverlapBox(center, pushCheckSize, 0f, pushBlockLayer);

        // Debug: show what it hits
        if (hit)
        {
            Debug.DrawLine(center, hit.transform.position, Color.magenta);
            Debug.Log($"Push hit {hit.name}, facing {facing}, moveInput {moveInput}");
        }

        if (!hit)
            return false;

        // Pressing toward the block (we can relax this check)
        bool pressingTowardBlock = Mathf.Sign(moveInput) == Mathf.Sign(facing);
        if (!pressingTowardBlock)
            return false;

        return true;
    }

    // ---------------- Carry System ----------------
    void TryPickup()
    {
        // Find nearest SMALL block in radius on blockLayer
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

        var blockRb  = carriedObject.GetComponent<Rigidbody2D>();
        // grab ALL colliders on the block (root + children)
        var blockCols = carriedObject.GetComponentsInChildren<Collider2D>(includeInactive: true);

        // 1) Disable collisions between player and this block (all collider pairs)
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

        // 2) Disable physics simulation on the BLOCK while carried
        if (blockRb)
        {
            blockRb.linearVelocity = Vector2.zero;
            blockRb.angularVelocity = 0f;
            blockRb.simulated = false;
        }

        // 3) Parent to carry point
        carriedObject.transform.SetParent(carryPoint, worldPositionStays: false);
        carriedObject.transform.localPosition = Vector3.zero;
        carriedObject.transform.localRotation = Quaternion.identity;
    }

    void DropObject()
    {
        if (carriedObject == null) return;

        var blockRb  = carriedObject.GetComponent<Rigidbody2D>();
        var blockCols = carriedObject.GetComponentsInChildren<Collider2D>(includeInactive: true);

        // detach before positioning
        carriedObject.transform.SetParent(null);

        // Decide drop position
        if (returnToPickupPosition)
        {
            carriedObject.transform.position = pickedWorldPosition;
        }
        else
        {
            Vector2 tryPos = (Vector2)transform.position + new Vector2(facing * placeForwardDistance, 0.0f);
            Vector2 rayStart = tryPos + Vector2.up * 0.5f;
            float rayLen = 3f;
            var hit = Physics2D.Raycast(rayStart, Vector2.down, rayLen, groundLayer);

            if (hit.collider != null)
            {
                float blockHalfHeight = 0.25f;
                var bcAny = carriedObject.GetComponent<Collider2D>();
                if (bcAny != null) blockHalfHeight = bcAny.bounds.extents.y;

                Vector2 placed = hit.point + Vector2.up * blockHalfHeight;
                carriedObject.transform.position = placed;
            }
            else
            {
                carriedObject.transform.position = tryPos;
            }
        }

        // 1) Re-enable physics on the BLOCK
        if (blockRb)
        {
            blockRb.simulated = true;
            blockRb.linearVelocity = Vector2.zero;
            blockRb.AddForce(Vector2.down * 0.1f, ForceMode2D.Impulse);
        }

        // 2) Restore player ↔ block collisions
        foreach (var pair in ignoredPairs)
        {
            if (pair.a && pair.b) Physics2D.IgnoreCollision(pair.a, pair.b, false);
        }
        ignoredPairs.Clear();

        // 3) Restore parent if it had one (optional)
        if (pickedOriginalParent != null)
            carriedObject.transform.SetParent(pickedOriginalParent);

        carriedObject = null;
        pickedOriginalParent = null;
    }

    private void ApplyFacingVisual()
    {
        if (!visualToFlip) return;
        var s = visualToFlip.localScale;
        s.x = Mathf.Abs(s.x) * (facing >= 0 ? 1 : -1);
        visualToFlip.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        if (!returnToPickupPosition)
        {
            Gizmos.color = Color.green;
            Vector3 p = transform.position + new Vector3(facing * placeForwardDistance, 0f, 0f);
            Gizmos.DrawWireSphere(p, 0.08f);
        }

        // Push check box
        Gizmos.color = Color.magenta;
        Vector3 center = transform.position + new Vector3(Mathf.Sign(facing) * pushCheckDistance, 0f, 0f);
        Gizmos.DrawWireCube(center, new Vector3(pushCheckSize.x, pushCheckSize.y, 0.01f));
    }
}
