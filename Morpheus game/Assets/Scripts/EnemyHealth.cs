using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float deathDestroyDelay = 0.8f; // if you don’t use an animation event

    int current;
    bool dead;

    Animator anim;
    Rigidbody2D rb;
    Collider2D[] allColliders;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb   = GetComponent<Rigidbody2D>();
        allColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
    }

    void Start()
    {
        current = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        if (dead) return;

        current -= dmg;
        if (current <= 0) Die();
    }

    void Die()
    {
        if (dead) return;
        dead = true;

        // 1) Stop behaviour scripts
        var patrol = GetComponent<EnemyPatrol>();   if (patrol) patrol.enabled = false;
        var dot    = GetComponentInChildren<DamageOnTouch>(true); if (dot) dot.enabled = false;
        var drop   = GetComponent<EnemyDropper>();  if (drop) drop.Drop();

        // 2) Make the corpse NON-INTERACTIVE immediately
        //    - disable ALL colliders (root + children, including HurtBox)
        if (allColliders != null)
        {
            foreach (var c in allColliders) if (c) c.enabled = false;
        }

        //    - remove from physics sim so it can't block the player
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // safest: no physics, no collisions
        }

        // 3) Play death animation
        if (anim) anim.SetTrigger("Die");

        // 4) Destroy after anim (or use event to call OnDeathAnimationComplete)
        Destroy(gameObject, deathDestroyDelay);
    }

    // Call this via Animation Event at the end of the Death clip (optional)
    public void OnDeathAnimationComplete()
    {
        Destroy(gameObject);
    }
}