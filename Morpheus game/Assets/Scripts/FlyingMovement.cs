using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingFormController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;   // horizontal speed once active
    public float flapForce = 6f;   // upward flap strength

    [Header("Visual flip")]
    [Tooltip("Assign the 'Visual' child (with Animator/SpriteRenderers). If left null, tries to find child named 'Visual'.")]
    public Transform visualToFlip;
    [Tooltip("Flip all SpriteRenderers via flipX (best for sprite anims). If off, flips by scaling visualToFlip.x.")]
    public bool useSpriteFlipX = true;

    private Rigidbody2D rb;
    private Animator anim;

    private int facingDir = 1;     
    private bool initialized = false;
    private bool canMove = false;  // becomes true after spawn finishes
    private float originalGravity = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();

        if (!visualToFlip)
        {
            var t = transform.Find("Visual");
            if (t) visualToFlip = t;
        }

        // Freeze physics during spawn
        originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        canMove = false;
    }

    // Called by MorphManager right after instantiate
    public void InitializeDirection(int dir)
    {
        facingDir = (dir >= 0) ? 1 : -1;
        initialized = true;
        ApplyFacingVisual();
    }

    void Start()
    {
        if (!initialized)
            facingDir = (transform.localScale.x < 0) ? -1 : 1;

        ApplyFacingVisual();
    }

    void Update()
    {
        if (!canMove)
        {
            // hard-freeze while spawning
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // constant horizontal speed while active
        rb.linearVelocity = new Vector2(facingDir * moveSpeed, rb.linearVelocity.y);

        // flap
        if (Input.GetKeyDown(KeyCode.Space))
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, flapForce);
    }

    // === Called by Animation Event at end of Spawn_Flyingform ===
    public void OnSpawnComplete()
    {
        canMove = true;
        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(facingDir * moveSpeed, 0f);

        ApplyFacingVisual();

        if (anim)
        {
            // blend into the fly loop
            anim.CrossFade("Fly_Flyingform", 0.05f, 0);
        }
    }

    // === Die on side or ceiling impact ===
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!canMove) return; // ignore during spawn freeze

        foreach (var c in col.contacts)
        {
            // Side hit (walls) => |normal.x| > 0.5
            // Ceiling hit (top) => normal.y < -0.5
            if (Mathf.Abs(c.normal.x) > 0.5f || c.normal.y < -0.5f)
            {
                var death = GetComponent<PlayerDeath>();
                if (death != null)
                {
                    death.Die();
                    break;
                }
            }
        }
    }

    // --- helpers ---
    private void ApplyFacingVisual()
    {
        bool flipLeft = (facingDir == -1);

        if (useSpriteFlipX)
        {
            var srs = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            for (int i = 0; i < srs.Length; i++)
                srs[i].flipX = flipLeft;
        }
        else if (visualToFlip)
        {
            var s = visualToFlip.localScale;
            s.x = Mathf.Abs(s.x) * (flipLeft ? -1f : 1f);
            visualToFlip.localScale = s;
        }
    }
}
