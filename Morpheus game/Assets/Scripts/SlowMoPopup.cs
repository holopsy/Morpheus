using UnityEngine;
using TMPro; // needed for both TextMeshProUGUI and TextMeshPro

public class SlowMoPopupAny : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;                     // The popup panel or object to show/hide

    // One (or both) of these can be assigned. The script will use whichever is present.
    public TextMeshProUGUI messageUGUI;         // UI (Canvas) TMP text
    public TextMeshPro message3D;               // 3D/world TMP text

    [Tooltip("Try to find a TMP text component under this object automatically if none assigned.")]
    public bool autoFindText = true;

    private System.Action _onDismiss;
    private float _prevTimeScale = 1f;
    private bool _active;
    private TMP_Text _message;                  // unified handle to whichever text is present

    void Awake()
    {
        if (!root) root = gameObject;

        if (autoFindText)
        {
            if (!messageUGUI) messageUGUI = GetComponentInChildren<TextMeshProUGUI>(true);
            if (!message3D)   message3D   = GetComponentInChildren<TextMeshPro>(true);
        }

        // Prefer UI text if both exist; otherwise use 3D text
        if (messageUGUI) _message = messageUGUI;
        else if (message3D) _message = message3D;
        else Debug.LogWarning("SlowMoPopupAny: No TextMeshPro text found. Assign messageUGUI or message3D.");

        root.SetActive(false);
    }

    /// <summary>Shows the popup with the given message and slows time.</summary>
    public void Show(string message, float slowScale = 0.1f, System.Action onDismiss = null)
    {
        if (!root) return;

        _onDismiss = onDismiss;
        if (_message) _message.text = message;

        _prevTimeScale = Time.timeScale;
        Time.timeScale = slowScale;

        root.SetActive(true);
        _active = true;
    }

    void Update()
    {
        if (!_active) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Hide();
        }
    }

    /// <summary>Hides the popup and restores time.</summary>
    public void Hide()
    {
        if (!_active) return;

        _active = false;
        root.SetActive(false);
        Time.timeScale = _prevTimeScale;

        var cb = _onDismiss; _onDismiss = null;
        cb?.Invoke();
    }

    void OnDisable()
    {
        // Safety: restore time if disabled mid-popup
        if (_active)
        {
            Time.timeScale = _prevTimeScale;
            _active = false;
        }
    }
}
