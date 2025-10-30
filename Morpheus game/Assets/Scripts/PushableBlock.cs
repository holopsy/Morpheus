using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PushableBlock : MonoBehaviour
{
    [Header("Push Tuning")]
    [Tooltip("Minimum strength a pusher must have to move this block.")]
    public float requiredStrength = 10f;

    [Tooltip("How hard the block is pushed each physics step.")]
    public float pushForce = 60f;

    [Tooltip("Horizontal speed cap while being pushed.")]
    public float maxPushSpeed = 2.5f;

    private Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void OnCollisionStay2D(Collision2D col)
    {
        // Only the Power form has this component
        var pusher = col.collider.GetComponentInParent<PowerFormPusher>();
        if (pusher == null) return;
        if (pusher.strength < requiredStrength) return;

        // Determine if the player is actually pressing INTO the block on this contact
        // Contact normal points from THIS block toward the other collider
        foreach (var contact in col.contacts)
        {
            int intent = pusher.GetPushIntent(contact.normal);
            if (intent == 0) continue;

            // cap speed and apply a gentle force
            float vx = rb.linearVelocity.x;
            if (Mathf.Abs(vx) < maxPushSpeed || Mathf.Sign(vx) != intent)
            {
                rb.AddForce(new Vector2(intent * pushForce, 0f), ForceMode2D.Force);
            }
            // one good push per frame is enough
            break;
        }
    }
}