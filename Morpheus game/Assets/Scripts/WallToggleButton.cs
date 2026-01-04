using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WallToggleButton : MonoBehaviour
{
    [Header("Targets")]
    public MovableWall2D[] walls;

    [Header("Input")]
    public KeyCode key = KeyCode.E;
    public string playerTag = "Player";

    [Header("Options")]
    public bool oneShot = false;

    [Header("Visual Swap")]
    [Tooltip("SpriteRenderer that shows the button (usually child named 'Visual'). If empty, auto-finds.")]
    public SpriteRenderer visualRenderer;

    public Sprite normalSprite;
    public Sprite pressedSprite;

    [Tooltip("Print warnings if visuals aren't assigned.")]
    public bool debugVisuals = true;

    bool playerInside;
    bool used;
    bool toggledOn;

    void Awake()
    {
        // Auto-find visual sprite renderer
        if (!visualRenderer)
        {
            var vis = transform.Find("Visual");
            if (vis) visualRenderer = vis.GetComponent<SpriteRenderer>();
            if (!visualRenderer) visualRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        ApplyVisual(force: true);
    }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = false;
    }

    void Update()
    {
        if (!playerInside) return;
        if (oneShot && used) return;

        if (Input.GetKeyDown(key))
        {
            // Toggle walls
            if (walls != null)
            {
                for (int i = 0; i < walls.Length; i++)
                    if (walls[i]) walls[i].Toggle();
            }

            // Toggle visual state
            toggledOn = !toggledOn;
            used = true;

            ApplyVisual(force: true);
        }
    }

    void LateUpdate()
    {
        // Keep enforcing the sprite each frame (prevents other scripts/animators from overriding it)
        ApplyVisual(force: false);
    }

    void ApplyVisual(bool force)
    {
        if (!visualRenderer)
        {
            if (debugVisuals && force)
                Debug.LogWarning($"{name}: No visualRenderer found. Create a child named 'Visual' with a SpriteRenderer OR assign visualRenderer in inspector.");
            return;
        }

        if (!normalSprite || !pressedSprite)
        {
            if (debugVisuals && force)
                Debug.LogWarning($"{name}: normalSprite/pressedSprite not assigned on WallToggleButton.");
            return;
        }

        Sprite target = toggledOn ? pressedSprite : normalSprite;
        if (visualRenderer.sprite != target)
            visualRenderer.sprite = target;
    }
}
