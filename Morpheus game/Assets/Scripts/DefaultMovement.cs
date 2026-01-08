using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DefaultMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Tiny Jump")]
    public float jumpForceSmall = 3f;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.15f;
    public LayerMask groundLayer = ~0;

    [Header("Wall Check (prevents sticking)")]
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float wallCheckRadius = 0.15f;
    public LayerMask wallLayer;

    [Header("Attack")]
    public KeyCode attackKey = KeyCode.F;
    public float attackCooldown = 0.35f;
    public LayerMask enemyLayer;
    public Vector2 attackBoxOffset = new Vector2(0.8f, 0f);
    public Vector2 attackBoxSize = new Vector2(1.2f, 1.0f);
    public int attackDamage = 1;

    [Header("Visuals / Animator")]
    public Transform visualToFlip;
    public Animator animator;
    public string jumpTriggerName = "Jump";
    public string onGroundBoolName = "OnGround";

    [Header("Spawn")]
    public float spawnLockDuration = 0.6f;
    public string spawnStateName = "Spawn_Defaultform";

    private Rigidbody2D rb;
    private float moveInput;
    private int facing = 1;
    private float lastAttackTime = -999f;
    private float spawnUnlockTime = 0f;
    private bool inSpawn = false;
    private bool grounded = false;

    bool onLeftWall, onRightWall;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        ApplyFacingVisual();

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

    private void ApplyFacingVisual()
    {
        if (visualToFlip != null)
        {
            var s = visualToFlip.localScale;
            s.x = Mathf.Abs(s.x) * (facing >= 0 ? 1 : -1);
            visualToFlip.localScale = s;
        }
    }

    public void EndSpawn() => inSpawn = false;

    void Update()
    {

        if (inSpawn && spawnLockDuration > 0f && Time.time >= spawnUnlockTime)
            inSpawn = false;

        moveInput = inSpawn ? 0f : Input.GetAxisRaw("Horizontal");

// ✅ FOOTSTEPS (correct place)
        if (grounded && Mathf.Abs(moveInput) > 0.01f)
            AudioManager.I.StartFootsteps(SoundLibrary.I.walk);
        else
            AudioManager.I.StopFootsteps();

        moveInput = inSpawn ? 0f : Input.GetAxisRaw("Horizontal");

        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        ApplyFacingVisual();

        if (!inSpawn && grounded && Input.GetKeyDown(jumpKey) && jumpForceSmall > 0.01f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForceSmall, ForceMode2D.Impulse);

            // jump SFX placeholder (you can add a real jump clip later)
            AudioManager.I?.PlaySFX(SoundLibrary.I?.jump);

            TrySetTrigger(animator, jumpTriggerName);
            TrySetBool(animator, onGroundBoolName, false);
        }

        if (animator)
        {
            animator.SetBool("IsRunning", !inSpawn && Mathf.Abs(moveInput) > 0.01f);
            animator.SetFloat("Speed", Mathf.Abs(rb ? rb.linearVelocity.x : 0f));
            TrySetBool(animator, onGroundBoolName, grounded);
        }

        if (!inSpawn && Input.GetKeyDown(attackKey) && Time.time >= lastAttackTime + attackCooldown)
        {
            AudioManager.I?.PlaySFX(SoundLibrary.I?.attack);

            DoAttackNow();
            if (animator) animator.SetTrigger("Attack");
        }
    }

    void FixedUpdate()
    {
        grounded = groundCheck
            ? Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer)
            : false;

        onLeftWall = wallCheckLeft && Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);
        onRightWall = wallCheckRight && Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);

        float x = inSpawn ? 0f : moveInput * moveSpeed;

        if ((onLeftWall && x < 0f) || (onRightWall && x > 0f))
            x = 0f;

        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
    }

    void DoAttackNow()
    {
        lastAttackTime = Time.time;

        Vector2 localOffset = new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);
        Vector2 center = (Vector2)transform.position + localOffset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, enemyLayer);

        var damaged = new System.Collections.Generic.HashSet<IDamageable>();

        for (int i = 0; i < hits.Length; i++)
        {
            var dmg = hits[i].GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            if (damaged.Contains(dmg)) continue;
            damaged.Add(dmg);

            dmg.TakeDamage(attackDamage);
        }
    }

    void TrySetTrigger(Animator anim, string trigger)
    {
        if (!anim || string.IsNullOrEmpty(trigger)) return;
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == trigger)
            { anim.SetTrigger(trigger); return; }
    }

    void TrySetBool(Animator anim, string name, bool value)
    {
        if (!anim || string.IsNullOrEmpty(name)) return;
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == name)
            { anim.SetBool(name, value); return; }
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
