using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class AgileFormController : MonoBehaviour
{
    [Header("Refs")]
    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask wallLayer; // set to Ground if you don't use a separate wall layer

    [Header("Balance")]
    public float moveSpeed = 8f;
    public float jumpForce = 7f;
    public float gravityScale = 2.5f;

    [Header("Wall")]
    public float wallSlideSpeed = 2f;   // slow downward speed while sliding
    public float wallStickTime = 1f;    // how long you keep the slow slide before falling normally
    public float wallJumpForceX = 6f;   // horizontal push away from wall
    public float wallJumpForceY = 7f;   // vertical push on wall jump

    [Header("Checks")]
    public float groundRadius = 0.15f;
    public float wallCheckRadius = 0.15f;

    // Optional animator hooks (safe to leave null)
    public Animator animator;
    public Transform visualToFlip;// set Speed (float) & OnGround (bool) if provided

    // --- internal ---
    Rigidbody2D rb;
    float moveInput;
    bool grounded;
    bool onLeftWall, onRightWall;
    bool wallSliding;
    float wallStickTimer;

    int lastFacing = 1;     // 1 right, -1 left
    int lastWallJumpDir = 0;// remembers the wall we last jumped from: -1 left, +1 right

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
    }

    void Update()
    {
        // Input
        moveInput = Input.GetAxisRaw("Horizontal");

        // Facing
        if (moveInput > 0.01f) lastFacing = 1;
        else if (moveInput < -0.01f) lastFacing = -1;

        if (visualToFlip != null && lastFacing != 0)
        {
            var s = visualToFlip.localScale;
            s.x = Mathf.Abs(s.x) * lastFacing;
            visualToFlip.localScale = s;
        }

        // Jump pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJump();
        }

        // Animator (optional)
        if (animator)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            animator.SetBool("OnGround", grounded);
        }
    }

    void FixedUpdate()
    {
        // Horizontal move
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        if (onLeftWall && !onRightWall) Debug.Log("Left wall detected");
        if (onRightWall && !onLeftWall) Debug.Log("Right wall detected");

        // Ground & wall checks
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        onLeftWall  = Physics2D.OverlapCircle(wallCheckLeft.position,  wallCheckRadius, wallLayer);
        onRightWall = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);
        bool touchingWall = onLeftWall || onRightWall;

        if (grounded)
        {
            lastWallJumpDir = 0;
            wallSliding = false;
            wallStickTimer = 0f;
        }

        // ---- FIXED wall slide logic ----
        bool holdingTowardLeft  = onLeftWall  && moveInput < 0;
        bool holdingTowardRight = onRightWall && moveInput > 0;

        // Only slide if airborne and pressing toward whichever wall you're touching
        wallSliding = !grounded && (holdingTowardLeft || holdingTowardRight);

        if (wallSliding)
        {
            // refresh timer when you first start sliding
            if (wallStickTimer <= 0f)
                wallStickTimer = wallStickTime;

            if (wallStickTimer > 0f)
            {
                // Clamp vertical speed for both sides
                if (rb.linearVelocity.y < -wallSlideSpeed)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);

                wallStickTimer -= Time.fixedDeltaTime;
            }
        }
        else
        {
            wallStickTimer = 0f;
        }
    }

    void TryJump()
    {
        // Ground jump
        if (grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            return;
        }

        // Wall jump: only if sliding & input is toward that wall & not same wall as last jump
        int currentWallDir = onLeftWall ? -1 : (onRightWall ? 1 : 0);
        bool holdingTowardWall = (onLeftWall && moveInput < 0) || (onRightWall && moveInput > 0);

        if (currentWallDir != 0 && holdingTowardWall && wallSliding)
        {
            // Prevent multiple jumps on the SAME wall
            if (currentWallDir == lastWallJumpDir) return;

            // Jump away from wall
            Vector2 v = new Vector2(-currentWallDir * wallJumpForceX, wallJumpForceY);
            rb.linearVelocity = v;

            // Lock to this wall until you touch ground or switch walls
            lastWallJumpDir = currentWallDir;

            // Stop slide immediately after jump
            wallSliding = false;
            wallStickTimer = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
        if (wallCheckLeft)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
        }
        if (wallCheckRight)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        }
    }
}
