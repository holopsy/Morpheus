using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    public Transform cameraTransform;
    public float followMultiplier = 0.9f; // 1 = locked to camera, lower = slight parallax

    private Vector3 startOffset;

    void Start()
    {
        if (!cameraTransform)
            cameraTransform = Camera.main.transform;

        startOffset = transform.position - cameraTransform.position;
    }

    void LateUpdate()
    {
        transform.position = cameraTransform.position + startOffset * followMultiplier;
    }
}