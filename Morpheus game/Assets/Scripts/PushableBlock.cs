using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PushableBlock : MonoBehaviour
{
    [Header("Push Tuning")]
    public float requiredStrength = 10f;
    public float pushForce = 60f;
    public float maxPushSpeed = 2.5f;

    [Header("X Lock")]
    [Tooltip("How long (seconds) X stays unlocked after the last valid Power push contact.")]
    public float unlockXGraceTime = 0.25f;

    Rigidbody2D rb;
    float unlockTimer;

    // Keep whatever constraints you set in Inspector (ex: Freeze Rotation Z),
    // and we’ll add/remove FreezePositionX on top.
    RigidbodyConstraints2D baseConstraints;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseConstraints = rb.constraints;
    }

    void FixedUpdate()
    {
        // Count down the unlock timer
        if (unlockTimer > 0f)
            unlockTimer -= Time.fixedDeltaTime;

        bool allowX = unlockTimer > 0f;

        if (!allowX)
        {
            // Lock X so non-Power forms physically cannot shove it sideways
            rb.constraints = baseConstraints | RigidbodyConstraints2D.FreezePositionX;

            // Also kill any leftover X velocity (keeps it stable)
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0f, 10f * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );

        }
        else
        {
            // Unlock X while Power is pushing
            rb.constraints = baseConstraints;

            // Clamp horizontal speed while being pushed
            float vx = Mathf.Clamp(rb.linearVelocity.x, -maxPushSpeed, maxPushSpeed);
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }
    }

    void OnCollisionStay2D(Collision2D col)
    {
        // Must be the active Power form
        var powerController = col.collider.GetComponentInParent<PowerFormController>();
        if (powerController == null) return;
        if (!powerController.isActiveAndEnabled) return;

        // Must have pusher component
        var pusher = col.collider.GetComponentInParent<PowerFormPusher>();
        if (pusher == null) return;
        if (!pusher.isActiveAndEnabled) return;
        if (pusher.strength < requiredStrength) return;

        // Require push intent into the block
        foreach (var contact in col.contacts)
        {
            int intent = pusher.GetPushIntent(contact.normal);
            if (intent == 0) continue;

            // Unlock X for a short moment so force can move it
            unlockTimer = unlockXGraceTime;

            rb.AddForce(new Vector2(intent * pushForce, 0f), ForceMode2D.Force);
            break;
        }
    }
}
