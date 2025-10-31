using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class FinishAreaTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CollectibleManager collectibleManager; // optional; will fallback to Instance
    [SerializeField] SlowMoPopupAny popup;                  // TMP/3D popup
    [SerializeField] GameEndUI endUI;                       // Win panel
    [SerializeField] string playerTag = "Player";

    [Header("Popup")]
    [Range(0.01f, 0.5f)] public float popupTimeScale = 0.1f;
    [TextArea(2, 4)]
    public string messageTemplate =
        "You’re missing <b>{MISSING}</b> of <b>{TOTAL}</b> collectibles!\n\nPress <b>Space</b> to keep searching.";

    [Header("On Complete")]
    public UnityEvent OnAllCollected;   // optional hooks
    public string nextSceneName = "";   // leave empty to use endUI

    Collider2D _col;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;

        if (!collectibleManager)
            collectibleManager = CollectibleManager.Instance ? CollectibleManager.Instance
                                                             : FindFirstObjectByType<CollectibleManager>();
        if (!popup)
            popup = FindFirstObjectByType<SlowMoPopupAny>(FindObjectsInactive.Include);
        if (!endUI)
            endUI = FindFirstObjectByType<GameEndUI>(FindObjectsInactive.Include);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        var mgr = collectibleManager ? collectibleManager : CollectibleManager.Instance;
        if (!mgr)
        {
            Debug.LogWarning("FinishAreaTrigger: No CollectibleManager found.");
            return;
        }

        int total = mgr.LevelTotal;       // <<< renamed
        int got   = mgr.LevelCollected;   // <<< renamed

        if (total <= 0 || got >= total)
        {
            Complete();
        }
        else
        {
            if (!popup)
            {
                Debug.LogWarning("FinishAreaTrigger: No SlowMoPopupAny assigned/found.");
                return;
            }
            string msg = messageTemplate
                .Replace("{MISSING}", (total - got).ToString())
                .Replace("{TOTAL}", total.ToString());
            popup.Show(msg, popupTimeScale, onDismiss: null);
        }
    }

    void Complete()
    {
        if (endUI) endUI.ShowWin("Level Complete!");
        else if (!string.IsNullOrEmpty(nextSceneName)) StartCoroutine(LoadNextFrame());
        OnAllCollected?.Invoke();
    }

    System.Collections.IEnumerator LoadNextFrame()
    {
        yield return null;
        SceneManager.LoadScene(nextSceneName);
    }
}
