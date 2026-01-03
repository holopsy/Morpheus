using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingFormController : MonoBehaviour
{
    [Header("Walk (before flying)")]
    public float walkSpeed = 2.2f;

    [Header("Fly (after takeoff)")]
    public float flyMoveSpeed = 5f;   // constant horizontal speed once flying
    public float flapForce = 6f;      // upward flap strength
    public KeyCode flapKey = KeyCode.Space;

    [Header("Safe Ground (what you're allowed to land on)")]
    [Tooltip("Layers that are allowed to be 'safe' to land on from above. Usually Ground only (not Walls/Pillars).")]
    public LayerMask safeGroundLayers;

    [Header("Animator Params")]
    public string speedParam = "Speed";
    public string isFlyingParam = "IsFlying";

    [Header("Visual flip")]
    [Tooltip("Assign the 'Visual' child (with Animator/SpriteRenderers). If left null, tries to find child named 'Visual'.")]
    public Transform visualToFlip;
    [Tooltip("Flip all SpriteRenderers via flipX (best for sprite anims). If off, flips by scaling visualToFlip.x.")]
    public bool useSpriteFlipX = true;

    private Rigidbody2D rb;
    private Animator anim;

    private int facingDir = 1;
    private bool initialized = false;
    private bool canMove = false;
    private bool isFlying = false;

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
        isFlying = false;

        // init animator values
        SetAnimBool(isFlyingParam, false);
        SetAnimFloat(speedParam, 0f);
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
            rb.linearVelocity = Vector2.zero;
            SetAnimFloat(speedParam, 0f);
            return;
        }

        if (!isFlying)
        {
            // === WALK MODE ===
            float inputX = Input.GetAxisRaw("Horizontal");

            // aim facing direction (direction lock is only for flying mode)
            if (inputX > 0.1f) facingDir = 1;
            else if (inputX < -0.1f) facingDir = -1;

            ApplyFacingVisual();

            // walk slowly
            rb.linearVelocity = new Vector2(inputX * walkSpeed, rb.linearVelocity.y);

            // drive anims
            SetAnimBool(isFlyingParam, false);
            SetAnimFloat(speedParam, Mathf.Abs(inputX));

            // takeoff on Space (direction becomes locked from this moment)
            if (Input.GetKeyDown(flapKey))
            {
                isFlying = true;

                SetAnimBool(isFlyingParam, true);

                // start moving in chosen direction + initial lift
                rb.linearVelocity = new Vector2(facingDir * flyMoveSpeed, flapForce);
            }

            return;
        }

        // === FLY MODE (direction locked) ===
        SetAnimBool(isFlyingParam, true);
        SetAnimFloat(speedParam, 1f); // always "moving" while flying

        rb.linearVelocity = new Vector2(facingDir * flyMoveSpeed, rb.linearVelocity.y);

        if (Input.GetKeyDown(flapKey))
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, flapForce);
    }

    // === Called by Animation Event at end of Spawn_Flyingform ===
    public void OnSpawnComplete()
    {
        canMove = true;
        rb.gravityScale = originalGravity;

        // Start in WALK mode: no forced horizontal speed
        rb.linearVelocity = new Vector2(0f, 0f);

        isFlying = false;
        SetAnimBool(isFlyingParam, false);
        SetAnimFloat(speedParam, 0f);

        ApplyFacingVisual();
    }

    // === Die on ANY collision, except landing on safe ground from above ===
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!canMove) return;

        foreach (var c in col.contacts)
        {
            bool hitFromAbove = c.normal.y > 0.6f;
            bool isSafeLayer = (safeGroundLayers.value & (1 << col.gameObject.layer)) != 0;

            if (hitFromAbove && isSafeLayer)
                return; // allowed landing

            var death = GetComponent<PlayerDeath>();
            if (death != null) death.Die();
            return;
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

    private void SetAnimFloat(string param, float value)
    {
        if (!anim || string.IsNullOrEmpty(param)) return;
        anim.SetFloat(param, value);
    }

    private void SetAnimBool(string param, bool value)
    {
        if (!anim || string.IsNullOrEmpty(param)) return;
        anim.SetBool(param, value);
    }
}
