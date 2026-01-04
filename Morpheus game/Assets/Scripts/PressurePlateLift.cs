using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PressurePlateLift : MonoBehaviour
{
    [Header("Target Lift Platforms")]
    public WeightedLiftPlatform[] platforms;

    [Header("Who can press it")]
    public string[] allowedTags = { "Player", "Box" };

    [Header("Visual Swap")]
    public SpriteRenderer visualRenderer;   // assign your Visual child sprite renderer
    public Sprite normalSprite;
    public Sprite pressedSprite;

    int pressCount;

    void Awake()
    {
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
        if (pressCount == 1)
        {
            SetActive(true);
            UpdateVisual();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!HasAllowedTagSafe(other)) return;

        pressCount = Mathf.Max(0, pressCount - 1);
        if (pressCount == 0)
        {
            SetActive(false);
            UpdateVisual();
        }
    }

    void SetActive(bool active)
    {
        if (platforms == null) return;
        for (int i = 0; i < platforms.Length; i++)
            if (platforms[i]) platforms[i].SetPlateActive(active);
    }

    void UpdateVisual()
    {
        if (!visualRenderer) return;
        if (pressedSprite == null || normalSprite == null) return;

        visualRenderer.sprite = (pressCount > 0) ? pressedSprite : normalSprite;
    }

    bool HasAllowedTagSafe(Collider2D other)
    {
        string otherTag = other.tag;
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
