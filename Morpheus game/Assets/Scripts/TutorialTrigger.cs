using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string message = "Press [E] to interact";

    public KeyCode requiredKey = KeyCode.E;
    public KeyCode[] extraAcceptedKeys;

    public bool oneShot = true;
    private bool used;

    [Header("Text Layout (Per Trigger)")]
    public Vector2 textOffset = Vector2.zero;
    public float textScale = 1f;
    public float baseFontSize = 36f;

    [Header("Behavior (Per Trigger)")]
    [Tooltip("If OFF: just show the text while inside the trigger, hide on exit (no key, no input blocking).")]
    public bool requireKeyToDismiss = true;

    [Header("Time Effect (Per Trigger)")]
    public TutorialManager.TimeEffect timeEffect = TutorialManager.TimeEffect.Freeze;

    [Tooltip("Used only if TimeEffect = Slow")]
    [Range(0.05f, 1f)]
    public float slowTimeScaleOverride = 0.2f;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used && oneShot) return;
        if (!other.CompareTag("Player")) return;
        if (TutorialManager.Instance == null) return;

        used = true;

        // If no key required -> show-only mode
        if (!requireKeyToDismiss)
        {
            TutorialManager.Instance.ShowOnly(message, textOffset, textScale, baseFontSize, timeEffect, slowTimeScaleOverride);
            return;
        }

        // Key-required mode (existing behavior)
        if (extraAcceptedKeys != null && extraAcceptedKeys.Length > 0)
        {
            var keys = new KeyCode[1 + extraAcceptedKeys.Length];
            keys[0] = requiredKey;
            for (int i = 0; i < extraAcceptedKeys.Length; i++)
                keys[i + 1] = extraAcceptedKeys[i];

            TutorialManager.Instance.StartStep(keys, message, textOffset, textScale, baseFontSize, timeEffect, slowTimeScaleOverride);
        }
        else
        {
            TutorialManager.Instance.StartStep(requiredKey, message, textOffset, textScale, baseFontSize, timeEffect, slowTimeScaleOverride);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (oneShot) return;
        if (!other.CompareTag("Player")) return;
        if (TutorialManager.Instance == null) return;

        // Hide prompt and restore time/freeze (works for both modes)
        TutorialManager.Instance.EndStep();
    }
}
