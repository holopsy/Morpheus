using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    // Set this once from your Main Menu button in the Inspector, or hardcode a name if you prefer.
    public static string FirstGameSceneName;

    public static void LoadFirstGameScene()
    {
        if (string.IsNullOrEmpty(FirstGameSceneName))
        {
            // fallback: current build index + 1
            int i = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(i + 1);
        }
        else
        {
            SceneManager.LoadScene(FirstGameSceneName);
        }
        Time.timeScale = 1f;
    }

    public static void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Time.timeScale = 1f;
    }

    public static void RestartCurrent()
    {
        Scene s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
        Time.timeScale = 1f;
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}