using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageOnTouch : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 1;
    public bool onlyAffectsPlayer = true;
    public float knockbackForce = 6f;
    public float hitCooldown = 0.5f; // prevents repeated damage per second

    private float _nextHitTime;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true; // must be trigger for contact
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // ignore unless cooldown passed
        if (Time.time < _nextHitTime) return;

        // only damage player
        if (onlyAffectsPlayer && !other.CompareTag("Player")) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            _nextHitTime = Time.time + hitCooldown;

            // hit from direction of enemy to player
            Vector2 hitFrom = transform.position;
            health.TakeDamage(damage, hitFrom, knockbackForce);
        }
    }
}