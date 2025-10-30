using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional offset to spawn slightly above ground, etc.")]
    public Vector2 spawnOffset = new Vector2(0f, 0.5f);

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // treat anything with PlayerHealth as the player
        if (!other.GetComponentInParent<PlayerHealth>()) return;

        Vector3 p = transform.position + (Vector3)spawnOffset;
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.SetCheckpoint(p);
    }
}