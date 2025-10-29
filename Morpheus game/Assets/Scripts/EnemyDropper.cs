using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyDropper : MonoBehaviour
{
    [Header("Collectible")]
    public GameObject collectiblePrefab; // assign your Collectible prefab
    [Min(0)] public int minCount = 1;
    [Min(0)] public int maxCount = 1;

    [Header("Spawn")]
    public Vector2 spawnOffset = Vector2.up * 0.2f;
    public float scatterRadius = 0.15f;

    [Header("Impulse (optional)")]
    public float launchForce = 2f;
    public Vector2 launchDirectionBias = new Vector2(0.6f, 1f); // mostly up/right

    public void Drop()
    {
        if (!collectiblePrefab) return;

        int count = Mathf.Clamp(Random.Range(minCount, maxCount + 1), 0, 99);
        for (int i = 0; i < count; i++)
        {
            Vector2 pos = (Vector2)transform.position + spawnOffset + Random.insideUnitCircle * scatterRadius;
            var go = Instantiate(collectiblePrefab, pos, Quaternion.identity);

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb)
            {
                Vector2 dir = (launchDirectionBias + Random.insideUnitCircle * 0.5f).normalized;
                rb.AddForce(dir * launchForce, ForceMode2D.Impulse);
            }
        }
    }
}