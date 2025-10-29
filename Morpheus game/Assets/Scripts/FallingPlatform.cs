using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Timing")]
    public float fallDelay = 0.4f;      // seconds before it starts falling
    public float destroyDelay = 3f;     // seconds after fall before destroy

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private bool triggered;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        rb.gravityScale = 0f; // stays static until triggered
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Detect if Player touched
        if (triggered) return;
        if (col.collider.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(FallRoutine());
        }
    }

    IEnumerator FallRoutine()
    {
        yield return new WaitForSeconds(fallDelay);

        rb.gravityScale = 2f; // turn on gravity
        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
        // Or, if you prefer respawn instead of destroy:
        // ResetPlatform();
    }

    void ResetPlatform()
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        transform.position = startPosition;
        triggered = false;
    }
}