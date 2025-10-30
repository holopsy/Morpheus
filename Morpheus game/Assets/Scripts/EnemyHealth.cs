using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float deathDestroyDelay = 0.8f; // used if you don’t add an animation event

    int current;
    bool dead;

    Animator anim;
    Rigidbody2D rb;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb   = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        current = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        if (dead) return;
        current -= dmg;
        Debug.Log($"{gameObject.name} took {dmg}. HP left: {current}");
        if (current <= 0) Die();
    }

    void Die()
    {
        if (dead) return;
        dead = true;

        // stop behaviour
        var patrol = GetComponent<EnemyPatrol>();     if (patrol) patrol.enabled = false;
        var touch  = GetComponent<DamageOnTouch>();   if (touch)  touch.enabled  = false;

        // stop movement/physics
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints |= RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
        }

        // play death anim
        if (anim) anim.SetTrigger("Die");

        // drop collectibles (now or via anim event if you want to time it)
        var dropper = GetComponent<EnemyDropper>();   if (dropper) dropper.Drop();

        // fallback destroy if no animation event used
        Destroy(gameObject, deathDestroyDelay);
    }

    // Hook this from the last frame of the Death clip (Animation Event)
    public void OnDeathAnimationComplete()
    {
        Destroy(gameObject);
    }
}