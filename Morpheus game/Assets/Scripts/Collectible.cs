using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    [Header("Type")]
    [Tooltip("If true, this counts toward 'All collectibles collected' for the level. " +
             "If false (loot), it will NOT affect level completion totals.")]
    public bool countsTowardCompletion = true;

    [Header("Value")]
    [Tooltip("How many currency points this is worth (Loot), or 1 for level items.")]
    public int value = 1;

    // ✅ New flag: if true, skip registration during OnEnable (used for enemy drops)
    [HideInInspector] public bool skipRegisterOnEnable = false;

    private bool _picked;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        // Skip automatic registration if requested (e.g., for enemy drops)
        if (skipRegisterOnEnable) return;

        // Make sure manager exists
        if (CollectibleManager.Instance == null)
        {
            var mgr = FindFirstObjectByType<CollectibleManager>();
            if (mgr != null) CollectibleManager.Instance = mgr;
        }

        // Register only if it affects completion
        if (countsTowardCompletion)
            CollectibleManager.Instance?.RegisterLevelCollectible(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_picked) return;
        if (!other.CompareTag("Player")) return;

        _picked = true;

        if (countsTowardCompletion)
            CollectibleManager.Instance?.NotifyLevelPicked(this);
        else
            CollectibleManager.Instance?.NotifyLootGained(value);

        Destroy(gameObject);
    }
}