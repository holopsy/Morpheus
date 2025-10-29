// DamageOnTouch.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageOnTouch : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public bool onlyAffectsPlayer = true;

    [Header("Cooldown (per victim)")]
    public float hitCooldown = 0.5f; // prevents rapid multi-hits on same frame

    // Optional: small knockback
    public float knockbackForce = 6f;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // typically hazards are triggers
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // If you want “standing on spikes keeps hurting”, use Stay; else, delete this.
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (onlyAffectsPlayer && !other.CompareTag("Player")) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        // Respect per-victim i-frames/cooldown inside PlayerHealth
        Vector2 from = transform.position;
        health.TakeDamage(damage, from, knockbackForce);
    }
}