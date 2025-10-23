using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class DefaultMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Attack")]
    public float attackRange = 1f;
    public int attackDamage = 1;
    public float attackCooldown = 0.5f;
    public LayerMask enemyLayer;           // set to the Enemy layer in Inspector
    public LineRenderer attackLine;        // optional: drag LineRenderer here
    public float lineDuration = 0.12f;

    Rigidbody2D rb;
    float moveInput;
    int facing = 1; // 1 = right, -1 = left
    float lastAttackTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (attackLine != null)
        {
            attackLine.enabled = false;
            attackLine.positionCount = 2;
            attackLine.startWidth = 0.05f;
            attackLine.endWidth = 0.05f;
        }
    }

    void Update()
    {
        // movement input
        moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        // attack input
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void Attack()
    {
        lastAttackTime = Time.time;

        Vector2 dir = (facing == 1) ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position + dir * (attackRange * 0.5f); // offset forward a bit

        // Overlap circle checks all colliders on the enemyLayer
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange, enemyLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            // support enemy health on the hit object or parent
            EnemyHealth eh = hits[i].GetComponentInParent<EnemyHealth>();
            if (eh != null)
            {
                eh.TakeDamage(attackDamage);
            }
        }

        // visual line
        if (attackLine != null)
        {
            Vector3 start = origin;
            Vector3 end = origin + dir * attackRange;
            StartCoroutine(ShowAttackLine(start, end));
        }
    }

    IEnumerator ShowAttackLine(Vector3 start, Vector3 end)
    {
        if (attackLine == null) yield break;
        attackLine.SetPosition(0, start);
        attackLine.SetPosition(1, end);
        attackLine.enabled = true;
        yield return new WaitForSeconds(lineDuration);
        attackLine.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        // visualize attack circle in editor
        Vector2 dir = (facing == 1) ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position + dir * (attackRange * 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}
