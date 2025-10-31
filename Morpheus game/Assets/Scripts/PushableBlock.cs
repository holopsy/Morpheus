using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PushableBlock : MonoBehaviour
{
    [Header("Push Tuning")]
    [Tooltip("Minimum strength a pusher must have to move this block.")]
    public float requiredStrength = 10f;

    [Tooltip("How hard the block is pushed each physics step when authorized.")]
    public float pushForce = 60f;

    [Tooltip("Horizontal speed cap while being pushed.")]
    public float maxPushSpeed = 2.5f;

    [Tooltip("Velocities smaller than this are snapped to 0 when not pushed (X only).")]
    public float snapDeadzoneX = 0.01f;

    Rigidbody2D rb;
    bool pushedThisStep;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Inspector for this block:
        // - Rigidbody2D: Body Type = Dynamic
        // - Constraints: Freeze Rotation Z (ON)   <-- let the inspector handle this
        // - Gravity Scale: as you like (falls into holes as usual)
    }

    void FixedUpdate()
    {
        if (!pushedThisStep)
        {
            // Nobody valid pushing -> kill X drift so other forms can't nudge it.
            float vx = Mathf.Abs(rb.linearVelocity.x) <= snapDeadzoneX ? 0f : 0f;
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
            // Y is untouched so it can fall into holes.
        }
        else
        {
            // Clamp horizontal speed when being pushed
            float vx = Mathf.Clamp(rb.linearVelocity.x, -maxPushSpeed, maxPushSpeed);
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        pushedThisStep = false;
    }

    void OnCollisionStay2D(Collision2D col)
    {
        // 1) The collider we’re touching must belong to the *active* Power form.
        //    We detect that by requiring a PowerFormController in its parents that is active+enabled.
        var powerController = col.collider.GetComponentInParent<PowerFormController>();
        if (powerController == null) return;                     // Not the Power form
        if (!powerController.isActiveAndEnabled) return;         // Power form not the active form right now

        // 2) That same hierarchy must have a PowerFormPusher (the component that knows input/strength).
        var pusher = col.collider.GetComponentInParent<PowerFormPusher>();
        if (pusher == null) return;
        if (!pusher.isActiveAndEnabled) return;
        if (pusher.strength < requiredStrength) return;          // Not strong enough

        // 3) Player must be pressing INTO the block on at least one contact.
        foreach (var contact in col.contacts)
        {
            // contact.normal points from THIS block toward the player's collider
            int intent = pusher.GetPushIntent(contact.normal);
            if (intent == 0) continue;

            // Apply horizontal push force
            rb.AddForce(new Vector2(intent * pushForce, 0f), ForceMode2D.Force);

            // Mark as valid push this step so FixedUpdate doesn't zero X
            pushedThisStep = true;

            break; // one good push per step is enough
        }
    }
}
