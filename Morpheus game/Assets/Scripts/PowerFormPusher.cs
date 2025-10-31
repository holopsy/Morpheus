using UnityEngine;

public class PowerFormPusher : MonoBehaviour
{
    [Header("Who can push")]
    public float strength = 20f;        // compare vs PushableBlock.requiredStrength

    // Internal input cache
    float inputX;

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
    }

    /// Returns -1 pushing left, +1 pushing right, 0 no push (based on contact normal + input).
    /// 'contactNormalFromBlock' points from the BLOCK toward the PLAYER collider.
    public int GetPushIntent(Vector2 contactNormalFromBlock)
    {
        if (contactNormalFromBlock.x > 0.5f)
        {
            // player is to the RIGHT of the block -> must press LEFT
            if (inputX < -0.1f) return -1;
        }
        else if (contactNormalFromBlock.x < -0.5f)
        {
            // player is to the LEFT of the block -> must press RIGHT
            if (inputX > 0.1f) return +1;
        }
        return 0;
    }
}