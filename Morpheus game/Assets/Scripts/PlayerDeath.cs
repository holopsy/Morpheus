using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Trigger name that transitions Any State -> Death")]
    public string dieTriggerName = "Die";

    [Header("Timing")]
    [Tooltip("Used only if you don't use an Animation Event. Seconds to wait before respawn.")]
    public float deathAnimDuration = 0.9f;

    [Header("Physics")]
    public bool freezeDuringDeath = true;

    private bool _dying;
    private Animator _anim;
    private Rigidbody2D _rb;
    private RigidbodyConstraints2D _prevConstraints;

    void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
        _rb   = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Your spikes need a Collider2D with IsTrigger = true and tag = "Spike"
        if (_dying) return;
        if (collision.CompareTag("Spike"))
        {
            Die();
        }
    }

    public void Die()
    {
        if (_dying) return;
        _dying = true;

        // Stop movement/control scripts (simple & generic)
        ToggleMovement(false);

        // Zero out motion and optionally freeze the body
        if (_rb)
        {
            _rb.linearVelocity = Vector2.zero;
            if (freezeDuringDeath)
            {
                _prevConstraints = _rb.constraints;
                _rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }

        // Fire the animator trigger so Death plays immediately
        if (_anim && !string.IsNullOrEmpty(dieTriggerName))
            _anim.SetTrigger(dieTriggerName);

        // Wait for animation to finish, then respawn
        Invoke(nameof(RespawnAfterDeath), deathAnimDuration);
    }

    // Optional Animation Event hook — call this at the end of the Death clip
    public void OnDeathAnimationComplete()
    {
        CancelInvoke(nameof(RespawnAfterDeath));
        RespawnAfterDeath();
    }

    private void RespawnAfterDeath()
    {
        // restore constraints so prefab isn't stuck
        if (_rb && freezeDuringDeath)
            _rb.constraints = _prevConstraints;

        // NEW — call checkpoint respawn instead of reloading scene
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.Respawn();
        }
        else
        {
            // fallback — reload scene if checkpoint system not found
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        _dying = false;
    }

    private void ToggleMovement(bool enabled)
    {
        // Disable any movement/controller scripts on this form (add more types as needed)
        var def   = GetComponent<DefaultMovement>();       if (def)   def.enabled   = enabled;
        var agile = GetComponent<AgileFormController>();   if (agile) agile.enabled = enabled;
        var power = GetComponent<MonoBehaviour>(); // replace with your Power controller if you have one
        // e.g., var power = GetComponent<PowerFormController>(); if (power) power.enabled = enabled;

        var fly   = GetComponent<FlyingFormController>();  if (fly)   fly.enabled   = enabled;

        // Add any other scripts that need disabling/enabling here.
    }
}
