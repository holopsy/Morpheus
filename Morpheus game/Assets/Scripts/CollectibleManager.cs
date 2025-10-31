using UnityEngine;
using System;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance;

    // ---- Level completion counters (ONLY placed items) ----
    public int LevelTotal      { get; private set; }
    public int LevelCollected  { get; private set; }
    public bool AllCollected   => LevelCollected >= LevelTotal;

    // ---- Optional: currency from loot (enemy drops) ----
    public int LootCurrency    { get; private set; }

    public event Action<int,int> OnLevelCountChanged; // (collected, total)
    public event Action<int>     OnLootChanged;       // (currency)

    // Keep track of level items so duplicates don't double-count
    private readonly HashSet<Collectible> _levelSet = new HashSet<Collectible>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LevelTotal = 0;
        LevelCollected = 0;
        LootCurrency = 0;
    }

    // Called by Collectible.OnEnable() for level items
    public void RegisterLevelCollectible(Collectible c)
    {
        if (c == null || _levelSet.Contains(c)) return;
        _levelSet.Add(c);
        LevelTotal += 1;
        OnLevelCountChanged?.Invoke(LevelCollected, LevelTotal);
    }

    // Called when a level item is picked
    public void NotifyLevelPicked(Collectible c)
    {
        if (c != null && _levelSet.Contains(c))
            _levelSet.Remove(c);

        LevelCollected = Mathf.Clamp(LevelCollected + 1, 0, LevelTotal);
        OnLevelCountChanged?.Invoke(LevelCollected, LevelTotal);
    }

    // Called when loot (enemy drop) is picked
    public void NotifyLootGained(int value)
    {
        LootCurrency = Mathf.Max(0, LootCurrency + Mathf.Max(0, value));
        OnLootChanged?.Invoke(LootCurrency);
    }

    // Utility: reset between scenes if you load manually
    public void ResetAll()
    {
        _levelSet.Clear();
        LevelTotal = 0;
        LevelCollected = 0;
        LootCurrency = 0;
        OnLevelCountChanged?.Invoke(LevelCollected, LevelTotal);
        OnLootChanged?.Invoke(LootCurrency);
    }
}
