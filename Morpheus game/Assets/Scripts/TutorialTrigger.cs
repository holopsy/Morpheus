using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string message = "Press [E] to interact";
    public KeyCode requiredKey = KeyCode.E;
    public bool oneShot = true;

    private bool used;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used && oneShot) return;
        if (!other.CompareTag("Player")) return;

        if (TutorialManager.Instance != null)
        {
            used = true;
            TutorialManager.Instance.StartStep(requiredKey, message);
        }
    }
}