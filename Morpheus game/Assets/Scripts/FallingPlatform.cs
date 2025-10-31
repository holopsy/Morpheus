using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Timing")]
    public float fallDelay = 0.4f;      // seconds before it starts falling
    public float disableDelay = 3f;     // seconds after fall before hiding

    [Header("Respawn")]
    [Tooltip("Re-enable & reset this platform when the player respawns at a checkpoint.")]
    public bool respawnOnCheckpoint = true;

    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer[] renderers;

    Vector3 startPosition;
    Quaternion startRotation;
    Vector3 startScale;

    bool triggered;
    bool hidden; // currently disabled (waiting for respawn)
    Coroutine fallRoutineCo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale    = transform.localScale;

        rb.gravityScale = 0f; // stays static until triggered
    }

    void OnEnable()
    {
        if (respawnOnCheckpoint)
            CheckpointEvents.OnPlayerRespawned += HandlePlayerRespawned;
    }

    void OnDisable()
    {
        if (respawnOnCheckpoint)
            CheckpointEvents.OnPlayerRespawned -= HandlePlayerRespawned;
    }

    void OnCollisionEnter2D(Collision2D colInfo)
    {
        if (triggered || hidden) return;
        if (colInfo.collider.CompareTag("Player"))
        {
            triggered = true;
            if (fallRoutineCo != null) StopCoroutine(fallRoutineCo);
            fallRoutineCo = StartCoroutine(FallRoutine());
        }
    }

    IEnumerator FallRoutine()
    {
        yield return new WaitForSeconds(fallDelay);

        rb.gravityScale = 2f; // start falling

        yield return new WaitForSeconds(disableDelay);

        HidePlatform();
        fallRoutineCo = null;
    }

    void HidePlatform()
    {
        hidden = true;
        triggered = false;

        // Stop physics + collisions
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        if (col) col.enabled = false;

        // Hide visuals
        if (renderers != null)
            foreach (var r in renderers) if (r) r.enabled = false;
    }

    void HandlePlayerRespawned()
    {
        // No matter what state the platform is in (idle, falling, or hidden), reset it.
        if (fallRoutineCo != null) { StopCoroutine(fallRoutineCo); fallRoutineCo = null; }
        ResetPlatform();
    }

    void ResetPlatform()
    {
        // Restore transform
        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = startScale;

        // Restore physics
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = true;

        // Restore collisions + visuals
        if (col) col.enabled = true;
        if (renderers != null)
            foreach (var r in renderers) if (r) r.enabled = true;

        hidden = false;
        triggered = false;
    }
}
