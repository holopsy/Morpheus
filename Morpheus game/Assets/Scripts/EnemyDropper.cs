using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyDropper : MonoBehaviour
{
    [Header("Collectible")]
    public GameObject collectiblePrefab;
    [Min(0)] public int minCount = 1;
    [Min(0)] public int maxCount = 1;

    [Tooltip("If true, enemy-dropped collectibles will NOT count toward level completion.")]
    public bool markDropsAsLoot = true;

    [Tooltip("If >= 0, override the collectible's value for loot (currency). -1 keeps prefab's value.")]
    public int lootValueOverride = -1;

    [Header("Spawn")]
    public Vector2 spawnOffset = new Vector2(0.2f, 0.2f);
    public float scatterRadius = 0.15f;
    [Tooltip("Z-axis offset applied to spawned collectibles (negative = behind the enemy).")]
    public float zOffset = -0.1f;

    [Header("Away-from-killer bias")]
    [Tooltip("How strongly we push position/scatter to the 'away' side (0..1).")]
    [Range(0f, 1f)] public float sideBias = 0.6f;

    [Header("Impulse (optional)")]
    public float launchForce = 2f;
    public Vector2 launchDirectionBias = new Vector2(0.6f, 1f); // mostly up/right

    private bool _registeredAsCollectible;

    void Start()
    {
        // ✅ Each enemy counts as one future collectible toward total
        if (markDropsAsLoot && CollectibleManager.Instance != null && !_registeredAsCollectible)
        {
            _registeredAsCollectible = true;
            CollectibleManager.Instance.RegisterLevelCollectible(null);
        }
    }

    public void Drop()
    {
        Vector2? killerPos = FindNearestPlayerPos();
        InternalDrop(killerPos);
    }

    public void DropFrom(Vector2 hitFromWorld)
    {
        InternalDrop(hitFromWorld);
    }

    void InternalDrop(Vector2? hitFromWorld)
    {
        if (!collectiblePrefab) return;

        int count = Mathf.Clamp(Random.Range(minCount, maxCount + 1), 0, 99);

        int side = 1;
        if (hitFromWorld.HasValue)
        {
            float dx = transform.position.x - hitFromWorld.Value.x;
            side = dx > 0f ? 1 : -1;
            if (Mathf.Approximately(dx, 0f)) side = 1;
        }
        else
        {
            var p = FindNearestPlayerPos();
            if (p.HasValue)
            {
                float dx = transform.position.x - p.Value.x;
                side = dx > 0f ? 1 : -1;
                if (Mathf.Approximately(dx, 0f)) side = 1;
            }
        }

        Vector2 baseOffset = spawnOffset;
        baseOffset.x = Mathf.Abs(baseOffset.x) * side;

        for (int i = 0; i < count; i++)
        {
            Vector2 rand = Random.insideUnitCircle * scatterRadius;
            rand.x = Mathf.Lerp(rand.x, Mathf.Abs(rand.x) * side, sideBias);

            Vector2 xy = (Vector2)transform.position + baseOffset + rand;
            Vector3 pos = new Vector3(xy.x, xy.y, transform.position.z + zOffset);

            var go = Instantiate(collectiblePrefab, pos, Quaternion.identity);
            var col = go.GetComponent<Collectible>();
            if (col)
            {
                col.skipRegisterOnEnable = true;
                if (markDropsAsLoot) col.countsTowardCompletion = true; // ✅ now counts toward collected progress
                if (lootValueOverride >= 0) col.value = lootValueOverride;
            }

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb)
            {
                Vector2 bias = new Vector2(Mathf.Abs(launchDirectionBias.x) * side, launchDirectionBias.y);
                Vector2 dir = (bias + Random.insideUnitCircle * 0.25f).normalized;
                rb.AddForce(dir * launchForce, ForceMode2D.Impulse);
            }
        }
    }

    Vector2? FindNearestPlayerPos()
    {
        var players = FindObjectsOfType<PlayerHealth>();
        Transform best = null;
        float bestSq = float.PositiveInfinity;
        foreach (var ph in players)
        {
            if (!ph) continue;
            float d2 = (ph.transform.position - transform.position).sqrMagnitude;
            if (d2 < bestSq) { bestSq = d2; best = ph.transform; }
        }
        if (best) return best.position;

        var tagged = GameObject.FindGameObjectsWithTag("Player");
        best = null; bestSq = float.PositiveInfinity;
        foreach (var go in tagged)
        {
            float d2 = (go.transform.position - transform.position).sqrMagnitude;
            if (d2 < bestSq) { bestSq = d2; best = go.transform; }
        }
        return best ? best.position : (Vector2?)null;
    }
}
