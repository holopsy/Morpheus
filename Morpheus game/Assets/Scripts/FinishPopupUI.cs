using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class FinishPopupUI : MonoBehaviour
{
    public enum ResultType { NotEnough, EnoughButNotAll, AllCollected }

    [Header("Root")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private CanvasGroup rootGroup;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Buttons Group")]
    [SerializeField] private GameObject buttonsGroup;

    [Header("Buttons (Main)")]
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnRestart;
    [SerializeField] private Button btnMainMenu;
    [SerializeField] private Button btnNext;

    [Header("Final Level Mode (Level 2)")]
    [Tooltip("Turn ON in the last level to show 'YOU WIN' messaging.")]
    public bool isFinalLevel = false;

    [TextArea(2, 4)]
    public string finalEnoughText =
        "You win!\n\nYou collected enough essences to finish the game.";

    [TextArea(2, 4)]
    public string finalPerfectText =
        "You win!\n\nPerfect completion!\nYou collected ALL essences!";

    [Header("Temporary Jump Buttons (Top Row)")]
    public bool showJumpButtons = true;
    [SerializeField] private GameObject jumpButtonsRow;
    [SerializeField] private Button btnJumpTutorial;
    [SerializeField] private Button btnJumpLevel1;
    [SerializeField] private Button btnJumpLevel2;

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
        if (!isOpen) HideInstant();
    }

    public void Show(ResultType result, int collected, int totalInLevel, int requiredToFinish)
    {
        EnsureRefs();

        if (rootObject && !rootObject.activeInHierarchy)
            rootObject.SetActive(true);

        OpenBase();

        // -------- TITLE --------
        if (titleText)
        {
            // Final level uses "YOU WIN" title
            titleText.text = isFinalLevel ? "YOU WIN!" : "You have completed the level!";
        }

        // -------- BODY --------
        if (bodyText)
        {
            if (isFinalLevel)
            {
                // Final level messaging
                if (result == ResultType.AllCollected)
                {
                    bodyText.text =
                        $"{finalPerfectText}\n\nCollected: {collected}/{totalInLevel}";
                }
                else
                {
                    // Enough-but-not-all OR even NotEnough (if you ever allow it)
                    bodyText.text =
                        $"{finalEnoughText}\n\nCollected: {collected}/{totalInLevel}\nRequired: {requiredToFinish}";
                }
            }
            else
            {
                // Normal levels messaging
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
        }

        ApplyButtonMode(result);
        ApplyJumpButtonsVisibility();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;  // 🔊 RESUME ALL AUDIO

        if (rootGroup)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
    }

    private void OpenBase()
    {
        isOpen = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;   // 🔇 PAUSE ALL AUDIO

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

        // Resume:
        // - allowed unless PERFECT completion
        bool showResume = (result != ResultType.AllCollected);
        if (btnResume) btnResume.gameObject.SetActive(showResume);

        // Next:
        // - allowed if ENOUGH or PERFECT
        bool showNext = (result == ResultType.EnoughButNotAll || result == ResultType.AllCollected);

        // Final level override: no Next button
        if (isFinalLevel)
        {
            if (btnNext) btnNext.gameObject.SetActive(false);
            return;
        }

        if (btnNext) btnNext.gameObject.SetActive(showNext);
    }

    private void ApplyJumpButtonsVisibility()
    {
        if (jumpButtonsRow)
            jumpButtonsRow.SetActive(showJumpButtons);
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

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false; // ✅ safety
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false; // ✅ safety
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void LoadNextLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false; // ✅ safety

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
    
    public void SetCustomText(string title, string body)
    {
        if (titleText) titleText.text = title;
        if (bodyText) bodyText.text = body;
    }
    
    public void PlayUIClick()
    {
        AudioManager.I.PlaySFX(SoundLibrary.I.uiSelect);
    }

    private void JumpToScene(string sceneName)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false; // ✅ safety

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
