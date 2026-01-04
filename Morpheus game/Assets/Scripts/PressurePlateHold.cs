using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PressurePlateHold : MonoBehaviour
{
    [Header("Wall Targets")]
    public MovableWall2D[] walls;

    [Header("Who can press it")]
    public string[] allowedTags = { "Player", "Box" };

    [Header("Visual Swap")]
    [Tooltip("SpriteRenderer that shows the plate visuals (usually the Visual child). If empty, auto-finds child named 'Visual'.")]
    public SpriteRenderer visualRenderer;

    [Tooltip("Sprite when NOT pressed.")]
    public Sprite normalSprite;

    [Tooltip("Sprite when pressed.")]
    public Sprite pressedSprite;

    int pressCount;

    void Awake()
    {
        // Auto-find Visual child sprite renderer if not assigned
        if (!visualRenderer)
        {
            var vis = transform.Find("Visual");
            if (vis) visualRenderer = vis.GetComponent<SpriteRenderer>();
        }

        UpdateVisual();
    }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasAllowedTagSafe(other)) return;

        pressCount++;
        UpdateWallsAndVisual();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!HasAllowedTagSafe(other)) return;

        pressCount = Mathf.Max(0, pressCount - 1);
        UpdateWallsAndVisual();
    }

    void UpdateWallsAndVisual()
    {
        bool shouldBeOpen = pressCount > 0;

        // Walls: hold-to-open
        if (walls != null)
        {
            for (int i = 0; i < walls.Length; i++)
                if (walls[i]) walls[i].SetRaised(shouldBeOpen);
        }

        // Visual: pressed/unpressed sprite
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (!visualRenderer) return;
        if (!normalSprite || !pressedSprite) return;

        visualRenderer.sprite = (pressCount > 0) ? pressedSprite : normalSprite;
    }

    // SAFE: does not throw "Tag ___ is not defined"
    bool HasAllowedTagSafe(Collider2D other)
    {
        string otherTag = other.tag; // always valid
        for (int i = 0; i < allowedTags.Length; i++)
        {
            string t = allowedTags[i];
            if (string.IsNullOrWhiteSpace(t)) continue;

            if (string.Equals(otherTag, t.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
