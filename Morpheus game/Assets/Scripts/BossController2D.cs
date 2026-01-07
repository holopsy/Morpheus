using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController2D : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHP = 5;
    public float hitInvincibilityTime = 0.35f;

    
    [Header("Phase 2")]
    [Tooltip("When HP <= this, boss enters phase 2 (faster / more aggressive).")]
    public int phase2AtOrBelowHP = 2;
    public float phase2SpeedMultiplier = 1.4f;

    [Header("Movement / Patrol")]
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 2.5f;
    public float waitAtEnds = 0.5f;

    [Header("Flip Visual")]
    public Transform visualToFlip;

    [Header("Attack (no animation needed)")]
    public bool enableTouchAttack = true;
    public int touchDamage = 1;
    public float touchCooldown = 0.8f;
    public Vector2 touchBoxOffset = new Vector2(0.9f, 0f);
    public Vector2 touchBoxSize = new Vector2(1.4f, 1.2f);
    public LayerMask playerLayer;

    [Header("Events")]
    public UnityEvent OnBossDefeated;

    [Header("Optional UI")]
    public BossHeartsUI heartsUI; // assign if you use the hearts

    private Rigidbody2D rb;
    private int currentHP;
    private bool dead;
    private float invUntil;
    private float touchUntil;

    private int facing = 1;
    private Vector2 target;
    private bool waiting;
    private float waitUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;

        if (!heartsUI) heartsUI = GetComponentInChildren<BossHeartsUI>(true);
        if (heartsUI) heartsUI.SetMax(maxHP);

        // If patrol points not set, make simple defaults
        if (!pointA || !pointB)
        {
            Debug.LogWarning("BossController2D: Assign pointA and pointB (patrol points).");
        }
    }

    private void Start()
    {
        if (pointA) target = pointB ? pointB.position : pointA.position;
        ApplyFacingVisual();
        if (heartsUI) heartsUI.SetHP(currentHP);
    }

    private void FixedUpdate()
    {
        if (dead) return;
        if (!pointA || !pointB) return;

        if (waiting)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (Time.time >= waitUntil) waiting = false;
            return;
        }

        float speed = moveSpeed * (IsPhase2() ? phase2SpeedMultiplier : 1f);

        Vector2 pos = rb.position;
        Vector2 dir = (target - pos);
        float dist = dir.magnitude;

        if (dist < 0.1f)
        {
            // reached end -> wait, then swap target
            waiting = true;
            waitUntil = Time.time + waitAtEnds;
            target = (Vector2) (target == (Vector2)pointA.position ? pointB.position : pointA.position);
            return;
        }

        dir.Normalize();
        rb.linearVelocity = new Vector2(dir.x * speed, rb.linearVelocity.y);

        // facing based on movement direction
        if (dir.x > 0.05f) facing = 1;
        else if (dir.x < -0.05f) facing = -1;

        ApplyFacingVisual();

        if (enableTouchAttack)
            TryTouchAttack();
    }

    private void TryTouchAttack()
    {
        if (Time.time < touchUntil) return;

        Vector2 center = (Vector2)transform.position + new Vector2(touchBoxOffset.x * facing, touchBoxOffset.y);
        Collider2D hit = Physics2D.OverlapBox(center, touchBoxSize, 0f, playerLayer);

        if (hit)
        {
            var ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                Vector2 hitFrom = (ph.transform.position - transform.position).normalized;
                ph.TakeDamage(touchDamage, hitFrom, 0f);
            }

            touchUntil = Time.time + touchCooldown;
        }
    }


    public void TakeDamage(int dmg)
    {
        if (dead) return;
        if (Time.time < invUntil) return;

        currentHP -= Mathf.Max(1, dmg);
        invUntil = Time.time + hitInvincibilityTime;

        if (heartsUI) heartsUI.SetHP(currentHP);

        if (currentHP <= 0)
            Die();
    }

    private bool IsPhase2() => currentHP <= phase2AtOrBelowHP;

    private void Die()
    {
        if (dead) return;
        dead = true;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // disable colliders so player doesn't get stuck
        var cols = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) c.enabled = false;

        OnBossDefeated?.Invoke();

        // you can destroy or play animation here
        Destroy(gameObject, 0.5f);
    }

    private void ApplyFacingVisual()
    {
        if (!visualToFlip) return;
        var s = visualToFlip.localScale;
        s.x = Mathf.Abs(s.x) * (facing >= 0 ? 1 : -1);
        visualToFlip.localScale = s;
    }

    private void OnDrawGizmosSelected()
    {
        if (enableTouchAttack)
        {
            Gizmos.color = Color.red;
            Vector2 center = (Vector2)transform.position + new Vector2(touchBoxOffset.x, touchBoxOffset.y);
            Gizmos.DrawWireCube(center, touchBoxSize);
            Vector2 center2 = (Vector2)transform.position + new Vector2(-touchBoxOffset.x, touchBoxOffset.y);
            Gizmos.DrawWireCube(center2, touchBoxSize);
        }

        if (pointA && pointB)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawWireSphere(pointA.position, 0.1f);
            Gizmos.DrawWireSphere(pointB.position, 0.1f);
        }
    }
}
