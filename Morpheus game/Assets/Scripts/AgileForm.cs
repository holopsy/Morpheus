using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class AgileFormController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 9f;

    [Header("Ground Check")]
    public Transform groundCheck;       
    public float groundRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Wall Check")]
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float wallCheckRadius = 0.15f;
    public LayerMask wallLayer;

    [Header("Wall Slide & Jump")]
    public float wallSlideSpeed = 0.5f;    // slow descent speed
    public float wallJumpPush = 7f;        // horizontal push
    public float wallJumpUp = 9f;          // vertical push
    public float regrabDelay = 0.15f;      // delay before sticking again after wall jump

    private Rigidbody2D rb;
    private float moveInput;
    private bool grounded;
    private bool onLeftWall, onRightWall;
    private bool wallSliding;
    private float cantGrabUntil;

    // 🟦 Track last wall jumped from
    private int lastWallDir = 0; // -1 = left, +1 = right, 0 = none

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // 🟦 Ground jump
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            lastWallDir = 0; // reset wall memory when on ground
        }

        // 🟦 Wall jump
        else if (Input.GetKeyDown(KeyCode.Space) && wallSliding)
        {
            int dir = onLeftWall ? -1 : 1; // which wall we're on

            // Only allow jump if it's a *different* wall than last time
            if (dir != lastWallDir)
            {
                rb.linearVelocity = new Vector2(-dir * wallJumpPush, wallJumpUp);

                wallSliding = false;
                cantGrabUntil = Time.time + regrabDelay;

                lastWallDir = dir; // remember this wall
            }
        }
    }

    void FixedUpdate()
    {
        // 🟦 Horizontal movement
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 🟦 Ground check
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        // 🟦 Wall check
        onLeftWall  = Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);
        onRightWall = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);
        bool touchingWall = onLeftWall || onRightWall;

        // 🟦 Reset last wall if grounded
        if (grounded) lastWallDir = 0;

        // 🟦 Wall slide (only if airborne, touching wall, not locked)
        if (!grounded && touchingWall && Time.time >= cantGrabUntil)
        {
            wallSliding = true;

            // clamp downward velocity
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
        else
        {
            wallSliding = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
        if (wallCheckLeft != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
        }
        if (wallCheckRight != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        }
    }
}
