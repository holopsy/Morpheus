using UnityEngine;
using UnityEngine.SceneManagement; // for reloading the level

public class PlayerDeath : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Spike"))
        {
            Die();
        }
    }

    void Die()
    {
        // For now, just reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}