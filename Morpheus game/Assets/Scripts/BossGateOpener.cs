using UnityEngine;

public class BossGateOpener : MonoBehaviour
{
    [Header("Open these when boss is defeated")]
    public MovableWall2D[] walls;

    [Header("Optional")]
    public bool openOnStartForTesting = false;

    void Start()
    {
        if (openOnStartForTesting)
            Open();
    }

    // Call this from your Boss script when it dies
    public void Open()
    {
        if (walls == null) return;
        for (int i = 0; i < walls.Length; i++)
            if (walls[i]) walls[i].SetRaised(true);
    }
}