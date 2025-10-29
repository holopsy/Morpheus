// PlayerHealth.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    public int current;

    [Header("Invulnerability")]
    public float invulnTime = 0.8f;    // seconds of i-frames after hit
    private float invulnUntil;

    [Header("Feedback")]
    public Animator animator;          // assign your form’s Animator (optional for hit flash)
    public string hurtTrigger = "Hurt"; // optional trigger if you have it

    private Rigidbody2D rb;
    private PlayerDeath death;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        death = GetComponent<PlayerDeath>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        if (current <= 0 || current > maxHealth)
            current = maxHealth;
    }

    public void TakeDamage(int amount, Vector2 hitFrom, float knockbackForce = 0f)
    {
        if (Time.time < invulnUntil) return; // i-frames

        current -= amount;
        invulnUntil = Time.time + invulnTime;

        // Knockback (optional)
        if (rb && knockbackForce > 0f)
        {
            Vector2 dir = ((Vector2)transform.position - hitFrom).normalized;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // clear horizontal for consistency
            rb.AddForce(new Vector2(Mathf.Sign(dir.x), 1f).normalized * knockbackForce, ForceMode2D.Impulse);
        }

        // Hit feedback
        if (animator && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        // Death
        if (current <= 0)
        {
            if (death != null)
                death.Die();
            else
                Debug.LogWarning("PlayerHealth: No PlayerDeath found to handle death.");
        }
    }

    public bool IsInvulnerable() => Time.time < invulnUntil;
}