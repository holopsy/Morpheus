using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingFormController : MonoBehaviour
{
    [Header("Walk (before flying)")]
    public float walkSpeed = 2.2f;

    [Header("Fly (after takeoff)")]
    public float flyMoveSpeed = 5f;   // base horizontal speed once flying
    public float flapForce = 6f;      // upward flap strength
    public KeyCode flapKey = KeyCode.Space;

    [Header("Speed Ramp (optional)")]
    [Tooltip("If ON: while flying, horizontal speed slowly increases over time.")]
    public bool enableSpeedRamp = true;
    [Tooltip("How much speed you gain per second while flying. Keep small for subtle effect.")]
    public float speedRampPerSecond = 0.15f;
    [Tooltip("Max extra speed added on top of flyMoveSpeed.")]
    public float maxRampBonus = 2.0f;

    [Header("Colliders (Option A)")]
    [Tooltip("Square / normal collider used while walking (before takeoff).")]
    public Collider2D walkCollider;
    [Tooltip("Long / slim collider used while flying (after takeoff).")]
    public Collider2D flyCollider;

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

    // Speed ramp state
    private float rampBonus = 0f;

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

        rampBonus = 0f;

        // Start in WALK collider mode by default
        SetColliderMode(flying: false);

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
           
            // ✅ FOOTSTEPS (walk mode only)
            if (Mathf.Abs(inputX) > 0.1f)
                AudioManager.I.StartFootsteps(SoundLibrary.I.walk);
            else
                AudioManager.I.StopFootsteps();


            // aim facing direction
            if (inputX > 0.1f) facingDir = 1;
            else if (inputX < -0.1f) facingDir = -1;

            ApplyFacingVisual();

            // ensure colliders are correct in walk mode
            SetColliderMode(flying: false);

            // walk slowly
            rb.linearVelocity = new Vector2(inputX * walkSpeed, rb.linearVelocity.y);

            // drive anims
            SetAnimBool(isFlyingParam, false);
            SetAnimFloat(speedParam, Mathf.Abs(inputX));

            // takeoff on Space
            if (Input.GetKeyDown(flapKey))
            {
                // 🔊 Fly SFX on takeoff (Space)
                AudioManager.I?.PlaySFX(SoundLibrary.I?.flying);

                isFlying = true;
                rampBonus = 0f;

                SetAnimBool(isFlyingParam, true);

                // switch to flying collider immediately
                SetColliderMode(flying: true);

                float speedNow = GetFlySpeed();
                rb.linearVelocity = new Vector2(facingDir * speedNow, flapForce);
            }

            return;
        }

        // === FLY MODE (direction locked) ===
        AudioManager.I.StopFootsteps();
        SetAnimBool(isFlyingParam, true);
        SetAnimFloat(speedParam, 1f);

        // colliders correct in fly mode
        SetColliderMode(flying: true);

        // slowly build speed the longer you keep flying
        if (enableSpeedRamp)
            rampBonus = Mathf.Min(maxRampBonus, rampBonus + speedRampPerSecond * Time.deltaTime);

        float flySpeedNow = GetFlySpeed();
        rb.linearVelocity = new Vector2(facingDir * flySpeedNow, rb.linearVelocity.y);

        if (Input.GetKeyDown(flapKey))
        {
            // 🔊 Fly SFX on every flap (Space)
            AudioManager.I?.PlaySFX(SoundLibrary.I?.flying);

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, flapForce);
        }
    }

    private float GetFlySpeed()
    {
        return flyMoveSpeed + (enableSpeedRamp ? rampBonus : 0f);
    }

    private void SetColliderMode(bool flying)
    {
        if (walkCollider) walkCollider.enabled = !flying;
        if (flyCollider)  flyCollider.enabled  = flying;
    }

    // Called by Animation Event at end of Spawn_Flyingform
    public void OnSpawnComplete()
    {
        canMove = true;
        rb.gravityScale = originalGravity;

        // Start in WALK mode
        rb.linearVelocity = new Vector2(0f, 0f);

        isFlying = false;
        rampBonus = 0f;

        SetColliderMode(flying: false);

        SetAnimBool(isFlyingParam, false);
        SetAnimFloat(speedParam, 0f);
        

        ApplyFacingVisual();
    }

    // Die on ANY collision, except landing on safe ground from above
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
