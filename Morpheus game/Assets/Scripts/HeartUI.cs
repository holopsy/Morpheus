using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerHealth playerHealth;   // optional; can stay empty
    [SerializeField] Image[] heartImages;         // assign your 3 images
    [SerializeField] Sprite fullHeart;            // assign your full heart sprite

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += Refresh;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    void Start()
    {
        if (playerHealth != null)
            Refresh(playerHealth.current, playerHealth.maxHealth);
    }

    void Refresh(int current, int max)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            bool show = i < max && i < current;   // invisible when empty
            heartImages[i].gameObject.SetActive(show);
            if (show) heartImages[i].sprite = fullHeart;
        }
    }

    // Called by the binder on each spawned form
    public void SetPlayerHealth(PlayerHealth newHealth)
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;

        playerHealth = newHealth;

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += Refresh;
            Refresh(playerHealth.current, playerHealth.maxHealth);
        }
    }
}