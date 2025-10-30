using UnityEngine;

[DisallowMultipleComponent]
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Respawn Setup")]
    public MorphManager morphManager;          // drag your GameManager (has MorphManager)
    public Transform defaultSpawnPoint;        // optional
    public GameObject respawnFormOverride;     // optional
    public bool refillHealthOnRespawn = true;  // refill on respawn

    private Vector3? lastCheckpointPos;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!morphManager) morphManager = GetComponent<MorphManager>();
    }

    public void SetCheckpoint(Vector3 position)
    {
        lastCheckpointPos = position;
    }

    public void Respawn()
    {
        if (!morphManager) return;

        // In case slow-mo or pause was active
        Time.timeScale = 1f;

        Vector3 spawnPos = lastCheckpointPos ?? (defaultSpawnPoint ? defaultSpawnPoint.position : morphManager.transform.position);
        GameObject form = respawnFormOverride ? respawnFormOverride : morphManager.defaultForm;

        morphManager.ForceRespawn(form, spawnPos, refillHealthOnRespawn);
    }
}