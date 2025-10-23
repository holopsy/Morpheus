using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PowerFormController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;       // slower than other forms
    public float jumpForce = 6f;       // average jump

    [Header("Carrying")]
    public Transform carryPoint;       // empty child where block sits
    private GameObject carriedObject;

    private Rigidbody2D rb;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Horizontal movement
        float moveX = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        // Jump (only if grounded)
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Pick up / drop with Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (carriedObject == null)
                TryPickup();
            else
                DropObject();
        }
    }

    void TryPickup()
    {
        // look for nearby "Movable" block
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 1f, LayerMask.GetMask("Block"));
        if (hit != null)
        {
            carriedObject = hit.gameObject;
            Rigidbody2D blockRb = carriedObject.GetComponent<Rigidbody2D>();
            Collider2D blockCol = carriedObject.GetComponent<Collider2D>();
            Collider2D playerCol = GetComponent<Collider2D>();

            // make block follow player
            blockRb.bodyType = RigidbodyType2D.Kinematic;
            Physics2D.IgnoreCollision(blockCol, playerCol, true);

            carriedObject.transform.SetParent(carryPoint);
            carriedObject.transform.localPosition = Vector3.zero;
        }
    }

    void DropObject()
    {
        if (carriedObject == null) return;

        Rigidbody2D blockRb = carriedObject.GetComponent<Rigidbody2D>();
        Collider2D blockCol = carriedObject.GetComponent<Collider2D>();
        Collider2D playerCol = GetComponent<Collider2D>();

        Physics2D.IgnoreCollision(blockCol, playerCol, false);
        blockRb.bodyType = RigidbodyType2D.Static;

        carriedObject.transform.SetParent(null);
        carriedObject = null;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.contacts[0].normal.y > 0.5f) isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        isGrounded = false;
    }
}
