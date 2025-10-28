using UnityEngine;
using System;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)] // Awake runs early so collectibles can register safely
public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance;

    // Public read-only counters
    public int Total  { get; private set; }
    public int Collected { get; private set; }
    public bool AllCollected => Collected >= Total;

    public event Action<int,int> OnCountChanged; // (collected, total)

    // Internals
    private readonly HashSet<Collectible> _registered = new HashSet<Collectible>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Total = 0;
        Collected = 0;
    }

    // Called by each Collectible in OnEnable
    public void RegisterCollectible(Collectible c)
    {
        if (c == null || _registered.Contains(c)) return;
        _registered.Add(c);
        Total = _registered.Count;
        OnCountChanged?.Invoke(Collected, Total);
    }

    // Called when a collectible is picked
    public void NotifyPicked(int value, Collectible c)
    {
        if (c != null) _registered.Remove(c); // keep set tidy (Total stays as initial count)
        Collected += value;
        if (Collected > Total) Collected = Total;
        OnCountChanged?.Invoke(Collected, Total);
    }
}