using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingFormController : MonoBehaviour
{
    public float moveSpeed = 5f;   // horizontal speed once active
    public float flapForce = 6f;   // upward flap strength

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

        // Freeze physics during spawn
        originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        canMove = false;
    }

    // Called by MorphManager
    public void InitializeDirection(int dir)
    {
        facingDir = (dir >= 0) ? 1 : -1;
        initialized = true;

        // Don't start moving yet; we wait for OnSpawnComplete
        // Optional: visually flip the sprite here if needed.
        // var sr = GetComponentInChildren<SpriteRenderer>();
        // if (sr) sr.flipX = (facingDir == -1);
    }

    void Start()
    {
        if (!initialized)
            facingDir = (transform.localScale.x < 0) ? -1 : 1;
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

        // 👇 Add this part
        if (anim)
        {
            anim.CrossFade("Fly_Flyingform", 0.05f, 0);
            // or use anim.Play("Fly_Flyingform", 0, 0f); if CrossFade doesn't work
        }
    }
}
