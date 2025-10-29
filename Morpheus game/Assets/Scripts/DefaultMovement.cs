using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class DefaultMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Attack")]
    public float attackCooldown = 0.35f;
    public LayerMask enemyLayer;
    [Tooltip("Center of the attack box relative to the player root when facing RIGHT.")]
    public Vector2 attackBoxOffset = new Vector2(0.8f, 0f);
    public Vector2 attackBoxSize   = new Vector2(1.2f, 1.0f);
    public int attackDamage = 1;

    [Header("Visuals / Animator")]
    public Transform visualToFlip;   // assign the "Visual" child
    public Animator animator;        // assign Animator on Visual

    [Header("Spawn")]
    [Tooltip("Seconds to ignore input after spawn. Set to your spawn clip length. If you use an Animation Event to call EndSpawn(), set this to 0.")]
    public float spawnLockDuration = 0.6f;
    [Tooltip("Animator state name of the spawn clip.")]
    public string spawnStateName = "Spawn_Defaultform";

    private Rigidbody2D rb;
    private float moveInput;
    private int facing = 1;          // 1 = right, -1 = left
    private float lastAttackTime = -999f;
    private float spawnUnlockTime = 0f;
    private bool inSpawn = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // Ensure visual matches current facing *before* playing spawn
        ApplyFacingVisual();

        // Play spawn and lock controls
        inSpawn = true;
        if (animator && !string.IsNullOrEmpty(spawnStateName))
        {
            animator.Play(spawnStateName, 0, 0f);
        }
        spawnUnlockTime = (spawnLockDuration > 0f) ? Time.time + spawnLockDuration : 0f;
    }

    // Called by MorphManager right after instantiation
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

    // Optional Animation Event hook at the end of the spawn clip
    public void EndSpawn()
    {
        inSpawn = false;
    }

    void Update()
    {
        // Unlock if time-based
        if (inSpawn && spawnLockDuration > 0f && Time.time >= spawnUnlockTime)
            inSpawn = false;

        // --- Movement input (ignored during spawn) ---
        moveInput = inSpawn ? 0f : Input.GetAxisRaw("Horizontal");

        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        // Keep visual aligned with current facing every frame
        ApplyFacingVisual();

        // Animator params
        if (animator)
        {
            animator.SetBool("IsRunning", !inSpawn && Mathf.Abs(moveInput) > 0.01f);
            animator.SetFloat("Speed", Mathf.Abs(rb ? rb.linearVelocity.x : 0f));
        }

        // --- Attack input (ignored during spawn) ---
        if (!inSpawn && Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            DoAttackNow();
            if (animator) animator.SetTrigger("Attack");
        }
    }

    void FixedUpdate()
    {
        // No horizontal motion during spawn
        float x = inSpawn ? 0f : moveInput * moveSpeed;
        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
    }

    void DoAttackNow()
    {
        lastAttackTime = Time.time;

        Vector2 localOffset = new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);
        Vector2 center = (Vector2)transform.position + localOffset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, enemyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            var eh = hits[i].GetComponentInParent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(attackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        int f = Application.isPlaying ? facing : 1;
        Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * f, attackBoxOffset.y);
        Gizmos.DrawWireCube(center, attackBoxSize);
    }
}
