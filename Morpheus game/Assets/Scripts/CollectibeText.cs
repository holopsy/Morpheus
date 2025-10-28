using UnityEngine;
using TMPro;

public class CollectibleUI : MonoBehaviour
{
    public TMP_Text counterText;

    void OnEnable()
    {
        if (CollectibleManager.Instance != null)
            CollectibleManager.Instance.OnCountChanged += UpdateCounter;
    }

    void OnDisable()
    {
        if (CollectibleManager.Instance != null)
            CollectibleManager.Instance.OnCountChanged -= UpdateCounter;
    }

    void Start()
    {
        // Initialize immediately
        if (CollectibleManager.Instance != null)
            UpdateCounter(CollectibleManager.Instance.Collected, CollectibleManager.Instance.Total);
    }

    void UpdateCounter(int collected, int total)
    {
        if (counterText) counterText.text = $"{collected} / {total}";
    }
}