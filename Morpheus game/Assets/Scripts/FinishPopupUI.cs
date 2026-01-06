using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class FinishPopupUI : MonoBehaviour
{
    public enum ResultType
    {
        NotEnough,
        EnoughButNotAll,
        AllCollected
    }

    [Header("Root")]
    [SerializeField] private GameObject rootObject;   // set this to FinishPopupRoot
    [SerializeField] private CanvasGroup rootGroup;   // CanvasGroup on FinishPopupRoot

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Buttons Group")]
    [SerializeField] private GameObject buttonsGroup;

    [Header("Buttons")]
    [SerializeField] private Button btnResume;     // ALWAYS closes popup
    [SerializeField] private Button btnRestart;    // restart level
    [SerializeField] private Button btnMainMenu;   // main menu
    [SerializeField] private Button btnNext;       // optional: next level (only when enough)

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string nextLevelSceneName = "";

    private bool isOpen;

    private void Awake()
    {
        if (!rootObject) rootObject = gameObject;

        EnsureRefs();
        WireButtons();
        HideInstant(); // hidden at start (but object stays enabled)
    }

    private void OnEnable()
    {
        // If you copied UI, refs may be missing until runtime.
        EnsureRefs();
        WireButtons();

        if (!isOpen)
            HideInstant();
    }

    // ---------------- PUBLIC ----------------

    public void Show(ResultType result, int collected, int totalInLevel, int requiredToFinish)
    {
        EnsureRefs();

        // IMPORTANT: if the root was disabled in the hierarchy, force it on
        if (rootObject && !rootObject.activeInHierarchy)
            rootObject.SetActive(true);

        OpenBase();

        if (titleText)
            titleText.text = "You have completed the level!";

        if (bodyText)
        {
            switch (result)
            {
                case ResultType.NotEnough:
                    bodyText.text =
                        $"You don’t have enough essences.\n\n" +
                        $"Collected: {collected}/{totalInLevel}\n" +
                        $"Required: {requiredToFinish}\n\n" +
                        $"Press RESUME to keep searching.";
                    break;

                case ResultType.EnoughButNotAll:
                    bodyText.text =
                        $"You collected enough essences to finish.\n\n" +
                        $"Collected: {collected}/{totalInLevel}\n\n" +
                        $"Press NEXT to move on, or RESTART to collect them all.";
                    break;

                case ResultType.AllCollected:
                    bodyText.text =
                        $"Perfect!\nYou collected ALL essences!\n\n" +
                        $"Collected: {collected}/{totalInLevel}\n\n" +
                        $"Press NEXT to move on.";
                    break;
            }
        }

        ApplyButtonMode(result);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        Time.timeScale = 1f;

        if (rootGroup)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
    }

    // ---------------- INTERNAL ----------------

    private void OpenBase()
    {
        isOpen = true;

        // Freeze the game
        Time.timeScale = 0f;

        if (!rootGroup)
        {
            Debug.LogWarning("FinishPopupUI: No CanvasGroup found. Add one to the popup root.");
            return;
        }

        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;
    }

    private void HideInstant()
    {
        isOpen = false;
        Time.timeScale = 1f;

        EnsureRefs();

        if (rootGroup)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
    }

    private void ApplyButtonMode(ResultType result)
    {
        if (buttonsGroup) buttonsGroup.SetActive(true);

        bool canGoNext = (result == ResultType.EnoughButNotAll || result == ResultType.AllCollected);

        if (btnNext)
            btnNext.gameObject.SetActive(canGoNext);
    }

    private void WireButtons()
    {
        if (btnResume)
        {
            btnResume.onClick.RemoveAllListeners();
            btnResume.onClick.AddListener(Close);
        }

        if (btnRestart)
        {
            btnRestart.onClick.RemoveAllListeners();
            btnRestart.onClick.AddListener(RestartLevel);
        }

        if (btnMainMenu)
        {
            btnMainMenu.onClick.RemoveAllListeners();
            btnMainMenu.onClick.AddListener(GoMainMenu);
        }

        if (btnNext)
        {
            btnNext.onClick.RemoveAllListeners();
            btnNext.onClick.AddListener(LoadNextLevel);
        }
    }

    private void EnsureRefs()
    {
        if (!rootObject) rootObject = gameObject;

        // Find/add CanvasGroup on the root
        if (!rootGroup)
        {
            rootGroup = rootObject.GetComponent<CanvasGroup>();
            if (!rootGroup)
                rootGroup = rootObject.AddComponent<CanvasGroup>();
        }
    }

    // ---------------- BUTTON ACTIONS ----------------

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void LoadNextLevel()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextLevelSceneName))
        {
            SceneManager.LoadScene(nextLevelSceneName);
            return;
        }

        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (next >= SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(mainMenuSceneName);
        else
            SceneManager.LoadScene(next);
    }
}
