using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    public int value = 1;
    private bool _picked;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        // Ensure the manager exists; if not yet, attempt to find it
        if (CollectibleManager.Instance == null)
        {
            var mgr = FindFirstObjectByType<CollectibleManager>();
            if (mgr != null) CollectibleManager.Instance = mgr;
        }
        CollectibleManager.Instance?.RegisterCollectible(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_picked) return;
        if (!other.CompareTag("Player")) return;

        _picked = true;
        CollectibleManager.Instance?.NotifyPicked(value, this);
        Destroy(gameObject);
    }
}