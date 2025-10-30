using UnityEngine;

public class PowerFormPusher : MonoBehaviour
{
    [Header("Who can push")]
    public float strength = 20f;        // compare vs block.requiredStrength

    [Header("Anim (optional)")]
    public Animator animator;           // gorilla Animator
    public string pushingBool = "IsPushing";

    // current horizontal intent from input
    float inputX;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        // optional: basic push pose if holding against something; we set it true only when actually pushing (below)
        if (animator && !string.IsNullOrEmpty(pushingBool))
            animator.SetBool(pushingBool, false);
    }

    /// Returns -1 pushing left, +1 pushing right, 0 no push
    public int GetPushIntent(Vector2 contactNormalFromBlock)
    {
        // If the block's contact normal points right (x > 0), the player is to the RIGHT of the block,
        // so to push it, the player must press LEFT (inputX < 0). And vice versa.
        if (contactNormalFromBlock.x > 0.5f)
        {
            // player is on the right, must push left
            if (inputX < -0.1f) { SetPushingAnim(true); return -1; }
        }
        else if (contactNormalFromBlock.x < -0.5f)
        {
            // player is on the left, must push right
            if (inputX > 0.1f) { SetPushingAnim(true); return +1; }
        }
        return 0;
    }

    void SetPushingAnim(bool v)
    {
        if (animator && !string.IsNullOrEmpty(pushingBool))
            animator.SetBool(pushingBool, v);
    }
}