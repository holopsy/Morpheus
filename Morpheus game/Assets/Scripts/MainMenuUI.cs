using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public string gameSceneName = "Level1"; // change to your first level

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        Debug.Log("Options clicked");
        // later: open options panel
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit"); // works only in build
    }
}