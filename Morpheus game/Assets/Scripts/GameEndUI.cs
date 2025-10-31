using UnityEngine;
using TMPro; // if you add a message label (optional)

public class GameEndUI : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] GameObject root;       // the win panel

    [Header("Optional")]
    [SerializeField] TMP_Text message;      // optional TMP text

    bool active;
    float prevScale = 1f;

    void Awake()
    {
        if (!root) root = gameObject;
        root.SetActive(false);
    }

    public void ShowWin(string msg = "Level Complete!")
    {
        if (message) message.text = msg;

        prevScale = Time.timeScale;
        Time.timeScale = 0f;    // pause game while menu is shown
        root.SetActive(true);
        active = true;
    }

    public void OnRestart()
    {
        root.SetActive(false);
        Time.timeScale = 1f;
        active = false;
        SceneLoader.RestartCurrent();
    }

    public void OnExit()
    {
        // Prefer returning to Main Menu over quitting entirely
        // If you want to quit the app, call SceneLoader.QuitGame();
        root.SetActive(false);
        Time.timeScale = 1f;
        active = false;

        // If your main menu scene is named "MainMenu", do:
        SceneLoader.LoadSceneByName("MainMenu");
    }

    void OnDisable()
    {
        if (active)
        {
            Time.timeScale = prevScale;
            active = false;
        }
    }
}