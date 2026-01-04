using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Timing")]
    public float fallDelay = 0.4f;      // seconds before it starts falling
    public float disableDelay = 3f;     // seconds after fall before hiding

    [Header("Trigger (surface-only, reliable)")]
    [Tooltip("Only objects with this tag can trigger the platform.")]
    public string playerTag = "Player";

    [Tooltip("How thick the 'top surface trigger zone' is (world units). Smaller = stricter.")]
    public float surfaceZoneHeight = 0.06f;

    [Tooltip("Shrink the surface zone width on left/right so corner grazes don't count.")]
    public float sideInset = 0.08f;

    [Tooltip("How far above the collider top to place the zone (helps avoid side contacts).")]
    public float surfaceZoneOffset = 0.01f;

    [Header("Fall Physics")]
    public float fallGravity = 2f;

    [Header("Respawn - Checkpoint")]
    public bool respawnOnCheckpoint = true;

    [Header("Respawn - Timed (optional)")]
    public bool timedRespawn = false;

    [Min(0.1f)]
    public float respawnTime = 2f;

    Rigidbody2D rb;
    Collider2D platformCol;
    SpriteRenderer[] renderers;

    Vector3 startPosition;
    Quaternion startRotation;
    Vector3 startScale;

    bool triggered;
    bool hidden;

    Coroutine fallRoutineCo;
    Coroutine timedRespawnCo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCol = GetComponent<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;

        SetIdleState();
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

    // We check in Stay so it triggers when you actually LAND, not when you graze a corner mid-air.
    void OnCollisionStay2D(Collision2D c)
    {
        if (triggered || hidden) return;
        if (!c.collider.CompareTag(playerTag)) return;

        if (!IsStandingOnTop(c.collider)) return;

        TriggerFall();
    }

    void TriggerFall()
    {
        if (triggered || hidden) return;

        triggered = true;

        if (timedRespawnCo != null) { StopCoroutine(timedRespawnCo); timedRespawnCo = null; }
        if (fallRoutineCo != null) StopCoroutine(fallRoutineCo);

        fallRoutineCo = StartCoroutine(FallRoutine());
    }

    bool IsStandingOnTop(Collider2D playerCol)
    {
        Bounds tb = platformCol.bounds;

        // Create a thin box just above the platform's top surface.
        float topY = tb.max.y + surfaceZoneOffset;

        float width = tb.size.x - (sideInset * 2f);
        if (width <= 0.01f) width = 0.01f;

        Vector2 boxSize = new Vector2(width, surfaceZoneHeight);
        Vector2 boxCenter = new Vector2(tb.center.x, topY + (surfaceZoneHeight * 0.5f));

        // OverlapBox (no layers): filter by tag manually.
        // This checks "is the player's collider actually overlapping the top surface zone?"
        Collider2D hit = Physics2D.OverlapBox(boxCenter, boxSize, 0f);
        if (hit == null) return false;

        // If multiple colliders exist, OverlapBox returns the first found.
        // To be safer, use OverlapBoxAll and check any with tag.
        // We'll do that for reliability:
        var hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag(playerTag))
                return true;
        }

        return false;
    }

    IEnumerator FallRoutine()
    {
        yield return new WaitForSeconds(fallDelay);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.gravityScale = fallGravity;

        yield return new WaitForSeconds(disableDelay);

        HidePlatform();
        fallRoutineCo = null;

        if (timedRespawn)
        {
            if (timedRespawnCo != null) StopCoroutine(timedRespawnCo);
            timedRespawnCo = StartCoroutine(TimedRespawnRoutine());
        }
    }

    IEnumerator TimedRespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        timedRespawnCo = null;

        if (hidden)
            ResetPlatform();
    }

    void HidePlatform()
    {
        hidden = true;
        triggered = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        if (platformCol) platformCol.enabled = false;

        if (renderers != null)
            foreach (var r in renderers) if (r) r.enabled = false;
    }

    void HandlePlayerRespawned()
    {
        if (fallRoutineCo != null) { StopCoroutine(fallRoutineCo); fallRoutineCo = null; }
        if (timedRespawnCo != null) { StopCoroutine(timedRespawnCo); timedRespawnCo = null; }
        ResetPlatform();
    }

    void ResetPlatform()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = startScale;

        if (platformCol) platformCol.enabled = true;

        if (renderers != null)
            foreach (var r in renderers) if (r) r.enabled = true;

        hidden = false;
        triggered = false;

        SetIdleState();
    }

    void SetIdleState()
    {
        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Visualize the surface zone in the editor for easy tuning
        var c = GetComponent<Collider2D>();
        if (c == null) return;

        Bounds tb = c.bounds;
        float topY = tb.max.y + surfaceZoneOffset;

        float width = tb.size.x - (sideInset * 2f);
        if (width <= 0.01f) width = 0.01f;

        Vector2 boxSize = new Vector2(width, surfaceZoneHeight);
        Vector2 boxCenter = new Vector2(tb.center.x, topY + (surfaceZoneHeight * 0.5f));

        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
#endif
}
