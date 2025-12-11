using UnityEngine;

public class ResettableObject : MonoBehaviour
{
    private Vector3 startPos;
    private Quaternion startRot;
    private Rigidbody2D rb;

    void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        CheckpointEvents.OnPlayerRespawned += ResetObject;
    }

    void OnDisable()
    {
        CheckpointEvents.OnPlayerRespawned -= ResetObject;
    }

    void ResetObject()
    {
        transform.position = startPos;
        transform.rotation = startRot;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}