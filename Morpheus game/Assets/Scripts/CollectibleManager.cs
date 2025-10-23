using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance; // singleton
    private int score = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log("Score: " + score);
    }
}