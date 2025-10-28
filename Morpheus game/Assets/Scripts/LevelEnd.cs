using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitGate : MonoBehaviour
{
    public string nextSceneName = ""; // optional, if you want to load next later

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var mgr = CollectibleManager.Instance;
        if (mgr != null && mgr.AllCollected)
        {
            Debug.Log("Level complete! All essences collected.");
            // TODO: load next scene or show victory UI
            // if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("You must collect all essences first!");
            // Optional: show UI prompt
        }
    }
}