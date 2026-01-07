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
    [SerializeField] private GameObject rootObject;
    [SerializeField] private CanvasGroup rootGroup;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Buttons Group")]
    [SerializeField] private GameObject buttonsGroup;

    [Header("Buttons (Main)")]
    [SerializeField] private Button btnResume;     // closes popup
    [SerializeField] private Button btnRestart;    // restart level
    [SerializeField] private Button btnMainMenu;   // main menu
    [SerializeField] private Button btnNext;       // next level (optional)

    [Header("Temporary Jump Buttons (Top Row)")]
    [Tooltip("Turn ON to show 3 quick-jump buttons at the top of the popup.")]
    public bool showJumpButtons = true;

    [SerializeField] private GameObject jumpButtonsRow; // parent object for the 3 buttons
    [SerializeField] private Button btnJumpTutorial;
    [SerializeField] private Button btnJumpLevel1;
    [SerializeField] private Button btnJumpLevel2;

    [Tooltip("Exact scene names from Build Settings.")]
    public string tutorialSceneName = "Tutorial";
    public string level1SceneName = "Level1";
    public string level2SceneName = "Level2";

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string nextLevelSceneName = "Level2";
    public bool useBuildIndexIfNextEmpty = true;

    private bool isOpen;

    private void Awake()
    {
        if (!rootObject) rootObject = gameObject;

        EnsureRefs();
        WireButtons();
        HideInstant();
    }

    private void OnEnable()
    {
        EnsureRefs();
        WireButtons();

        if (!isOpen)
            HideInstant();
    }

    // ---------------- PUBLIC ----------------

    public void SetCustomText(string title, string body)
    {
        if (titleText) titleText.text = title;
        if (bodyText) bodyText.text = body;
    }

    public void Show(ResultType result, int collected, int totalInLevel, int requiredToFinish)
    {
        EnsureRefs();

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
        ApplyJumpButtonsVisibility();
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
        Time.timeScale = 0f;

        if (!rootGroup)
        {
            Debug.LogWarning("FinishPopupUI: No CanvasGroup found on popup root.");
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

    private void ApplyJumpButtonsVisibility()
    {
        if (jumpButtonsRow)
            jumpButtonsRow.SetActive(showJumpButtons);
    }

    private void WireButtons()
    {
        // Main buttons
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

        // Jump buttons (temporary)
        if (btnJumpTutorial)
        {
            btnJumpTutorial.onClick.RemoveAllListeners();
            btnJumpTutorial.onClick.AddListener(() => JumpToScene(tutorialSceneName));
        }

        if (btnJumpLevel1)
        {
            btnJumpLevel1.onClick.RemoveAllListeners();
            btnJumpLevel1.onClick.AddListener(() => JumpToScene(level1SceneName));
        }

        if (btnJumpLevel2)
        {
            btnJumpLevel2.onClick.RemoveAllListeners();
            btnJumpLevel2.onClick.AddListener(() => JumpToScene(level2SceneName));
        }
    }

    private void EnsureRefs()
    {
        if (!rootObject) rootObject = gameObject;

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

        if (useBuildIndexIfNextEmpty)
        {
            int current = SceneManager.GetActiveScene().buildIndex;
            int next = current + 1;

            if (next >= SceneManager.sceneCountInBuildSettings)
                SceneManager.LoadScene(mainMenuSceneName);
            else
                SceneManager.LoadScene(next);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void JumpToScene(string sceneName)
    {
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("FinishPopupUI: Jump scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
            Debug.LogWarning($"FinishPopupUI: Scene '{sceneName}' not found in Build Settings or name is wrong.");

        SceneManager.LoadScene(sceneName);
    }
}
