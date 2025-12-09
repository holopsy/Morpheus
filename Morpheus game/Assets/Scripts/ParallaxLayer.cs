using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float xMultiplier = 0.3f;

    private Transform cam;
    private Vector3 previousCamPos;

    void Start()
    {
        cam = Camera.main.transform;
        previousCamPos = cam.position;
    }

    void LateUpdate()
    {
        Vector3 camDelta = cam.position - previousCamPos;

        // Only horizontal parallax
        transform.position += new Vector3(camDelta.x * xMultiplier, 0f, 0f);

        // Hard-follow camera Y (no parallax)
        transform.position = new Vector3(
            transform.position.x,
            cam.position.y,
            transform.position.z
        );

        previousCamPos = cam.position;
    }
}