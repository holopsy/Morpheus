using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyDropper : MonoBehaviour
{
    [Header("Collectible")]
    public GameObject collectiblePrefab;
    [Min(0)] public int minCount = 1;
    [Min(0)] public int maxCount = 1;

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

    /// <summary>
    /// Old API: auto-detect killer side (nearest player) and drop away from them.
    /// </summary>
    public void Drop()
    {
        Vector2? killerPos = FindNearestPlayerPos();
        InternalDrop(killerPos);
    }

    /// <summary>
    /// Preferred API: pass the world position of the hitter/attacker.
    /// </summary>
    public void DropFrom(Vector2 hitFromWorld)
    {
        InternalDrop(hitFromWorld);
    }

    // ---------------- internals ----------------
    void InternalDrop(Vector2? hitFromWorld)
    {
        if (!collectiblePrefab) return;

        int count = Mathf.Clamp(Random.Range(minCount, maxCount + 1), 0, 99);

        // Determine horizontal side: +1 = drop to RIGHT of enemy, -1 = drop to LEFT
        int side = 1;
        if (hitFromWorld.HasValue)
        {
            float dx = transform.position.x - hitFromWorld.Value.x;
            side = dx > 0f ? 1 : -1; // if killer is left of enemy, drop to right (away)
            if (Mathf.Approximately(dx, 0f)) side = 1;
        }
        else
        {
            // Fallback: use nearest player
            var p = FindNearestPlayerPos();
            if (p.HasValue)
            {
                float dx = transform.position.x - p.Value.x;
                side = dx > 0f ? 1 : -1;
                if (Mathf.Approximately(dx, 0f)) side = 1;
            }
        }

        // Build a side-biased offset
        Vector2 baseOffset = spawnOffset;
        baseOffset.x = Mathf.Abs(baseOffset.x) * side; // ensure offset is on chosen side

        for (int i = 0; i < count; i++)
        {
            // Scatter, but bias to the chosen side
            Vector2 rand = Random.insideUnitCircle * scatterRadius;
            rand.x = Mathf.Lerp(rand.x, Mathf.Abs(rand.x) * side, sideBias);

            Vector2 xy = (Vector2)transform.position + baseOffset + rand;
            Vector3 pos = new Vector3(xy.x, xy.y, transform.position.z + zOffset);

            var go = Instantiate(collectiblePrefab, pos, Quaternion.identity);

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb)
            {
                // Launch mostly upward, but bias horizontally to the chosen side
                Vector2 bias = new Vector2(Mathf.Abs(launchDirectionBias.x) * side, launchDirectionBias.y);
                Vector2 dir = (bias + Random.insideUnitCircle * 0.25f).normalized;
                rb.AddForce(dir * launchForce, ForceMode2D.Impulse);
            }
        }
    }

    Vector2? FindNearestPlayerPos()
    {
        // Prefer PlayerHealth components
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

        // Fallback to tag
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
