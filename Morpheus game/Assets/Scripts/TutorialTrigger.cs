using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string message = "Press [E] to interact";

    public KeyCode requiredKey = KeyCode.E;
    public KeyCode[] extraAcceptedKeys;

    public bool oneShot = true;
    private bool used;

    // 🆕 Per-trigger text layout
    [Header("Text Layout (Per Trigger)")]
    public Vector2 textOffset = Vector2.zero;
    public float textScale = 1f;
    public float baseFontSize = 36f;

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

        // Build accepted-keys list (required + extras)
        if (extraAcceptedKeys != null && extraAcceptedKeys.Length > 0)
        {
            var keys = new KeyCode[1 + extraAcceptedKeys.Length];
            keys[0] = requiredKey;
            for (int i = 0; i < extraAcceptedKeys.Length; i++)
                keys[i + 1] = extraAcceptedKeys[i];

            TutorialManager.Instance.StartStep(keys, message, textOffset, textScale, baseFontSize);
        }
        else
        {
            TutorialManager.Instance.StartStep(requiredKey, message, textOffset, textScale, baseFontSize);
        }
    }
}