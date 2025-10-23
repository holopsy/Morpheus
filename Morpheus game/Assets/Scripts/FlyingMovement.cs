using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingFormController : MonoBehaviour
{
    public float moveSpeed = 5f;   // constant horizontal speed
    public float flapForce = 6f;   // upward flap strength

    private Rigidbody2D rb;
    private int facingDir = 1;     // 1 = right, -1 = left
    private bool initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Called by MorphManager right after instantiate
    public void InitializeDirection(int dir)
    {
        facingDir = (dir >= 0) ? 1 : -1;
        initialized = true;

        // apply immediate horizontal velocity in that direction
        rb.linearVelocity = new Vector2(facingDir * moveSpeed, rb.linearVelocity.y);

        // (optional) flip the visual if your sprite should mirror
        // var sr = GetComponentInChildren<SpriteRenderer>();
        // if (sr) sr.flipX = (facingDir == -1);
    }

    void Start()
    {
        // Fallback: if MorphManager didn’t call InitializeDirection,
        // infer from scale (not ideal, but safe)
        if (!initialized)
        {
            facingDir = (transform.localScale.x < 0) ? -1 : 1;
            rb.linearVelocity = new Vector2(facingDir * moveSpeed, rb.linearVelocity.y);
        }
    }

    void Update()
    {
        // keep moving in locked direction
        rb.linearVelocity = new Vector2(facingDir * moveSpeed, rb.linearVelocity.y);

        // flap upwards on Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, flapForce);
        }
    }
}