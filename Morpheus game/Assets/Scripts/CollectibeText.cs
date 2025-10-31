using UnityEngine;
using TMPro;

public class CollectibleText : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] CollectibleManager manager;     // optional, auto-fills
    [SerializeField] TMP_Text label;                 // assign your TMP Text; auto-fills from self if null

    [Header("Display")]
    [Tooltip("If ON: shows (remaining). If OFF: shows collected/total.")]
    public bool showRemaining = false;

    [Tooltip("Optional prefix, e.g., \"Coins: \". Leave empty for none.")]
    public string prefix = "";

    void Awake()
    {
        if (!manager)
            manager = CollectibleManager.Instance ?? FindFirstObjectByType<CollectibleManager>();
        if (!label)
            label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (!manager)
            manager = CollectibleManager.Instance ?? FindFirstObjectByType<CollectibleManager>();

        if (manager != null)
            manager.OnLevelCountChanged += HandleLevelCountChanged;

        // initial paint
        Refresh();
    }

    void OnDisable()
    {
        if (manager != null)
            manager.OnLevelCountChanged -= HandleLevelCountChanged;
    }

    void Start() => Refresh();

    void HandleLevelCountChanged(int collected, int total) => SetText(collected, total);

    void Refresh()
    {
        if (manager == null || label == null) return;
        SetText(manager.LevelCollected, manager.LevelTotal);
    }

    void SetText(int collected, int total)
    {
        if (!label) return;

        if (showRemaining)
        {
            int remaining = Mathf.Max(0, total - collected);
            label.text = string.IsNullOrEmpty(prefix) ? $"{remaining}" : $"{prefix}{remaining}";
        }
        else
        {
            label.text = string.IsNullOrEmpty(prefix) ? $"{collected}/{total}" : $"{prefix}{collected}/{total}";
        }
    }
}