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
        // contactNormalFromBlock points from BLOCK -> PLAYER
        // If player is on the RIGHT of the block, normal.x will be +.
        // To push the block to the RIGHT, player must press RIGHT (inputX > 0).
        // To push the block to the LEFT, player must press LEFT (inputX < 0).

        if (contactNormalFromBlock.x > 0.5f)
        {
            // Player is to the RIGHT of the block
            if (inputX > 0.1f) return +1;   // push block RIGHT
        }
        else if (contactNormalFromBlock.x < -0.5f)
        {
            // Player is to the LEFT of the block
            if (inputX < -0.1f) return -1;  // push block LEFT
        }

        return 0;
    }
}