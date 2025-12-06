using UnityEngine;

public class ParallaxInfinite : MonoBehaviour
{
    public float parallaxMultiplier = 0.5f;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    private float textureUnitSizeX;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;

        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        Texture2D texture = sprite.texture;

        textureUnitSizeX = texture.width / sprite.pixelsPerUnit;
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(deltaMovement.x * parallaxMultiplier, deltaMovement.y * parallaxMultiplier, 0);
        lastCameraPosition = cameraTransform.position;

        float cameraDiff = cameraTransform.position.x - transform.position.x;

        if (Mathf.Abs(cameraDiff) >= textureUnitSizeX)
        {
            float offset = (cameraDiff > 0) ? textureUnitSizeX : -textureUnitSizeX;
            transform.position += new Vector3(offset, 0, 0);
        }
    }
}