using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class FinishAreaTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CollectibleManager collectibleManager; 
    [SerializeField] SlowMoPopupAny popup;                  
    [SerializeField] GameEndUI endUI;                       
    [SerializeField] string playerTag = "Player";

    [Header("Required Collectibles")]
    [Tooltip("If -1, player must collect ALL collectibles. If a positive number, only that many are required.")]
    public int requiredCollectibles = -1;

    [Header("Popup")]
    [Range(0.01f, 0.5f)] public float popupTimeScale = 0.1f;
    [TextArea(2, 4)]
    public string messageTemplate =
        "You’re missing <b>{MISSING}</b> of <b>{REQUIRED}</b> collectibles!\n\nPress <b>Space</b> to keep searching.";

    [Header("On Complete")]
    public UnityEvent OnAllCollected;
    public string nextSceneName = "";

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

        int total     = mgr.LevelTotal;
        int collected = mgr.LevelCollected;

        // Determine required amount
        int required = (requiredCollectibles <= 0) ? total : requiredCollectibles;

        if (collected >= required)
        {
            // Completed
            Complete();
        }
        else
        {
            // Not enough → show popup
            if (!popup)
            {
                Debug.LogWarning("FinishAreaTrigger: No SlowMoPopupAny assigned/found.");
                return;
            }

            string msg = messageTemplate
                .Replace("{MISSING}", (required - collected).ToString())
                .Replace("{REQUIRED}", required.ToString());

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
