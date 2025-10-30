using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    [Tooltip("Current HP at runtime")]
    public int current;

    [Header("Invulnerability")]
    public float invulnTime = 0.8f;    // seconds of i-frames
    private float invulnUntil;

    [Header("Animator Hooks")]
    public Animator animator;                  // assign your form’s Animator (or leave null to auto-find)
    public string hurtTrigger = "Hurt";        // must match Animator parameter exactly (case-sensitive)
    public string dieTrigger  = "Die";         // must match Animator parameter exactly

    private Rigidbody2D rb;
    private PlayerDeath death;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        death = GetComponent<PlayerDeath>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        if (current <= 0 || current > maxHealth) current = maxHealth;
        Debug.Log($"Player HP initialized: {current}/{maxHealth}");
    }

    public void TakeDamage(int amount, Vector2 hitFrom, float knockbackForce = 0f)
    {
        if (Time.time < invulnUntil) return; // still invulnerable

        current = Mathf.Max(0, current - amount);
        invulnUntil = Time.time + invulnTime;

        // Knockback (optional)
        if (rb && knockbackForce > 0f)
        {
            Vector2 dir = ((Vector2)transform.position - hitFrom).normalized;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.AddForce(new Vector2(Mathf.Sign(dir.x), 1f).normalized * knockbackForce, ForceMode2D.Impulse);
        }

        // Hurt feedback (only if the trigger exists)
        TrySetTrigger(animator, hurtTrigger);

        Debug.Log($"Player took {amount} damage. HP: {current}/{maxHealth}");

        if (current <= 0)
        {
            // Death animation trigger (safe)
            TrySetTrigger(animator, dieTrigger);

            if (death != null) death.Die();
            else Debug.LogWarning("PlayerHealth: No PlayerDeath on player to handle death.");
        }
    }

    // Safely fire a trigger only if it exists on the Animator
    private void TrySetTrigger(Animator anim, string triggerName)
    {
        if (!anim || string.IsNullOrEmpty(triggerName)) return;
        // Check parameter exists to avoid console spam
        foreach (var p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
            {
                anim.SetTrigger(triggerName);
                return;
            }
        }
        // If you see this log, create the trigger in the Animator or update the name in this script
        // Debug.LogWarning($"Animator trigger '{triggerName}' not found on {anim.gameObject.name}.");
    }
}
