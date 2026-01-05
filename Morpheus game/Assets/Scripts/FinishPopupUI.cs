using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class FinishPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private GameObject rootObject; // usually the same object this script is on

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Buttons")]
    [SerializeField] private GameObject buttonsRow; // MainMenu/Restart/Continue row parent
    [SerializeField] private Button btnMainMenu;
    [SerializeField] private Button btnRestart;
    [SerializeField] private Button btnContinue;

    [SerializeField] private Button btnOk; // single OK button for "not enough" popup

    [Header("Scene Names")]
    [Tooltip("Scene name for main menu (must be added to Build Settings).")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("If empty, Continue loads next scene by build index (+1). If set, Continue loads this scene.")]
    public string nextLevelSceneName = "";

    private bool isOpen;

    private void Reset()
    {
        rootGroup = GetComponentInChildren<CanvasGroup>();
        rootObject = gameObject;
    }

    private void Awake()
    {
        if (rootObject == null) rootObject = gameObject;

        // Wire buttons safely
        if (btnMainMenu != null) btnMainMenu.onClick.AddListener(GoMainMenu);
        if (btnRestart != null) btnRestart.onClick.AddListener(RestartLevel);
        if (btnContinue != null) btnContinue.onClick.AddListener(ContinueNext);
        if (btnOk != null) btnOk.onClick.AddListener(Close);
    }

    private void Start()
    {
        CloseImmediate();
    }

    // ---------- Public API ----------

    public void ShowNotEnough(int have, int need)
    {
        OpenBase();

        titleText.text = "NOT ENOUGH ESSENCES";
        bodyText.text = $"You have {have}/{need}.\nFind the rest and come back!";

        SetMode(notEnoughMode: true);
    }

    public void ShowWin(int have, int needOrTotal, bool allowContinue = true)
    {
        OpenBase();

        titleText.text = "LEVEL COMPLETE";
        bodyText.text = (needOrTotal <= 0)
            ? $"Collected: {have}"
            : $"Collected: {have}/{needOrTotal}";

        SetMode(notEnoughMode: false);

        if (btnContinue != null)
            btnContinue.interactable = allowContinue;
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        // Resume game
        Time.timeScale = 1f;

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        if (rootObject != null)
            rootObject.SetActive(false);
    }

    // ---------- Internals ----------

    private void OpenBase()
    {
        isOpen = true;

        if (rootObject != null)
            rootObject.SetActive(true);

        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }

        // Pause game (UI still works)
        Time.timeScale = 0f;
    }

    private void CloseImmediate()
    {
        isOpen = false;
        Time.timeScale = 1f;

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        if (rootObject != null)
            rootObject.SetActive(false);
    }

    private void SetMode(bool notEnoughMode)
    {
        if (buttonsRow != null) buttonsRow.SetActive(!notEnoughMode);
        if (btnOk != null) btnOk.gameObject.SetActive(notEnoughMode);
    }

    // ---------- Button Actions ----------

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

    private void ContinueNext()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextLevelSceneName))
        {
            SceneManager.LoadScene(nextLevelSceneName);
            return;
        }

        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        // If next scene doesn't exist, go main menu
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(next);
        }
    }
}
