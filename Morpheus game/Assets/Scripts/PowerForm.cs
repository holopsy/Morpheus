using UnityEngine;

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
    public LayerMask blockLayer;                // layer of movable blocks
    public float pickupRadius = 1f;             // how far to search for a block

    [Tooltip("If true, drop returns the block to the exact position it was picked from. If false, drops at your feet snapped to ground.")]
    public bool returnToPickupPosition = true;

    [Tooltip("Horizontal distance in front of player to try placing the block when returnToPickupPosition = false.")]
    public float placeForwardDistance = 0.6f;

    private GameObject carriedObject;
    private Vector3 pickedWorldPosition;        // stored when picked
    private Transform pickedOriginalParent;     // (just in case the block was parented)

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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

        // (Power form no jump)
        // if (!inSpawn && Input.GetKeyDown(KeyCode.W) && isGrounded) { ... }  // removed

        if (!inSpawn && Input.GetKeyDown(KeyCode.Space))
        {
            if (carriedObject == null) TryPickup();
            else DropObject();
        }

        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        ApplyFacingVisual();

        if (animator)
        {
            animator.SetBool("IsRunning", !inSpawn && Mathf.Abs(moveInput) > 0.01f);
            animator.SetBool("IsCarrying", carriedObject != null);
        }
    }

    void FixedUpdate()
    {
        isGrounded = groundCheck
            ? Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer)
            : false;

        float x = inSpawn ? 0f : moveInput * moveSpeed;
        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
    }

    // ---------------- Carry System ----------------
    void TryPickup()
    {
        // Find nearest block in radius on blockLayer
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
        var blockCol = carriedObject.GetComponent<Collider2D>();
        var playerCol = GetComponent<Collider2D>();

        if (blockRb != null)
        {
            blockRb.linearVelocity = Vector2.zero;
            blockRb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (blockCol && playerCol)
            Physics2D.IgnoreCollision(blockCol, playerCol, true);

        carriedObject.transform.SetParent(carryPoint, worldPositionStays: false);
        carriedObject.transform.localPosition = Vector3.zero;
        carriedObject.transform.localRotation = Quaternion.identity;
    }

    void DropObject()
    {
        if (carriedObject == null) return;

        var blockRb  = carriedObject.GetComponent<Rigidbody2D>();
        var blockCol = carriedObject.GetComponent<Collider2D>();
        var playerCol = GetComponent<Collider2D>();

        // detach before positioning
        carriedObject.transform.SetParent(null);

        // Decide drop position
        if (returnToPickupPosition)
        {
            // return to exact position we picked from
            carriedObject.transform.position = pickedWorldPosition;
        }
        else
        {
            // place at feet in front, snapped to ground if possible
            Vector2 tryPos = (Vector2)transform.position + new Vector2(facing * placeForwardDistance, 0.0f);
            Vector2 rayStart = tryPos + Vector2.up * 0.5f;      // start a bit above
            float rayLen = 3f;
            var hit = Physics2D.Raycast(rayStart, Vector2.down, rayLen, groundLayer);

            if (hit.collider != null)
            {
                // place the block so it rests on ground (account for its collider height)
                float blockHalfHeight = 0.25f;
                var bc = carriedObject.GetComponent<Collider2D>();
                if (bc != null) blockHalfHeight = bc.bounds.extents.y;

                Vector2 placed = hit.point + Vector2.up * blockHalfHeight;
                carriedObject.transform.position = placed;
            }
            else
            {
                // fallback: just drop at tryPos
                carriedObject.transform.position = tryPos;
            }
        }

        // restore parent if it had one (optional)
        if (pickedOriginalParent != null)
            carriedObject.transform.SetParent(pickedOriginalParent);

        // restore collisions and physics
        if (blockCol && playerCol)
            Physics2D.IgnoreCollision(blockCol, playerCol, false);

        if (blockRb != null)
            blockRb.bodyType = RigidbodyType2D.Dynamic;

        // clear refs
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
    }
}
