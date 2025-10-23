using UnityEngine;

public class Collectible : MonoBehaviour
{
    public int value = 1; // how many points this collectible is worth

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Add to player score/manager
            CollectibleManager.Instance.AddScore(value);

            // Destroy the collectible
            Destroy(gameObject);
        }
    }
}