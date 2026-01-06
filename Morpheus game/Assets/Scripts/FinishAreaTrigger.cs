using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FinishAreaTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CollectibleManager collectibleManager;
    [SerializeField] private FinishPopupUI finishPopupUI;
    [SerializeField] private string playerTag = "Player";

    [Header("Required Collectibles")]
    [Tooltip("If -1, player must collect ALL collectibles. If a positive number, only that many are required to FINISH.")]
    public int requiredCollectibles = -1;

    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;

        if (!collectibleManager)
            collectibleManager = CollectibleManager.Instance
                ? CollectibleManager.Instance
                : FindFirstObjectByType<CollectibleManager>();

        if (!finishPopupUI)
            finishPopupUI = FindFirstObjectByType<FinishPopupUI>(FindObjectsInactive.Include);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        var mgr = collectibleManager ? collectibleManager : CollectibleManager.Instance;
        if (!mgr)
        {
            Debug.LogWarning("FinishAreaTrigger: No CollectibleManager found.");
            return;
        }

        if (!finishPopupUI)
        {
            Debug.LogWarning("FinishAreaTrigger: No FinishPopupUI found/assigned.");
            return;
        }

        int total = mgr.LevelTotal;
        int collected = mgr.LevelCollected;

        // required to finish: if -1 (or 0), require all (total)
        int requiredToFinish = (requiredCollectibles <= 0) ? total : requiredCollectibles;

        // Decide which popup state to show
        FinishPopupUI.ResultType result;

        if (collected < requiredToFinish)
        {
            result = FinishPopupUI.ResultType.NotEnough;
        }
        else if (collected >= total)
        {
            result = FinishPopupUI.ResultType.AllCollected;
        }
        else
        {
            result = FinishPopupUI.ResultType.EnoughButNotAll;
        }

        finishPopupUI.Show(result, collected, total, requiredToFinish);
    }
}
