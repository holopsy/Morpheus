using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerHealth))]
public class FallDamage2D : MonoBehaviour
{
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.15f;
    public LayerMask groundLayer = ~0;

    [Header("Wall Checks (OPTIONAL, mainly for Agile)")]
    [Tooltip("If set, touching a wall will RESET fall tracking so wall-slides never count as a giant fall.")]
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float wallCheckRadius = 0.15f;
    public LayerMask wallLayer;
    public bool resetFallWhileTouchingWall = true;

    [Header("Fall Damage Tuning (per form)")]
    public float safeFallDistance = 6f;
    public float damageStepDistance = 3f;
    public int maxDamagePerLanding = 3;

    [Header("Free-fall Gate")]
    [Tooltip("Only apply fall damage if you ever reached a downward speed faster than this (negative).")]
    public float requiredMinFallSpeedY = -8f;

    Rigidbody2D rb;
    PlayerHealth hp;

    bool grounded;
    bool wasGrounded;

    float highestYInAir;
    float minVelY; // most negative Y velocity while airborne

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hp = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        grounded = false;
        wasGrounded = false;
        highestYInAir = transform.position.y;
        minVelY = 0f;
    }

    void FixedUpdate()
    {
        grounded = groundCheck
            ? Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer)
            : false;

        bool touchingWall = false;
        if (resetFallWhileTouchingWall && (wallCheckLeft || wallCheckRight))
        {
            bool left = wallCheckLeft && Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);
            bool right = wallCheckRight && Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);
            touchingWall = left || right;
        }

        float y = transform.position.y;

        // ✅ KEY FIX: if you're sliding/touching a wall while airborne, reset tracking continuously
        if (!grounded && touchingWall)
        {
            highestYInAir = y;
            minVelY = 0f;
            wasGrounded = false; // ensures we only consider a landing after we truly leave wall
            return;
        }

        if (!grounded)
        {
            highestYInAir = Mathf.Max(highestYInAir, y);
            minVelY = Mathf.Min(minVelY, rb.linearVelocity.y);
        }
        else
        {
            if (!wasGrounded)
            {
                float fallDistance = highestYInAir - y;

                // Gate: must have been real free-fall at some point
                if (minVelY <= requiredMinFallSpeedY)
                    ApplyFallDamage(fallDistance);
            }

            highestYInAir = y;
            minVelY = 0f;
        }

        wasGrounded = grounded;
    }

    void ApplyFallDamage(float fallDistance)
    {
        float excess = fallDistance - safeFallDistance;
        if (excess <= 0f) return;

        int dmg = Mathf.CeilToInt(excess / Mathf.Max(0.01f, damageStepDistance));
        dmg = Mathf.Clamp(dmg, 1, maxDamagePerLanding);

        hp.TakeDamage(dmg, transform.position, 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
        if (wallCheckLeft)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
        }
        if (wallCheckRight)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        }
    }
}
