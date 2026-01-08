using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class AgileFormController : MonoBehaviour
{
    [Header("Refs")]
    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public Transform visualToFlip;
    public Animator animator;
    public PlayerHealth playerHealth;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Header("Balance")]
    public float moveSpeed = 8f;
    public float jumpForce = 7f;
    public float gravityScale = 2.5f;

    [Header("Wall")]
    public float wallSlideSpeed = 2f;
    public float wallStickTime = 1f;
    public float wallJumpForceX = 6f;
    public float wallJumpForceY = 7f;
    public float postWallJumpNoSlideTime = 0.12f;

    [Header("Wall Coyote")]
    public float wallCoyoteTime = 0.12f;

    [Header("Checks")]
    public float groundRadius = 0.15f;
    public float wallCheckRadius = 0.15f;

    [Header("Animator Param Names")]
    public string pSpeed = "Speed";
    public string pGrounded = "Grounded";
    public string pWallSlide = "WallSlide";
    public string pYVel = "YVel";
    public string tSpawn = "Spawn";
    public string tDie = "Die";
    public string tJump = "Jump";

    Rigidbody2D rb;
    float moveInput;
    bool grounded;
    bool onLeftWall, onRightWall;
    bool wallSliding;
    float wallStickTimer;
    float wallRegrabUntil;
    int lastFacing = 1;

    int lastWallJumpDir = 0;

    float wallCoyoteTimer = 0f;
    int lastWallSide = 0;
    

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

        moveInput = Input.GetAxisRaw("Horizontal");

// ✅ FOOTSTEPS (correct place)
        if (grounded && !wallSliding && Mathf.Abs(moveInput) > 0.01f)
            AudioManager.I.StartFootsteps(SoundLibrary.I.walk);
        else
            AudioManager.I.StopFootsteps();

        // Facing
        if (moveInput > 0.01f) lastFacing = 1;
        else if (moveInput < -0.01f) lastFacing = -1;

        if (visualToFlip && lastFacing != 0)
        {
            var s = visualToFlip.localScale;
            s.x = Mathf.Abs(s.x) * lastFacing;
            visualToFlip.localScale = s;
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();


        if (animator)
        {
            SafeSetFloat(animator, pSpeed, Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / moveSpeed));
            SafeSetBool(animator, pGrounded, grounded);
            SafeSetBool(animator, pWallSlide, wallSliding);
            SafeSetFloat(animator, pYVel, rb.linearVelocity.y);
        }
    }

    void FixedUpdate()
    {
        // Horizontal movement
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Checks
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        onLeftWall = Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);
        onRightWall = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);

        bool touchingWall = onLeftWall || onRightWall;
        int wallSide = onLeftWall ? -1 : (onRightWall ? 1 : 0);

        // Wall coyote update
        if (touchingWall)
        {
            wallCoyoteTimer = wallCoyoteTime;
            lastWallSide = wallSide;
        }
        else
        {
            wallCoyoteTimer -= Time.fixedDeltaTime;
        }

        // Reset jump lock on ground
        if (grounded)
        {
            lastWallJumpDir = 0;
            wallSliding = false;
            wallStickTimer = 0f;
        }

        bool allowSlideNow = Time.time >= wallRegrabUntil;

        bool holdingTowardWall =
            (onLeftWall && moveInput < 0) ||
            (onRightWall && moveInput > 0);

        // Wall slide is ONLY when pushing toward the wall
        wallSliding = !grounded && touchingWall && holdingTowardWall && allowSlideNow;

        if (wallSliding)
        {
            if (wallStickTimer <= 0f)
                wallStickTimer = wallStickTime;

            if (wallStickTimer > 0)
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

        // Animator sync
        if (animator)
        {
            SafeSetFloat(animator, pSpeed, Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / moveSpeed));
            SafeSetBool(animator, pGrounded, grounded);
            SafeSetBool(animator, pWallSlide, wallSliding);
            SafeSetFloat(animator, pYVel, rb.linearVelocity.y);
        }
    }

    void TryJump()
    {
        // Normal jump
        if (grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            // Jump SFX (placeholder uses walk)
            AudioManager.I?.PlaySFX(SoundLibrary.I?.jump);

            SafeSetTrigger(animator, tJump);
            return;
        }

        // Direct wall jump
        int wallSide = onLeftWall ? -1 : (onRightWall ? 1 : 0);

        if (wallSide != 0)
        {
            if (wallSide == lastWallJumpDir) return;
            DoWallJump(wallSide);
            return;
        }

        // Wall coyote jump
        if (wallCoyoteTimer > 0f)
        {
            if (lastWallSide != 0 && lastWallSide != lastWallJumpDir)
            {
                DoWallJump(lastWallSide);
                return;
            }
        }
    }

    void DoWallJump(int wallDir)
    {
        rb.linearVelocity = new Vector2(-wallDir * wallJumpForceX, wallJumpForceY);

        lastWallJumpDir = wallDir;
        wallSliding = false;
        wallStickTimer = 0f;

        wallRegrabUntil = Time.time + postWallJumpNoSlideTime;

        // Jump SFX (wall jump)
        AudioManager.I?.PlaySFX(SoundLibrary.I?.jump);

        SafeSetTrigger(animator, tJump);
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
