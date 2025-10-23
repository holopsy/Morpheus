using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEnd : MonoBehaviour
{
    public GameObject winUI; // assign the Canvas with WinText + Button in Inspector

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Show Win Screen
            winUI.SetActive(true);

            // Pause the game (optional)
            Time.timeScale = 0f;
        }
    }

    // Called by the Button OnClick
    public void RestartLevel()
    {
        Time.timeScale = 1f; // resume
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}