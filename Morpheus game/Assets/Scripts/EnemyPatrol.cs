using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    public float speed = 2f;
    public Transform groundCheck;        // child placed slightly ahead of feet
    public float groundCheckDistance = 0.9f;
    public LayerMask groundLayer;        // assign platforms/ground
    public LayerMask obstacleLayer;      // assign walls/obstacles (so no "tag" required)

    Rigidbody2D rb;
    bool movingRight = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Move horizontally
        float h = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(h * speed, rb.linearVelocity.y);
    }

    void Update()
    {
        // Ground check a little ahead of the enemy depending on facing
        Vector2 checkPos = groundCheck.position;
        // cast downwards from forward edge
        RaycastHit2D groundInfo = Physics2D.Raycast(checkPos, Vector2.down, groundCheckDistance, groundLayer);

        if (groundInfo.collider == null)
        {
            Flip();
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // If collided object is in obstacleLayer, flip
        if ((obstacleLayer.value & (1 << col.gameObject.layer)) != 0)
        {
            Flip();
            return;
        }

        // Also if contact normal suggests a frontal hit, flip
        foreach (var contact in col.contacts)
        {
            if (Mathf.Abs(contact.normal.x) > 0.5f)
            {
                Flip();
                break;
            }
        }
    }

    void Flip()
    {
        movingRight = !movingRight;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (movingRight ? 1f : -1f);
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }
    }
}
