using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class AgileFormController : MonoBehaviour
{
    [Header("Refs")]
    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public Transform visualToFlip;     // Sprite root (your "Visual" child)
    public Animator animator;          // Animator on "Visual"
    public PlayerHealth playerHealth;  // optional; used for Death trigger

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask wallLayer; // set = groundLayer if you don’t use a separate wall layer

    [Header("Balance")]
    public float moveSpeed = 8f;
    public float jumpForce = 7f;
    public float gravityScale = 2.5f;

    [Header("Wall")]
    public float wallSlideSpeed = 2f;     // max downward speed while sliding
    public float wallStickTime = 1f;      // how long we clamp vertical speed when sliding
    public float wallJumpForceX = 6f;     // horizontal push away from wall
    public float wallJumpForceY = 7f;     // vertical push on wall jump
    [Tooltip("Small delay after wall-jump before sliding can start again (lets Jump anim show).")]
    public float postWallJumpNoSlideTime = 0.12f;

    [Header("Checks")]
    public float groundRadius = 0.15f;
    public float wallCheckRadius = 0.15f;

    [Header("Animator Param Names")]
    public string pSpeed = "Speed";         // float 0..1
    public string pGrounded = "Grounded";   // bool
    public string pWallSlide = "WallSlide"; // bool
    public string pYVel = "YVel";           // float
    public string tSpawn = "Spawn";         // trigger
    public string tDie = "Die";             // trigger
    public string tJump = "Jump";           // trigger  << NEW

    // --- internal ---
    Rigidbody2D rb;
    float moveInput;
    bool grounded;
    bool onLeftWall, onRightWall;
    bool wallSliding;
    float wallStickTimer;
    float wallRegrabUntil;                  // time until sliding allowed again after wall-jump
    int lastFacing = 1;                     // 1 right, -1 left
    int lastWallJumpDir = 0;                // -1 left, +1 right

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!visualToFlip && animator) visualToFlip = animator.transform;
        if (!playerHealth) playerHealth = GetComponent<PlayerHealth>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!visualToFlip && animator) visualToFlip = animator.transform;
        if (!playerHealth) playerHealth = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        SafeSetTrigger(animator, tSpawn);
        if (playerHealth) playerHealth.OnDeath += OnDied;
    }

    void OnDisable()
    {
        if (playerHealth) playerHealth.OnDeath -= OnDied;
    }

    void Update()
    {
        // --- Input ---
        moveInput = Input.GetAxisRaw("Horizontal");

        // --- Facing & flip ---
        if (moveInput > 0.01f) lastFacing = 1;
        else if (moveInput < -0.01f) lastFacing = -1;

        if (visualToFlip && lastFacing != 0)
        {
            var s = visualToFlip.localScale;
            s.x = Mathf.Abs(s.x) * lastFacing;
            visualToFlip.localScale = s;
        }

        // --- Jump ---
        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();

        // --- Animator params (mirrored again in FixedUpdate) ---
        if (animator)
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / Mathf.Max(0.001f, moveSpeed));
            SafeSetFloat(animator, pSpeed, speed01);
            SafeSetBool(animator, pGrounded, grounded);
            SafeSetBool(animator, pWallSlide, wallSliding);
            SafeSetFloat(animator, pYVel, rb.linearVelocity.y);
        }
    }

    void FixedUpdate()
    {
        // --- Horizontal move ---
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // --- Checks ---
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        onLeftWall  = Physics2D.OverlapCircle(wallCheckLeft.position,  wallCheckRadius, wallLayer);
        onRightWall = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);

        // --- Wall slide conditions ---
        bool touchingWall = onLeftWall || onRightWall;
        bool holdingTowardLeft  = onLeftWall  && moveInput < 0;
        bool holdingTowardRight = onRightWall && moveInput > 0;
        bool holdingTowardWall  = holdingTowardLeft || holdingTowardRight;

        // Only slide if airborne, touching a wall, pressing INTO that wall, and the post-jump lock has expired
        bool allowSlideNow = Time.time >= wallRegrabUntil;
        wallSliding = !grounded && touchingWall && holdingTowardWall && allowSlideNow;

        if (grounded)
        {
            lastWallJumpDir = 0;
            wallSliding = false;
            wallStickTimer = 0f;
        }

        if (wallSliding)
        {
            if (wallStickTimer <= 0f) wallStickTimer = wallStickTime;

            if (wallStickTimer > 0f)
            {
                if (rb.linearVelocity.y < -wallSlideSpeed)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);

                wallStickTimer -= Time.fixedDeltaTime;
            }
        }
        else
        {
            wallStickTimer = 0f;
        }

        // Reflect latest states to animator
        if (animator)
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / Mathf.Max(0.001f, moveSpeed));
            SafeSetFloat(animator, pSpeed, speed01);
            SafeSetBool(animator, pGrounded, grounded);
            SafeSetBool(animator, pWallSlide, wallSliding);
            SafeSetFloat(animator, pYVel, rb.linearVelocity.y);
        }
    }

    void TryJump()
    {
        // Ground jump
        if (grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            SafeSetTrigger(animator, tJump);              // << play Jump
            return;
        }

        // Wall jump
        int currentWallDir = onLeftWall ? -1 : (onRightWall ? 1 : 0);
        bool holdingTowardWall = (onLeftWall && moveInput < 0) || (onRightWall && moveInput > 0);

        if (currentWallDir != 0 && holdingTowardWall && wallSliding)
        {
            if (currentWallDir == lastWallJumpDir) return;

            Vector2 v = new Vector2(-currentWallDir * wallJumpForceX, wallJumpForceY);
            rb.linearVelocity = v;

            lastWallJumpDir = currentWallDir;

            // stop sliding and lock re-grab briefly so Jump anim can show
            wallSliding = false;
            wallStickTimer = 0f;
            wallRegrabUntil = Time.time + postWallJumpNoSlideTime;

            SafeSetTrigger(animator, tJump);              // << play Jump
        }
    }

    void OnDied()
    {
        SafeSetTrigger(animator, tDie);
    }

    // --- Safe Animator setters ---
    static void SafeSetTrigger(Animator a, string name)
    {
        if (!a || string.IsNullOrEmpty(name)) return;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
            { a.SetTrigger(name); return; }
    }

    static void SafeSetBool(Animator a, string name, bool val)
    {
        if (!a || string.IsNullOrEmpty(name)) return;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == name)
            { a.SetBool(name, val); return; }
    }

    static void SafeSetFloat(Animator a, string name, float val)
    {
        if (!a || string.IsNullOrEmpty(name)) return;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Float && p.name == name)
            { a.SetFloat(name, val); return; }
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
